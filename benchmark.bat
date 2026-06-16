@echo off
REM EMPYREAN - offline benchmark harness (Windows)
REM Creator: EZHD KING
REM The only sanctioned way to produce performance numbers (PROJECT.md 3.3 / 20.4).
setlocal
cd /d "%~dp0osu"
echo EMPYREAN benchmark harness - BenchmarkDotNet
dotnet run --project osu.Game.Benchmarks\osu.Game.Benchmarks.csproj -c Release -- %*
endlocal
