@echo off
cd /d "%~dp0"
echo ======================================================================
echo  Launching OpenDream (.NET 9 C# Space Station 13 Engine)
echo ======================================================================

:: Clean up any old instances
taskkill /f /im OpenDreamClient.exe >nul 2>&1
taskkill /f /im OpenDreamServer.exe >nul 2>&1
for /f "tokens=5" %%a in ('netstat -aon ^| find ":25566" ^| find "LISTENING"') do taskkill /f /pid %%a >nul 2>&1

set COMPILER="%~dp0OpenDream\DMCompiler\bin\Release\net9.0\DMCompiler.exe"
set SERVER="%~dp0OpenDream\OpenDreamServer\bin\Release\net9.0\OpenDreamServer.exe"
set CLIENT="%~dp0OpenDream\OpenDreamClient\bin\Release\net9.0-windows\OpenDreamClient.exe"

echo [1/3] Compiling station environment...
%COMPILER% "%~dp0OpenDream\TestGame\environment.dme"

echo [2/3] Starting OpenDream C# Server (port 25566)...
start "OpenDream Server" %SERVER% "%~dp0OpenDream\TestGame\environment.json"

ping -n 3 127.0.0.1 >nul

echo [3/3] Launching OpenDream C# Client Window...
start "" %CLIENT%

echo ======================================================================
echo  OpenDream SS13 is active! (Connect to 127.0.0.1:25566)
echo ======================================================================
