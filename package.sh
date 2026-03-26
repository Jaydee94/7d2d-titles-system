#!/usr/bin/env bash
# =============================================================================
# package.sh — Build and package the TitlesSystem mod and client add-on
#
# Usage:
#   ./package.sh [--skip-tests] [--out-dir DIR] [--server-root PATH] [--game-root PATH]
#
# Options:
#   --skip-tests         Skip unit test run.
#   --out-dir DIR        Directory to write the zip files to (default: repo root).
#   --server-root PATH   Path to 7DTD dedicated server install (for server mod DLL).
#                        Auto-detected from common Linux/Windows locations.
#   --game-root PATH     Path to 7DTD game client install (for client mod DLL).
#                        Defaults to --server-root value if omitted, which also
#                        works when a full game installation provides both.
#
# Outputs:
#   TitlesSystem-v{VERSION}.zip          — server-side mod (install in server Mods/)
#   TitlesSystemClientMod-v{VERSION}.zip — client-side add-on (install in client Mods/)
#
# IMPORTANT:
#   Deployable artifacts must be built against real game DLLs so IModApi type
#   identity matches the server/client at runtime. CI stubs are only for tests.
# =============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MOD_DIR="$SCRIPT_DIR/TitlesSystem"
CLIENT_MOD_DIR="$SCRIPT_DIR/TitlesSystemClientMod"
TESTS_DIR="$SCRIPT_DIR/TitlesSystem.Tests"
SKIP_TESTS=false
OUT_DIR="$SCRIPT_DIR"
SERVER_ROOT_ARG=""
GAME_ROOT_ARG=""

while [[ $# -gt 0 ]]; do
    case "$1" in
        --skip-tests)  SKIP_TESTS=true; shift ;;
        --out-dir)     OUT_DIR="$2"; shift 2 ;;
        --server-root) SERVER_ROOT_ARG="$2"; shift 2 ;;
        --game-root)   GAME_ROOT_ARG="$2"; shift 2 ;;
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
    echo "ERROR: No 7DTD server DLLs found. Release packaging requires real game DLLs."
    echo "       Provide --server-root PATH or install the dedicated server locally."
    exit 1
fi

# Client mod game root: prefer explicit --game-root, fall back to server root
# (a full game client install also satisfies the server DLL check above).
CLIENT_GAME_ROOT="${GAME_ROOT_ARG:-$GAME_ROOT}"

MOD_VERSION=$(grep -oP '(?<=value=")[^"]+' "$MOD_DIR/ModInfo.xml" | sed -n '3p')
if [[ -z "$MOD_VERSION" ]]; then
    echo "ERROR: Could not read version from TitlesSystem/ModInfo.xml"
    exit 1
fi

echo "=== 7D2D Titles System — Package ==="
echo "  Version      : $MOD_VERSION"
echo "  Server mod   : $OUT_DIR/TitlesSystem-v${MOD_VERSION}.zip"
echo "  Client mod   : $OUT_DIR/TitlesSystemClientMod-v${MOD_VERSION}.zip"
echo "  Build mode   : Game DLLs"
echo "  Server root  : $GAME_ROOT"
echo "  Client root  : $CLIENT_GAME_ROOT"
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
"$DOTNET" clean \
    "$(to_build_path "$CLIENT_MOD_DIR/TitlesSystemClientMod.csproj")" \
    -c Release \
    -p:OutputPath=bin/Release \
    --nologo

echo "--- Building TitlesSystem (server mod) ---"
"$DOTNET" build \
    "$(to_build_path "$MOD_DIR/TitlesSystem.csproj")" \
    -c Release \
    -p:OutputPath=bin/Release \
    -p:GITHUB_ACTIONS=false \
    -p:RequireRealGameDlls=true \
    -p:GameRoot="$(to_build_path "$GAME_ROOT")" \
    -t:Rebuild \
    --nologo

echo "--- Building TitlesSystemClientMod (client add-on) ---"
"$DOTNET" build \
    "$(to_build_path "$CLIENT_MOD_DIR/TitlesSystemClientMod.csproj")" \
    -c Release \
    -p:OutputPath=bin/Release \
    -p:RequireRealGameDlls=true \
    -p:GameRoot="$(to_build_path "$CLIENT_GAME_ROOT")" \
    -t:Rebuild \
    --nologo

# --- Package server mod ---
STAGING_SERVER="$(mktemp -d)/TitlesSystem"
mkdir -p "$STAGING_SERVER/Config"
cp "$MOD_DIR/ModInfo.xml"                  "$STAGING_SERVER/"
cp "$MOD_DIR/Config/TitlesRanks.xml"       "$STAGING_SERVER/Config/"
cp "$MOD_DIR/bin/Release/TitlesSystem.dll" "$STAGING_SERVER/"

SERVER_ZIP="$OUT_DIR/TitlesSystem-v${MOD_VERSION}.zip"
mkdir -p "$OUT_DIR"
(cd "$(dirname "$STAGING_SERVER")" && zip -r "$SERVER_ZIP" TitlesSystem/)

# --- Package client mod ---
STAGING_CLIENT="$(mktemp -d)/TitlesSystemClientMod"
mkdir -p "$STAGING_CLIENT/Config/XUi"
cp "$CLIENT_MOD_DIR/ModInfo.xml"                                   "$STAGING_CLIENT/"
cp "$CLIENT_MOD_DIR/Config/XUi/windows.xml"                        "$STAGING_CLIENT/Config/XUi/"
cp "$CLIENT_MOD_DIR/bin/Release/TitlesSystemClientMod.dll"         "$STAGING_CLIENT/"

CLIENT_ZIP="$OUT_DIR/TitlesSystemClientMod-v${MOD_VERSION}.zip"
(cd "$(dirname "$STAGING_CLIENT")" && zip -r "$CLIENT_ZIP" TitlesSystemClientMod/)

echo "--- Packages created ---"
echo "  Server mod : $SERVER_ZIP"
echo "  Client mod : $CLIENT_ZIP"
echo ""
echo "Server mod: unzip TitlesSystem into server Mods/ and restart."
echo "Client mod: unzip TitlesSystemClientMod into client Mods/ and restart."
