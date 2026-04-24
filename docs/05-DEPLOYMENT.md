# 05 — Deployment

Deployment auf einer einzelnen **Ubuntu Server 24.04 LTS VM** mit k3s, Nginx als externem Reverse-Proxy und GitHub Actions Self-Hosted Runner.

---

## 1. VM-Anforderungen

| Ressource | Minimum | Empfohlen |
|-----------|---------|-----------|
| CPU | 2 vCPU | 4 vCPU |
| RAM | 4 GB | 8 GB |
| Disk | 40 GB | 80 GB SSD |
| OS | Ubuntu 22.04 | **Ubuntu 24.04 LTS** |
| Netzwerk | Public IPv4, Port 80/443 offen | + 22 (SSH, ideally via VPN/Wireguard) |

---

## 2. Initial-Setup (Einmalig pro VM)

### 2.1 System aktualisieren & Basis-Tools

```bash
sudo apt update && sudo apt upgrade -y
sudo apt install -y curl wget git ufw fail2ban unattended-upgrades \
                    ca-certificates gnupg lsb-release
```

### 2.2 Firewall (UFW)

```bash
sudo ufw default deny incoming
sudo ufw default allow outgoing
sudo ufw allow 22/tcp      # SSH
sudo ufw allow 80/tcp      # HTTP (Cert-Renewal)
sudo ufw allow 443/tcp     # HTTPS
# k3s intern (NICHT extern öffnen):
sudo ufw allow from 10.42.0.0/16
sudo ufw allow from 10.43.0.0/16
sudo ufw --force enable
```

### 2.3 fail2ban aktivieren

```bash
sudo systemctl enable --now fail2ban
```

### 2.4 Auto-Updates

```bash
sudo dpkg-reconfigure --priority=low unattended-upgrades
```

---

## 3. k3s installieren

```bash
# Installation mit Traefik deaktiviert (wir nutzen externes Nginx)
curl -sfL https://get.k3s.io | sh -s - \
  --disable traefik \
  --write-kubeconfig-mode 644

# Verifizieren
sudo systemctl status k3s
kubectl get nodes
```

**kubeconfig für den aktuellen User:**
```bash
mkdir -p ~/.kube
sudo cp /etc/rancher/k3s/k3s.yaml ~/.kube/config
sudo chown $(id -u):$(id -g) ~/.kube/config
```

**Helm installieren (optional):**
```bash
curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash
```

---

## 4. PostgreSQL im Cluster

### Option A — Einfacher Start (StatefulSet mit local-path)

```yaml
# k8s/base/postgres.yaml
apiVersion: v1
kind: Secret
metadata:
  name: postgres-secret
type: Opaque
stringData:
  POSTGRES_USER: dashboard
  POSTGRES_PASSWORD: CHANGE_ME
  POSTGRES_DB: dashboard
---
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: postgres
spec:
  serviceName: postgres
  replicas: 1
  selector:
    matchLabels:
      app: postgres
  template:
    metadata:
      labels:
        app: postgres
    spec:
      containers:
        - name: postgres
          image: postgres:16-alpine
          envFrom:
            - secretRef:
                name: postgres-secret
          ports:
            - containerPort: 5432
          volumeMounts:
            - name: data
              mountPath: /var/lib/postgresql/data
  volumeClaimTemplates:
    - metadata:
        name: data
      spec:
        accessModes: [ ReadWriteOnce ]
        resources:
          requests:
            storage: 10Gi
---
apiVersion: v1
kind: Service
metadata:
  name: postgres
spec:
  selector:
    app: postgres
  ports:
    - port: 5432
  clusterIP: None
```

### Option B — Managed ausserhalb (Production)
Z.B. bei einem Cloud-Anbieter oder auf der VM als systemd-Dienst. Skaliert besser, einfachere Backups.

**Für den Start: Option A, später ggf. migrieren.**

---

## 5. Application-Deployments

### 5.1 Namespace

```bash
kubectl create namespace dashboard-prod
```

### 5.2 Secrets

```bash
kubectl -n dashboard-prod create secret generic api-secrets \
  --from-literal=ConnectionStrings__Default="Host=postgres;Database=dashboard;Username=dashboard;Password=CHANGE_ME" \
  --from-literal=Jwt__Secret="$(openssl rand -base64 64)"
```

