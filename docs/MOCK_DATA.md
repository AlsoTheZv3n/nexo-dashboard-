# Mock Data — Strategie & Austausch-Playbook

Ziel: Das Dashboard soll **ab Tag 1 nicht leer** sein (KPI-Kacheln mit sinnvollen Zahlen, Charts mit Kurven, Listen mit Einträgen), aber der Übergang zu **realen Daten** muss ein **einziger, klar lokalisierbarer Schalter** sein — kein "Refactoring, um Mocks rauszuziehen".

Die Architektur trennt Mock-Daten an drei Schichten. Jede Schicht hat einen expliziten **Aus-Schalter** und einen Austausch-Pfad.

---

## 1. Layer-Übersicht

| Schicht                   | Wer liefert Daten?                      | Wann aktiv?                          | Aus-Schalter                                                                  |
|---------------------------|------------------------------------------|--------------------------------------|--------------------------------------------------------------------------------|
| **Frontend-Tests**        | MSW (`src/mocks/handlers.ts`)           | Nur bei `pnpm test`                  | Nichts zu tun — MSW ist dev/test-only                                         |
| **Frontend-Entwicklung**  | Dev-Proxy → lokaler Backend-Container   | `pnpm dev`                          | —                                                                               |
| **Backend Base-Seed**     | `DbSeeder`                               | Immer bei leerer DB                  | —   (seedet 3 User + 4 Script-Metadata-Rows — echte User können ersetzen)     |
| **Backend Demo-Seed**     | `DemoDataSeeder`                         | Wenn `Demo:SeedEnabled=true`         | Config-Flag: `Demo:SeedEnabled=false` (Default in Prod)                       |

**Kein Mocking in Produktions-Code.** Es gibt nirgendwo `if (demo) returnFake()` im Application-Layer. Mock-Daten leben ausschliesslich hinter der DB-Grenze, und zwar als **normale Zeilen in normalen Tabellen**. Was bedeutet: Die echte Welt produziert exakt dieselbe Form von Daten — der Code sieht keinen Unterschied.

---

## 2. Layer 1 — Frontend-Tests (MSW)

- Handler unter [frontend/src/mocks/handlers.ts](../frontend/src/mocks/handlers.ts).
- Geladen via [frontend/src/test/setup.ts](../frontend/src/test/setup.ts), nur für Vitest.
- Prod-Build (`vite build`) inkludiert MSW **nicht** — es ist `devDependency`.
- Einzelne Tests können Handler **pro Test** überschreiben (`server.use(http.get(...))`) — das ist der Standard für negative Testfälle.

**Austausch-Pfad:** Keiner nötig. MSW ist per Definition isoliert.

---

## 3. Layer 2 — Frontend-Entwicklung

- `pnpm dev` startet Vite auf `http://localhost:5173`.
- `vite.config.ts` proxyt `/api` → `http://localhost:5000` (lokaler Backend-Container).
- Wenn der Backend läuft und `Demo:SeedEnabled=true` ist, sieht die UI **demo-gefüllte** echte DB-Daten — nicht Mocks im Frontend.

**Austausch-Pfad:** Siehe Layer 4.

---

## 4. Layer 3 — Backend Base-Seed (`DbSeeder`)

[backend/Dashboard.Infrastructure/Persistence/DbSeeder.cs](../backend/Dashboard.Infrastructure/Persistence/DbSeeder.cs) wird **immer** ausgeführt, aber nur wenn die jeweilige Tabelle **leer** ist:

- 3 Default-User: `admin` / `operator` / `viewer` (je Passwort = Benutzername)
- 4 Script-Metadata-Zeilen für die in `powershell/scripts/` vorhandenen Skripte

**Das ist kein Mocking.** Das sind sinnvolle Starter-Einträge wie ein WordPress-Admin-Account. Die Default-Passwörter sind nach Go-Live **sofort** zu ändern (`POST /users/{id}/reset-password` oder via CLI). Die Script-Rows werden beim nächsten Start durch den Script-Scanner aus echten Dateien vertikal synchronisiert (Phase 3+).

**Austausch-Pfad in Prod:**
1. Admin loggt sich mit `admin`/`admin` ein.
2. Neue Admin-Accounts anlegen, dann den Default-Admin deaktivieren oder mit neuem Passwort überschreiben.
3. Default-User `operator`/`viewer` löschen oder deaktivieren.
4. Base-Seed läuft danach nicht mehr (Tabellen sind nicht-leer).

---

## 5. Layer 4 — Backend Demo-Seed (`DemoDataSeeder`)

[backend/Dashboard.Infrastructure/Persistence/DemoDataSeeder.cs](../backend/Dashboard.Infrastructure/Persistence/DemoDataSeeder.cs).

**Genau ein Flag.** `Demo:SeedEnabled=true` aktiviert den Demo-Seed. Default ist `false`.

| Umgebung                        | Flag                             | Herkunft                                                                 |
|---------------------------------|----------------------------------|--------------------------------------------------------------------------|
| Lokale Entwicklung              | `true`                           | `docker-compose.dev.yml` setzt `Demo__SeedEnabled: "true"`               |
| CI (Integration-Tests)          | nicht gesetzt → `false`          | `ApiFactory` nutzt Environment `IntegrationTests`, überspringt Seed eh  |
| k3s dev-Overlay                 | `true`                           | `k8s/overlays/dev/kustomization.yaml` setzt `Demo__SeedEnabled` im ConfigMap-Patch |
| k3s prod-Overlay                | **unset**                        | Prod sieht niemals Demo-Daten                                            |

### Was der Demo-Seeder schreibt

