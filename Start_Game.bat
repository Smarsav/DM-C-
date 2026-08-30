@echo off
title Space Station 13 Master Launcher
cls
echo ======================================================================
echo          SPACE STATION 13 & OPENDREAM MASTER LAUNCHER
echo ======================================================================
echo.
echo  [1] Connect to Full 1:1 PsychonautStation Server (Port 1337)
echo  [2] Rebuild & Launch Full PsychonautStation Server
echo  [3] Play OpenDream C# Engine (Port 25566)
echo  [4] Run DMToCSharp Native Desktop App (60 FPS)
echo  [5] Run Full Automated Test Suite (18/18 Unit Tests)
echo  [6] Exit
echo.
echo ======================================================================
set /p choice="Please select an option (1-6): "

if "%choice%"=="1" (
    echo.
    echo Connecting to Full PsychonautStation Server...
    start byond://127.0.0.1:1337
    exit /b 0
)

if "%choice%"=="2" (
    echo.
    echo Launching PsychonautStation Server...
    call "%~dp0\psychonaut_station\RUN_SERVER.cmd"
    exit /b 0
)

if "%choice%"=="3" (
    echo.
    echo Launching OpenDream C# Engine...
    call "%~dp0\Play_OpenDream.bat"
    exit /b 0
)

if "%choice%"=="4" (
    echo.
    echo Launching Native C# Desktop SS13 App...
    call "%~dp0\Play_SS13.bat"
    exit /b 0
)

if "%choice%"=="5" (
    echo.
    echo Running automated test suite...
    bin\DMToCSharp.exe test
    pause
    exit /b 0
)

if "%choice%"=="6" (
    exit /b 0
)

echo Invalid selection.
pause
