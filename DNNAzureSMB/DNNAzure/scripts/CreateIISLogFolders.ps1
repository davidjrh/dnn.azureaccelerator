<#
    This script is to fix up the problems with the permissions in the site's log folder. This folder is created by IIS the first time the site is invoked. The problem is that IIS creates
    the folder without inheriting the parent folder permissions. This is a problem, because the Azure diagnostic agent (the one in charge of uploading the log files to the storage, doesn't
    have read permissions to that folder.
    This script creates the folder if it doesn't exist and, if it exists, it fixes up the permissions, to inherit the permissions from the parent folder
#>

$scriptPath = split-path -parent $MyInvocation.MyCommand.Definition;
$logPath = Join-Path $scriptPath "IISLogFix.log"

[void] [System.Reflection.Assembly]::LoadWithPartialName("Microsoft.WindowsAzure.ServiceRuntime")

function Append-Log($text)
{
    $((Get-Date -Format "yyyy-MM-dd HH:mm:ss") + " - " + $text) >> $logPath
    Write-Host $((Get-Date -Format "yyyy-MM-dd HH:mm:ss") + " - " + $text)
    foreach ($i in $input) {
        $((Get-Date -Format "yyyy-MM-dd HH:mm:ss") + " - " + $i) >> $logPath
        Write-Host $((Get-Date -Format "yyyy-MM-dd HH:mm:ss") + " - " + $i)
    }
}

function Create-LogPath($siteId) {
	$iisLogPath = Join-Path $([Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment]::GetLocalResource("DiagnosticStore").RootPath) $("LogFiles\Web\W3SVC" + $siteId)
	
	Append-Log $("Checking path " + $iisLogPath + "...")
    if (Test-Path $iisLogPath) {
        Append-Log "The IIS log folder already exists. Upgrading its permissions..."
	    $inheritance = Get-Acl $iisLogPath
	    $inheritance.SetAccessRuleProtection($false, $false)
	    Set-Acl $iisLogPath -AclObject $inheritance
    } else {
        Append-Log "The IIS log folder doesn't exists. Creating it..."
        New-Item $iisLogPath -ItemType directory
    }	
}

Append-Log "Creating log folder for the DotNetNuke site..."
Create-LogPath 1 # We guess that the DotNetNuke app will have Id = 1
Append-Log "Creating log folder for the Offline site..."
Create-LogPath 2 # We guess that the Offline app will have Id = 2
