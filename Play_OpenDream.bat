@echo off
cd /d "%~dp0"
echo ======================================================================
echo  Launching OpenDream (.NET 9 C# Space Station 13 Engine)
echo ======================================================================

set DOTNET=%LocalAppData%\Microsoft\dotnet\dotnet.exe
if not exist "%DOTNET%" set DOTNET=dotnet

echo [1/3] Compiling environment bytecode...
"%DOTNET%" exec --roll-forward LatestMajor "OpenDream\DMCompiler\bin\Release\net5.0\DMCompiler.dll" "OpenDream\TestGame\environment.dme"

echo [2/3] Starting OpenDream C# Game Server on port 25566...
start "OpenDream Game Server" "%DOTNET%" exec --roll-forward LatestMajor "OpenDream\OpenDreamServer\bin\Release\net5.0\OpenDreamServer.dll" "OpenDream\TestGame\environment.json"

ping -n 3 127.0.0.1 >nul

echo [3/3] Launching OpenDream C# Game Client...
start "OpenDream Client" "%DOTNET%" exec --roll-forward LatestMajor "OpenDream\OpenDreamClient\bin\Release\net5.0-windows\OpenDreamClient.dll"

echo ======================================================================
echo  OpenDream SS13 is running! (Server + Client)
echo ======================================================================
