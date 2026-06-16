#!/usr/bin/env bash
# EMPYREAN — Linux install script (Ubuntu 24.04 target)
# Creator: EZHD KING
#
# Installs the .NET 8 SDK prerequisite check, builds, and drops a launcher into
# ~/.local/bin. Other distros are intentionally unsupported by this script — forks
# are welcome to add them (PROJECT.md §8).
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "ERROR: .NET 8 SDK not found."
    echo "On Ubuntu 24.04:  sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0"
    exit 1
fi

echo "EMPYREAN install — building Release..."
"${HERE}/build_linux.sh" Release

mkdir -p "${HOME}/.local/bin"
LAUNCHER="${HOME}/.local/bin/empyrean"
cat > "${LAUNCHER}" << LAUNCH
#!/usr/bin/env bash
exec "${HERE}/run_linux.sh" "\$@"
LAUNCH
chmod +x "${LAUNCHER}"

echo "Installed launcher: ${LAUNCHER}"
echo "Ensure ~/.local/bin is on your PATH, then run:  empyrean"
