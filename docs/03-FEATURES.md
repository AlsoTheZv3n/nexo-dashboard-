# 03 — Features

Priorisierung nach **MoSCoW**:

| Prio | Bedeutung |
|------|-----------|
| **M** | Must-Have (MVP) |
| **S** | Should-Have (kurz nach MVP) |
| **C** | Could-Have (wenn Zeit) |
| **W** | Won't-Have (vorerst) |

---

## 1. Authentication & User Management

| ID | Feature | Prio | Notizen |
|----|---------|------|---------|
| AUTH-01 | Login mit Username/Passwort | M | JWT, BCrypt |
| AUTH-02 | Logout | M | Token invalidation (Redis blacklist oder kurze TTL) |
| AUTH-03 | Refresh-Token-Flow | M | HttpOnly-Cookie |
| AUTH-04 | Rollen (Admin / Operator / Viewer) | M | `[Authorize(Roles=)]` |
| AUTH-05 | User-Management UI (Admin) | S | CRUD für User |
| AUTH-06 | Passwort ändern | S | Eigenes Konto |
| AUTH-07 | MFA (TOTP) | C | Phase 6+ |
| AUTH-08 | SSO (OIDC) | W | Später mit Authentik/Keycloak |
| AUTH-09 | Audit-Log UI | S | Wer hat was wann gemacht |

---

## 2. Dashboard / Overview

| ID | Feature | Prio | Notizen |
|----|---------|------|---------|
| DSH-01 | KPI-Kacheln (Zahl + Trend-Pfeil) | M | 4–6 Top-Metriken |
| DSH-02 | Zeitreihen-Chart (Line/Area) | M | Recharts, letzte 7/30/90 Tage |
| DSH-03 | Verteilungs-Chart (Pie/Bar) | M | Recharts |
| DSH-04 | Datum-Range-Picker | M | Filter für alle Charts |
| DSH-05 | Auto-Refresh (konfigurierbar) | S | 30 s / 1 min / off |
| DSH-06 | Dashboard-Layout anpassbar | C | Drag-and-Drop via `react-grid-layout` |
| DSH-07 | Export PDF/PNG | C | Phase 5+ |
| DSH-08 | Benutzerspezifische Dashboards | C | Mehrere Views |

---

## 3. PowerShell-Script-Management

| ID | Feature | Prio | Notizen |
|----|---------|------|---------|
| PSM-01 | Script-Liste anzeigen | M | TanStack Table mit Such-/Filter |
| PSM-02 | Script-Details anzeigen | M | Name, Beschreibung, Parameter, Source |
| PSM-03 | Script ausführen (mit Params) | M | Form dynamisch aus Param-Def generiert |
| PSM-04 | Ausführungs-Historie | M | Tabelle aller Executions |
| PSM-05 | Output einer Execution anzeigen | M | stdout/stderr, strukturiert |
| PSM-06 | Execution abbrechen | S | CancellationToken im Worker |
| PSM-07 | Scheduled Executions (Cron) | C | `Hangfire` oder k8s CronJob |
| PSM-08 | Script via UI hochladen | W | Sicherheitsrisiko, nur via Git |
| PSM-09 | Script-Versionierung | S | Hash + Git-Commit-Ref speichern |

---

## 4. Metrics & Monitoring

| ID | Feature | Prio | Notizen |
|----|---------|------|---------|
| MTR-01 | Custom-Metriken via API posten | M | `POST /api/v1/metrics` |
| MTR-02 | Metriken als Zeitreihe abfragen | M | `GET /api/v1/metrics/timeseries` |
| MTR-03 | Aggregationen (avg/sum/min/max) | M | SQL-Window-Functions |
| MTR-04 | Alerts (Schwellwerte) | S | Notification via Email/Webhook |
| MTR-05 | Alert-Historie | S | Eigene Seite |
| MTR-06 | Prometheus-Export | C | `/metrics` endpoint |

---

## 5. Data-Tables (TanStack Table)

