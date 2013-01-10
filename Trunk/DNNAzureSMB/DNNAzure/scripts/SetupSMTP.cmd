if "%EMULATED%"=="true" goto SKIP

cd %ROLEROOT%\APPROOT\bin\scripts
reg add HKLM\Software\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell /v ExecutionPolicy /d Unrestricted /f
powershell .\SetupSMTP.ps1
cscript ConfigSMTP.vbs

:SKIP