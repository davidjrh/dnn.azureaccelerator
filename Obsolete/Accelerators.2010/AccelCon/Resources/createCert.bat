"%ProgramFiles%\Microsoft SDKs\Windows\v7.0A\Bin\makecert.exe" -n "CN=Two Degrees,O=Slalom Consulting,OU=Custom Development,L=WindowsAzure,S=WA,C=US" -pe -ss Root -sr LocalMachine -sky exchange -m 96 -a sha1 -len 2048 -r

"%ProgramFiles%\Microsoft SDKs\Windows\v7.0A\Bin\makecert.exe" -n "CN=accelerator.cloudapp.net" -pe -ss My -sr CurrentUser -sky exchange -m 96 -in "Two Degrees" -is Root -ir CurrentUser -a sha1 -eku 1.3.6.1.5.5.7.3.1