| ID | Feature | Prio | Notizen |
|----|---------|------|---------|
| TBL-01 | Sortierung (mehrspaltig) | M | Built-in |
| TBL-02 | Spalten-Filter | M | Per-Column |
| TBL-03 | Globale Suche | M | Debounced |
| TBL-04 | Pagination (Server-Side) | M | API mit page/pageSize |
| TBL-05 | Spalten ein-/ausblenden | S | User-Präferenz persistent |
| TBL-06 | Export CSV | S | Client-Side |
| TBL-07 | Row-Selection + Bulk-Actions | S | Mehrfach-Auswahl |
| TBL-08 | Inline-Editing | C | Für wenige Felder |
| TBL-09 | Virtualisierung (grosse Datensets) | C | `@tanstack/react-virtual` |

---

## 6. Charts (Recharts)

| ID | Feature | Prio | Notizen |
|----|---------|------|---------|
| CHT-01 | LineChart (Zeitreihe) | M | Mit Tooltip, Legend |
| CHT-02 | AreaChart | M | Stacked + Normal |
| CHT-03 | BarChart | M | Vertikal + Horizontal |
| CHT-04 | PieChart / DonutChart | M | Mit Prozent-Labels |
| CHT-05 | Responsive Container | M | ResponsiveContainer |
| CHT-06 | Dark-Mode-Support | M | Farben via Tailwind-Theme |
| CHT-07 | Heatmap | C | Custom-Implementation |
| CHT-08 | Gauge-Chart | C | Custom mit SVG |

---

## 7. Notifications

| ID | Feature | Prio | Notizen |
|----|---------|------|---------|
| NTF-01 | Toast-Notifications (Frontend) | M | `sonner` (shadcn) |
| NTF-02 | In-App Notification-Center | S | Badge + Liste |
| NTF-03 | Email-Notifications | S | SMTP über MailKit |
| NTF-04 | Webhook-Notifications | C | Generische Slack/Teams-Integration |

---

## 8. UI/UX-Basics

| ID | Feature | Prio | Notizen |
|----|---------|------|---------|
| UI-01 | Dark / Light Mode | M | shadcn-Theme, Preference persistent |
| UI-02 | Responsive (Desktop + Tablet) | M | Tailwind breakpoints |
| UI-03 | Mobile (Phone) | S | Nach Desktop-Polish |
| UI-04 | i18n (de-CH + en) | S | `react-i18next` |
| UI-05 | Skeleton-Loader | M | Bei allen Data-Fetches |
| UI-06 | Error-Boundaries | M | Globaler Fallback |
| UI-07 | Offline-Indicator | C | navigator.onLine |
| UI-08 | Keyboard-Shortcuts | C | `cmdk` für Command-Palette |

---

## 9. Admin / Config

| ID | Feature | Prio | Notizen |
|----|---------|------|---------|
| ADM-01 | Settings-Seite | M | App-Config editierbar |
| ADM-02 | Health-Check-Page | M | DB, PS-Worker, Disk-Space |
| ADM-03 | Logs-Viewer | S | Letzte N Zeilen aus Serilog |
| ADM-04 | Backup-Management | C | DB-Backup manuell triggern |

---

## 10. API / Developer-Experience

| ID | Feature | Prio | Notizen |
|----|---------|------|---------|
| DEV-01 | OpenAPI / Swagger UI | M | Unter `/api/swagger` |
| DEV-02 | API-Versioning | M | `/api/v1/`, Header-Version |
| DEV-03 | API-Keys (statt JWT) | S | Für Service-to-Service |
| DEV-04 | Request/Response-Logging | M | Serilog, ohne Secrets |
| DEV-05 | Structured Errors (RFC 7807) | M | Globaler Exception-Handler |

---

## MVP-Definition

**Alles "M" aus Abschnitten 1, 2, 3, 5, 6, 8, 10 = MVP.**

Konkret: Login funktioniert, Dashboard zeigt KPIs + Charts, User kann PS-Scripts aus einer Tabelle triggern und sieht Output. Deployed auf k3s hinter Nginx mit SSL.

**Nicht MVP:** Alerts, Email-Notifications, Scheduled Jobs, Prometheus-Export, MFA, i18n, Mobile-Optimierung.
