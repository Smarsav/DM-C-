# Build OpenDream with .NET 9 SDK
$dotnet = "$env:LocalAppData\Microsoft\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) { $dotnet = "dotnet" }

Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host " Building OpenDream (.NET 9 SS13 C# Engine)" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan

& $dotnet build -c Release "OpenDream\OpenDream.sln"
