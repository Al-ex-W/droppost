# vps — DigitalOcean setup

Runs on a DigitalOcean Droplet (Ubuntu 22.04). Two components:
- **WireGuard** — tunnel endpoint; the only way to reach the local PC
- **nginx** — public HTTPS frontend; validates API keys and proxies to rustypaste

## Recommended Droplet

Basic plan, $6/mo: 1 vCPU, 1 GB RAM, 25 GB SSD. More than sufficient.

## Prerequisites

- Ubuntu 22.04 LTS Droplet
- Your domain's DNS A record pointing to the Droplet IP
- SSH access as root (or a user with sudo)

## Setup

```bash
git clone <this-repo> droppost
cd droppost/vps
sudo bash setup.sh
```

The script installs WireGuard, nginx, and Certbot, then walks you through configuration.

## Post-setup steps

### 1. Fill in WireGuard config

After `setup.sh` runs, it prints the VPS public key.
Edit `/etc/wireguard/wg0.conf` — add the local PC's public key:

```ini
[Peer]
PublicKey = LOCAL_PC_PUBLIC_KEY
AllowedIPs = 10.8.0.2/32
```

Then start the tunnel:

```bash
systemctl enable --now wg-quick@wg0
```

### 2. Configure nginx

Copy and fill in the nginx server block:

```bash
cp nginx/droppost.conf /etc/nginx/sites-available/droppost
# Edit /etc/nginx/sites-available/droppost:
#   Replace YOUR_DOMAIN with your actual domain
ln -s /etc/nginx/sites-available/droppost /etc/nginx/sites-enabled/droppost
nginx -t && systemctl reload nginx
```

Get a TLS certificate:

```bash
certbot --nginx -d YOUR_DOMAIN
```

### 3. Add API keys

```bash
cp nginx/keys.conf.example /etc/nginx/conf.d/droppost-keys.conf
# Edit /etc/nginx/conf.d/droppost-keys.conf
# Add one line per user (see the example file for format)
nginx -t && systemctl reload nginx
```

Keys are stored in `/etc/nginx/conf.d/droppost-keys.conf` — this file is on the server only, never in git.

## Managing API keys

To add a key:
```bash
# Generate a key
openssl rand -hex 32
# Add this line to /etc/nginx/conf.d/droppost-keys.conf:
#   "Bearer <the-key>" 1;  # username
systemctl reload nginx
```

To revoke a key: remove the line and reload nginx.

## Checking the tunnel

```bash
wg show            # WireGuard status
ping 10.8.0.2      # Ping local PC through tunnel
curl http://10.8.0.2:8000/  # Hit rustypaste directly
```
