# 06 — Phases

Schrittweiser Aufbau von **Phase 0 (Setup)** bis **Phase 7 (Observability)**. Jede Phase hat klare **Deliverables** und ein **Definition of Done (DoD)**. Keine Phase wird gestartet, bevor die vorherige ihr DoD erreicht hat.

**Grundregel:** Nach jeder Phase ist das System funktional und lokal oder auf der VM lauffähig.

---

## Phase 0 — Foundation & Repo-Setup

**Dauer:** ~0.5 – 1 Tag

### Tasks
1. Git-Repo anlegen (GitHub Private)
2. Ordnerstruktur aus README anlegen
3. `.gitignore`, `.editorconfig`, `.gitattributes` committen
4. Docs-Folder mit diesen Markdown-Files
5. `LICENSE` wählen
6. Branching-Model definieren (GitFlow Lite: `main` protected, Feature-Branches, PR-Required)
7. Commitlint + Husky einrichten (optional)
8. Readme mit Badges (Build, Coverage — Platzhalter zuerst)

### Deliverables
- ✅ Leeres Repo mit Skelett-Struktur
- ✅ Alle Docs-Files committed
- ✅ CODEOWNERS (falls Team)

### DoD
- `git clone` + Ordner-Struktur sichtbar
- Linter-Configs ready (werden in späteren Phasen aktiv)

---

## Phase 1 — Backend MVP (API + DB)

**Dauer:** ~3 – 5 Tage

### Tasks

**1.1 Solution-Setup**
```bash
cd backend
dotnet new sln -n Dashboard
dotnet new webapi -n Dashboard.Api
dotnet new classlib -n Dashboard.Core
dotnet new classlib -n Dashboard.Infrastructure
dotnet new classlib -n Dashboard.PowerShell
dotnet new xunit -n Dashboard.Tests
dotnet sln add **/*.csproj
```

**1.2 Dependencies**
- `Microsoft.EntityFrameworkCore` + `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Serilog.AspNetCore` + `Serilog.Sinks.Console`
- `FluentValidation.AspNetCore`
- `Swashbuckle.AspNetCore`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `BCrypt.Net-Next`

**1.3 Basis-Implementierung**
- `Program.cs` mit Serilog, EF, Swagger, JWT-Auth, CORS
- `User`-Entity + `AuthController` (`/login`, `/refresh`)
- Erste Migration: `users`-Tabelle
- `HealthController` (`/health/live`, `/health/ready`)
- `ScriptsController` + `ExecutionsController` mit **Stubs** (return mock data)
- FluentValidation für Request-DTOs
- Globaler Exception-Handler → RFC 7807

**1.4 PostgreSQL lokal**
```bash
docker run -d --name dashboard-pg \
  -e POSTGRES_USER=dashboard -e POSTGRES_PASSWORD=dev -e POSTGRES_DB=dashboard \
  -p 5432:5432 postgres:16-alpine
```

**1.5 Seed-Daten**
- 1 Admin-User (`admin` / `admin`)
- 3 fake `ps_scripts` Metadata-Einträge

### Deliverables
- ✅ API läuft lokal auf `http://localhost:5000`
- ✅ Swagger unter `/swagger`
- ✅ Login funktioniert (JWT zurückbekommen)
- ✅ Authentifizierter Request auf `GET /api/v1/scripts` liefert Mock-Liste

### DoD
- Alle Endpoints haben Integration-Tests (xUnit + Testcontainers)
- Coverage ≥ 50 % in `Dashboard.Core`
- `dotnet format` + Analyzer-Warnungen = 0

---

## Phase 2 — Frontend MVP (Shell + Auth + Tabelle)

**Dauer:** ~3 – 5 Tage

### Tasks

**2.1 Vite + TS + Tailwind + shadcn/ui**
```bash
cd frontend
pnpm create vite@latest . -- --template react-ts
pnpm install
pnpm install -D tailwindcss@3 postcss autoprefixer
npx tailwindcss init -p
pnpm dlx shadcn@latest init
```

