#!/usr/bin/env bash
# EMPYREAN — Linux build script (Ubuntu 24.04 target)
# Creator: EZHD KING
#
# Builds the desktop client in Release. Release is the only competitively meaningful
# configuration: Debug carries asserts, extra logging, and disabled inlining that
# distort every timing measurement (PROJECT.md §20, §22 Phase 6).
set -euo pipefail

CONFIG="${1:-Release}"

echo "EMPYREAN build (Linux) — config: ${CONFIG}"
cd "$(dirname "$0")/osu"

# Use the locally checked-out osu-framework if it is wired in (see UseLocalFramework.sh).
dotnet build osu.Desktop/osu.Desktop.csproj -c "${CONFIG}" -p:DebugType=none

echo "Build complete. Run with ./run_linux.sh"
