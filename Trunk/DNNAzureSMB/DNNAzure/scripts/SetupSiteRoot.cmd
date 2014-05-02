if "%EMULATED%" == "true" goto SKIP

REM Enable short names on volume C:
fsutil.exe 8dot3name set c: 0

REM Add a short name to the local storage folder
fsutil.exe file setshortname "C:\Resources\Directory\%RoleDeploymentID%.DNNAzure.SitesRoot" Sites

reg add HKLM\Software\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell /v ExecutionPolicy /d Unrestricted /f
powershell .\CreateIISLogFolders.ps1 >> CreateIISLog.log 2>> CreateIISLog_err.log

:SKIP

EXIT /B 0