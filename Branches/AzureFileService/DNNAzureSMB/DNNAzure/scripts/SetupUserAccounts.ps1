# BEGIN Support functions
[System.Reflection.Assembly]::LoadWithPartialName("Microsoft.WindowsAzure.ServiceRuntime")
[System.Reflection.Assembly]::LoadWithPartialName("Microsoft.WindowsAzure.Storage")

function Get-Setting($key, $defaultValue = "")
{
    if ([Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment]::IsAvailable)
    {
        try
        {
            return [Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment]::GetConfigurationSettingValue($key)
        }
        catch [Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironmentException]
        {
        }
    }

    if ([System.Configuration.ConfigurationManager]::AppSettings.AllKeys.Contains($key))
    {
        return [System.Configuration.ConfigurationManager]::AppSettings[$key] 
    }
    return $defaultValue;
}

Add-Type -assemblyName "System.Security" 

    [System.Reflection.Assembly]::LoadWithPartialName("System.Runtime.InteropServices")
        
    function Get-UnsecuredString($secureString)
    {
        if (!$secureString)
        {
            throw new ArgumentNullException("secureString")
        }
        $ptrUnsecureString = [System.IntPtr]::Zero
        try
        {
            $ptrUnsecureString = [System.Runtime.InteropServices.Marshal]::SecureStringToGlobalAllocUnicode($secureString)
            return [System.Runtime.InteropServices.Marshal]::PtrToStringUni($ptrUnsecureString)
        }
        finally
        {
            [System.Runtime.InteropServices.Marshal]::ZeroFreeGlobalAllocUnicode($ptrUnsecureString)
        }
    }


    function Decrypt-Password($encryptedPassword)
    {
        $password = $null

        if (![System.String]::IsNullOrEmpty($encryptedPassword))
        {
            try
            {
                $encryptedBytes = [System.Convert]::FromBase64String($encryptedPassword)
                $envelope = New-Object System.Security.Cryptography.Pkcs.EnvelopedCms
                $envelope.Decode($encryptedBytes)
                $store = New-Object System.Security.Cryptography.X509Certificates.X509Store My, LocalMachine
                [void] $store.Open #System.Security.Cryptography.X509Certificates.OpenFlags.ReadOnly
                $envelope.Decrypt($store.Certificates)
                $passwordChars = [System.Text.Encoding]::UTF8.GetChars($envelope.ContentInfo.Content)
                $password = New-Object System.Security.SecureString
                $passwordChars | ForEach-Object {
                    $password.AppendChar($_)
                }
                [System.Array]::Clear($envelope.ContentInfo.Content, 0, $envelope.ContentInfo.Content.Length)
                [System.Array]::Clear($passwordChars, 0, $passwordChars.Length)
                $password.MakeReadOnly()
            }
            catch [System.Security.Cryptography.CryptographicException]
            {
                # Unable to decrypt password. Make sure that the cert used for encryption was uploaded to the Azure service
                $password = $null
            }
            catch [System.FormatException]
            {
                # Encrypted password is not a valid base64 string
                $password = $null
            }
        }
        if (!$password)
        {            
            Write-Error "Unable to decrypt password. Make sure that the cert used for encryption was uploaded to the Azure service"
            return ""
        }
        return Get-UnsecuredString($password)
    }


