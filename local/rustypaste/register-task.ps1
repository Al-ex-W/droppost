#Requires -RunAsAdministrator
$action    = New-ScheduledTaskAction -Execute 'C:\ProgramData\rustypaste\rustypaste.exe' -Argument '--config "C:\ProgramData\rustypaste\config.toml"' -WorkingDirectory 'C:\ProgramData\rustypaste'
$trigger   = New-ScheduledTaskTrigger -AtStartup
$settings  = New-ScheduledTaskSettingsSet -ExecutionTimeLimit ([TimeSpan]::Zero) -RestartCount 5 -RestartInterval (New-TimeSpan -Minutes 1) -StartWhenAvailable
$principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
Register-ScheduledTask -TaskName 'rustypaste' -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Force
Start-ScheduledTask -TaskName 'rustypaste'
Start-Sleep 2
Get-ScheduledTask -TaskName 'rustypaste' | Select-Object TaskName, State
netstat -ano | findstr :8000
