# Nexo Operations Dashboard

Ein Dashboard auf **.NET 9** + **React 18 + shadcn/ui**, mit PowerShell-Integration, containerisiert via Docker, orchestriert über **k3s** auf einer Single-VM, gefrontet durch **Nginx** mit Let's-Encrypt-SSL.

| Ebene      | Status    |
|------------|-----------|
| Phase 0 — Foundation       | ✅ |
| Phase 1 — Backend MVP      | 🚧 |
| Phase 2 — Frontend MVP     | ⏳ |
| Phase 3 — PowerShell       | ⏳ |
| Phase 4 — Container + k3s  | ⏳ |
| Phase 5 — Prod-Deployment  | ⏳ |
| Phase 6 — Charts + Metrics | ⏳ |
| Phase 7 — Observability    | ⏳ |

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
