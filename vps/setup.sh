#!/usr/bin/env bash
# VPS setup script — Ubuntu 22.04
# Run as root: sudo bash setup.sh
set -euo pipefail

# ─── Configuration (edit if needed) ──────────────────────────────────────────
WG_PORT=51820
WG_VPS_IP="10.8.0.1/24"
WG_LOCAL_IP="10.8.0.2/32"
WG_IFACE="wg0"
RUSTYPASTE_PORT=8000
# ─────────────────────────────────────────────────────────────────────────────

step() { echo -e "\n\033[36m=== $* ===\033[0m"; }
info() { echo "  $*"; }
warn() { echo -e "\033[33m  WARN: $*\033[0m"; }

if [[ $EUID -ne 0 ]]; then
    echo "Run as root: sudo bash $0"
    exit 1
fi

step "Installing dependencies"
apt-get update -qq
apt-get install -y wireguard nginx certbot python3-certbot-nginx ufw

step "WireGuard — generating server keys"
if [[ -f /etc/wireguard/server.key ]]; then
    warn "Server key already exists — skipping key generation"
else
    wg genkey | tee /etc/wireguard/server.key | wg pubkey > /etc/wireguard/server.pub
    chmod 600 /etc/wireguard/server.key
    info "Keys written to /etc/wireguard/server.key and server.pub"
fi

VPS_PRIVATE_KEY=$(cat /etc/wireguard/server.key)
VPS_PUBLIC_KEY=$(cat /etc/wireguard/server.pub)

step "WireGuard — writing server config"
cat > /etc/wireguard/${WG_IFACE}.conf << EOF
[Interface]
PrivateKey = ${VPS_PRIVATE_KEY}
Address = ${WG_VPS_IP}
ListenPort = ${WG_PORT}

# Enable NAT so the local PC can be reached through the tunnel
PostUp   = iptables -A FORWARD -i ${WG_IFACE} -j ACCEPT; iptables -t nat -A POSTROUTING -o eth0 -j MASQUERADE
PostDown = iptables -D FORWARD -i ${WG_IFACE} -j ACCEPT; iptables -t nat -D POSTROUTING -o eth0 -j MASQUERADE

# ─── Add the local PC peer below ────────────────────────────────────────────
# Uncomment and fill in after you have the local PC's WireGuard public key.
#
# [Peer]
# PublicKey = LOCAL_PC_PUBLIC_KEY
# AllowedIPs = ${WG_LOCAL_IP}
EOF
chmod 600 /etc/wireguard/${WG_IFACE}.conf
info "Written to /etc/wireguard/${WG_IFACE}.conf"

step "Kernel — enabling IP forwarding"
grep -qxF 'net.ipv4.ip_forward=1' /etc/sysctl.conf || echo 'net.ipv4.ip_forward=1' >> /etc/sysctl.conf
sysctl -p -q

step "Firewall (ufw)"
ufw allow 22/tcp    comment "SSH"
ufw allow 80/tcp    comment "HTTP (certbot)"
ufw allow 443/tcp   comment "HTTPS"
ufw allow ${WG_PORT}/udp comment "WireGuard"
ufw --force enable
info "ufw enabled"

step "nginx — installing site config"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [[ ! -f /etc/nginx/sites-available/droppost ]]; then
    cp "${SCRIPT_DIR}/nginx/droppost.conf" /etc/nginx/sites-available/droppost
    info "Copied nginx config to /etc/nginx/sites-available/droppost"
    info "Edit it: replace YOUR_DOMAIN with your actual domain, then run:"
    info "  ln -s /etc/nginx/sites-available/droppost /etc/nginx/sites-enabled/droppost"
    info "  certbot --nginx -d YOUR_DOMAIN"
    info "  systemctl reload nginx"
else
    warn "/etc/nginx/sites-available/droppost already exists — skipping"
fi

if [[ ! -f /etc/nginx/conf.d/droppost-keys.conf ]]; then
    cp "${SCRIPT_DIR}/nginx/keys.conf.example" /etc/nginx/conf.d/droppost-keys.conf
    warn "Placeholder keys.conf installed — edit /etc/nginx/conf.d/droppost-keys.conf and add real keys!"
fi

# Remove default nginx site if present
rm -f /etc/nginx/sites-enabled/default

nginx -t
systemctl enable --now nginx

step "Done"
echo ""
echo "  ┌─────────────────────────────────────────────────────┐"
echo "  │  VPS WireGuard public key (share with local setup): │"
echo "  │                                                       │"
echo "  │  ${VPS_PUBLIC_KEY}"
echo "  │                                                       │"
echo "  └─────────────────────────────────────────────────────┘"
echo ""
echo "  Next steps:"
echo "    1. Add the local PC's WireGuard public key to /etc/wireguard/wg0.conf"
echo "    2. systemctl enable --now wg-quick@wg0"
echo "    3. Edit /etc/nginx/sites-available/droppost (set YOUR_DOMAIN)"
echo "    4. ln -s /etc/nginx/sites-available/droppost /etc/nginx/sites-enabled/droppost"
echo "    5. certbot --nginx -d YOUR_DOMAIN"
echo "    6. Edit /etc/nginx/conf.d/droppost-keys.conf — add real API keys"
echo "    7. nginx -t && systemctl reload nginx"
