<#
    Configure WinRM by using one of the installed certificates with "Server Authentication" in the property "Intended Purpose"
#>

function Execute-Cmd($cmd) {
    $filePath = [System.IO.Path]::GetTempPath() + [System.Guid]::NewGuid().ToString() + ".cmd"
    $cmd | Out-File -LiteralPath:$filePath -Force -Encoding:"Default"
    Invoke-Expression -Command:$filePath
    Remove-Item -LiteralPath:$filePath -Force
}

$cert = Get-ChildItem Cert:\LocalMachine\My\ | WHERE {$_.EnhancedKeyUsageList.FriendlyName -contains "Server Authentication" -and $_.FriendlyName -eq "WMSVC"} | SELECT -First 1 -property @{N='CertId';E={$_.DnsNameList[0].Unicode}}, @{N='Thumbprint';E={$_.Thumbprint}}
If (!$cert) {
    $cert = Get-ChildItem Cert:\LocalMachine\My\ | WHERE {$_.EnhancedKeyUsageList.FriendlyName -contains "Server Authentication"} | SELECT -First 1 -property @{N='CertId';E={$_.DnsNameList[0].Unicode}}, @{N='Thumbprint';E={$_.Thumbprint}}
}
If ($cert) {
    $thumbprint = $cert.Thumbprint
    $certId = $cert.CertId

    Write-Output "Configuring WinRM to allow access throught Powershell..."
    Execute-Cmd "winrm create winrm/config/listener?Address=*+Transport=HTTPS `@`{Hostname=`"$certId`"`;CertificateThumbprint=`"$thumbprint`"`}"
    Set-Item WSMan:\localhost\Shell\MaxMemoryPerShellMB 2000
    Write-Output "WinRM configured"
}
Else {
    Write-Error "There isn't a valid certificate"
}