**2.2 Tools installieren**
- Router: `react-router-dom`
- State: `@tanstack/react-query`, `axios`
- Forms: `react-hook-form`, `@hookform/resolvers`, `zod`
- Tabellen: `@tanstack/react-table`
- Charts: `recharts`
- Icons: `lucide-react`

**2.3 shadcn-Komponenten hinzufügen**
```bash
npx shadcn@latest add button card input form table dropdown-menu \
  select dialog sonner skeleton tabs badge avatar
```

**2.4 App-Struktur**
```
src/
├── app/                    # Routing, Layout
│   ├── layouts/
│   └── routes/
├── features/
│   ├── auth/
│   ├── dashboard/
│   ├── executions/
│   └── scripts/
├── components/
│   └── ui/                 # shadcn
├── lib/
│   ├── api.ts              # axios instance + interceptors
│   ├── auth.ts
│   └── utils.ts
├── hooks/
└── main.tsx
```

**2.5 Core-Screens**
- Login-Seite
- Dashboard-Layout mit Sidebar
- Scripts-Liste (TanStack Table, sortierbar, filterbar)
- Script-Details + "Run"-Button (Modal mit Form)
- Executions-Liste

### Deliverables
- ✅ Login → Dashboard sichtbar
- ✅ Scripts-Tabelle zeigt API-Daten
- ✅ Execution triggerbar (auch wenn Output noch Mock ist)
- ✅ Dark/Light-Mode-Toggle

### DoD
- ESLint + Prettier konfiguriert, CI-ready
- `pnpm tsc --noEmit` grün
- Vitest + React Testing Library: mindestens 5 Component-Tests
- Mobile funktional OK (nicht optimiert)

---

## Phase 3 — PowerShell-Integration

**Dauer:** ~2 – 3 Tage

### Tasks

**3.1 `Dashboard.PowerShell`-Projekt**
- `PowerShellExecutor`-Service mit `Runspaces`
- Script-Loader: liest aus `/app/scripts/*.ps1`
- Output-Serializer: `PSObject` → JSON
- Timeout-Handling (`CancellationToken`)

**3.2 Param-Definition-Schema**
Jedes Script hat JSON-Metadata neben `.ps1`:
```json
// Get-SystemHealth.ps1.meta.json
{
  "name": "Get-SystemHealth",
  "description": "Prüft System-Gesundheit",
  "parameters": [
    { "name": "MinFreeGB", "type": "int", "default": 10, "required": false }
  ]
}
```

**3.3 Backend-Updates**
- `ScriptsController.GetAll()` scannt Scripts + Meta
- `ExecutionsController.Create()` → `PowerShellExecutor.ExecuteAsync()`
- Execution-Status: Pending → Running → Completed / Failed / Cancelled

**3.4 Frontend-Updates**
- Param-Form dynamisch aus Metadata generiert (Zod-Schema runtime-generieren)
- Execution-Details-Seite mit Output (monospace, scrollbar)

**3.5 3 echte Beispiel-Scripts**
- `Get-SystemHealth.ps1`
- `Get-DiskUsage.ps1`
- `Test-NetworkConnectivity.ps1`

### Deliverables
- ✅ UI triggert echten PS-Code
- ✅ Output wird persistiert + angezeigt
- ✅ Pester-Tests für die 3 Scripts

### DoD
- Keine blockierten API-Threads (Script > 5s → weiterhin API responsive)
- Script-Cancel funktioniert
- Fehler im Script → Exec-Status = Failed + stderr erfasst

---

## Phase 4 — Containerisierung + Lokales k3s

**Dauer:** ~2 – 3 Tage

### Tasks

**4.1 Dockerfiles**
- `docker/api.Dockerfile` (multi-stage, mit PS Core)
- Optional `docker/frontend.Dockerfile` für Cluster-Deployment

**4.2 Docker-Compose für lokale Dev**
```yaml
# docker-compose.dev.yml
services:
  postgres:
    image: postgres:16-alpine
    environment: ...
    ports: ["5432:5432"]
  api:
    build: { dockerfile: docker/api.Dockerfile }
    ports: ["5000:8080"]
```

