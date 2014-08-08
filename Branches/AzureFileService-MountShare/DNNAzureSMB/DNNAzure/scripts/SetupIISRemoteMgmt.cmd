if "%EMULATED%" == "true" goto SKIP
if not "%ENABLED%" == "true" goto SKIP

dism /online /enable-feature /featurename:IIS-WebServerRole 
dism /online /enable-feature /featurename:IIS-WebServerManagementTools
dism /online /enable-feature /featurename:IIS-ManagementService
reg add HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WebManagement\Server /v EnableRemoteManagement /t REG_DWORD /d 1 /f  
net start wmsvc >> SetupIISRemoteMgmt_log.txt 2>> SetupIISRemoteMgmt_err.txt
sc config wmsvc start=auto

:SKIP

EXIT /B 0