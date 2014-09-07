<#
	Setup the file service credentials for the current user
#>
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

function Execute-Cmd($command) {
    & "cmd.exe" /c $command
}

Add-Type -Path ($env:RoleRoot + '\approot\bin\Microsoft.WindowsAzure.Storage.dll')
$storageConnectionString = Get-Setting "AcceleratorConnectionString"
$account = [Microsoft.WindowsAzure.Storage.CloudStorageAccount]::Parse($storageConnectionString)
$accountName = $account.Credentials.AccountName
$accountKey = $account.Credentials.ExportBase64EncodedKey()

## Store the credentials
cmdkey.exe /add:$accountName.file.core.windows.net /user:$accountName /pass:$accountKey

## Create a DotNetNuke shortcut folder in the desktop
$dnnPath = $(Get-Website | Where { $_.Name -like "DotNetNuke" }).PhysicalPath
IF ($dnnPath) {
    $destination = Join-Path $([Environment]::GetFolderPath("Desktop")) DotNetNuke
    IF ($(Test-Path $destination)) {
        Execute-Cmd $("rmdir /Q " + $destination)
    }
    Execute-Cmd $("mklink /d " + $destination + " " + $dnnPath)
}

<#
	Setup the background info for Remote Desktop
#>

Add-Type @"
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
namespace Wallpaper
{
   public enum Style : int
   {
       Tile, Center, Stretch, NoChange
   }
   public class Setter {
      public const int SetDesktopWallpaper = 20;
      public const int UpdateIniFile = 0x01;
      public const int SendWinIniChange = 0x02;
      [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
      private static extern int SystemParametersInfo (int uAction, int uParam, string lpvParam, int fuWinIni);
      public static void SetWallpaper ( string path, Wallpaper.Style style ) {
         SystemParametersInfo( SetDesktopWallpaper, 0, path, UpdateIniFile | SendWinIniChange );
         RegistryKey key = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", true);
         key.SetValue(@"WallpaperStyle", "10"); 
         key.SetValue(@"TileWallpaper", "0") ;
         key.Close();
      }
   }
}
"@

[Wallpaper.Setter]::SetWallpaper( "$pwd\DNNCloud.gif", 0)
$programData = Get-Childitem env:ALLUSERSPROFILE | %{ $_.Value }
.\BGInfo.exe "$pwd\DNNCloud.bgi" /TIMER:0 /NOLICPROMPT /SILENT


<#
	Pin IIS Manager to the toolbar
#>
$shell = new-object -com "Shell.Application"  
$folder = $shell.Namespace($Env:windir + '\system32\inetsrv')
$item = $folder.Parsename('InetMgr.exe')
$item.invokeverb('taskbarpin')

<#
	Disable IE Esc
#>
$AdminKey = “HKLM:\SOFTWARE\Microsoft\Active Setup\Installed Components\{A509B1A7-37EF-4b3f-8CFC-4F3A74704073}”
Set-ItemProperty -Path $AdminKey -Name “IsInstalled” -Value 0
Stop-Process -Name Explorer
Write-Host “IE Enhanced Security Configuration (ESC) has been disabled.” -ForegroundColor Green
