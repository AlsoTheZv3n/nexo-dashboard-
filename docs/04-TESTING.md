# 04 — Testing

## Test-Pyramide

```
           /\
          /E2\         ← wenige (Playwright)
         /────\
        / Integ \      ← mittel (Testcontainers + API)
       /────────\
      / Unit     \     ← viele (xUnit / Vitest / Pester)
     /────────────\
```

**Ziel-Verteilung:**
- ~70 % Unit
- ~20 % Integration
- ~10 % E2E

---

## 1. Backend (.NET)

### 1.1 Unit-Tests (xUnit)

**Scope:** Pure Business-Logik in `Dashboard.Core`, keine DB, kein HTTP.

**Tools:**
- `xunit`
- `Moq` oder `NSubstitute` für Mocks
- `FluentAssertions` für Asserts
- `Bogus` für Test-Daten

**Struktur:**
```
backend/Dashboard.Tests/
├── Unit/
│   ├── Core/
│   │   ├── Services/
│   │   └── Validators/
│   └── PowerShell/
├── Integration/
│   ├── Api/
│   └── Infrastructure/
└── GlobalUsings.cs
```

**Konvention:**
```csharp
[Fact]
public async Task Execute_WhenScriptNotFound_ReturnsNotFound()
{
    // Arrange
    var sut = new ExecutionService(_mockRepo.Object);

    // Act
    var result = await sut.ExecuteAsync(Guid.NewGuid(), []);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.Error.Should().Be(ExecutionError.ScriptNotFound);
}
```

**Coverage-Ziel:** ≥ 70 % in `Dashboard.Core`

---

### 1.2 Integration-Tests (xUnit + Testcontainers)

**Scope:** Echte DB, echte HTTP-Pipeline. Keine externen APIs (mocken).

**Tools:**
- `Microsoft.AspNetCore.Mvc.Testing` → `WebApplicationFactory<Program>`
- `Testcontainers.PostgreSql` → echte PostgreSQL in Docker
- `Respawn` → DB zwischen Tests resetten

**Beispiel:**
```csharp
public class ExecutionsEndpointTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ExecutionsEndpointTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_Execution_Returns202_AndJobId()
    {
        // Arrange
        await _client.AuthenticateAsync("admin");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/executions", new
        {
            scriptId = Seed.Script.Id,
            parameters = new { Target = "localhost" }
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var body = await response.Content.ReadFromJsonAsync<ExecutionResponse>();
        body!.Id.Should().NotBeEmpty();
    }
}
```

---

### 1.3 PowerShell-Integration-Tests

**Scope:** Verifizieren, dass PS-Scripts korrekt geladen und ausgeführt werden.

**Approach:** Fixture mit Test-Scripts in `Dashboard.Tests/TestScripts/*.ps1`. Executor lädt diese statt Production-Scripts.

**Timeout-Test wichtig:** PS-Script mit `Start-Sleep 60` → Test mit 5s Timeout → muss korrekt abbrechen.

---

## 2. Frontend (React / Vitest)

### 2.1 Unit / Component-Tests

**Tools:**
- `vitest`
- `@testing-library/react`
- `@testing-library/user-event`
- `msw` (Mock Service Worker) für API-Mocks

**Scope:**
- Component-Rendering
- User-Interaktionen (Click, Tipp, Form-Submit)
- Custom Hooks

**Beispiel:**
```tsx
describe('ExecutionsTable', () => {
  it('shows loading skeleton while fetching', () => {
    render(<ExecutionsTable />, { wrapper: QueryWrapper });
    expect(screen.getByTestId('skeleton')).toBeInTheDocument();
  });

  it('displays executions after fetch', async () => {
    server.use(
      http.get('/api/v1/executions', () => HttpResponse.json({ items: [mockExec] }))
    );
    render(<ExecutionsTable />, { wrapper: QueryWrapper });
    await waitFor(() => {
      expect(screen.getByText(mockExec.scriptName)).toBeInTheDocument();
    });
  });
});
```

**Setup-Datei:** `src/test-setup.ts` mit `msw`-Server und globalen Mocks.

---

### 2.2 Nicht testen (bewusst)

- Externe Libs (Recharts, shadcn-Komponenten) — die sind schon getestet
- CSS / Styling — visual regression wäre separat (Chromatic/Percy)
- TanStack-Query-Interna

