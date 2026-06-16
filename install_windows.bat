@echo off
REM EMPYREAN - Windows install script
REM Creator: EZHD KING
setlocal
where dotnet >nul 2>nul
if errorlevel 1 (
    echo ERROR: .NET 8 SDK not found. Install from https://dotnet.microsoft.com/download/dotnet/8.0
    exit /b 1
)
echo EMPYREAN install - building Release...
call "%~dp0build_windows.bat" Release
if errorlevel 1 exit /b 1
echo Done. Launch with run_windows.bat
endlocal
