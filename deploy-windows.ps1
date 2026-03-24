# =============================================================================
# deploy-windows.ps1 — Build and deploy the 7D2D Titles System mod on Windows
#
# Usage:
#   .\deploy-windows.ps1
#   .\deploy-windows.ps1 -ServerRoot "C:\7dtd"
#   .\deploy-windows.ps1 -ServerRoot "C:\7dtd" -StartServer
#
# Parameters:
#   -ServerRoot   Path to the 7 Days to Die Dedicated Server installation.
#                 Auto-detected from common Steam locations if omitted.
#   -StartServer  If specified, the dedicated server is launched automatically
#                 after a successful deploy.
# =============================================================================

[CmdletBinding()]
param (
    [string] $ServerRoot = "",
    [switch] $StartServer
)

$ErrorActionPreference = "Stop"

# --- Resolve paths ------------------------------------------------------------

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ModDir    = Join-Path $ScriptDir "TitlesSystem"

# Auto-detect ServerRoot from common Steam installation paths
if (-not $ServerRoot) {
    $Candidates = @(
        "C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server",
        "C:\Program Files\Steam\steamapps\common\7 Days to Die Dedicated Server",
        "D:\SteamLibrary\steamapps\common\7 Days to Die Dedicated Server",
        "E:\SteamLibrary\steamapps\common\7 Days to Die Dedicated Server"
    )
    foreach ($Candidate in $Candidates) {
        if (Test-Path $Candidate) {
            $ServerRoot = $Candidate
            break
        }
    }
}

# --- Validate -----------------------------------------------------------------

if (-not $ServerRoot) {
    Write-Error @"
ERROR: Could not find the 7 Days to Die Dedicated Server installation.
Please provide the path with -ServerRoot:
  .\deploy-windows.ps1 -ServerRoot "C:\path\to\7dtd"
"@
    exit 1
}

$ManagedDir = Join-Path $ServerRoot "7DaysToDie_Data\Managed"
$UseStubs   = $false

if (-not (Test-Path (Join-Path $ManagedDir "Assembly-CSharp.dll"))) {
    Write-Warning "Assembly-CSharp.dll not found in $ManagedDir — building with CI stubs instead."
    $UseStubs = $true
}

Write-Host ""
Write-Host "=== 7D2D Titles System — Windows Deploy ===" -ForegroundColor Cyan
Write-Host "  Mod directory : $ModDir"
Write-Host "  Server root   : $ServerRoot"
Write-Host "  Deploy to     : $ServerRoot\Mods\TitlesSystem"
if ($UseStubs) {
    Write-Host "  Build mode    : CI stubs (no game DLLs found)" -ForegroundColor Yellow
} else {
    Write-Host "  Build mode    : Game DLLs"
}
Write-Host ""

# --- Check for build tools ----------------------------------------------------

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error @"
ERROR: 'dotnet' not found in PATH.
Install the .NET SDK 8+ from https://dotnet.microsoft.com/download and retry.
"@
    exit 1
}

# --- Build --------------------------------------------------------------------

Push-Location $ModDir
try {
    if ($UseStubs) {
        $env:GITHUB_ACTIONS = "true"
        & dotnet build TitlesSystem.csproj -c Release
        Remove-Item Env:GITHUB_ACTIONS -ErrorAction SilentlyContinue
    } else {
        & dotnet build TitlesSystem.csproj -p:GameRoot="$ServerRoot" -c Release
    }

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed — see output above."
        exit $LASTEXITCODE
    }
} finally {
    Pop-Location
}

# --- Deploy -------------------------------------------------------------------

$ModDest   = Join-Path $ServerRoot "Mods\TitlesSystem"
$ConfigDest = Join-Path $ModDest "Config"

New-Item -ItemType Directory -Force -Path $ConfigDest | Out-Null

$DllSource = Join-Path $ModDir "bin\Release\TitlesSystem.dll"
if (Test-Path $DllSource) {
    Copy-Item $DllSource -Destination $ModDest -Force
    Write-Host "[TitlesSystem] Deployed TitlesSystem.dll" -ForegroundColor Green
} else {
    Write-Warning "DLL not found at $DllSource — skipping DLL copy."
}

Copy-Item (Join-Path $ModDir "ModInfo.xml")              -Destination $ModDest  -Force
Copy-Item (Join-Path $ModDir "Config\TitlesRanks.xml")   -Destination $ConfigDest -Force
Write-Host "[TitlesSystem] Deployed ModInfo.xml and Config\TitlesRanks.xml" -ForegroundColor Green

Write-Host ""
Write-Host "=== Deploy complete! ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Mod installed to:"
Write-Host "  $ModDest"
Write-Host ""
Write-Host "To test ranks, connect to the server console and run:"
Write-Host '  rank set YourSteamName 1000'
Write-Host ""

# --- Optionally start the server ----------------------------------------------

if ($StartServer) {
    $ServerBat = Join-Path $ServerRoot "StartDedicatedServer.bat"
    if (-not (Test-Path $ServerBat)) {
        Write-Warning "StartDedicatedServer.bat not found at $ServerBat — skipping server start."
    } else {
        Write-Host "Starting 7DTD Dedicated Server..." -ForegroundColor Cyan
        Start-Process -FilePath "cmd.exe" -ArgumentList "/c `"$ServerBat`"" -WorkingDirectory $ServerRoot
    }
}
