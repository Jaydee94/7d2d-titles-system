#!/usr/bin/env bash
# =============================================================================
# package.sh — Build and package the TitlesSystem mod (mirrors CI / release.yml)
#
# Usage:
#   ./package.sh [--skip-tests] [--out-dir DIR]
#
# Options:
#   --skip-tests     Skip unit test run.
#   --out-dir DIR    Directory to write the zip to (default: repo root).
#
# Output:
#   TitlesSystem-v<VERSION>.zip  — drop-in Mods/ archive, same as GitHub release.
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
