#!/usr/bin/env bash
# =============================================================================
# package.sh — Build and package the TitlesSystem mod (mirrors CI / release.yml)
#
# Usage:
#   ./package.sh [--skip-tests] [--out-dir DIR] [--server-root PATH]
#
# Options:
#   --skip-tests         Skip unit test run.
#   --out-dir DIR        Directory to write the zip to (default: repo root).
#   --server-root PATH   Path to the 7DTD dedicated server installation.
#                        Auto-detected from common paths if omitted.
#
# Why real game DLLs are required for the deployable artifact:
#   The mod's entry point (TitlesSystemMod) implements the game's IModApi
#   interface.  If the DLL is compiled against stub types, the CLR considers
#   the stub IModApi and the server's IModApi to be different types.  The
#   server's mod loader then fails to find any IModApi implementation and
#   logs "No ModAPI found in mod DLLs".
#   Building against the real managed DLLs (Assembly-CSharp.dll etc.) ensures
#   IModApi type identity matches at runtime.  The managed DLLs are
#   cross-platform .NET assemblies, so Windows server DLLs work fine to
#   produce a DLL that runs on a Linux server.
#
# Output:
#   TitlesSystem-v<VERSION>.zip  — drop-in Mods/ archive.
#
# The zip contains:
#   TitlesSystem/
#   ├── ModInfo.xml
#   ├── TitlesSystem.dll
#   └── Config/
#       └── TitlesRanks.xml
# =============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MOD_DIR="$SCRIPT_DIR/TitlesSystem"
TESTS_DIR="$SCRIPT_DIR/TitlesSystem.Tests"
SKIP_TESTS=false
OUT_DIR="$SCRIPT_DIR"
SERVER_ROOT_ARG=""

# --- Parse args --------------------------------------------------------------

while [[ $# -gt 0 ]]; do
    case "$1" in
        --skip-tests)   SKIP_TESTS=true; shift ;;
        --out-dir)      OUT_DIR="$2"; shift 2 ;;
        --server-root)  SERVER_ROOT_ARG="$2"; shift 2 ;;
        *) echo "Unknown option: $1"; exit 1 ;;
    esac
done

# --- Locate dotnet -----------------------------------------------------------
# Prefer native Linux dotnet; fall back to Windows dotnet.exe via WSL interop.

DOTNET=""
WSL_MODE=false
if command -v dotnet &>/dev/null; then
    DOTNET="dotnet"
elif command -v dotnet.exe &>/dev/null; then
    DOTNET="dotnet.exe"
    WSL_MODE=true
else
    echo "ERROR: 'dotnet' not found in PATH."
    echo "Install the .NET SDK 8+ from https://dotnet.microsoft.com/download"
    exit 1
fi

# Helper: convert a Linux path to a Windows/build path when using dotnet.exe
to_build_path() {
    if [[ "$WSL_MODE" == true ]]; then
        wslpath -w "$1"
    else
        echo "$1"
    fi
}

# Helper: check if a game root dir contains Assembly-CSharp.dll
has_game_dlls() {
    local root="$1"
    [[ -f "$root/7DaysToDie_Data/Managed/Assembly-CSharp.dll" ]] \
    || [[ -f "$root/7DaysToDieServer_Data/Managed/Assembly-CSharp.dll" ]]
}

# --- Find game DLLs ----------------------------------------------------------
# The deployable DLL must be compiled against the real game assemblies so that
# IModApi type identity matches at runtime on the server.
#
# Search order:
#   1. --server-root argument
#   2. Common Linux dedicated server paths
#   3. Common Windows Steam paths (via WSL wslpath)

GAME_ROOT=""
BUILD_STUB=false

if [[ -n "$SERVER_ROOT_ARG" ]]; then
    # User supplied it explicitly — trust them.
    GAME_ROOT="$SERVER_ROOT_ARG"
    if ! has_game_dlls "$GAME_ROOT"; then
        echo "ERROR: Assembly-CSharp.dll not found under '$GAME_ROOT'"
        echo "Check --server-root points to the 7DTD dedicated server installation."
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

if [[ -z "$GAME_ROOT" ]] && [[ "$WSL_MODE" == true ]]; then
    WIN_CANDIDATES=(
        "C:/Program Files (x86)/Steam/steamapps/common/7 Days to Die Dedicated Server"
        "C:/Program Files/Steam/steamapps/common/7 Days to Die Dedicated Server"
        "D:/SteamLibrary/steamapps/common/7 Days to Die Dedicated Server"
        "E:/SteamLibrary/steamapps/common/7 Days to Die Dedicated Server"
    )
    for c in "${WIN_CANDIDATES[@]}"; do
        WSL_CHECK=$(wslpath "$c" 2>/dev/null) || continue
        if has_game_dlls "$WSL_CHECK"; then
            # Store as Linux/WSL path — to_build_path() will convert to Windows
            # format when passing to dotnet.exe.
            GAME_ROOT="$WSL_CHECK"
            break
        fi
    done
fi

if [[ -z "$GAME_ROOT" ]]; then
    echo "WARNING: No 7DTD server installation found."
    echo "         Building with CI stubs — the resulting DLL will NOT load on"
    echo "         a real server (IModApi type mismatch)."
    echo ""
    echo "         To produce a working deployable DLL, either:"
    echo "           ./package.sh --server-root /path/to/7dtd"
    echo "         or install the 7DTD Dedicated Server via Steam."
    echo ""
    BUILD_STUB=true
fi

# --- Read version from ModInfo.xml -------------------------------------------

MOD_VERSION=$(grep -oP '(?<=value=")[^"]+' "$MOD_DIR/ModInfo.xml" | sed -n '3p')
if [[ -z "$MOD_VERSION" ]]; then
    echo "ERROR: Could not read version from TitlesSystem/ModInfo.xml"
    exit 1
