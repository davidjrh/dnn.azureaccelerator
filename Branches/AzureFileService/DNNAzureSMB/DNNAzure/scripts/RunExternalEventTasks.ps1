param (
    [string]$eventName = $(throw "-eventName is required."),
    [string]$localFolder = ""
)

# Function to unzip file contents
function Expand-ZIPFile($file, $destination)
{
    $shell = new-object -com shell.application
    $zip = $shell.NameSpace($file)
    foreach($item in $zip.items())
    {
        # Unzip the file with 0x14 (overwrite silently)
        $shell.Namespace($destination).copyhere($item, 0x14)
    }
}

# Function to write a log
function Write-Log($message) {
    $date = get-date -Format "yyyy-MM-dd HH:mm:ss"
    $content = "`n$date - $message"
    Add-Content $localfolder\ExternalEventTasksRun.log $content
}


# Function to process an external event task
function Process-ExternalEventTask($folder) {
    try {
        cd "$folder"

        Get-ChildItem "$folder" | Where-Object {$_.Name.ToLower() -match "task[0-9][0-9][0-9].[cmd|ps1]"} | Sort-Object $_.Name | ForEach-Object {
            Write-Log "Executing $folder\$_"        
            if ($_.Name.ToLower().EndsWith(".ps1")) {
                powershell.exe "$folder\$_"
            }
            elseif ($_.Name.ToLower().EndsWith(".cmd")) {
                cmd.exe /C "$folder\$_"
            }
        }
    }
    catch {
        Write-Log "Error while processing event task $folder : " + $_.Exception.Message
    }
}


# MAIN =========================================
if ($eventName -eq "") {
    exit
}

if ($localFolder -eq "") {
    $localFolder = "$pwd"
}

# Get all the External Event task folders
$folders = Get-ChildItem -Directory -Recurse -Force -Filter "$eventName"


# Split the URL in individual startup tasks
foreach ($folder in $folders) {
    Process-ExternalEventTask $folder.FullName
}

Write-Log "External event tasks execution finished ($eventName)"