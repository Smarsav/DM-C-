$dotnet = "$env:LocalAppData\Microsoft\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    $dotnet = "dotnet"
}

Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host " Building OpenDream (.NET 9 SS13 C# Engine)" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan

Push-Location "OpenDream"
& $dotnet build OpenDream.sln -c Release
$code = $LASTEXITCODE
Pop-Location
exit $code
