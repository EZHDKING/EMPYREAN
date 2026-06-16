#!/usr/bin/env bash
# EMPYREAN — build a single-file Windows .exe and a Linux .AppImage from one Ubuntu 24.04 host.
# Creator: EZHD KING
#
# Output (in dist/):
#   Empyrean.exe        - self-contained single-file Windows build (cross-compiled, no Wine needed)
#   Empyrean.AppImage   - self-contained Linux build, one portable executable file
#
# .NET 8 cross-compiles to Windows from Linux: -r selects the target and `dotnet publish` produces a
# self-contained build. The AppImage is assembled from a Linux publish + appimagetool. Release is the
# only competitively meaningful configuration (PROJECT.md §20).
#
# Usage:
#   ./build_all.sh                  # build both
#   ./build_all.sh Release windows  # build only one: linux | windows | both (default)
#
# Requirements: .NET 8 SDK. For the AppImage: FUSE (or the script falls back to extract-and-run);
# appimagetool is downloaded automatically to dist/tools if not already present.

set -euo pipefail

CONFIG="${1:-Release}"
TARGET="${2:-both}"

HERE="$(cd "$(dirname "$0")" && pwd)"
CSPROJ="${HERE}/osu/osu.Desktop/osu.Desktop.csproj"
DIST="${HERE}/dist"
TOOLS="${DIST}/tools"
ICON_SRC="${HERE}/osu/osu.Desktop/lazer.ico"

mkdir -p "${DIST}" "${TOOLS}"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "ERROR: the .NET 8 SDK ('dotnet') is not on PATH. Install it first." >&2
    exit 1
fi

echo "=================================================================="
echo " EMPYREAN single-file build   (config: ${CONFIG}, target: ${TARGET})"
echo "=================================================================="

# ---------------------------------------------------------------------------
# Windows: one self-contained Empyrean.exe
# ---------------------------------------------------------------------------
build_windows() {
    local work="${DIST}/_win_tmp"
    echo
    echo "--- Windows -> ${DIST}/Empyrean.exe ----------------------------"
    rm -rf "${work}"

    # PublishSingleFile bundles managed + native into one .exe. IncludeNativeLibraries... pulls the
    # native deps into the bundle. ReadyToRun precompiles for faster startup.
    dotnet publish "${CSPROJ}" \
        -c "${CONFIG}" \
        -r win-x64 \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -p:DebugType=none \
        -p:PublishReadyToRun=true \
        -o "${work}"

    # The assembly name is "osu!" so the produced exe is "osu!.exe"; rename to Empyrean.exe.
    if [[ -f "${work}/osu!.exe" ]]; then
        mv -f "${work}/osu!.exe" "${DIST}/Empyrean.exe"
    else
        # Fallback: grab whatever single .exe was produced.
        local exe
        exe="$(find "${work}" -maxdepth 1 -name '*.exe' | head -1)"
        [[ -n "${exe}" ]] && mv -f "${exe}" "${DIST}/Empyrean.exe"
    fi

    # Any ruleset DLLs that are loaded dynamically and not bundled must sit beside the exe. With
    # single-file they are normally embedded; if your build externalises them, copy them here.
    rm -rf "${work}"
    echo "Done: ${DIST}/Empyrean.exe"
    echo "NOTE: osu! loads rulesets/native libs at runtime. If Empyrean.exe fails to start, the"
    echo "      single-file bundle may have excluded a ruleset DLL — test and, if needed, switch to"
    echo "      the folder publish (PublishSingleFile=false) and ship the folder instead."
}

# ---------------------------------------------------------------------------
# Linux: one self-contained Empyrean.AppImage
# ---------------------------------------------------------------------------
build_linux_appimage() {
    local appdir="${DIST}/Empyrean.AppDir"
    echo
    echo "--- Linux -> ${DIST}/Empyrean.AppImage -------------------------"
    rm -rf "${appdir}"
    mkdir -p "${appdir}/usr/bin"

    # Publish the self-contained Linux build into the AppDir.
    dotnet publish "${CSPROJ}" \
        -c "${CONFIG}" \
        -r linux-x64 \
        --self-contained true \
        -p:DebugType=none \
        -p:PublishReadyToRun=true \
        -o "${appdir}/usr/bin"

    # AppRun: the entry point AppImage executes. It sets EMPYREAN's renderer/threading profile and
    # launches the published binary (assembly name "osu!" -> "osu!" launcher in the publish dir).
    cat > "${appdir}/AppRun" << 'APPRUN'
#!/usr/bin/env bash
HERE="$(dirname "$(readlink -f "${0}")")"
export OSU_GRAPHICS_RENDERER="${OSU_GRAPHICS_RENDERER:-vulkan}"
export OSU_EXECUTION_MODE="${OSU_EXECUTION_MODE:-MultiThreaded}"
cd "${HERE}/usr/bin"
# The published apphost is named after the assembly ("osu!"). Launch it; fall back to dotnet.
if [[ -x "./osu!" ]]; then
    exec "./osu!" "$@"
else
    exec dotnet "./osu!.dll" "$@"
fi
APPRUN
    chmod +x "${appdir}/AppRun"

    # Desktop entry (required by AppImage). Categories/name are cosmetic.
    cat > "${appdir}/empyrean.desktop" << 'DESKTOP'
[Desktop Entry]
Type=Application
Name=EMPYREAN
Comment=osu!lazer, but better
Exec=AppRun
Icon=empyrean
Categories=Game;
Terminal=false
DESKTOP

    # Icon: AppImage wants a PNG named to match Icon=. Convert the .ico if possible, else make a
    # 1x1 placeholder so packaging still succeeds (replace with a real 256x256 PNG for a nice icon).
    if command -v convert >/dev/null 2>&1 && [[ -f "${ICON_SRC}" ]]; then
        convert "${ICON_SRC}[0]" -resize 256x256 "${appdir}/empyrean.png" 2>/dev/null || true
    fi
    if [[ ! -f "${appdir}/empyrean.png" ]]; then
        # Minimal valid 1x1 PNG (base64) as a last-resort placeholder.
        base64 -d > "${appdir}/empyrean.png" << 'PNG'
iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M8AAAMBAQDJ/1eRAAAAAElFTkSuQmCC
PNG
    fi

    # Fetch appimagetool if we don't have it (GitHub is reachable).
    local tool="${TOOLS}/appimagetool-x86_64.AppImage"
    if [[ ! -x "${tool}" ]]; then
        echo "Downloading appimagetool..."
        curl -fL -o "${tool}" \
            "https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage"
        chmod +x "${tool}"
    fi

    # Package. ARCH is required by appimagetool. Try direct run; if FUSE is unavailable, use the
    # built-in extract-and-run fallback so this works in containers/CI too.
    rm -f "${DIST}/Empyrean.AppImage"
    if ! ARCH=x86_64 "${tool}" "${appdir}" "${DIST}/Empyrean.AppImage" 2>/dev/null; then
        echo "Direct appimagetool run failed (likely no FUSE); retrying with extract-and-run..."
        ARCH=x86_64 "${tool}" --appimage-extract-and-run "${appdir}" "${DIST}/Empyrean.AppImage"
    fi

    chmod +x "${DIST}/Empyrean.AppImage"
    rm -rf "${appdir}"
    echo "Done: ${DIST}/Empyrean.AppImage"
}

case "${TARGET}" in
    linux)            build_linux_appimage ;;
    windows|win)      build_windows ;;
    both|"")          build_windows; build_linux_appimage ;;
    *) echo "ERROR: unknown target '${TARGET}'. Use: linux | windows | both" >&2; exit 1 ;;
esac

echo
echo "=================================================================="
echo " Build complete:"
[[ -f "${DIST}/Empyrean.exe" ]]      && echo "   ${DIST}/Empyrean.exe"
[[ -f "${DIST}/Empyrean.AppImage" ]] && echo "   ${DIST}/Empyrean.AppImage"
echo "=================================================================="
