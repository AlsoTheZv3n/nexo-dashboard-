# Nexo Operations Dashboard

Ein Dashboard auf **.NET 9** + **React 18 + shadcn/ui**, mit PowerShell-Integration, containerisiert via Docker, orchestriert über **k3s** auf einer Single-VM, gefrontet durch **Nginx** mit Let's-Encrypt-SSL.

| Ebene      | Status | Notizen |
|------------|--------|---------|
| Phase 0 — Foundation       | ✅ | docs, gitignore, CLAUDE.md |
| Phase 1 — Backend MVP      | ✅ | 35 tests grün (23 unit + 12 integration mit Testcontainers Postgres) |
| Phase 2 — Frontend MVP     | ✅ | 11 Vitest tests, typecheck + prod build grün |
| Phase 3 — PowerShell       | ✅ | 5 xUnit + 7 Pester tests, 3 Beispiel-Scripts |
| Phase 4 — Container + k3s  | ✅ | Dockerfiles, compose, Kustomize base+dev/prod, CI workflow |
| Phase 5 — Prod-Deployment  | ✅ | `deploy.yml` (build→GHCR→Trivy gate→self-hosted `kubectl apply` mit SHA-Pin + Health + Rollback), `deploy/vm-bootstrap.sh` für Ubuntu 24; Runner-Token noch einmalig manuell |
| Phase 6 — Charts + Metrics | ✅ | Metrics-API, auto-emit bei Execution-Abschluss, Recharts-Dashboard (Area/Pie/Bar + KPIs + Range + Auto-Refresh); +16 backend, +11 frontend, +3 Pester tests |
| Phase 7 — Observability    | ✅ | Serilog CLEF JSON + Request-Redaction, OpenTelemetry + `/metrics`, k8s Observability-Stack (Prometheus/Loki/Promtail/Grafana), Backup-CronJob, Trivy + `dotnet/pnpm audit` im CI, [RUNBOOK](docs/RUNBOOK.md) |

## Docs

Alle Architektur- und Prozess-Docs liegen unter [docs/](docs/):

- [00-OVERVIEW.md](docs/00-OVERVIEW.md)
- [01-TECH_STACK.md](docs/01-TECH_STACK.md)
- [02-ARCHITECTURE.md](docs/02-ARCHITECTURE.md)
- [03-FEATURES.md](docs/03-FEATURES.md)
- [04-TESTING.md](docs/04-TESTING.md)
- [05-DEPLOYMENT.md](docs/05-DEPLOYMENT.md)
- [06-PHASES.md](docs/06-PHASES.md)

Projekt-Kompass für Claude-Sessions: [CLAUDE.md](CLAUDE.md).

## Quickstart

```bash
# Backend
cd backend
"/c/Program Files/dotnet/dotnet.exe" build Dashboard.sln        # Windows
# oder auf Linux/Mac:
# dotnet build Dashboard.sln
dotnet run --project Dashboard.Api

# Frontend (Phase 2+)
cd frontend
pnpm install
pnpm dev

# PostgreSQL lokal
docker run -d --name dashboard-pg \
  -e POSTGRES_USER=dashboard -e POSTGRES_PASSWORD=dev -e POSTGRES_DB=dashboard \
  -p 5432:5432 postgres:16-alpine
```

Default Dev-Credentials: `admin` / `admin` (nach initialer Migration seeded).

## Tests

```bash
# Backend
dotnet test backend/Dashboard.sln --collect:"XPlat Code Coverage"

# Frontend
cd frontend && pnpm test

# PowerShell
pwsh -c "Invoke-Pester -CI -Path powershell/tests"
```

## Lizenz

[MIT](LICENSE).
