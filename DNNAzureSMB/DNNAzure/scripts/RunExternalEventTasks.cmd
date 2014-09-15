cd %ROLEROOT%\approot\bin\scripts
REM md %ROLEROOT%\approot\bin\scripts\external

echo %DATE% %TIME% - Calling external event tasks (%1%) >> ExternalEventTasks.log
reg add HKLM\Software\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell /v ExecutionPolicy /d Unrestricted /f
powershell .\RunExternalEventTasks.ps1 -eventName "%1%" >> ExternalEventTasks.log 2>&1
echo %DATE% %TIME% - End calling external event tasks (%1%) >> ExternalEventTasks.log
