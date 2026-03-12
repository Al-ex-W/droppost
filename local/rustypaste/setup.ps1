#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Installs rustypaste as a Windows Scheduled Task (runs as SYSTEM on boot).

.DESCRIPTION
    - Downloads the rustypaste binary to C:\ProgramData\rustypaste\
    - Copies config.toml.template if config.toml doesn't exist yet
    - Sets a Windows Firewall rule to allow port 8000 from the VPS WireGuard IP only
    - Registers and starts a Scheduled Task so rustypaste survives reboots

.NOTES
    Run as Administrator.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ─── Configuration ────────────────────────────────────────────────────────────
$WIREGUARD_VPS_IP   = "10.8.0.1"     # VPS WireGuard IP — matches vps/wireguard/wg0.conf
$RUSTYPASTE_PORT    = 8000
$INSTALL_DIR        = "C:\ProgramData\rustypaste"
$UPLOAD_DIR         = "$INSTALL_DIR\uploads"
$BINARY_PATH        = "$INSTALL_DIR\rustypaste.exe"
$CONFIG_PATH        = "$INSTALL_DIR\config.toml"
$TASK_NAME          = "rustypaste"
# ──────────────────────────────────────────────────────────────────────────────

function Write-Step($msg) { Write-Host "`n=== $msg ===" -ForegroundColor Cyan }

# 1. Create directories
Write-Step "Creating directories"
New-Item -ItemType Directory -Force -Path $INSTALL_DIR | Out-Null
New-Item -ItemType Directory -Force -Path $UPLOAD_DIR  | Out-Null
Write-Host "  $INSTALL_DIR"
Write-Host "  $UPLOAD_DIR"

# 2. Build rustypaste via cargo
Write-Step "Building rustypaste"
if (-not (Get-Command cargo -ErrorAction SilentlyContinue)) {
    Write-Error "cargo not found. Install Rust first: winget install Rustlang.Rustup, then restart your terminal."
    exit 1
}
Write-Host "  Running: cargo install rustypaste (this takes a few minutes the first time)"
cargo install rustypaste 2>&1 | Write-Host

$cargobin = cargo locate-project --quiet 2>$null
$cargoBinPath = Join-Path $env:USERPROFILE ".cargo\bin\rustypaste.exe"
if (-not (Test-Path $cargoBinPath)) {
    Write-Error "Build succeeded but rustypaste.exe not found at $cargoBinPath"
    exit 1
}
Copy-Item -Path $cargoBinPath -Destination $BINARY_PATH -Force
Write-Host "  Installed to $BINARY_PATH"

# 3. Copy config template if config.toml doesn't exist
Write-Step "Config"
$templatePath = Join-Path $PSScriptRoot "config.toml.template"
if (-not (Test-Path $CONFIG_PATH)) {
    Copy-Item -Path $templatePath -Destination $CONFIG_PATH
    Write-Host "  Copied template to $CONFIG_PATH"
    Write-Host ""
    Write-Warning "ACTION REQUIRED: Edit $CONFIG_PATH before continuing."
    Write-Host "  Make sure the [server] address matches your WireGuard IP (10.8.0.2)."
    Write-Host "  Re-run this script after editing the config."
    exit 0
} else {
    Write-Host "  Config already exists at $CONFIG_PATH — skipping copy."
}

# 4. Windows Firewall rule
Write-Step "Windows Firewall"
# Remove old rule if it exists
netsh advfirewall firewall delete rule name="$TASK_NAME-inbound" | Out-Null
# Allow inbound on port 8000 ONLY from the VPS WireGuard IP
netsh advfirewall firewall add rule `
    name="$TASK_NAME-inbound" `
    protocol=TCP `
    dir=in `
    localport=$RUSTYPASTE_PORT `
    action=allow `
    remoteip=$WIREGUARD_VPS_IP | Out-Null
Write-Host "  Firewall rule added: TCP port $RUSTYPASTE_PORT from $WIREGUARD_VPS_IP only"

# 5. Scheduled Task
Write-Step "Scheduled Task"
$action    = New-ScheduledTaskAction `
    -Execute $BINARY_PATH `
    -Argument "--config `"$CONFIG_PATH`"" `
    -WorkingDirectory $INSTALL_DIR
$trigger   = New-ScheduledTaskTrigger -AtStartup
$settings  = New-ScheduledTaskSettingsSet `
    -ExecutionTimeLimit ([TimeSpan]::Zero) `
    -RestartCount 5 `
    -RestartInterval (New-TimeSpan -Minutes 1) `
    -StartWhenAvailable
$principal = New-ScheduledTaskPrincipal `
    -UserId "SYSTEM" `
    -LogonType ServiceAccount `
    -RunLevel Highest

# Unregister if already exists
if (Get-ScheduledTask -TaskName $TASK_NAME -ErrorAction SilentlyContinue) {
    Unregister-ScheduledTask -TaskName $TASK_NAME -Confirm:$false
}

Register-ScheduledTask `
    -TaskName  $TASK_NAME `
    -Action    $action `
    -Trigger   $trigger `
    -Settings  $settings `
    -Principal $principal `
    -Force | Out-Null

Start-ScheduledTask -TaskName $TASK_NAME
Write-Host "  Task '$TASK_NAME' registered and started."

# 6. Done
Write-Step "Done"
Write-Host "  rustypaste is running at http://10.8.0.2:$RUSTYPASTE_PORT"
Write-Host ""
Write-Host "  Useful commands:"
Write-Host "    Get-ScheduledTask -TaskName '$TASK_NAME'   # check status"
Write-Host "    Stop-ScheduledTask -TaskName '$TASK_NAME'  # stop"
Write-Host "    Start-ScheduledTask -TaskName '$TASK_NAME' # start"
Write-Host ""
Write-Host "  Logs: check Event Viewer > Task Scheduler"
