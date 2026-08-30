# OpenDream SS13 Launcher (.NET 9 C#)
$dotnet = "$env:LocalAppData\Microsoft\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) { $dotnet = "dotnet" }

Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host " Launching OpenDream (.NET 9 C# Space Station 13 Engine)" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan

Write-Host "[1/3] Compiling environment bytecode with DMCompiler..." -ForegroundColor Yellow
& $dotnet exec --roll-forward LatestMajor "OpenDream\DMCompiler\bin\Release\net9.0\DMCompiler.dll" "OpenDream\TestGame\environment.dme"

Write-Host "[2/3] Starting OpenDream Server on port 25566..." -ForegroundColor Yellow
Start-Process -FilePath $dotnet -ArgumentList "exec --roll-forward LatestMajor OpenDream\OpenDreamServer\bin\Release\net9.0\OpenDreamServer.dll OpenDream\TestGame\environment.json"

Start-Sleep -Seconds 2

Write-Host "[3/3] Launching OpenDream Client window..." -ForegroundColor Green
Start-Process -FilePath $dotnet -ArgumentList "exec --roll-forward LatestMajor OpenDream\OpenDreamClient\bin\Release\net9.0-windows\OpenDreamClient.dll"

Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host " OpenDream SS13 is now active! Connect to 127.0.0.1:25566" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan
