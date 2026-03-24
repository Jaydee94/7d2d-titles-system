#!/usr/bin/env bash
# =============================================================================
# package.sh — Build and package the TitlesSystem mod
#
# Usage:
#   ./package.sh [--skip-tests] [--out-dir DIR] [--server-root PATH]
#
# Options:
#   --skip-tests         Skip unit test run.
#   --out-dir DIR        Directory to write the zip to (default: repo root).
#   --server-root PATH   Path to 7DTD dedicated server install.
#                        Auto-detected from common Linux/Windows locations.
#
# IMPORTANT:
#   Deployable artifacts must be built against real game DLLs so IModApi type
#   identity matches the server at runtime. CI stubs are only for tests.
# =============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MOD_DIR="$SCRIPT_DIR/TitlesSystem"
TESTS_DIR="$SCRIPT_DIR/TitlesSystem.Tests"
SKIP_TESTS=false
OUT_DIR="$SCRIPT_DIR"
SERVER_ROOT_ARG=""

while [[ $# -gt 0 ]]; do
    case "$1" in
        --skip-tests)  SKIP_TESTS=true; shift ;;
        --out-dir)     OUT_DIR="$2"; shift 2 ;;
        --server-root) SERVER_ROOT_ARG="$2"; shift 2 ;;
        *) echo "Unknown option: $1"; exit 1 ;;
    esac
done

DOTNET=""
WSL_MODE=false
if command -v dotnet &>/dev/null; then
    DOTNET="dotnet"
elif command -v dotnet.exe &>/dev/null; then
    DOTNET="dotnet.exe"
    WSL_MODE=true
else
    echo "ERROR: 'dotnet' not found in PATH."
    exit 1
fi

to_build_path() {
    if [[ "$WSL_MODE" == true ]]; then
        wslpath -w "$1"
    else
        echo "$1"
    fi
}

has_game_dlls() {
    local root="$1"
    [[ -f "$root/7DaysToDie_Data/Managed/Assembly-CSharp.dll" ]] \
    || [[ -f "$root/7DaysToDieServer_Data/Managed/Assembly-CSharp.dll" ]]
}

GAME_ROOT=""
BUILD_STUB=false

if [[ -n "$SERVER_ROOT_ARG" ]]; then
    GAME_ROOT="$SERVER_ROOT_ARG"
    if ! has_game_dlls "$GAME_ROOT"; then
        echo "ERROR: Assembly-CSharp.dll not found under '$GAME_ROOT'"
        exit 1
    fi
fi

if [[ -z "$GAME_ROOT" ]]; then
    LINUX_CANDIDATES=(
        "/home/steam/.steam/SteamApps/common/7 Days to Die Dedicated Server"
        "/opt/7dtd"
        "$HOME/.steam/steam/SteamApps/common/7 Days To Die Dedicated Server"
        "$HOME/.local/share/Steam/steamapps/common/7 Days to Die Dedicated Server"
    )
    for c in "${LINUX_CANDIDATES[@]}"; do
        if has_game_dlls "$c"; then
            GAME_ROOT="$c"
            break
        fi
    done
fi

if [[ -z "$GAME_ROOT" && "$WSL_MODE" == true ]]; then
    WIN_CANDIDATES=(
        "C:/Program Files (x86)/Steam/steamapps/common/7 Days to Die Dedicated Server"
        "C:/Program Files/Steam/steamapps/common/7 Days to Die Dedicated Server"
        "D:/SteamLibrary/steamapps/common/7 Days to Die Dedicated Server"
        "E:/SteamLibrary/steamapps/common/7 Days to Die Dedicated Server"
    )
    for c in "${WIN_CANDIDATES[@]}"; do
        WSL_CHECK=$(wslpath "$c" 2>/dev/null) || continue
        if has_game_dlls "$WSL_CHECK"; then
            GAME_ROOT="$WSL_CHECK"
            break
        fi
    done
fi

if [[ -z "$GAME_ROOT" ]]; then
    echo "WARNING: No 7DTD server DLLs found. Falling back to CI stubs."
    echo "         This build is for CI/test only and may not load on live servers."
    BUILD_STUB=true
fi

MOD_VERSION=$(grep -oP '(?<=value=")[^"]+' "$MOD_DIR/ModInfo.xml" | sed -n '3p')
if [[ -z "$MOD_VERSION" ]]; then
    echo "ERROR: Could not read version from TitlesSystem/ModInfo.xml"
    exit 1
fi

echo "=== 7D2D Titles System — Package ==="
echo "  Version     : $MOD_VERSION"
echo "  Output      : $OUT_DIR/TitlesSystem-v${MOD_VERSION}.zip"
if [[ "$BUILD_STUB" == true ]]; then
    echo "  Build mode  : CI stubs (non-deployable)"
else
    echo "  Build mode  : Game DLLs"
    echo "  Server root : $GAME_ROOT"
fi
echo ""

if [[ "$SKIP_TESTS" == false ]]; then
    echo "--- Running unit tests (CI stub mode) ---"
    "$DOTNET" test \
        "$(to_build_path "$TESTS_DIR/TitlesSystem.Tests.csproj")" \
        -p:GITHUB_ACTIONS=true \
        --verbosity normal \
        --nologo
    echo ""
fi

echo "--- Cleaning previous build output ---"
"$DOTNET" clean \
    "$(to_build_path "$MOD_DIR/TitlesSystem.csproj")" \
    -c Release \
    -p:OutputPath=bin/Release \
    --nologo

echo "--- Building TitlesSystem ---"
if [[ "$BUILD_STUB" == true ]]; then
    "$DOTNET" build \
        "$(to_build_path "$MOD_DIR/TitlesSystem.csproj")" \
        -c Release \
        -p:OutputPath=bin/Release \
        -p:GITHUB_ACTIONS=true \
        -t:Rebuild \
        --nologo
else
    "$DOTNET" build \
        "$(to_build_path "$MOD_DIR/TitlesSystem.csproj")" \
        -c Release \
        -p:OutputPath=bin/Release \
        -p:GITHUB_ACTIONS=false \
        -p:GameRoot="$(to_build_path "$GAME_ROOT")" \
        -t:Rebuild \
        --nologo
fi

STAGING="$(mktemp -d)/TitlesSystem"
mkdir -p "$STAGING/Config"
cp "$MOD_DIR/ModInfo.xml"                  "$STAGING/"
cp "$MOD_DIR/Config/TitlesRanks.xml"       "$STAGING/Config/"
cp "$MOD_DIR/bin/Release/TitlesSystem.dll" "$STAGING/"

ASSET="TitlesSystem-v${MOD_VERSION}.zip"
OUT_ZIP="$OUT_DIR/$ASSET"
mkdir -p "$OUT_DIR"
(cd "$(dirname "$STAGING")" && zip -r "$OUT_ZIP" TitlesSystem/)

echo "--- Package created ---"
echo "  $OUT_ZIP"
echo ""
echo "Install: unzip into server Mods/ and restart."
