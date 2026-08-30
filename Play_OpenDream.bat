@echo off
cd /d "%~dp0"
echo ======================================================================
echo  Launching OpenDream (.NET 9 C# Space Station 13 Engine)
echo ======================================================================

set DOTNET=%LocalAppData%\Microsoft\dotnet\dotnet.exe
if not exist "%DOTNET%" set DOTNET=dotnet

set COMPILER_DLL=%~dp0OpenDream\DMCompiler\bin\Release\net5.0\DMCompiler.dll
set SERVER_DLL=%~dp0OpenDream\OpenDreamServer\bin\Release\net5.0\OpenDreamServer.dll
set CLIENT_DLL=%~dp0OpenDream\OpenDreamClient\bin\Release\net5.0-windows\OpenDreamClient.dll

echo [1/3] Compiling station environment...
"%DOTNET%" exec --roll-forward LatestMajor "%COMPILER_DLL%" "%~dp0OpenDream\TestGame\environment.dme"

echo [2/3] Starting OpenDream C# Server (port 25566)...
start "OpenDream Server" /b "%DOTNET%" exec --roll-forward LatestMajor "%SERVER_DLL%" "%~dp0OpenDream\TestGame\environment.json"

ping -n 3 127.0.0.1 >nul

echo [3/3] Launching OpenDream C# Client Window...
start "" "%DOTNET%" exec --roll-forward LatestMajor "%CLIENT_DLL%"

echo ======================================================================
echo  OpenDream SS13 is active! (Connect to 127.0.0.1:25566)
echo ======================================================================
