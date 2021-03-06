if "%EMULATED%" == "true" goto SKIP
if not "%ENABLED%" == "true" goto SKIP
if "%PRODUCTS%" == "" goto SKIP

cd %ROLEROOT%\APPROOT\bin\scripts
md %ROLEROOT%\APPROOT\bin\scripts\AppData 

reg add "hku\.default\software\microsoft\windows\currentversion\explorer\user shell folders" /v "Local AppData" /t REG_EXPAND_SZ /d "%ROLEROOT%\APPROOT\bin\scripts\AppData" /f

if "%CUSTOMFEED%" == "" goto NOCUSTOMFEED
WebpiCmd.exe /Install /Products:%PRODUCTS% /Feeds:%CUSTOMFEED% /AcceptEula /Log:wpiinstalllog.txt
goto ENDCUSTOMFEED

:NOCUSTOMFEED
WebpiCmd.exe /Install /Products:%PRODUCTS% /AcceptEula /Log:wpiinstalllog.txt

:ENDCUSTOMFEED
reg add "hku\.default\software\microsoft\windows\currentversion\explorer\user shell folders" /v "Local AppData" /t REG_EXPAND_SZ /d %%USERPROFILE%%\AppData\Local /f

:SKIP

EXIT /B 0