# local — Windows setup

Sets up the local machine as the file storage backend. Two components:
- **WireGuard** — secure tunnel to the VPS (official Windows client, runs in system tray)
- **rustypaste** — lightweight paste/file server (native Windows binary, runs as a scheduled task)

rustypaste only listens on the WireGuard interface IP (`10.8.0.2`). A Windows Firewall rule blocks all other inbound access to its port.

## Prerequisites

- [WireGuard for Windows](https://www.wireguard.com/install/) installed
- PowerShell 5.1+ (built into Windows 10/11)
- Run PowerShell as Administrator for setup

## Setup

### 1. WireGuard

Generate your local keypair (run in PowerShell or WireGuard app):

```powershell
# In WireGuard app: click "Add Tunnel" > "Add empty tunnel"
# It will generate a key pair for you — copy the public key and give it to the VPS admin.
```

Fill in `wireguard/wg0.conf.template`:
- Replace `YOUR_LOCAL_PRIVATE_KEY` with your private key
- Replace `YOUR_VPS_PUBLIC_KEY` with the VPS public key (from vps setup)
- Replace `YOUR_VPS_IP` with the VPS IP address

Save the filled-in file as `wireguard/wg0.conf` (gitignored), then import it into the WireGuard app.

### 2. rustypaste

```powershell
# As Administrator:
cd local\rustypaste
.\setup.ps1
```

The script will:
1. Download the latest rustypaste binary to `C:\ProgramData\rustypaste\`
2. Prompt you to fill in `config.toml` (copied from template) if it doesn't exist yet
3. Add a Windows Firewall rule to restrict port 8000 to `10.8.0.1` (VPS WireGuard IP) only
4. Register and start a Windows Scheduled Task that runs rustypaste as SYSTEM on boot

### 3. Verify

Once the WireGuard tunnel is active and rustypaste is running:

```powershell
# From PowerShell on local machine:
curl http://10.8.0.2:8000/
# Should return rustypaste landing page

# From VPS (through tunnel):
curl http://10.8.0.2:8000/
```

## File locations

| Path | Description |
|------|-------------|
| `C:\ProgramData\rustypaste\rustypaste.exe` | Binary |
| `C:\ProgramData\rustypaste\config.toml` | Active config (gitignored) |
| `C:\ProgramData\rustypaste\uploads\` | Stored files |

## Updating rustypaste

Edit `RUSTYPASTE_VERSION` in `setup.ps1` and re-run it. The scheduled task will be updated automatically.
