@echo off
REM EMPYREAN - Windows run script
REM Creator: EZHD KING
REM Launches with the EMPYREAN low-latency renderer profile (reversible env hints).
setlocal
cd /d "%~dp0osu\osu.Desktop\bin\Release\net8.0"

REM Vulkan is fastest on modern NVIDIA RTX cards; framework falls back if unavailable.
if "%OSU_GRAPHICS_RENDERER%"=="" set OSU_GRAPHICS_RENDERER=vulkan
if "%OSU_EXECUTION_MODE%"=="" set OSU_EXECUTION_MODE=MultiThreaded

if "%1"=="--benchmark" (
    "osu!.exe" --benchmark
    goto :eof
)
"osu!.exe" %*
endlocal
