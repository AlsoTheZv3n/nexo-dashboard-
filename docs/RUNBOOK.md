# Runbook — Operations Dashboard

Incident-First-Response für die Nexo-Dashboard-Produktion. Jedes Szenario hat **Symptom** → **Triage** → **Response** → **Recovery-Verify**. Stop-the-bleed zuerst, Root-Cause danach.

> **Rolle bei einem Incident**: Der Oncall entscheidet; diese Playbooks sind Entscheidungshilfe, nicht Vorschrift.

## 0. Common Commands

```bash
# SSH auf die VM
ssh -i ~/.ssh/nexo-vm ubuntu@dashboard.example.ch

# Cluster-Zugang (auf der VM)
kubectl get pods -n dashboard-prod
kubectl logs -n dashboard-prod deploy/api --tail=200 -f
kubectl describe pod -n dashboard-prod -l app.kubernetes.io/name=api
kubectl top pods -n dashboard-prod        # nur wenn metrics-server installiert

# Grafana (port-forward vom Laptop)
kubectl -n observability port-forward svc/grafana 3000:3000
# → http://localhost:3000 (admin / <GF_SECURITY_ADMIN_PASSWORD>)
```

**Wichtig:** `kubectl delete` niemals ohne Rückfrage in Prod.

---

## 1. API antwortet nicht (502/504, Health-Check rot)

### Symptom
- `https://dashboard.example.ch/api/v1/health/ready` liefert 502/504 oder Timeout.
- Nutzer sehen "Backend nicht erreichbar".
- Alert: Grafana "api_error_rate > 1 %" feuert.

### Triage (≤ 2 min)
```bash
kubectl get pods -n dashboard-prod -l app.kubernetes.io/name=api
kubectl logs -n dashboard-prod deploy/api --tail=200
kubectl get events -n dashboard-prod --sort-by=.lastTimestamp | tail -20
```

Drei Unterfälle:

| Indikator                                   | Pfad                                    |
|---------------------------------------------|-----------------------------------------|
| Pods `CrashLoopBackOff`                     | → A (App-Crash)                         |
| Pods `Running` aber `0/1 READY`             | → B (Readiness-Fail, oft DB-Verbindung) |
| Pods `Running 1/1` aber 502 von Nginx      | → C (Ingress-/Nginx-Problem)            |

### A — App-Crash

```bash
kubectl logs -n dashboard-prod deploy/api --previous --tail=300
```

1. **Panik im Start-up-Log?** Fehlendes Secret / kaputte Migration / Config-Typo. Fix: Secret korrigieren, Rollout:
   ```bash
   kubectl rollout restart deploy/api -n dashboard-prod
   ```
2. **Out-of-Memory (Exit 137)?** RAM zu knapp. Quick-fix: Limit hochziehen (Edit `k8s/base/api.yaml` → PR → Deploy). Root-cause: Memory-Leak-Hunt via Grafana `process_working_set_bytes`.
3. **Startup-Deadlock?** Seeder hängt (DB weg). → B.

### B — Readiness-Fail

Readiness = DB-Check (`/api/v1/health/ready` macht `CanConnectAsync`).
```bash
kubectl exec -n dashboard-prod deploy/api -- wget -qO- localhost:8080/api/v1/health/ready
kubectl logs -n dashboard-prod statefulset/postgres --tail=100
kubectl exec -n dashboard-prod statefulset/postgres -- pg_isready -U dashboard
```

- Postgres-Pod nicht `Running`? → §3.
- DB läuft, aber API kommt nicht dran? Secret-Mismatch. Decode den Connection-String und vergleiche:
  ```bash
  kubectl get secret api-secret -n dashboard-prod -o jsonpath='{.data.ConnectionStrings__Default}' | base64 -d
  ```

### C — Ingress / Nginx

```bash
# Nginx auf der VM
sudo systemctl status nginx
sudo tail -100 /var/log/nginx/error.log
# k3s NodePort erreichbar?
curl -sI http://127.0.0.1:30080
```

- 502 mit "upstream connection refused" → k3s-Service-Port stimmt nicht / Traefik neugestartet. `kubectl rollout status deploy/api -n dashboard-prod`.
- 504 mit Nginx-Timeout → Request-Dauer > `proxy_read_timeout`. Verifizieren via `/metrics` → `http_server_request_duration_seconds`. Ggf. Timeout in `deploy/nginx/dashboard.conf` erhöhen, `sudo nginx -t && sudo systemctl reload nginx`.

### Recovery-Verify
```bash
curl -fsSL https://dashboard.example.ch/api/v1/health/ready
# Grafana: api_error_rate sinkt unter 1 % für 5 Minuten
```

---

