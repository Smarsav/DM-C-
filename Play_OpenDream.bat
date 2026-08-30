@echo off
cd /d "%~dp0"
echo ======================================================================
echo  Launching OpenDream (.NET 9 C# Space Station 13 Engine)
echo ======================================================================

set COMPILER=OpenDream\DMCompiler\bin\Release\net5.0\DMCompiler.exe
set SERVER=OpenDream\OpenDreamServer\bin\Release\net5.0\OpenDreamServer.exe
set CLIENT=OpenDream\OpenDreamClient\bin\Release\net5.0-windows\OpenDreamClient.exe

echo [1/3] Compiling test environment...
"%COMPILER%" "OpenDream\TestGame\environment.dme"

echo [2/3] Starting OpenDream C# Server (port 25566)...
start "OpenDream Game Server" "%SERVER%" "OpenDream\TestGame\environment.json"

ping -n 3 127.0.0.1 >nul

echo [3/3] Launching OpenDream C# Client Window...
start "OpenDream Client" "%CLIENT%"

echo ======================================================================
echo  OpenDream SS13 is running!
echo ======================================================================