Auf eine **leere** Executions-Tabelle (einziger Trigger für Idempotenz):

- **~100–300 `ps_executions`** über die letzten 30 Tage, ~85 % erfolgreich, ~10 % fehlgeschlagen, ~5 % gecancelt.
- **Metrics** in die `metrics`-Tabelle:
  - `executions.completed` / `executions.failed` / `executions.duration_seconds` stündlich
  - `host.cpu.percent` und `host.disk.free_percent` stündlich über 30 Tage
- **1 AlertRule**: "CPU > 90% for 5 min" (ungesetzt, demo-freundlich)
- **1 ScheduledExecution**: stündlicher Health-Check
- **2 AuditLogEntries**: `auth.login` + ein `demo.seed`-Marker

Alle Demo-Rows tragen entweder den Tag `"source":"demo-seed"` in ihrem `tags_json`/`details_json` oder sind durch die semantische Form erkennbar (Alert-Name enthält "demo", Schedule-Name enthält "(demo)").

### Warum genau so

- **Idempotent über das leere-DB-Signal**: `DemoDataSeeder` prüft `db.Executions.AnyAsync()` und bricht ab, wenn irgendeine Execution existiert. Ein Produktions-Cluster wird nie versehentlich "demo-überschwemmt".
- **Keine zusätzliche Schema-Ebene**: Demo-Rows haben dasselbe Schema wie Prod-Rows. Exakt dieselbe Query, dieselben Indexe, dieselben Charts.
- **In-DB statt im-Code**: Der Application-Layer sieht keine Weiche. Nur der Startup-Code entscheidet "Seed ja/nein".

---

## 6. Der Austausch-Prozess — von Demo zu Real

Zwei realistische Szenarien.

### 6.1 "Ich will ab sofort nur noch echte Daten sehen."

**Voraussetzung:** Dev-Cluster oder Test-VM, keine produktive Nutzer-Basis.

```bash
# 1. Flag ausschalten (lokal in docker-compose.dev.yml ändern oder per ENV setzen)
export Demo__SeedEnabled=false

# 2. Demo-Daten wegräumen (optional, wenn bestehend)
#    Kleine Helfer-Queries:
psql -U dashboard -d dashboard <<'SQL'
DELETE FROM metrics WHERE tags_json::jsonb ->> 'source' = 'demo-seed';
DELETE FROM ps_executions;  -- alle, falls DB noch keine echten hatte
DELETE FROM alert_rules WHERE name ILIKE '%demo%' OR name ILIKE 'CPU > 90%%';
DELETE FROM scheduled_executions WHERE name ILIKE '%(demo)%';
DELETE FROM audit_log WHERE action = 'demo.seed';
SQL

# 3. API neu starten. DemoSeeder sieht `Demo__SeedEnabled=false` und bleibt stumm.
docker compose -f docker-compose.dev.yml up -d --force-recreate api
```

Danach produziert jede echte Script-Ausführung (`POST /api/v1/executions`) organisch neue Rows und Metrics — derselbe Code-Pfad wie vorher.

### 6.2 "Produktiver Rollout"

```bash
# Prod-Overlay setzt Demo__SeedEnabled nicht → Default false. Nichts zu tun.
# Base-Seeder läuft weiter beim ersten Start (erstellt die 3 Default-User); danach
# bist du verantwortlich, echte Admins anzulegen und die Defaults zu entschärfen.

kubectl -n dashboard-prod exec deploy/api -- dotnet Dashboard.Api.dll
# kein Demo-Seed → leere Executions/Metrics → Dashboard zeigt "0" bis echte
# Scripts laufen. Das ist gewollt.
```

### 6.3 Demo-Daten **parallel** zu Real-Daten

Nicht empfohlen — die Echtheits-Grenze verschwimmt. Wenn es trotzdem nötig ist (Screenshots, Demos, Schulung):

1. Separate Datenbank (`dashboard_demo`) provisionieren.
2. Eigene Ingress-Host-Regel (`demo.dashboard.example.ch`) → Pod-Variante mit `Demo__SeedEnabled=true` und `ConnectionStrings__Default` zur Demo-DB.
3. Nie denselben API-Pod gegen beide DBs fahren.

---

## 7. Was **nicht** Mock ist

- Die **UI-Layouts** und Leer-Zustände ("No entries.", "Waiting for output…"). Die sind reale Produktions-UI.
- Die **MSW-Handler im Test-Setup**. Das sind Test-Fixtures, keine Mocks für die laufende App.
- Die **3 Default-User** im Base-Seed. Das ist ein notwendiger Bootstrap, wie jedes Admin-Tool ihn hat. Ersetzen, nicht "entfernen".

---

## 8. Regeln für neue Features

Wenn du ein neues Feature baust, das "etwas anzeigen soll":

1. **Keine Mock-Daten im Frontend-Code.** Keine `if (!data) return FAKE_DATA`. Leer-Zustände sind UI-Arbeit, nicht Daten-Arbeit.
2. **Backend-Endpoint vor UI**. Wenn der Endpoint noch nicht da ist → Endpoint schreiben (auch wenn er nur `[]` liefert).
3. **Demo-Anreicherung gehört in `DemoDataSeeder`**, nicht in die Controller.
4. **Tests bekommen ihre Daten aus MSW / ApiFactory-Seed** — nicht vom DemoSeeder. Tests sollen deterministisch sein, Demo-Daten sind es absichtlich nicht (Zufallszahlen).

Halten sich alle an diese Regeln, ist "Mock-Daten rausnehmen" immer der eine Config-Flag — nie ein Code-Change.
