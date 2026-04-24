# 01 — Tech Stack

Alle Versionen sind für den Start **pinned**. Upgrades erfolgen bewusst, nicht automatisch.

---

## Backend

| Komponente | Version | Zweck |
|------------|---------|-------|
| .NET | **9.0 LTS** | Runtime + SDK |
| ASP.NET Core | **9.0** | Web API Framework |
| Entity Framework Core | **9.0** | ORM |
| Npgsql.EntityFrameworkCore.PostgreSQL | **9.0.x** | PostgreSQL Provider |
| Serilog | **4.x** | Structured Logging |
| MediatR | **12.x** | CQRS-Pattern (optional ab Phase 2) |
| FluentValidation | **11.x** | Request Validation |
| Swashbuckle.AspNetCore | **7.x** | OpenAPI / Swagger |
| System.Management.Automation | **7.4.x** | PowerShell SDK (In-Process) |
| Microsoft.AspNetCore.Authentication.JwtBearer | **9.0** | JWT Auth |
| BCrypt.Net-Next | **4.x** | Password Hashing |

### Warum .NET 9 und nicht 8?
.NET 9 ist STS (Standard Term Support, 18 Monate). Für ein Dashboard-Projekt, das du aktiv entwickelst, ist das OK. Wenn **Long-Term-Stabilität** wichtiger ist → **.NET 8 LTS** (bis Nov 2026). Der Code bleibt identisch bis auf Target Framework.

---

## PowerShell

| Komponente | Version | Zweck |
|------------|---------|-------|
| PowerShell | **7.4** (Core) | Zielumgebung für Scripts |
| Pester | **5.x** | PS-Unit-Tests |
| PSScriptAnalyzer | **1.22+** | Linting |

**Integration:** Über `System.Management.Automation` NuGet-Package als In-Process-Runspaces. Keine `Process.Start("pwsh.exe")`-Shell-Aufrufe — typisiert, performanter, sicherer.

---

## Frontend

| Komponente | Version | Zweck |
|------------|---------|-------|
| Node.js | **20 LTS** | Runtime |
| pnpm | **9.x** | Package Manager (schneller als npm) |
| React | **18.3** | UI-Framework |
| TypeScript | **5.5+** | Typisierung |
| Vite | **5.x** | Build-Tool + Dev-Server |
| React Router | **6.x** | Routing |
| TanStack Query | **5.x** | Server-State / Caching |
| TanStack Table | **8.x** | Data-Tables |
| Recharts | **2.12+** | Charts |
| Tailwind CSS | **3.4+** | Utility-First Styling |
| shadcn/ui | **latest** | Komponenten (copy-paste, keine npm-Lib) |
| Radix UI Primitives | via shadcn | Accessibility |
| Lucide React | **0.4x+** | Icons |
| Zod | **3.x** | Schema Validation (Frontend + shared) |
| React Hook Form | **7.x** | Forms |

### shadcn/ui Setup-Notizen
- **KEIN** npm-Package — `npx shadcn@latest init` und Komponenten werden in `src/components/ui/` kopiert
- Damit sind sie vollständig anpassbar und nicht abhängig von Lib-Updates
- Nötig: `tailwindcss-animate`, `class-variance-authority`, `clsx`, `tailwind-merge`

---

## Datenbank

| Komponente | Version | Zweck |
|------------|---------|-------|
| PostgreSQL | **16** | Primäre DB |
| pgvector | **0.7+** | Optional für Embeddings/ML |
| Redis | **7.x** | Cache / Session-Store (optional, ab Phase 5) |

---

## Container & Orchestrierung

| Komponente | Version | Zweck |
|------------|---------|-------|
| Docker Engine | **27.x** | Container-Runtime (Build) |
| k3s | **v1.30+** | Lightweight Kubernetes |
| kubectl | matching k3s | CLI |
| Helm | **3.15+** | Optional für DB / Ingress-Charts |
| Kustomize | integriert in kubectl | Env-Overlays (dev/prod) |

### Warum k3s?
- Single-Binary, ~50 MB RAM für Control-Plane
- SQLite als Default-Datastore (kein etcd nötig)
- Traefik als Default-Ingress (lässt sich durch Nginx-Ingress ersetzen)
- Perfekt für Single-VM-Setups

---

## Reverse Proxy & Netzwerk

| Komponente | Version | Zweck |
|------------|---------|-------|
| Nginx | **1.26+** | Reverse Proxy, SSL-Termination |
| Certbot | **2.x** | Let's Encrypt SSL |
| UFW | default | Firewall |

---

## CI/CD

| Komponente | Version | Zweck |
|------------|---------|-------|
| GitHub Actions | SaaS | CI-Workflows |
| GitHub Actions Self-Hosted Runner | **2.3xx+** | Runner auf VM für Deployment |
| Docker Buildx | via Docker | Multi-Stage Builds |
| GitHub Container Registry (GHCR) | SaaS | Image-Registry |

### Alternative: GitLab
Falls du auf GitLab migrieren willst — `gitlab-runner` statt GitHub Runner, `.gitlab-ci.yml` statt `.github/workflows/`. Architektur bleibt identisch.

---

## Testing

| Komponente | Version | Scope |
|------------|---------|-------|
| xUnit | **2.9+** | .NET Unit + Integration |
| Moq | **4.20+** | Mocking (.NET) |
| FluentAssertions | **6.x** | Readable Asserts |
| Testcontainers.NET | **3.x** | Integration-Tests mit echter DB |
| Vitest | **1.x** | Frontend Unit-Tests |
| React Testing Library | **16.x** | Component-Tests |
| Playwright | **1.4x+** | E2E-Tests |
| Pester | **5.x** | PowerShell Tests |

---

## Dev-Tools

| Tool | Zweck |
|------|-------|
| Visual Studio 2022 / Rider / VS Code | IDE |
| EditorConfig | Code-Style |
| Prettier | Frontend Formatting |
| ESLint | Frontend Linting |
| dotnet format | .NET Formatting |
| Husky.NET | Git Hooks |
| Commitlint | Conventional Commits |

---

## Observability (ab Phase 7)

| Komponente | Zweck |
|------------|-------|
| OpenTelemetry | Tracing/Metrics in .NET |
| Prometheus | Metrics-Sammlung |
| Grafana | Dashboards für Infra-Metrics |
| Loki | Log-Aggregation |
| Alloy | Agent für Logs/Metrics |

---

## Lizenz-Check

Alle gelisteten OSS-Projekte sind MIT, Apache-2.0 oder BSD. Keine GPL-Komponenten.