fi

echo "=== 7D2D Titles System — Package ==="
echo "  Version     : $MOD_VERSION"
echo "  Mod dir     : $MOD_DIR"
echo "  Output      : $OUT_DIR/TitlesSystem-v${MOD_VERSION}.zip"
if [[ "$BUILD_STUB" == true ]]; then
    echo "  Build mode  : CI stubs (WARNING: not for deployment)"
else
    echo "  Build mode  : Game DLLs"
    echo "  Server root : $GAME_ROOT"
fi
echo ""

# --- Unit tests (always use CI stub mode — tests only cover pure logic) ------

if [[ "$SKIP_TESTS" == false ]]; then
    echo "--- Running unit tests (CI stub mode) ---"
    "$DOTNET" test \
        "$(to_build_path "$TESTS_DIR/TitlesSystem.Tests.csproj")" \
        --verbosity normal \
        --nologo
    echo ""
fi

# --- Build deployable DLL ----------------------------------------------------

echo "--- Building TitlesSystem ---"
if [[ "$BUILD_STUB" == true ]]; then
    "$DOTNET" build \
        "$(to_build_path "$MOD_DIR/TitlesSystem.csproj")" \
        -c Release \
        -p:OutputPath=bin/Release \
        -p:GITHUB_ACTIONS=true \
        --nologo
else
    "$DOTNET" build \
        "$(to_build_path "$MOD_DIR/TitlesSystem.csproj")" \
        -c Release \
        -p:OutputPath=bin/Release \
        -p:GameRoot="$(to_build_path "$GAME_ROOT")" \
        --nologo
fi
echo ""

# --- Stage and zip -----------------------------------------------------------

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
echo "Install: unzip into your server's Mods/ directory and restart the server."


set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MOD_DIR="$SCRIPT_DIR/TitlesSystem"
TESTS_DIR="$SCRIPT_DIR/TitlesSystem.Tests"
SKIP_TESTS=false
OUT_DIR="$SCRIPT_DIR"

# --- Parse args --------------------------------------------------------------

while [[ $# -gt 0 ]]; do
    case "$1" in
        --skip-tests) SKIP_TESTS=true; shift ;;
        --out-dir)    OUT_DIR="$2"; shift 2 ;;
        *) echo "Unknown option: $1"; exit 1 ;;
    esac
done

# --- Locate dotnet -----------------------------------------------------------
# Prefer native Linux dotnet; fall back to Windows dotnet.exe via WSL interop.

DOTNET=""
WSL_MODE=false
if command -v dotnet &>/dev/null; then
    DOTNET="dotnet"
elif command -v dotnet.exe &>/dev/null; then
    # Running inside WSL — use the Windows SDK via interop.
    # All paths passed to dotnet.exe must be Windows-format paths.
    DOTNET="dotnet.exe"
    WSL_MODE=true
else
    echo "ERROR: 'dotnet' not found in PATH."
    echo "Install the .NET SDK 8+ from https://dotnet.microsoft.com/download"
    exit 1
fi

# Helper: convert a Linux path to Windows path when needed
to_build_path() {
    if [[ "$WSL_MODE" == true ]]; then
        wslpath -w "$1"
    else
        echo "$1"
    fi
}

# --- Read version from ModInfo.xml -------------------------------------------
# Mirrors release.yml: grep the 3rd value="..." attribute (Version field)

MOD_VERSION=$(grep -oP '(?<=value=")[^"]+' "$MOD_DIR/ModInfo.xml" | sed -n '3p')
if [[ -z "$MOD_VERSION" ]]; then
    echo "ERROR: Could not read version from TitlesSystem/ModInfo.xml"
    exit 1
fi

echo "=== 7D2D Titles System — Package ==="
echo "  Version   : $MOD_VERSION"
echo "  Mod dir   : $MOD_DIR"
echo "  Output    : $OUT_DIR/TitlesSystem-v${MOD_VERSION}.zip"
echo ""

# --- Build (CI stub mode) ----------------------------------------------------
# GITHUB_ACTIONS=true switches TitlesSystem.csproj to use the CI stub project
# instead of local game DLL references — same flag the CI runner sets automatically.

echo "--- Building TitlesSystem (CI stub mode) ---"
GITHUB_ACTIONS=true "$DOTNET" build \
    "$(to_build_path "$MOD_DIR/TitlesSystem.csproj")" \
    -c Release \
    -p:OutputPath=bin/Release \
    -p:GITHUB_ACTIONS=true \
    --nologo

echo ""

# --- Run unit tests ----------------------------------------------------------

if [[ "$SKIP_TESTS" == false ]]; then
    echo "--- Running unit tests ---"
    "$DOTNET" test \
        "$(to_build_path "$TESTS_DIR/TitlesSystem.Tests.csproj")" \
        --verbosity normal \
        --nologo
    echo ""
fi

# --- Stage files -------------------------------------------------------------

STAGING="$(mktemp -d)/TitlesSystem"
mkdir -p "$STAGING/Config"

cp "$MOD_DIR/ModInfo.xml"                  "$STAGING/"
cp "$MOD_DIR/Config/TitlesRanks.xml"       "$STAGING/Config/"
cp "$MOD_DIR/bin/Release/TitlesSystem.dll" "$STAGING/"

# --- Create zip --------------------------------------------------------------

ASSET="TitlesSystem-v${MOD_VERSION}.zip"
OUT_ZIP="$OUT_DIR/$ASSET"

mkdir -p "$OUT_DIR"
(cd "$(dirname "$STAGING")" && zip -r "$OUT_ZIP" TitlesSystem/)

echo "--- Package created ---"
echo "  $OUT_ZIP"
echo ""
echo "Install: unzip into your server's Mods/ directory and restart the server."
