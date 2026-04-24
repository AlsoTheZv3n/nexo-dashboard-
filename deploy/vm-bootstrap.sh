#!/usr/bin/env bash
# ============================================================
# Nexo Dashboard — Single-VM bootstrap
# Idempotent: safe to re-run. Installs/configures:
#   - System hardening (UFW, fail2ban, unattended-upgrades)
#   - k3s (Traefik disabled; we front with external Nginx)
#   - Nginx + certbot
#   - Directory scaffolding for the GitHub Actions self-hosted runner
#
# USAGE
#   sudo DASHBOARD_HOST=dashboard.example.ch \
#        LETSENCRYPT_EMAIL=ops@example.com \
#        ./deploy/vm-bootstrap.sh
#
# The GitHub Actions runner is NOT installed by this script — the
# registration token is short-lived and must be minted interactively:
#   https://github.com/<org>/<repo>/settings/actions/runners/new
# The script prints the exact commands to paste when the time comes.
# ============================================================
set -euo pipefail

: "${DASHBOARD_HOST:?DASHBOARD_HOST must be set (e.g. dashboard.example.ch)}"
: "${LETSENCRYPT_EMAIL:?LETSENCRYPT_EMAIL must be set}"

log() { printf '\n\033[1;36m==> %s\033[0m\n' "$*"; }

# --- 0. sanity -----------------------------------------------------------------
if [[ $EUID -ne 0 ]]; then
  echo "Run me as root (or with sudo)." >&2
  exit 1
fi

. /etc/os-release
if [[ "${ID:-}" != "ubuntu" ]]; then
  echo "This bootstrap is tested on Ubuntu 24.04. You are on ${PRETTY_NAME:-unknown}." >&2
  exit 1
fi

# --- 1. base packages + auto updates ------------------------------------------
log "Updating apt + installing base packages"
export DEBIAN_FRONTEND=noninteractive
apt-get update -y
apt-get upgrade -y
apt-get install -y \
  curl wget git ufw fail2ban unattended-upgrades \
  ca-certificates gnupg lsb-release jq \
  nginx certbot python3-certbot-nginx

dpkg-reconfigure --priority=low unattended-upgrades || true
systemctl enable --now fail2ban

# --- 2. firewall --------------------------------------------------------------
log "Configuring UFW"
ufw default deny incoming
ufw default allow outgoing
ufw allow 22/tcp
ufw allow 80/tcp
ufw allow 443/tcp
# k3s pod + service CIDRs — not externally reachable, but allow intra-node traffic
ufw allow from 10.42.0.0/16
ufw allow from 10.43.0.0/16
yes | ufw enable || true
ufw status verbose

# --- 3. k3s -------------------------------------------------------------------
if ! systemctl is-active --quiet k3s; then
  log "Installing k3s (Traefik disabled)"
  curl -sfL https://get.k3s.io | sh -s - \
    --disable traefik \
    --write-kubeconfig-mode 644
else
  log "k3s already running — skipping install"
fi

# Give the non-root user (first sudoer) a workable kubeconfig
primary_user=$(getent passwd 1000 | cut -d: -f1 || true)
if [[ -n "$primary_user" ]]; then
  install -d -o "$primary_user" -g "$primary_user" "/home/${primary_user}/.kube"
  cp /etc/rancher/k3s/k3s.yaml "/home/${primary_user}/.kube/config"
  chown "$primary_user:$primary_user" "/home/${primary_user}/.kube/config"
fi

# --- 4. namespaces + secrets placeholders -------------------------------------
log "Creating prod namespace"
kubectl create namespace dashboard-prod --dry-run=client -o yaml | kubectl apply -f -

cat <<EOF

Next, create the secrets OUT OF BAND (values are intentionally not stored here):

  kubectl -n dashboard-prod create secret generic postgres-secret \\
    --from-literal=POSTGRES_USER=dashboard \\
    --from-literal=POSTGRES_PASSWORD='<STRONG_PASSWORD>' \\
    --from-literal=POSTGRES_DB=dashboard

  kubectl -n dashboard-prod create secret generic api-secret \\
    --from-literal='ConnectionStrings__Default=Host=postgres;Port=5432;Database=dashboard;Username=dashboard;Password=<STRONG_PASSWORD>' \\
    --from-literal='Jwt__SigningKey=<32+ random bytes base64>'

EOF

# --- 5. Nginx site config -----------------------------------------------------
SITE="/etc/nginx/sites-available/dashboard"
if [[ ! -f "$SITE" ]]; then
  log "Installing Nginx site config (you'll get SSL in the next step)"
  cp deploy/nginx/dashboard.conf "$SITE"
  sed -i "s|dashboard.example.ch|${DASHBOARD_HOST}|g" "$SITE"
  ln -sf "$SITE" /etc/nginx/sites-enabled/dashboard
  rm -f /etc/nginx/sites-enabled/default
  # Temporary HTTP-only site while certbot runs
  sed -i '/listen      443/,$d' "$SITE"
  nginx -t
  systemctl reload nginx
fi

# --- 6. SSL via certbot -------------------------------------------------------
if [[ ! -d "/etc/letsencrypt/live/${DASHBOARD_HOST}" ]]; then
  log "Requesting Let's Encrypt cert for ${DASHBOARD_HOST}"
  mkdir -p /var/www/letsencrypt
  certbot --nginx -d "${DASHBOARD_HOST}" \
    --non-interactive --agree-tos --email "${LETSENCRYPT_EMAIL}" \
    --redirect
  # Re-apply our full site config (certbot will have minimally patched it; our file is the source of truth)
  cp deploy/nginx/dashboard.conf "$SITE"
  sed -i "s|dashboard.example.ch|${DASHBOARD_HOST}|g" "$SITE"
  nginx -t
  systemctl reload nginx
else
  log "Cert already present — skipping certbot"
fi

# --- 7. Runner prep (interactive step documented) -----------------------------
RUNNER_HOME=/opt/actions-runner
if [[ ! -d "$RUNNER_HOME" ]]; then
  log "Preparing runner home at ${RUNNER_HOME} (install remains manual)"
  useradd --system --create-home --home-dir "$RUNNER_HOME" --shell /bin/bash runner || true
  chown -R runner:runner "$RUNNER_HOME"
fi

cat <<EOF

=======================================================================
 Bootstrap complete.

 Remaining manual steps:
  1) Generate a registration token at:
       https://github.com/<org>/<repo>/settings/actions/runners/new
  2) On this VM:
       sudo -iu runner
       cd /opt/actions-runner
       curl -o actions-runner-linux-x64.tar.gz -L \\
         https://github.com/actions/runner/releases/latest/download/actions-runner-linux-x64-<VERSION>.tar.gz
       tar xzf actions-runner-linux-x64.tar.gz
       ./config.sh --url https://github.com/<org>/<repo> --token <REG_TOKEN> \\
         --labels self-hosted,linux,prod --unattended
       exit
       sudo ./svc.sh install runner
       sudo ./svc.sh start
  3) Add sudoers grant (visudo) so the runner can apply kubectl + reload nginx:
       runner ALL=(ALL) NOPASSWD: /usr/local/bin/kubectl, /bin/systemctl reload nginx, /usr/sbin/nginx -t

 After these three steps a push to main will trigger deploy.yml all the way
 through build → Trivy gate → self-hosted apply → health check.
=======================================================================
EOF
