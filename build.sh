#!/usr/bin/env bash
# =============================================================================
# build.sh — Build script for the 7D2D Titles System mod
#
# Usage:
#   ./build.sh [GAME_ROOT] [MODS_DIR]
#
# Arguments (both optional):
#   GAME_ROOT  — Path to 7 Days to Die installation. Auto-detected if omitted.
#   MODS_DIR   — Path to the game's Mods/ folder. If set, the compiled DLL is
#                copied there so the server picks it up on next start.
#
# Examples:
#   ./build.sh
#   ./build.sh /opt/7dtd
#   ./build.sh /opt/7dtd /opt/7dtd/Mods
#   GAME_ROOT=/opt/7dtd MODS_DIR=/opt/7dtd/Mods ./build.sh
# =============================================================================

set -euo pipefail

# --- Resolve paths -----------------------------------------------------------

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MOD_DIR="$SCRIPT_DIR/TitlesSystem"

GAME_ROOT="${1:-${GAME_ROOT:-}}"
MODS_DIR="${2:-${MODS_DIR:-}}"

# Auto-detect GAME_ROOT if not provided
if [[ -z "$GAME_ROOT" ]]; then
    CANDIDATES=(
        "/home/steam/.steam/SteamApps/common/7 Days to Die Dedicated Server"
        "/opt/7dtd"
        "$HOME/.steam/steam/SteamApps/common/7 Days To Die Dedicated Server"
        "$HOME/.local/share/Steam/steamapps/common/7 Days to Die Dedicated Server"
    )
    for candidate in "${CANDIDATES[@]}"; do
        if [[ -d "$candidate" ]]; then
            GAME_ROOT="$candidate"
            break
        fi
    done
fi

# --- Validate ----------------------------------------------------------------

if [[ -z "$GAME_ROOT" ]]; then
    echo "ERROR: Could not find 7 Days to Die installation."
    echo "Please provide the path as the first argument or set GAME_ROOT."
    echo "  e.g.: ./build.sh /path/to/7dtd"
    exit 1
fi

MANAGED_DIR="$GAME_ROOT/7DaysToDie_Data/Managed"
if [[ ! -f "$MANAGED_DIR/Assembly-CSharp.dll" ]]; then
    echo "ERROR: Assembly-CSharp.dll not found in $MANAGED_DIR"
    echo "Make sure GAME_ROOT points to the 7 Days to Die installation directory."
    exit 1
fi

echo "=== 7D2D Titles System Build ==="
echo "  Mod directory : $MOD_DIR"
echo "  Game root     : $GAME_ROOT"
[[ -n "$MODS_DIR" ]] && echo "  Deploy to     : $MODS_DIR/TitlesSystem"
echo ""

# --- Check for build tools ---------------------------------------------------

if command -v dotnet &>/dev/null; then
    BUILD_CMD="dotnet build"
    BUILD_TOOL="dotnet"
elif command -v msbuild &>/dev/null; then
    BUILD_CMD="msbuild /p:Configuration=Release"
    BUILD_TOOL="msbuild"
else
    echo "ERROR: Neither 'dotnet' nor 'msbuild' found in PATH."
    echo "Install the .NET SDK (https://dotnet.microsoft.com/download) and retry."
    exit 1
fi

echo "  Build tool    : $BUILD_TOOL"
echo ""

# --- Build -------------------------------------------------------------------

cd "$MOD_DIR"

BUILD_ARGS=""
[[ -n "$GAME_ROOT" ]] && BUILD_ARGS="$BUILD_ARGS -p:GameRoot=$GAME_ROOT"
[[ -n "$MODS_DIR"  ]] && BUILD_ARGS="$BUILD_ARGS -p:MODS_DIR=$MODS_DIR"

$BUILD_CMD $BUILD_ARGS

# --- Deploy (if MODS_DIR provided) -------------------------------------------

if [[ -n "$MODS_DIR" ]]; then
    TARGET_DIR="$MODS_DIR/TitlesSystem"
    mkdir -p "$TARGET_DIR"

    # Copy compiled DLL
    DLL_PATH="$MOD_DIR/bin/Release/TitlesSystem.dll"
    if [[ -f "$DLL_PATH" ]]; then
        cp "$DLL_PATH" "$TARGET_DIR/"
        echo "[TitlesSystem] Deployed DLL to $TARGET_DIR"
    else
        echo "WARNING: DLL not found at $DLL_PATH — skipping DLL copy."
    fi

    # Copy mod metadata and config
    cp "$MOD_DIR/ModInfo.xml" "$TARGET_DIR/"
    cp -r "$MOD_DIR/Config" "$TARGET_DIR/"
    echo "[TitlesSystem] Deployed ModInfo.xml and Config/ to $TARGET_DIR"
fi

echo ""
echo "=== Build complete! ==="
echo ""
echo "To install manually, copy the following to your server's Mods/ folder:"
echo "  $MOD_DIR/bin/Release/TitlesSystem.dll"
echo "  $MOD_DIR/ModInfo.xml"
echo "  $MOD_DIR/Config/"
