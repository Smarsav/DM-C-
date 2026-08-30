[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile "dotnet-install.ps1"
$installDir = "$env:LocalAppData\Microsoft\dotnet"
& ".\dotnet-install.ps1" -Channel "9.0" -InstallDir $installDir
[Environment]::SetEnvironmentVariable("PATH", "$installDir;" + [Environment]::GetEnvironmentVariable("PATH", "User"), "User")
$env:PATH = "$installDir;" + $env:PATH
& "$installDir\dotnet.exe" --version
