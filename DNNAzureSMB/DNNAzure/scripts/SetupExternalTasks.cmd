if "%EMULATED%"=="true" goto SKIP
IF "%EXTERNALTASKURL%"=="" goto SKIP

cd %ROLEROOT%\approot\bin\scripts
md %ROLEROOT%\approot\bin\scripts\external

reg add HKLM\Software\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell /v ExecutionPolicy /d Unrestricted /f
powershell .\SetupExternalTasks.ps1 -tasksUrl "%EXTERNALTASKURL%" >> ExternalTasks.log 2>> ExternalTasks_err.log

:SKIP
EXIT /B 0