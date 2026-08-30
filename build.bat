@echo off
setlocal
echo ======================================================================
echo  Building DMToCSharp (Bidirectional DreamMaker ^<^-^> C# Compiler)
echo ======================================================================

if not exist bin mkdir bin

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe

"%CSC%" /nologo /target:exe /out:bin\DMToCSharp.exe /recurse:src\*.cs

if %ERRORLEVEL% equ 0 (
    echo [Build Success] Output: bin\DMToCSharp.exe
) else (
    echo [Build Failed]
)
exit /b %ERRORLEVEL%
