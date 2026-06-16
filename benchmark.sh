#!/usr/bin/env bash
# EMPYREAN — offline benchmark harness (Linux)
# Creator: EZHD KING
#
# Runs the BenchmarkDotNet suite in osu.Game.Benchmarks. This is the ONLY sanctioned way
# to produce performance numbers for EMPYREAN. Per PROJECT.md §3.3 and §20.4, no
# performance claim is valid unless it comes from a measured, reproducible run like this.
#
# Usage:
#   ./benchmark.sh                 # interactive: pick a benchmark
#   ./benchmark.sh --filter '*HitObject*'   # run a subset
#
# Record results in docs/BENCHMARKS.md using the template there: state what changed,
# why, the expected gain, and the MEASURED gain — including when the gain was negligible.
set -euo pipefail
cd "$(dirname "$0")/osu"
echo "EMPYREAN benchmark harness — BenchmarkDotNet"
echo "results are written under BenchmarkDotNet.Artifacts/"
dotnet run --project osu.Game.Benchmarks/osu.Game.Benchmarks.csproj -c Release -- "$@"