function SetupLocalPolicies($accountToAdd) {
    $sidstr = $null
    try {
	    $ntprincipal = new-object System.Security.Principal.NTAccount "$accountToAdd"
	    $sid = $ntprincipal.Translate([System.Security.Principal.SecurityIdentifier])
	    $sidstr = $sid.Value.ToString()
    } catch {
	    $sidstr = $null
    }

    Write-Host "Configuring symbolic link permissions for user account $($accountToAdd)" 

    if( [string]::IsNullOrEmpty($sidstr) ) {
        Write-Error "User account account $($accountToAdd) not found!"
	    exit -1
    }

    Write-Host "User account SID: $($sidstr)" 

    $tmp = [System.IO.Path]::GetTempFileName()

    Write-Host "Exporting current Local Security Policy" 
    secedit.exe /export /cfg "$($tmp)" 

    $c = Get-Content -Path $tmp 

    $currentLogonSetting = ""

    foreach($s in $c) {
	    if( $s -like "SeInteractiveLogonRight*") {
		    $x = $s.split("=",[System.StringSplitOptions]::RemoveEmptyEntries)
		    $currentLogonSetting = $x[1].Trim()
	    }
    }

    if ($currentLogonSetting -notlike "*$($sidstr)*") {
	    Write-Host "Modifying Local Security Policies"
	    
        if ($currentLogonSetting -notlike "*$($sidstr)*") {
	        if([string]::IsNullOrEmpty($currentLogonSetting) ) {
		        $currentLogonSetting = "*$($sidstr)"
	        } else {
		        $currentLogonSetting = "*$($sidstr),$($currentLogonSetting)"
	        }
        }
	
        Write-Host "SeInteractiveLogonRight = $currentLogonSetting"
	
	    $outfile = @"
[Unicode]
Unicode=yes
[Version]
signature="`$CHICAGO`$"
Revision=1
[Privilege Rights]
SeInteractiveLogonRight = $($currentLogonSetting)
"@

	    $tmp2 = [System.IO.Path]::GetTempFileName()	
	
	    Write-Host "Importing new settings to Local Security Policy" 
	    $outfile | Set-Content -Path $tmp2 -Encoding Unicode -Force

	    Push-Location (Split-Path $tmp2)
	
	    try {
		    secedit.exe /configure /db "secedit.sdb" /cfg "$($tmp2)" /areas USER_RIGHTS 
	    } finally {	
		    Pop-Location
	    }
    } else {
	    Write-Host "No actions required. Account already in ""SeInteractiveLogonRight""" 
    }

    Write-Host "Done." 
}


function SetSiteRootPermissions($accountToAdd) {
	if (-Not [Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment]::IsEmulated) {
		return;
	}

    Write-Host "Changing site root folder permissions..."
    $rootPath = [Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment]::GetLocalResource("SitesRoot").RootPath
    $acl = Get-Acl $rootPath
    $permission = "$accountToAdd","FullControl","ContainerInherit,ObjectInherit","None","Allow"
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
    $acl.SetAccessRule($accessRule)
    $acl | Set-Acl $rootPath    
}



# END Support functions


# Main script

$username = Get-Setting "fileshareUserName"
$password = Get-Setting "fileshareUserPassword"

Add-Type -Path ($env:RoleRoot + '\approot\bin\Microsoft.WindowsAzure.Storage.dll')
$storageConnectionString = Get-Setting "AcceleratorConnectionString"
$account = [Microsoft.WindowsAzure.Storage.CloudStorageAccount]::Parse($storageConnectionString)
$accountName = $account.Credentials.AccountName
$accountKey = $account.Credentials.ExportBase64EncodedKey()

# Create user account
cmd.exe /C net.exe user "$username" "$password" /expires:never /add /Y
cmd.exe /C wmic.exe USERACCOUNT WHERE "Name='$username'" SET PasswordExpires=FALSE

# Setup local policies
$machineName = [System.Environment]::MachineName
$accountToAdd = "$machineName\$username"
SetupLocalPolicies $accountToAdd

# Persist File Service credentials
Push-Location
cd "$env:RoleRoot\approot\bin\scripts"
.\psexec.exe -accepteula -h -u "$username" -p "$password" cmd.exe /C cmdkey.exe /add:$accountName.file.core.windows.net /user:$accountName /pass:$accountKey
Pop-Location

# Set site root folder permissions (needed for Emulator)
SetSiteRootPermissions $accountToAdd

# Setup FTP accounts
$ftpEnabled = Get-Setting "FTP.Enabled" "false"
if ($ftpEnabled.ToLowerInvariant() -eq "true") {
    $ftpRootUserName = Get-Setting "FTP.Root.Username" 
    $ftpRootEncryptedPassword = Get-Setting "FTP.Root.EncryptedPassword"
    $ftpRootPassword = Decrypt-Password($ftpRootEncryptedPassword)
    cmd.exe /C net.exe user "$ftpRootUserName" "$ftpRootPassword" /expires:never /add /Y
    cmd.exe /C wmic.exe USERACCOUNT WHERE "Name='$ftpRootUserName'" SET PasswordExpires=FALSE


    $ftpPortalUserName = Get-Setting "FTP.Portals.Username"
    if ($ftpPortalUserName -ne "") {
        $ftpPortalEncryptedPassword = Get-Setting "FTP.Portals.EncryptedPassword"
        $ftpPortalPassword = Decrypt-Password($ftpPortalEncryptedPassword)
        cmd.exe /C net.exe user "$ftpPortalUserName" "$ftpPortalPassword" /expires:never /add /Y
        cmd.exe /C wmic.exe USERACCOUNT WHERE "Name='$ftpPortalUserName'" SET PasswordExpires=FALSE
    }
}

