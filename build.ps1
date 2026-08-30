# DMToCSharp Build Script
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host " Building DMToCSharp (Bidirectional DreamMaker <-> C# Compiler)" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan

if (-not (Test-Path "bin")) {
    New-Item -ItemType Directory -Path "bin" | Out-Null
}

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    $csc = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}

$sources = Get-ChildItem -Path "src" -Recurse -Filter "*.cs" | ForEach-Object { $_.FullName }

& $csc /nologo /target:exe /out:bin\DMToCSharp.exe $sources

if ($LASTEXITCODE -eq 0) {
    Write-Host "[Build Success] Output: bin\DMToCSharp.exe" -ForegroundColor Green
} else {
    Write-Host "[Build Failed] Exit Code: $LASTEXITCODE" -ForegroundColor Red
}
exit $LASTEXITCODE
