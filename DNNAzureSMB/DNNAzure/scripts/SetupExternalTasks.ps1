param (
    [string]$tasksUrl = $(throw "-taskUrl is required."),
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
    Add-Content $localfolder\SetupExternalTasks.log $content
}


# Function to process an external task Url
function Process-ExternalTask($taskUrl, $localFolder, $index) {
    try {
        # Define local filename
        $file = "$localFolder\ExternalTasks_$index.cmd"
        if ($taskUrl.ToLower().EndsWith(".zip")) {
            $file = "$localFolder\ExternalTasks_$index.zip"
        }
        elseif ($taskUrl.ToLower().EndsWith(".ps1")) {
            $file = "$localFolder\ExternalTasks_$index.ps1"
        }

        # Download the tasks file
        Write-Log "Downloading $taskUrl to file $file..."
        $webclient = New-Object System.Net.WebClient
        $webclient.DownloadFile($taskUrl, $file)
        Write-Log "Download completed"

        # If the tasks are zipped, unzip them first
        if ($taskUrl.ToLower().EndsWith(".zip")) {
            
            New-Item -ItemType Directory -Force -Path "$localFolder\$index"
            cd "$localFolder\$index"

            Write-Log "Unzipping $file..."
            Expand-ZIPFile -file "$file" -destination "$localFolder\$index"
            Write-Log "Unzip completed"            

            # When a .zip file is specied, only files called "Task???.cmd" and "Task???.ps1" will be executed
            # This allows to include assemblies and other file dependencies in the zip file
            Get-ChildItem "$localFolder\$index" | Where-Object {$_.Name.ToLower() -match "task[0-9][0-9][0-9].[cmd|ps1]"} | Sort-Object $_.Name | ForEach-Object {
                Write-Log "Executing $localfolder\$index\$_"        
                if ($_.Name.ToLower().EndsWith(".ps1")) {
                    powershell.exe "$localFolder\$index\$_"
                }
                elseif ($_.Name.ToLower().EndsWith(".cmd")) {
                    cmd.exe /C "$localFolder\$index\$_"
                }
            }
        }
        elseif ($taskUrl.ToLower().EndsWith(".ps1")) {
            powershell.exe $file
        }
        elseif ($taskUrl.ToLower().EndsWith(".cmd")) {
            cmd.exe /C $file
        }
    }
    catch {
        Write-Log "Error while processing task $i : " + $_.Exception.Message
    }
    cd $localFolder
}


# MAIN =========================================
if ($tasksUrl -eq "") {
    exit
}

if ($localFolder -eq "") {
    $localFolder = "$pwd\External"
}

# Clean the folder if exists
if (Test-Path $localFolder) {
    Remove-Item $localFolder\* -Recurse -Force
}

# Create folder if does not exist
New-Item -ItemType Directory -Force -Path $localFolder
cd $localFolder

# Split the URL in individual startup tasks
$tasks = $tasksUrl.Split(" ",[System.StringSplitOptions]::RemoveEmptyEntries)
$i = 0
foreach ($task in $tasks) {
    Process-ExternalTask $task.Trim() $localFolder $i
    $i += 1
}

Write-Log "External tasks execution finished"