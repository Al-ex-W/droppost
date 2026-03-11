# droppost

A private airdrop / pastebin service for sharing files and pastes with friends.

## Architecture

```
Internet
   │
   ▼
VPS (DigitalOcean)
  nginx — HTTPS, API key auth
   │
   │  WireGuard tunnel (only channel to local)
   ▼
Local PC (Windows)
  rustypaste — stores and serves files
```

- The local machine is never directly exposed to the internet.
- All public traffic hits the VPS. nginx validates API keys, then proxies to rustypaste on the local machine through the WireGuard tunnel.
- Each user gets a unique API key. Keys are managed on the VPS in a gitignored config file.

## Subprojects

| Directory | Description |
|-----------|-------------|
| `local/`  | Windows setup: rustypaste binary + WireGuard client |
| `vps/`    | VPS setup: nginx + WireGuard server |
| `web/`    | Web upload UI (WIP) |
| `tray/`   | Windows system tray app (WIP) |

## Setup order

1. Set up the VPS first (`vps/README.md`) — you need the VPS WireGuard public key before configuring the local side.
2. Set up the local machine (`local/README.md`).
3. Verify the tunnel is working, then start rustypaste.
4. Add API keys on the VPS.

## API

All endpoints require `Authorization: Bearer <your-api-key>`.

| Method | Path | Description |
|--------|------|-------------|
| `POST /` | multipart `file` field | Upload a file. Returns the URL. |
| `POST /` | multipart `file` field + `expire` field | Upload with custom expiry (e.g. `1h`, `7d`). |
| `GET /<id>` | — | Download a file. |
| `DELETE /<id>` | — | Delete a file (if configured). |

Default expiry: **24 hours**.
