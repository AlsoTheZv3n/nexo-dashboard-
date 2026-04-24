# CLAUDE.md

Projekt-spezifische Anleitung für Claude-Sessions in diesem Repo. Kurz, verbindlich, kein Erzähltext.

---

## 1. Was ist das?

**Operations Dashboard** — .NET 9 Web-API + React SPA + PowerShell-Executor, deploybar auf einer Single-VM mit k3s hinter Nginx.
Verbindliche Dokumente in `docs/`:

- [docs/00-OVERVIEW.md](docs/00-OVERVIEW.md)
- [docs/01-TECH_STACK.md](docs/01-TECH_STACK.md)
- [docs/02-ARCHITECTURE.md](docs/02-ARCHITECTURE.md)
- [docs/03-FEATURES.md](docs/03-FEATURES.md)
- [docs/04-TESTING.md](docs/04-TESTING.md)
- [docs/05-DEPLOYMENT.md](docs/05-DEPLOYMENT.md)
- [docs/06-PHASES.md](docs/06-PHASES.md)

Vor Architektur-Entscheidungen **zuerst** das passende Doc lesen. Die ADRs in `docs/02-ARCHITECTURE.md` sind Entscheidungen — nicht Diskussionen neu eröffnen.

---

## 2. Repository-Layout

```
nexo-dashboard/
├── backend/                # .NET 9 Solution (Dashboard.sln)
│   ├── Dashboard.Api/           # ASP.NET Core Web API (Composition Root)
│   ├── Dashboard.Core/          # Entities, Interfaces, Domain Logic — KEINE Aussenabhängigkeiten
│   ├── Dashboard.Infrastructure/# EF Core, DbContext, Repositories, JWT
│   ├── Dashboard.PowerShell/    # PS Executor (System.Management.Automation)
│   └── Dashboard.Tests/         # xUnit Unit + Integration (Testcontainers)
├── frontend/               # Vite + React 18 + TS 5 + Tailwind + shadcn/ui
├── powershell/             # *.ps1 + *.meta.json + Pester Tests
├── k8s/                    # Kustomize Base + dev/prod Overlays
├── docker/                 # Dockerfiles
├── deploy/nginx/           # Prod Nginx Site-Config
├── .github/workflows/      # CI + Deploy
└── docs/                   # Architektur- und Prozess-Docs
```

**Clean Architecture Abhängigkeitsrichtung:** `Api → Core ← Infrastructure`, `Api → PowerShell → Core`.
`Dashboard.Core` hat **keine** Package-Referenzen ausser BCL. Wer das bricht, muss es begründen.

---

## 3. Version-Pins (siehe docs/01-TECH_STACK.md)

- .NET 9.0 (LTS-Skeptiker: .NET 8 LTS — dann nur `TargetFramework` ändern)
- Node 20 LTS, pnpm 9+
- PostgreSQL 16, Redis 7 (ab Phase 5 optional)
- React 18.3, Vite 5, TypeScript 5.5+, Tailwind 3.4+
- Pester 5, xUnit 2.9+, Vitest 1.x, Playwright 1.4x+

Upgrades sind **bewusst**, nicht automatisch. Keine `latest`-Tags in Prod-Images.

---

## 4. Phasen-Status

Quelle der Wahrheit: [docs/06-PHASES.md](docs/06-PHASES.md). Jede Phase hat ein DoD.
**Keine neue Phase starten, bevor die vorherige ihr DoD erreicht hat.**

Aktueller Stand (aktualisieren, wenn Phasen abgeschlossen werden):
- [x] Phase 0 — Foundation / Repo-Setup
- [x] Phase 1 — Backend MVP (API + DB) — 35 tests (23 unit + 12 integration via Testcontainers)
- [x] Phase 2 — Frontend MVP — 11 Vitest/RTL tests, typecheck + build green
- [x] Phase 3 — PowerShell-Integration — 5 xUnit + 7 Pester tests
- [x] Phase 4 — Containerisierung + lokales k3s — Dockerfiles, compose, Kustomize base+overlays, CI
- [ ] Phase 5 — Prod-Deployment (VM + Nginx + Runner) — deploy/nginx/dashboard.conf written, runner install pending
- [x] Phase 6 — Charts + Metrics — metrics API (POST/bulk/timeseries/summary/status-breakdown/top-scripts), auto-emit on execution finish, Recharts dashboard (Area/Pie/Bar + KPI cards + date range + auto-refresh); 56 backend + 22 frontend + 10 Pester tests
- [ ] Phase 7 — Observability + Härtung

---

## 5. Dev-Setup (Windows)

Die Standard-PATH zeigt u.U. auf `.NET x86` (ohne SDKs). Die echten SDKs liegen unter:

```
C:\Program Files\dotnet\dotnet.exe
```

