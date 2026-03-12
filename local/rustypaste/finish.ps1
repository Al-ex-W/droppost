#Requires -RunAsAdministrator
netsh advfirewall firewall delete rule name="rustypaste-inbound"
netsh advfirewall firewall add rule name="rustypaste-inbound" protocol=TCP dir=in localport=8000 action=allow remoteip=10.8.0.1

$action    = New-ScheduledTaskAction -Execute "C:\ProgramData\rustypaste\rustypaste.exe" -Argument '--config "C:\ProgramData\rustypaste\config.toml"' -WorkingDirectory "C:\ProgramData\rustypaste"
$trigger   = New-ScheduledTaskTrigger -AtStartup
$settings  = New-ScheduledTaskSettingsSet -ExecutionTimeLimit ([TimeSpan]::Zero) -RestartCount 5 -RestartInterval (New-TimeSpan -Minutes 1) -StartWhenAvailable
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
Register-ScheduledTask -TaskName "rustypaste" -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Force
Start-ScheduledTask -TaskName "rustypaste"
Write-Host "Done — rustypaste is running." -ForegroundColor Green