**4.3 k8s-Manifests mit Kustomize**
```
k8s/
├── base/
│   ├── namespace.yaml
│   ├── postgres.yaml
│   ├── api-deployment.yaml
│   ├── api-service.yaml
│   ├── api-nodeport.yaml
│   └── kustomization.yaml
└── overlays/
    ├── dev/
    │   └── kustomization.yaml
    └── prod/
        └── kustomization.yaml
```

**4.4 Lokales k3d-Cluster**
```bash
k3d cluster create dashboard --port "8080:30080@server:0"
kubectl apply -k k8s/overlays/dev
```

**4.5 Health-Endpoints validiert im Cluster**

### Deliverables
- ✅ `docker compose up` funktioniert (API + DB + Frontend via Vite)
- ✅ `kubectl apply -k k8s/overlays/dev` deployt lokal
- ✅ API via `http://localhost:8080/api/v1/health/live` erreichbar

### DoD
- Liveness + Readiness-Probes grün
- Secrets nicht im Repo (`.env.example` + `.env.local`)
- Image-Build in < 60s (mit Cache)

---

## Phase 5 — Production-Deployment (VM + Nginx + Runner)

**Dauer:** ~2 – 4 Tage

### Tasks

**5.1 VM-Setup** (siehe `05-DEPLOYMENT.md` Abschnitt 2–3)
- Ubuntu 24.04 LTS
- UFW + fail2ban
- k3s installieren
- Nginx + Certbot installieren

**5.2 DNS + SSL**
- A-Record auf VM-IP
- `certbot --nginx -d dashboard.example.ch`

**5.3 k8s-Production-Setup**
- Namespace `dashboard-prod`
- Secrets setzen (DB, JWT)
- PostgreSQL StatefulSet deployen
- API-Deployment (Image-Tag: specific SHA, nicht `latest`)

**5.4 Nginx-Config**
- Site-Config committen in `deploy/nginx/dashboard.conf`
- In CI automatisch auf VM deployen

**5.5 GitHub Runner**
- Self-Hosted Runner installieren
- Sudoers für Nginx-Reload + rsync-Permissions setzen

**5.6 CI + Deploy Workflows**
- `.github/workflows/ci.yml` (läuft auf ubuntu-latest)
- `.github/workflows/deploy.yml` (build auf ubuntu-latest, deploy auf self-hosted)

**5.7 Erstes Prod-Deployment**
- Tag / PR nach main → Auto-Deploy
- Manueller Trigger via `workflow_dispatch`

### Deliverables
- ✅ `https://dashboard.example.ch` zeigt Login
- ✅ Login + Script-Execution funktioniert prod
- ✅ Healthcheck grün
- ✅ Automatisches Deploy bei Push auf `main`

### DoD
- SSL-Rating: A oder A+ bei ssllabs.com
- Security-Headers: A+ bei securityheaders.com
- Rollback getestet (k8s undo, Frontend-Symlink)
- README mit Prod-URL

---

## Phase 6 — Charts + Metrics

**Dauer:** ~3 – 5 Tage

### Tasks

**6.1 Backend-Metrics-Modul**
- Endpoint `POST /api/v1/metrics` (pushed key/value/timestamp)
- Endpoint `GET /api/v1/metrics/timeseries?key=...&from=...&to=...&bucket=1h`
- EF-Entity `metrics` mit Indexen auf `(key, timestamp)`
- Aggregations-SQL: Window-Functions

**6.2 Frontend-Dashboard-Seite**
- KPI-Cards (4–6 Stück)
- LineChart mit Recharts (Zeitreihen)
- PieChart (z.B. Status-Verteilung)
- BarChart (z.B. Top 5 Scripts by Exec-Count)
- Datum-Range-Picker (shadcn `calendar` + `popover`)
- Auto-Refresh-Option

**6.3 Datenquellen**
- PS-Script `Collect-Metrics.ps1`, läuft per CronJob, pusht an API
- Oder: API-intern Metriken bei bestimmten Events schreiben

**6.4 Theming**
- Chart-Colors als CSS-Variablen → respektieren Dark/Light-Mode

### Deliverables
- ✅ Dashboard zeigt echte Daten in Charts
- ✅ Filter per Zeitraum + Metric-Key
- ✅ Mobile-View OK