## 2. Ausführungen hängen (Status = Running)

### Symptom
- Executions bleiben unbegrenzt auf Status `Running`.
- Nutzer sehen in der UI: Spinner läuft ewig.

### Triage
```bash
kubectl exec -n dashboard-prod statefulset/postgres -- psql -U dashboard -c \
  "select id, created_at, started_at from ps_executions where status = 1 and started_at < now() - interval '10 minutes' order by started_at;"
```

Wenn eine Execution länger als der konfigurierte `PowerShell:TimeoutSeconds` (default 60s) auf `Running` steht → der API-Pod wurde neu gestartet während ein Script lief, oder der in-process Runspace hängt.

### Response
1. **API-Pod neu rollen**, damit gekillte Task.Run-Jobs nicht wiederkehren:
   ```bash
   kubectl rollout restart deploy/api -n dashboard-prod
   ```
2. **Stuck rows reparieren** (SQL):
   ```sql
   update ps_executions
     set status = 3,                    -- Failed
         stderr = 'Interrupted by runner restart',
         completed_at = now()
   where status = 1 and started_at < now() - interval '10 minutes';
   ```
3. Nach zweitem Auftreten: Issue anlegen "Migrate executions to proper Worker (docs/02-ARCHITECTURE.md §3.3 Variante B)".

---

## 3. Datenbank voll / Postgres nicht ready

### Symptom
- Pod `postgres-0` in `CrashLoopBackOff` oder API-Readiness rot.
- `pg_isready` liefert Fehler.

### Triage
```bash
kubectl describe pod -n dashboard-prod postgres-0
kubectl logs -n dashboard-prod postgres-0 --tail=200

# Storage-Füllstand
kubectl exec -n dashboard-prod postgres-0 -- df -h /var/lib/postgresql/data
```

### Response
1. **Disk > 90 % voll**:
   ```bash
   # Toter Speicher (vacuum full ist heavy; erst das billigere probieren)
   kubectl exec -n dashboard-prod postgres-0 -- psql -U dashboard -c "vacuum analyze;"
   # Große Tabellen finden
   kubectl exec -n dashboard-prod postgres-0 -- psql -U dashboard -c \
     "select relname, pg_size_pretty(pg_total_relation_size(oid)) from pg_class
      where relkind = 'r' order by pg_total_relation_size(oid) desc limit 10;"
   ```
   - `metrics` oder `ps_executions` oft Hauptverursacher. Retention-Policy anwenden:
     ```sql
     delete from metrics where timestamp < now() - interval '90 days';
     delete from ps_executions where created_at < now() - interval '180 days';
     vacuum analyze;
     ```
2. **PVC zu klein**: Der `storage: 10Gi` im StatefulSet-PVC-Template lässt sich nur expandieren, wenn die StorageClass `allowVolumeExpansion: true` hat (local-path auf k3s: nein). Fallback: Backup + Recreate mit größerer PVC (siehe §6).

### Recovery-Verify
```bash
kubectl exec -n dashboard-prod postgres-0 -- pg_isready -U dashboard
curl -fsSL https://dashboard.example.ch/api/v1/health/ready
```

---

## 4. Zertifikat läuft ab (oder ist abgelaufen)

### Symptom
- Browser: "NET::ERR_CERT_DATE_INVALID".
- Oncall-Alert 14 Tage vor Ablauf (cron + certbot-expiry-hook empfohlen).

### Triage
```bash
sudo certbot certificates
openssl s_client -connect dashboard.example.ch:443 -servername dashboard.example.ch </dev/null 2>/dev/null | openssl x509 -noout -dates
```

### Response
```bash
# Standard: certbot renew läuft als systemd-timer (certbot.timer). Manuell forcieren:
sudo certbot renew --force-renewal
sudo nginx -t && sudo systemctl reload nginx
```

Wenn `certbot renew` scheitert (Port 80 nicht erreichbar): UFW-Regel für 80/tcp prüfen (`sudo ufw status`) und A-Record-Ziel kontrollieren. Fallback: manuelle DNS-01-Challenge.

### Recovery-Verify
```bash
curl -vI https://dashboard.example.ch/ 2>&1 | grep -E "expire date|subject:"
```

---

## 5. GitHub Actions Runner down

### Symptom
- PR zeigt "Waiting for self-hosted runner".
- Deploys laufen nicht.

### Triage + Response (auf der VM)
```bash
sudo systemctl status actions.runner.<org>-<repo>.<runner-name>
sudo journalctl -u actions.runner.<org>-<repo>.<runner-name> -n 200 --no-pager
sudo systemctl restart actions.runner.<org>-<repo>.<runner-name>
```

