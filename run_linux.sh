#!/usr/bin/env bash
# EMPYREAN — Linux run script (Ubuntu 24.04 target)
# Creator: EZHD KING
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"

# `--benchmark` defers to the real offline harness (BenchmarkDotNet).
if [[ "${1:-}" == "--benchmark" ]]; then
    shift
    exec "${HERE}/benchmark.sh" "$@"
fi

cd "${HERE}/osu/osu.Desktop/bin/Release/net8.0"

# --- EMPYREAN low-latency renderer profile (reversible env hints) --------------------
# Vulkan: lowest CPU submission cost on modern NVIDIA RTX cards.
export OSU_GRAPHICS_RENDERER="${OSU_GRAPHICS_RENDERER:-vulkan}"
# Multithreaded: separate input/update/draw/audio threads = lowest input latency.
export OSU_EXECUTION_MODE="${OSU_EXECUTION_MODE:-MultiThreaded}"

exec dotnet "osu!.dll" "$@"
