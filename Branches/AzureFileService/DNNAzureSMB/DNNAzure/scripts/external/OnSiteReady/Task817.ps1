# This external event task setups the MSDeploy user

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
# END Support functions

# Main script
$products = Get-Setting "WebPlatformInstaller.Products"
if ($products -like "*WDeploy*") {
    Add-Type -Path ($env:RoleRoot + '\approot\bin\Microsoft.WindowsAzure.Storage.dll')
    $storageConnectionString = Get-Setting "AcceleratorConnectionString"
    $account = [Microsoft.WindowsAzure.Storage.CloudStorageAccount]::Parse($storageConnectionString)
    $accountName = $account.Credentials.AccountName
    $accountKey = $account.Credentials.ExportBase64EncodedKey()

	Push-Location
	cd "${env:ProgramFiles}\IIS\Microsoft Web Deploy V3\Scripts"
	.\SetupSiteForPublish.ps1 -siteName "DotNetNuke" -deploymentUserName "$accountName" -deploymentUserPassword "$accountKey"
	Pop-Location
}