### 5.3 API-Deployment (`k8s/base/api.yaml`)

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: dashboard-api
spec:
  replicas: 2
  selector:
    matchLabels:
      app: dashboard-api
  template:
    metadata:
      labels:
        app: dashboard-api
    spec:
      containers:
        - name: api
          image: ghcr.io/OWNER/dashboard-api:latest
          ports:
            - containerPort: 8080
          envFrom:
            - secretRef:
                name: api-secrets
          env:
            - name: ASPNETCORE_ENVIRONMENT
              value: Production
            - name: ASPNETCORE_URLS
              value: http://+:8080
          livenessProbe:
            httpGet:
              path: /health/live
              port: 8080
            initialDelaySeconds: 30
          readinessProbe:
            httpGet:
              path: /health/ready
              port: 8080
          resources:
            requests:
              cpu: 200m
              memory: 256Mi
            limits:
              cpu: 1000m
              memory: 512Mi
---
apiVersion: v1
kind: Service
metadata:
  name: dashboard-api
spec:
  selector:
    app: dashboard-api
  ports:
    - port: 80
      targetPort: 8080
```

### 5.4 Frontend-Deployment

Frontend wird als statisches Artefakt von **Nginx ausserhalb** des Clusters ausgeliefert (performanter als im Cluster). Alternative: Eigener Nginx-Container im Cluster mit Static-Files.

**Empfohlene Variante (einfacher):** Frontend-Assets werden im CI in `/var/www/dashboard/` auf der VM entpackt.

---

## 6. Nginx (ausserhalb des Clusters)

### 6.1 Nginx installieren

```bash
sudo apt install -y nginx certbot python3-certbot-nginx
```

### 6.2 Konfiguration `/etc/nginx/sites-available/dashboard`

```nginx
# HTTP → HTTPS Redirect
server {
    listen 80;
    server_name dashboard.example.ch;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name dashboard.example.ch;

    # SSL (managed by Certbot)
    ssl_certificate     /etc/letsencrypt/live/dashboard.example.ch/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/dashboard.example.ch/privkey.pem;
    ssl_protocols       TLSv1.2 TLSv1.3;
    ssl_ciphers         HIGH:!aNULL:!MD5;

    # Security Headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Frame-Options            "DENY" always;
    add_header X-Content-Type-Options     "nosniff" always;
    add_header Referrer-Policy            "strict-origin-when-cross-origin" always;
    add_header Content-Security-Policy    "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self'" always;

    # Frontend (Static)
    root /var/www/dashboard;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    # API → k3s
    # Port kommt von NodePort-Service oder Ingress-Controller, hier Beispiel mit NodePort 30080
    location /api/ {
        proxy_pass http://127.0.0.1:30080;
        proxy_http_version 1.1;
        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 300s;
    }

    # Swagger (optional, nur intern öffnen wenn exposed)
    location /api/swagger/ {
        allow 127.0.0.1;
        allow 10.0.0.0/8;
        deny all;
        proxy_pass http://127.0.0.1:30080;
    }

    # Gzip
    gzip on;
    gzip_types text/plain text/css application/json application/javascript;
    gzip_min_length 1024;
}
```

### 6.3 Aktivieren + SSL

```bash
sudo ln -s /etc/nginx/sites-available/dashboard /etc/nginx/sites-enabled/
sudo rm /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl reload nginx

# SSL via Let's Encrypt
sudo certbot --nginx -d dashboard.example.ch
# Auto-Renewal ist via systemd-Timer bereits eingerichtet
```

### 6.4 k3s-Service für Nginx erreichbar machen

**NodePort-Service:**
```yaml
# k8s/base/api-nodeport.yaml
apiVersion: v1
kind: Service
metadata:
  name: dashboard-api-nodeport
spec:
  type: NodePort
  selector:
    app: dashboard-api
  ports:
    - port: 80
      targetPort: 8080
      nodePort: 30080
```

**Alternative:** Nginx-Ingress-Controller installieren und per `hostPort` auf die VM mappen.

---

## 7. GitHub Actions Self-Hosted Runner

### 7.1 Runner-User einrichten

```bash
sudo useradd -m -s /bin/bash ghrunner
sudo usermod -aG docker ghrunner        # Falls Docker ausserhalb k3s nötig
```

### 7.2 Runner installieren (als `ghrunner`)

```bash
sudo -u ghrunner -i
mkdir actions-runner && cd actions-runner
curl -o actions-runner.tar.gz -L https://github.com/actions/runner/releases/download/vX.X.X/actions-runner-linux-x64-X.X.X.tar.gz
tar xzf actions-runner.tar.gz