**Coverage-Ziel:** ≥ 60 % in `src/` (ohne `ui/`-Ordner von shadcn)

---

## 3. E2E (Playwright)

**Scope:** Kritische User-Journeys end-to-end, gegen echte Dev-Umgebung (k3d oder Docker-Compose).

**Kernflows:**
1. Login → Dashboard sieht KPIs
2. PS-Script auswählen → ausführen → Output sehen
3. Tabelle filtern + sortieren + exportieren
4. Logout

**Struktur:**
```
e2e/
├── tests/
│   ├── auth.spec.ts
│   ├── dashboard.spec.ts
│   ├── executions.spec.ts
│   └── tables.spec.ts
├── fixtures/
├── playwright.config.ts
└── global-setup.ts  ← loggt Test-User ein + speichert Storage-State
```

**Browser:** Chromium default, Firefox + WebKit in Nightly-Run.

**CI:** Playwright in separatem Job, Artifacts (Screenshots + Videos) bei Failure.

---

## 4. PowerShell-Tests (Pester 5)

**Scope:** Logik in eigenen `*.ps1`-Scripts.

**Beispiel:**
```powershell
Describe 'Get-SystemHealth' {
    BeforeAll {
        . $PSScriptRoot/../scripts/Get-SystemHealth.ps1
    }

    Context 'when system is healthy' {
        It 'returns Status = Ok' {
            Mock Get-CimInstance { return @{ Name='OK' } }
            $result = Get-SystemHealth
            $result.Status | Should -Be 'Ok'
        }
    }

    Context 'when disk is below threshold' {
        It 'returns Status = Warning' {
            Mock Get-PSDrive { return @{ Free = 1GB } }
            $result = Get-SystemHealth -MinFreeGB 10
            $result.Status | Should -Be 'Warning'
        }
    }
}
```

**Coverage:** Pester generiert `JaCoCo`-XML → in CI hochladen.

---

## 5. Statische Analyse / Linting

| Layer | Tool | Wann |
|-------|------|------|
| .NET | `dotnet format` + `.editorconfig` | Pre-Commit + CI |
| .NET | `Microsoft.CodeAnalysis.NetAnalyzers` | Build |
| .NET | `SonarAnalyzer.CSharp` (optional) | CI |
| Frontend | ESLint + Prettier | Pre-Commit + CI |
| Frontend | `typescript --noEmit` (tsc) | CI |
| PS | `PSScriptAnalyzer` | CI |
| Docker | `hadolint` | CI |
| k8s | `kubeval` + `kustomize build \| kubectl apply --dry-run` | CI |
| Security | `Trivy` (Container-Scan) | CI |

---

## 6. Test-Daten

**Backend:** `Bogus` für random, `TestData.cs` mit festen Seed-Objekten für reproduzierbare Tests.

**Frontend:** `src/mocks/fixtures/` mit JSON-Dateien, importiert in `msw`-Handlers.

**DB-Seeding für E2E:** `seed-e2e.sql`-Script wird bei `global-setup.ts` ausgeführt.

---

## 7. CI-Integration

Alle Tests laufen in der CI-Pipeline (siehe `05-DEPLOYMENT.md`):

```yaml
# Vereinfachter Auszug
jobs:
  backend-tests:
    runs-on: ubuntu-latest
    services:
      postgres: ...
    steps:
      - dotnet test --collect:"XPlat Code Coverage"

  frontend-tests:
    runs-on: ubuntu-latest
    steps:
      - pnpm test:unit --coverage

  ps-tests:
    runs-on: ubuntu-latest
    steps:
      - pwsh -c "Invoke-Pester -CI"

  e2e:
    needs: [backend-tests, frontend-tests]
    runs-on: ubuntu-latest
    steps:
      - docker compose -f docker-compose.e2e.yml up -d
      - pnpm playwright test
```

**Blockiert Merge:** Unit + Integration + Lint. E2E läuft, blockiert aber nicht Merge (flaky-resistent konfigurieren).

---

## 8. Test-First-Regeln

- **Bugfix:** Erst Failing-Test schreiben, dann fixen.
- **Neue Feature:** Happy-Path + 1–2 Error-Cases mindestens.
- **Refactoring:** Keine neuen Tests, aber bestehende müssen grün bleiben.
