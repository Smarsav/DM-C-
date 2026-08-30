# Build binary first
cmd /c build.bat

Add-Type -AssemblyName System.IO.Compression.FileSystem

$releaseDir = "release"
if (Test-Path $releaseDir) {
    Remove-Item -Recurse -Force $releaseDir
}
New-Item -ItemType Directory -Path $releaseDir | Out-Null

Copy-Item "bin\DMToCSharp.exe" -Destination $releaseDir
Copy-Item "LICENSE" -Destination $releaseDir
Copy-Item "README.md" -Destination $releaseDir

$zipPath = "DMToCSharp-v1.0.0-win-x64.zip"
if (Test-Path $zipPath) {
    Remove-Item -Force $zipPath
}

[System.IO.Compression.ZipFile]::CreateFromDirectory((Resolve-Path $releaseDir).Path, (Join-Path (Get-Location) $zipPath))

Write-Host "======================================================================" -ForegroundColor Green
Write-Host " Release v1.0.0 created successfully!" -ForegroundColor Green
Write-Host " Archive: $zipPath" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Green

Get-Item $zipPath | Select-Object Name, Length, LastWriteTime
