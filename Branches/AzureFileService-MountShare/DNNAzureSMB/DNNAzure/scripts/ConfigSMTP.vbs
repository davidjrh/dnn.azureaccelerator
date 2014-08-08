Option Explicit
Dim smtpServer, relayIpList
' Get the default instance of the SMTP server
Set smtpServer = GetObject("IIS://localhost/smtpsvc/1")
' Get the IPList
Set relayIpList = smtpServer.Get("RelayIpList")

' Add localhost to that list
relayIpList.GrantByDefault = false
relayIpList.IpGrant = "127.0.0.1"
' Save changes
smtpServer.Put "RelayIpList",relayIpList
smtpServer.SetInfo

' Uncomment this if you want to use a smart host
' More info on: http://nicoploner.blogspot.com.es/2011/10/sending-e-mails-using-iis-smtp-server.html
'' set the outbound connector to a smart host
'smtpServer.SmartHostType = 2
'smtpServer.SmartHost = "smtp.mysmarthost.tld"
'' use basic authentication
'smtpServer.RouteAction = 264
'smtpServer.RouteUserName = "myName"
'smtpServer.RoutePassword = "myPassword"
'' save changes
'smtpServer.SetInfo
