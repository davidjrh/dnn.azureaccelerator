<#
	Setup the background info for Remote Desktop
#>
$programData = Get-Childitem env:ALLUSERSPROFILE | %{ $_.Value }

$WshShell = New-Object -comObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$programData\Microsoft\Windows\Start Menu\Programs\Startup\Startup.lnk")
$Shortcut.TargetPath = "powershell.exe"
$Shortcut.Arguments = "$pwd\StartupItems.ps1"
$Shortcut.WorkingDirectory = "$pwd"
$Shortcut.WindowStyle = 7
$Shortcut.Save()