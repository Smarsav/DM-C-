# Automated OpenDream Full Integration Test
$dotnet = "$env:LocalAppData\Microsoft\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) { $dotnet = "dotnet" }

Write-Host "=== 1. Starting OpenDream Server ===" -ForegroundColor Cyan
taskkill /f /im OpenDream* 2>$null
for ($port = 25566; $port -le 25566; $port++) {
    $p = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
    if ($p) { Stop-Process -Id $p.OwningProcess -Force -ErrorAction SilentlyContinue }
}

$serverProcess = Start-Process -FilePath "$PSScriptRoot\..\OpenDream\OpenDreamServer\bin\Release\net9.0\OpenDreamServer.exe" -ArgumentList "$PSScriptRoot\..\OpenDream\TestGame\environment.json" -PassThru -NoNewWindow
Start-Sleep -Seconds 2

if ($serverProcess.HasExited) {
    Write-Error "OpenDreamServer failed to stay alive!"
    exit 1
}

Write-Host "OpenDreamServer is RUNNING (PID: $($serverProcess.Id))" -ForegroundColor Green

Write-Host "=== 2. Starting OpenDream Client ===" -ForegroundColor Cyan
$clientProcess = Start-Process -FilePath "$PSScriptRoot\..\OpenDream\OpenDreamClient\bin\Release\net9.0-windows\OpenDreamClient.exe" -PassThru

Start-Sleep -Seconds 3

if ($clientProcess.HasExited) {
    Write-Error "OpenDreamClient crashed on startup!"
    Stop-Process -Id $serverProcess.Id -Force
    exit 1
}

Write-Host "OpenDreamClient is RUNNING (PID: $($clientProcess.Id))" -ForegroundColor Green
Write-Host "=== Full Integration Test PASSED ===" -ForegroundColor Green