Token abgelaufen? Neu konfigurieren:
```bash
cd /opt/actions-runner
sudo -u runner ./config.sh remove --token <REMOVAL_TOKEN>
sudo -u runner ./config.sh --url https://github.com/<org>/<repo> --token <REG_TOKEN>
sudo systemctl start actions.runner.*
```

---

## 6. Backup wiederherstellen (Drill)

**Regel:** Backup ohne Restore-Drill ist kein Backup. Mindestens quartalsweise gegen Staging durchspielen.

### Verfügbare Dumps
```bash
kubectl exec -n dashboard-prod deploy/postgres -- ls -lh /backups || \
  kubectl get pvc -n dashboard-prod db-backups
# Inhalt des PVC via temporärem Pod
kubectl run -n dashboard-prod --rm -it pvc-inspector \
  --image=alpine --overrides='{"spec":{"containers":[{"name":"pvc-inspector","image":"alpine","command":["sh"],"stdin":true,"tty":true,"volumeMounts":[{"name":"b","mountPath":"/backups"}]}],"volumes":[{"name":"b","persistentVolumeClaim":{"claimName":"db-backups"}}]}}' -- sh
```

### Restore nach Staging
```bash
# 1. Dump in den staging-pg-Pod kopieren
kubectl cp /local/path/dashboard-20260424-020000.sql.gz \
  dashboard-staging/postgres-0:/tmp/restore.sql.gz

# 2. Separate DB anlegen + importieren
kubectl exec -n dashboard-staging postgres-0 -- bash -c '
  dropdb --if-exists dashboard_restore &&
  createdb dashboard_restore &&
  gunzip -c /tmp/restore.sql.gz | psql -U dashboard -d dashboard_restore
'

# 3. Schema- und Row-Counts vergleichen
kubectl exec -n dashboard-staging postgres-0 -- psql -U dashboard -d dashboard_restore -c "\dt"
kubectl exec -n dashboard-staging postgres-0 -- psql -U dashboard -d dashboard_restore -c \
  "select 'users' as t, count(*) from users
   union all select 'ps_scripts', count(*) from ps_scripts
   union all select 'ps_executions', count(*) from ps_executions
   union all select 'metrics', count(*) from metrics;"
```

### Drill-Logbuch
Nach jedem Drill in dieser Datei ein Eintrag:
```
- 2026-04-24: Restore 2026-04-23 dump OK, RTO 7 min, Row-Counts identisch (sw)
```

---

## 7. Deployment-Rollback

### Sofort-Rollback
```bash
# Option A: k8s native
kubectl rollout undo deploy/api -n dashboard-prod
kubectl rollout status deploy/api -n dashboard-prod

# Option B: per Tag auf die letzte grüne SHA pinnen
#   docs/05-DEPLOYMENT.md beschreibt den Image-Tag-Workflow. Kurzfassung:
#   1. Vorherige SHA aus Git: git log --grep '^feat:' --format='%H %s' | head -5
#   2. k8s/overlays/prod/kustomization.yaml `newTag` anpassen
#   3. kubectl apply -k k8s/overlays/prod
```

**Wichtig:** Ein Rollback ohne vorherigen Sanity-Check ("hilft das überhaupt?") ist ein oft gewählter Fehler. Erst die letzten N Requests in Grafana ansehen, dann entscheiden.

---

## 8. Security-Alerts

### Trivy meldet High/Critical CVE im Image
1. CVE ID googeln → betrifft uns tatsächlich (z.B. ist betroffenes Binary genutzt)?
2. **Ja**: Patch-Image rebuilden (Base-Image-Tag bumpen, `docker/api.Dockerfile` bzw. `frontend.Dockerfile`) → PR mit "fix(security): CVE-XXXX-YYYY" → deploy.
3. **Nein**: als akzeptiert dokumentieren — per `.trivyignore` im Repo, mit Kommentar-Begründung.

### `dotnet list --vulnerable` meldet Paket
```bash
dotnet list backend/Dashboard.sln package --vulnerable --include-transitive
```
- Direkter Pakete: Version pinnen und bumpen.
- Transitiv: `Directory.Packages.props` zwingt die neue Version.

---

## 9. Observability-Stack down (Grafana/Loki/Prometheus)

Observability ist Tier-2 — ein Ausfall blockiert niemanden, aber wir sehen nichts mehr.

```bash
kubectl get pods -n observability
kubectl logs -n observability deploy/grafana --tail=200
kubectl logs -n observability statefulset/loki --tail=200
```

Standardfixes: Restart, PVC-Füllstand prüfen (Loki `retention_period: 168h` reicht bei durchschnittlicher Last ~5 GB aus — falls voll: bump `storage`).

---

## Change-Log

- 2026-04-24: Initial-Version (Phase 7).