# Token aus GitHub Repo → Settings → Actions → Runners → New self-hosted runner
./config.sh --url https://github.com/OWNER/REPO --token TOKEN --labels dashboard-prod
```

### 7.3 Als systemd-Service

```bash
sudo ./svc.sh install ghrunner
sudo ./svc.sh start
sudo ./svc.sh status
```

### 7.4 Runner-Permissions (kubectl, Nginx-Reload)

```bash
# kubectl-Zugriff für ghrunner
sudo mkdir -p /home/ghrunner/.kube
sudo cp /etc/rancher/k3s/k3s.yaml /home/ghrunner/.kube/config
sudo chown -R ghrunner:ghrunner /home/ghrunner/.kube

# Sudoers für Nginx-Reload (beschränkt!)
echo "ghrunner ALL=(root) NOPASSWD: /usr/sbin/nginx -t, /usr/bin/systemctl reload nginx" \
  | sudo tee /etc/sudoers.d/ghrunner
```

---

## 8. CI/CD-Pipelines

### 8.1 CI — `.github/workflows/ci.yml`

```yaml
name: CI
on:
  pull_request:
  push:
    branches: [main]

jobs:
  backend:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:16-alpine
        env:
          POSTGRES_USER: test
          POSTGRES_PASSWORD: test
          POSTGRES_DB: test
        ports: [5432:5432]
        options: >-
          --health-cmd pg_isready
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '9.0.x' }
      - run: dotnet restore backend/
      - run: dotnet build backend/ --no-restore
      - run: dotnet test backend/ --no-build --collect:"XPlat Code Coverage"

  frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: pnpm/action-setup@v4
        with: { version: 9 }
      - uses: actions/setup-node@v4
        with: { node-version: '20', cache: 'pnpm', cache-dependency-path: frontend/pnpm-lock.yaml }
      - run: pnpm install --frozen-lockfile
        working-directory: frontend
      - run: pnpm lint
        working-directory: frontend
      - run: pnpm tsc --noEmit
        working-directory: frontend
      - run: pnpm test
        working-directory: frontend
      - run: pnpm build
        working-directory: frontend

  powershell:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - shell: pwsh
        run: Invoke-Pester -Path ./powershell/tests -CI
```

### 8.2 Deploy — `.github/workflows/deploy.yml`

```yaml
name: Deploy
on:
  push:
    branches: [main]
  workflow_dispatch:

jobs:
  build-and-push:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write
    outputs:
      sha: ${{ steps.vars.outputs.sha }}
    steps:
      - uses: actions/checkout@v4

      - uses: docker/setup-buildx-action@v3

      - uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - id: vars
        run: echo "sha=${GITHUB_SHA::7}" >> $GITHUB_OUTPUT

      - uses: docker/build-push-action@v5
        with:
          context: .
          file: docker/api.Dockerfile
          push: true
          tags: |
            ghcr.io/${{ github.repository }}/api:latest
            ghcr.io/${{ github.repository }}/api:${{ steps.vars.outputs.sha }}
          cache-from: type=gha
          cache-to:   type=gha,mode=max

      - uses: pnpm/action-setup@v4
        with: { version: 9 }
      - uses: actions/setup-node@v4
        with: { node-version: '20' }
      - run: pnpm install --frozen-lockfile
        working-directory: frontend
      - run: pnpm build
        working-directory: frontend
        env:
          VITE_API_BASE_URL: /api

      - uses: actions/upload-artifact@v4
        with:
          name: frontend-dist
          path: frontend/dist

  deploy:
    needs: build-and-push
    runs-on: [self-hosted, dashboard-prod]
    steps:
      - uses: actions/checkout@v4

      - uses: actions/download-artifact@v4
        with:
          name: frontend-dist
          path: frontend-dist

      # Frontend deployen
      - name: Deploy Frontend
        run: |
          sudo rsync -av --delete frontend-dist/ /var/www/dashboard/
          sudo nginx -t && sudo systemctl reload nginx

      # API deployen
      - name: Update k8s manifests
        run: |
          cd k8s/overlays/prod
          kustomize edit set image \
            ghcr.io/${{ github.repository }}/api=ghcr.io/${{ github.repository }}/api:${{ needs.build-and-push.outputs.sha }}

      - name: Apply to k3s
        run: |
          kubectl apply -k k8s/overlays/prod
          kubectl -n dashboard-prod rollout status deployment/dashboard-api --timeout=5m
