# Operations Dashboard

Ein erweiterbares Dashboard auf Basis von **.NET 9 + PowerShell** Backend und **React + shadcn/ui** Frontend, containerisiert mit Docker und orchestriert via **k3s** auf einer einzelnen Linux-VM. Deployment erfolgt über einen **Self-Hosted GitHub Actions Runner**, gefrontet durch **Nginx** als Reverse Proxy mit SSL.

---

## Quick Links

| Dokument | Zweck |
|----------|-------|
| [docs/01-TECH_STACK.md](docs/01-TECH_STACK.md) | Alle Technologien, Versionen, Libraries |
| [docs/02-ARCHITECTURE.md](docs/02-ARCHITECTURE.md) | Systemarchitektur, Komponenten, Datenfluss |
| [docs/03-FEATURES.md](docs/03-FEATURES.md) | Feature-Katalog mit MoSCoW-Priorisierung |
| [docs/04-TESTING.md](docs/04-TESTING.md) | Test-Strategie (Unit / Integration / E2E) |
| [docs/05-DEPLOYMENT.md](docs/05-DEPLOYMENT.md) | VM-Setup, k3s, Nginx, CI/CD-Pipeline |
| [docs/06-PHASES.md](docs/06-PHASES.md) | Schrittweise Implementierung (Phase 0–7) |

---

## Projektziele

1. **Ein Dashboard-UI** für operative Daten (Metriken, Tabellen, Charts)
2. **PowerShell-Integration** — Scripts ausführen, Resultate in der API persistieren
3. **Produktionstauglich** — Container, Orchestrierung, SSL, Monitoring, CI/CD
4. **Single-VM-deploybar** — läuft vollständig auf einer Ubuntu-VM mit k3s
5. **Erweiterbar** — Module lassen sich hinzufügen, ohne Core zu brechen

---

## High-Level Stack

```
┌─────────────────────────────────────────────────┐
│  Browser                                        │
│    └─ React SPA (Vite + TS + Tailwind + shadcn) │
└─────────────────┬───────────────────────────────┘
                  │ HTTPS
┌─────────────────▼───────────────────────────────┐
│  Ubuntu VM                                      │
│  ┌─────────────────────────────────────────┐    │
│  │  Nginx (Reverse Proxy + SSL)            │    │
│  └──────────┬─────────────────────┬────────┘    │
│             │                     │             │
│  ┌──────────▼──────────┐  ┌───────▼──────────┐  │
│  │  k3s Cluster        │  │  Static Frontend │  │
│  │   ├─ API (.NET)     │  │  (Nginx serves)  │  │
│  │   ├─ PS Executor    │  └──────────────────┘  │
│  │   ├─ PostgreSQL     │                        │
│  │   └─ Ingress        │                        │
│  └─────────────────────┘                        │
│                                                 │
│  ┌─────────────────────────────────────────┐    │
│  │  GitHub Actions Self-Hosted Runner      │    │
│  └─────────────────────────────────────────┘    │
└─────────────────────────────────────────────────┘
```

---

## Getting Started (wenn Phase 0 + 1 fertig sind)

```bash
# Backend
cd backend/Dashboard.Api
dotnet run

# Frontend
cd frontend
pnpm install
pnpm dev

# Lokales k3s (später)
k3d cluster create dashboard --port "8080:80@loadbalancer"
kubectl apply -f k8s/
```

---

## Repository-Struktur (Ziel)

```
dashboard-project/
├── backend/
│   ├── Dashboard.Api/              # ASP.NET Core Web API
│   ├── Dashboard.Core/             # Domain Logic
│   ├── Dashboard.PowerShell/       # PS Executor Module
│   ├── Dashboard.Infrastructure/   # EF Core, Persistence
│   └── Dashboard.Tests/            # xUnit Tests
├── frontend/
│   ├── src/
│   ├── public/
│   └── package.json
├── powershell/
│   ├── scripts/                    # PS-Scripts (*.ps1)
│   └── tests/                      # Pester Tests
├── k8s/
│   ├── base/                       # Kustomize Base
│   └── overlays/
│       ├── dev/
│       └── prod/
├── docker/
│   ├── api.Dockerfile
│   └── frontend.Dockerfile
├── .github/
│   └── workflows/
│       ├── ci.yml
│       └── deploy.yml
├── docs/
├── .gitignore
└── README.md
```

---

## Lizenz

MIT (oder private, je nach Kontext)