### DoD
- Performance: Dashboard lädt in < 500 ms (Backend-Query < 100 ms für 30 Tage Daten)
- Keine Layout-Shifts beim Laden (Skeleton-Loader)

---

## Phase 7 — Observability + Härtung

**Dauer:** ~3 – 5 Tage

### Tasks

**7.1 Structured Logging**
- Serilog → stdout (JSON)
- Request-Logging-Middleware (ohne Secrets)

**7.2 Metrics für Infra**
- OpenTelemetry in .NET → Prometheus-Exporter
- Prometheus im Cluster
- Grafana-Dashboard für API-Latency, Error-Rate, Throughput

**7.3 Log-Aggregation**
- Loki + Promtail im Cluster
- Grafana-Integration
- Basis-Alerts (Error-Rate > 1 %, Disk > 80 %, Pod-Restarts)

**7.4 DB-Backup automatisiert**
- CronJob für `pg_dump`
- Externes Storage (restic → S3/Backblaze/Wasabi)
- Wiederherstellung 1× getestet

**7.5 Security-Audit**
- `Trivy` im CI für Images
- `dotnet list package --vulnerable`
- Penetration-Basics: OWASP ZAP oder Nikto gegen Dev-Instance

**7.6 Runbook**
- `docs/RUNBOOK.md` mit typischen Incidents + Response-Steps
  - API down
  - DB voll
  - Zertifikat läuft ab (sollte auto-renew, aber Fallback)
  - Runner-Ausfall

### Deliverables
- ✅ Grafana-Dashboard für Infra + App
- ✅ Alerts per Email oder Webhook
- ✅ Backup-Restore erfolgreich getestet
- ✅ Runbook existiert

### DoD
- Keine High/Critical CVEs in Trivy-Scan
- Incident-Simulation: simulierten DB-Ausfall mit Runbook behoben in < 15 min

---

## Phase 8+ — Ausbaustufen (optional / später)

| Phase | Thema | Komplexität |
|-------|-------|-------------|
| 8 | Scheduled Executions (Cron im UI) | Mittel |
| 9 | Multi-Tenancy (Mandanten-fähig) | Hoch |
| 10 | SSO via OIDC (Keycloak / Authentik) | Mittel |
| 11 | Notification-Center (Email / Webhook / Slack) | Mittel |
| 12 | Mobile-App (React Native) mit gleicher API | Hoch |
| 13 | KI-Integration (LLM-gestützte Summary von Executions) | Niedrig-Mittel |

---

## Meilensteine & Timeline (Schätzung Solo-Dev, 50 %)

| Meilenstein | Phasen | Dauer |
|-------------|--------|-------|
| **M1 — API-Skelett lokal** | 0 + 1 | ~1–1.5 Wochen |
| **M2 — UI + Auth funktional** | 2 | ~1 Woche |
| **M3 — PS-Integration komplett** | 3 | ~3–4 Tage |
| **M4 — Containerisiert + k3s lokal** | 4 | ~3–4 Tage |
| **M5 — Live auf VM** | 5 | ~4–6 Tage |
| **M6 — MVP-done mit Charts** | 6 | ~1 Woche |
| **M7 — Production-ready hardened** | 7 | ~1 Woche |

**Total MVP (bis M5):** ~4–5 Wochen bei 50 %
**Total inkl. Härtung (bis M7):** ~7–8 Wochen bei 50 %

---

## Anti-Patterns / Fallen, die ich bewusst vermeide

1. **Kein Go-Live ohne Backup-Restore-Test.** Backups ohne Recovery-Test sind kein Backup.
2. **Kein `latest`-Tag in Prod.** Immer pinned SHA → nachvollziehbar + rollbackbar.
3. **Keine Secrets im Repo.** Selbst nicht in `dev/`. `.env.local` + `.env.example`.
4. **Kein manueller `kubectl apply` in Prod.** Alles über CI, nachvollziehbar in Git-Historie.
5. **Keine Premature-Abstraction.** Erst Feature funktioniert, dann refactor — nicht umgekehrt.
6. **Keine 100 % Coverage-Ziele.** Qualität > Quantität. Kern-Logik hart testen, UI-Glue locker.
