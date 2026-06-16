@echo off
REM EMPYREAN - Windows build script
REM Creator: EZHD KING
REM Release is the only competitively meaningful configuration (PROJECT.md section 20).
setlocal
set CONFIG=%1
if "%CONFIG%"=="" set CONFIG=Release

echo EMPYREAN build (Windows) - config: %CONFIG%
cd /d "%~dp0osu"
dotnet build osu.Desktop\osu.Desktop.csproj -c %CONFIG% -p:DebugType=none
if errorlevel 1 exit /b 1
echo Build complete. Run with run_windows.bat
endlocal