In Bash-Scripts: `DOTNET='/c/Program Files/dotnet/dotnet.exe'` und `"$DOTNET" <cmd>` verwenden.
Alternativ Session-PATH fixen: `export PATH="/c/Program Files/dotnet:$PATH"`.

pnpm ist unter `C:\Users\<user>\AppData\Roaming\npm\` global — in Bash sichtbar als `pnpm`.

### Häufige Befehle

```bash
# Backend
DOTNET='/c/Program Files/dotnet/dotnet.exe'
"$DOTNET" build backend/Dashboard.sln
"$DOTNET" test backend/Dashboard.sln --collect:"XPlat Code Coverage"
"$DOTNET" run --project backend/Dashboard.Api

# Frontend
cd frontend && pnpm install && pnpm dev
pnpm test           # Vitest
pnpm test:e2e       # Playwright (wenn eingerichtet)
pnpm lint

# PostgreSQL lokal
docker run -d --name dashboard-pg \
  -e POSTGRES_USER=dashboard -e POSTGRES_PASSWORD=dev -e POSTGRES_DB=dashboard \
  -p 5432:5432 postgres:16-alpine

# EF Migrations
"$DOTNET" tool install --global dotnet-ef  # einmalig
"$DOTNET" ef migrations add <Name> -p backend/Dashboard.Infrastructure -s backend/Dashboard.Api
"$DOTNET" ef database update -p backend/Dashboard.Infrastructure -s backend/Dashboard.Api

# PowerShell-Tests
pwsh -c "Invoke-Pester -CI -Path powershell/tests"
```

---

## 6. Tests (siehe docs/04-TESTING.md)

**Verhältnis:** ~70 % Unit · ~20 % Integration · ~10 % E2E.

**Harte Regeln:**
- Integration-Tests gegen **echte** PostgreSQL via Testcontainers — niemals gegen InMemory-Provider, niemals mit DB-Mocks.
- E2E-Flakiness blockiert **nicht** den Merge. Unit + Integration + Lint **tun** es.
- Jeder Bugfix beginnt mit einem **failing test**.
- Coverage-Ziele: `Dashboard.Core` ≥ 70 %, `frontend/src` (ohne `ui/`) ≥ 60 %.

---

## 7. Security-Hardrules

- Keine Secrets im Repo. `.env.local` ist gitignored, `.env.example` ist committed.
- k8s-Secrets via `kubectl create secret`, nicht im Manifest.
- PowerShell-Execution: **nur gewhitelistete** Scripts aus `powershell/scripts/`. Keine User-Uploads.
- Passwörter: BCrypt (cost ≥ 12). JWT: HS256 für MVP, RS256 später.
- EF Core: immer parametrisiert. Kein raw SQL mit String-Interpolation.
- Prod-Images: SHA-getaggt, nicht `latest`. Trivy-Scan im CI.

---

## 8. API-Konventionen

- Basis: `/api/v1/`
- JSON camelCase, Errors als RFC 7807 Problem Details
- Pagination: `?page=1&pageSize=20`, `X-Total-Count` Response-Header
- Swagger unter `/swagger` (nur Dev/Staging — Prod: 401 oder disabled)
- Authorisierung via `[Authorize(Roles = "Admin")]` — Rollen: `Admin`, `Operator`, `Viewer`

---

## 9. Git-Workflow

- `main` ist geschützt. Feature-Branches: `feature/<kurzbeschreibung>`, `fix/...`, `chore/...`.
- Conventional Commits empfohlen (`feat:`, `fix:`, `chore:`, `docs:`, `test:`).
- PRs brauchen grüne CI. Rebase bevorzugt, Merge-Commits nur für grössere Feature-Branches.
- Keine `--force` auf `main` oder geteilte Branches.

---

## 10. Anti-Patterns (aus docs/06-PHASES.md §Anti-Patterns)

1. Kein Go-Live ohne getesteten Backup-Restore.
2. Kein `latest` in Prod.
3. Keine Secrets im Repo — auch nicht in `dev/`.
4. Kein manuelles `kubectl apply` in Prod — alles durch CI.
5. Keine Premature Abstraction. Erst duplizieren, dann generalisieren.
6. Keine 100 %-Coverage-Ziele. Kern-Logik hart testen, UI-Glue locker.

---

## 11. Für Claude: wie arbeiten in diesem Repo

- Immer zuerst Phase-DoD checken (docs/06-PHASES.md) — implementiere nicht Phase 6, wenn Phase 2 DoD offen ist.
- Bei Änderungen am API-Contract: auch `frontend/src/lib/api.ts` und Integration-Tests anfassen.
- Bei neuen Entities: Migration erzeugen (`dotnet ef migrations add`) und committen — nie Schema zur Laufzeit ändern.
- Nach jeder Phase den Abschnitt **4. Phasen-Status** in diesem File aktualisieren.
- Nicht proaktiv neue `.md`-Dokumente anlegen. Wenn etwas wichtig ist, passt es in ein vorhandenes Doc.
