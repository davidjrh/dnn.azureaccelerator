if "%EMULATED%" == "true" goto SKIP

REM Enable short names on volume C:
fsutil.exe 8dot3name set c: 0

REM Add a short name to the local storage folder
fsutil.exe file setshortname "C:\Resources\Directory\%RoleDeploymentID%.DNNAzure.SitesRoot" Sites

:SKIP

EXIT /B 0