```

---

## 9. Dockerfiles

### 9.1 API — `docker/api.Dockerfile`

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY backend/*.sln ./
COPY backend/Dashboard.Api/*.csproj          Dashboard.Api/
COPY backend/Dashboard.Core/*.csproj         Dashboard.Core/
COPY backend/Dashboard.Infrastructure/*.csproj Dashboard.Infrastructure/
COPY backend/Dashboard.PowerShell/*.csproj   Dashboard.PowerShell/
RUN dotnet restore

COPY backend/ ./
RUN dotnet publish Dashboard.Api -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# PowerShell Core installieren (falls nicht in base image)
RUN apt-get update && apt-get install -y --no-install-recommends wget \
 && wget -q "https://github.com/PowerShell/PowerShell/releases/download/v7.4.6/powershell_7.4.6-1.deb_amd64.deb" \
 && dpkg -i powershell_*.deb || apt-get install -f -y \
 && rm powershell_*.deb \
 && apt-get clean && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
COPY powershell/scripts /app/scripts

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Dashboard.Api.dll"]
```

### 9.2 Frontend — nicht nötig
Frontend wird im CI gebaut, `dist/` als Artefakt direkt auf VM kopiert.

---

## 10. Rollback-Strategie

### k8s Rollback (API)
```bash
kubectl -n dashboard-prod rollout undo deployment/dashboard-api
```

### Frontend Rollback
CI legt Builds in `/var/www/dashboard-releases/<sha>/` ab und verlinkt via Symlink `/var/www/dashboard` → `releases/<sha>`. Rollback = Symlink auf vorherige Release zeigen + Nginx-Reload.

### DB-Migrations
- **Forward-only** — nie destruktive Migrations ohne explizites Backup
- **Automatisch:** `dotnet ef database update` beim API-Startup (ENV-Flag-gesteuert: `APPLY_MIGRATIONS=true`)
- **Pre-Deploy Backup:** CI macht `pg_dump` via `kubectl exec` vor jedem Deploy

---

## 11. Monitoring & Logs (Basis)

- **Logs:** API schreibt in stdout → `kubectl logs -n dashboard-prod -l app=dashboard-api -f`
- **Zentralisiert später:** Loki + Promtail installieren, Grafana dazu
- **Healthchecks:** 
  - `GET /health/live` — nur App läuft
  - `GET /health/ready` — DB erreichbar, PS-Worker läuft

---

## 12. Backup-Strategie

### DB-Backup (CronJob im Cluster)

```yaml
apiVersion: batch/v1
kind: CronJob
metadata:
  name: postgres-backup
spec:
  schedule: "0 2 * * *"         # Täglich 02:00
  jobTemplate:
    spec:
      template:
        spec:
          containers:
            - name: pgdump
              image: postgres:16-alpine
              command:
                - /bin/sh
                - -c
                - |
                  pg_dump -h postgres -U $POSTGRES_USER $POSTGRES_DB \
                    | gzip > /backup/dashboard-$(date +%Y%m%d).sql.gz
              envFrom:
                - secretRef:
                    name: postgres-secret
              volumeMounts:
                - name: backup
                  mountPath: /backup
          volumes:
            - name: backup
              persistentVolumeClaim:
                claimName: backup-pvc
          restartPolicy: OnFailure
```

Dann per `rsync` oder `restic` auf externes Storage (NAS, S3, Backblaze).

---

## 13. Checkliste "Go-Live"

- [ ] Domain zeigt auf VM-IP
- [ ] SSL-Zertifikat aktiv (A+ auf ssllabs.com)
- [ ] Firewall konfiguriert
- [ ] fail2ban läuft
- [ ] k3s läuft + kubectl funktioniert
- [ ] PostgreSQL läuft + initial Migration applied
- [ ] Nginx-Config syntax-check grün
- [ ] GitHub Runner online + deploy durchgelaufen
- [ ] Healthchecks grün (`/health/live` + `/health/ready`)
- [ ] Login funktioniert mit initialem Admin-User
- [ ] Erste PS-Script-Execution erfolgreich
- [ ] Backup-CronJob hat 1× erfolgreich gelaufen
- [ ] Security-Headers via securityheaders.com geprüft
