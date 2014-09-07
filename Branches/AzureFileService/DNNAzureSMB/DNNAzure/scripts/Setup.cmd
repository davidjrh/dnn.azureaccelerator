cd %ROLEROOT%\APPROOT\bin\scripts

reg.exe add HKLM\Software\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell /v ExecutionPolicy /d Unrestricted /f

REM Setup User Accounts, File Storage credentials and permissions
powershell.exe .\SetupUserAccounts.ps1 >> SetupUserAccounts.log 2>> SetupUserAccounts_err.log

REM Create IIS Log Folders
powershell .\CreateIISLogFolders.ps1 >> CreateIISLog.log 2>> CreateIISLog_err.log

REM Unlock IIS runtime section to setup IIS compression
%WINDIR%\System32\inetsrv\appcmd.exe unlock config /section:system.webServer/serverRuntime

REM Create Start Menu items
powershell .\CreateStartMenuItems.ps1 >> CreateStartMenuItems.log 2>> CreateStartMenuItems_err.log


REM Setup SMTP service
if not "%SMTPENABLED%"=="true" goto SKIPSMTP
powershell .\SetupSMTP.ps1
cscript ConfigSMTP.vbs
:SKIPSMTP

:SKIP

EXIT /B 0