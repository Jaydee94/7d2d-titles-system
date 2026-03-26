# =============================================================================
# deploy-windows.ps1 - Build and deploy the 7D2D Titles System mod on Windows
#
# Usage:
#   .\deploy-windows.ps1
#   .\deploy-windows.ps1 -ServerRoot "C:\7dtd-server"
#   .\deploy-windows.ps1 -ServerRoot "C:\7dtd-server" -GameRoot "C:\7dtd"
#   .\deploy-windows.ps1 -ServerRoot "C:\7dtd-server" -StartServer
#
# Parameters:
#   -ServerRoot   Path to the 7 Days to Die Dedicated Server installation.
#                 Auto-detected from common Steam locations if omitted.
#   -GameRoot     Path to the 7 Days to Die game client installation (used to
#                 build and deploy the optional TitlesSystemClientMod add-on).
#                 Auto-detected from common Steam client locations if omitted.
#                 If no game client is found, the client mod step is skipped.
#   -StartServer  If specified, the dedicated server is launched automatically
#                 after a successful deploy.
# =============================================================================

[CmdletBinding()]
param (
    [string] $ServerRoot = "",
    [string] $GameRoot   = "",
    [switch] $StartServer
)

$ErrorActionPreference = "Stop"

# --- Resolve paths ------------------------------------------------------------

$ScriptDir      = Split-Path -Parent $MyInvocation.MyCommand.Path
$ModDir         = Join-Path $ScriptDir "TitlesSystem"
$ClientModDir   = Join-Path $ScriptDir "TitlesSystemClientMod"

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

# Auto-detect GameRoot (game client) from common Steam installation paths
if (-not $GameRoot) {
    $ClientCandidates = @(
        "C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die",
        "C:\Program Files\Steam\steamapps\common\7 Days to Die",
        "D:\SteamLibrary\steamapps\common\7 Days to Die",
        "E:\SteamLibrary\steamapps\common\7 Days to Die"
    )
    foreach ($Candidate in $ClientCandidates) {
        if (Test-Path $Candidate) {
            $GameRoot = $Candidate
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
$ManagedDirServer = Join-Path $ServerRoot "7DaysToDieServer_Data\Managed"
$UseStubs   = $false

if (-not (Test-Path (Join-Path $ManagedDir "Assembly-CSharp.dll"))) {
    if (Test-Path (Join-Path $ManagedDirServer "Assembly-CSharp.dll")) {
        $ManagedDir = $ManagedDirServer
    } else {
        Write-Warning "Assembly-CSharp.dll not found in $ManagedDir or $ManagedDirServer - building with CI stubs instead."
        $UseStubs = $true
    }
}

# Determine whether the game client DLLs are available for the client mod.
# The client mod can fall back to CI stubs if the full game client is absent.
$ClientManagedDir    = if ($GameRoot) { Join-Path $GameRoot "7DaysToDie_Data\Managed" } else { $null }
$UseClientStubs      = $true
$DeployClientMod     = $false

if ($ClientManagedDir -and (Test-Path (Join-Path $ClientManagedDir "Assembly-CSharp.dll"))) {
    $UseClientStubs  = $false
    $DeployClientMod = $true
} elseif ($GameRoot) {
    # GameRoot was provided/detected but DLLs not found — still deploy, build with stubs
    Write-Warning "Assembly-CSharp.dll not found in $ClientManagedDir - building client mod with CI stubs."
    $DeployClientMod = $true
}

Write-Host ""
Write-Host "=== 7D2D Titles System - Windows Deploy ===" -ForegroundColor Cyan
Write-Host "  Mod directory        : $ModDir"
Write-Host "  Server root          : $ServerRoot"
Write-Host "  Deploy server mod to : $ServerRoot\Mods\TitlesSystem"
if ($UseStubs) {
    Write-Host "  Server build mode    : CI stubs (no game DLLs found)" -ForegroundColor Yellow
} else {
    Write-Host "  Server build mode    : Game DLLs"
}
if ($DeployClientMod) {
    Write-Host "  Client mod dir       : $ClientModDir"
    Write-Host "  Game root            : $GameRoot"
    Write-Host "  Deploy client mod to : $GameRoot\Mods\TitlesSystemClientMod"
    if ($UseClientStubs) {
        Write-Host "  Client build mode    : CI stubs (no game client DLLs found)" -ForegroundColor Yellow
    } else {
        Write-Host "  Client build mode    : Game DLLs"
    }
} else {
    Write-Host "  Client mod           : skipped (no game client found; use -GameRoot to enable)" -ForegroundColor Yellow
}
Write-Host ""

# --- Check for build tools ----------------------------------------------------

$DotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
$DotnetExe = if ($DotnetCmd) { $DotnetCmd.Source } else { $null }

if (-not $DotnetExe) {
    $DotnetCandidates = @(
        "$env:ProgramFiles\\dotnet\\dotnet.exe",
        "$env:ProgramFiles(x86)\\dotnet\\dotnet.exe"
    )

    foreach ($Candidate in $DotnetCandidates) {
        if ($Candidate -and (Test-Path $Candidate)) {
            $DotnetExe = $Candidate
            break
        }
    }
}

if (-not $DotnetExe) {
    Write-Error @"
ERROR: 'dotnet' not found in PATH.
Install the .NET SDK 8+ from https://dotnet.microsoft.com/download and retry.
Example (Windows): winget install Microsoft.DotNet.SDK.8
"@
    exit 1
}

# --- Build --------------------------------------------------------------------

# Build server mod
Push-Location $ModDir
try {
    $BuildSucceeded = $false

    if ($UseStubs) {
        $env:GITHUB_ACTIONS = "true"
        & $DotnetExe build TitlesSystem.csproj -c Release
        Remove-Item Env:GITHUB_ACTIONS -ErrorAction SilentlyContinue

        if ($LASTEXITCODE -eq 0) {
            $BuildSucceeded = $true
        }
    } else {
        & $DotnetExe build TitlesSystem.csproj -p:GameRoot="$ServerRoot" -c Release

        if ($LASTEXITCODE -eq 0) {
            $BuildSucceeded = $true
        } else {
            Write-Warning "Build against game DLLs failed. Retrying with CI stubs for compatibility..."
            $env:GITHUB_ACTIONS = "true"
            & $DotnetExe build TitlesSystem.csproj -c Release
            Remove-Item Env:GITHUB_ACTIONS -ErrorAction SilentlyContinue

            if ($LASTEXITCODE -eq 0) {
                $BuildSucceeded = $true
                $UseStubs = $true
                Write-Host "[TitlesSystem] Build succeeded using CI stubs fallback." -ForegroundColor Yellow
            }
        }
    }

    if (-not $BuildSucceeded) {
        Write-Error "Build failed - see output above."
        exit $LASTEXITCODE
    }
} finally {
    Pop-Location
}

# Build client mod (if game client was found or specified)
if ($DeployClientMod) {
    if (-not (Test-Path $ClientModDir)) {
        Write-Warning "TitlesSystemClientMod directory not found at $ClientModDir - skipping client mod build."
        $DeployClientMod = $false
    }
}

if ($DeployClientMod) {
    Push-Location $ClientModDir
    try {
        $ClientBuildSucceeded = $false

        if ($UseClientStubs) {
            $env:GITHUB_ACTIONS = "true"
            & $DotnetExe build TitlesSystemClientMod.csproj -c Release
            Remove-Item Env:GITHUB_ACTIONS -ErrorAction SilentlyContinue

            if ($LASTEXITCODE -eq 0) {
                $ClientBuildSucceeded = $true
            }
        } else {
            & $DotnetExe build TitlesSystemClientMod.csproj -p:GameRoot="$GameRoot" -c Release

            if ($LASTEXITCODE -eq 0) {
                $ClientBuildSucceeded = $true
            } else {
                Write-Warning "Client mod build against game DLLs failed. Retrying with CI stubs..."
                $env:GITHUB_ACTIONS = "true"
                & $DotnetExe build TitlesSystemClientMod.csproj -c Release
                Remove-Item Env:GITHUB_ACTIONS -ErrorAction SilentlyContinue

                if ($LASTEXITCODE -eq 0) {
                    $ClientBuildSucceeded = $true
                    $UseClientStubs = $true
                    Write-Host "[TitlesSystemClientMod] Build succeeded using CI stubs fallback." -ForegroundColor Yellow
                }
            }
        }

        if (-not $ClientBuildSucceeded) {
            Write-Warning "TitlesSystemClientMod build failed - client mod will not be deployed."
            $DeployClientMod = $false
        }
    } finally {
        Pop-Location
    }
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
    Write-Warning "DLL not found at $DllSource - skipping DLL copy."
}

Copy-Item (Join-Path $ModDir "ModInfo.xml")              -Destination $ModDest  -Force
Copy-Item (Join-Path $ModDir "Config\TitlesRanks.xml")   -Destination $ConfigDest -Force
Write-Host "[TitlesSystem] Deployed ModInfo.xml and Config\TitlesRanks.xml" -ForegroundColor Green

# Deploy client mod (if built successfully)
if ($DeployClientMod) {
    $ClientModDest       = Join-Path $GameRoot "Mods\TitlesSystemClientMod"
    $ClientConfigXUiDest = Join-Path $ClientModDest "Config\XUi"

    New-Item -ItemType Directory -Force -Path $ClientConfigXUiDest | Out-Null

    $ClientDllSource = Join-Path $ClientModDir "bin\Release\TitlesSystemClientMod.dll"
    if (Test-Path $ClientDllSource) {
        Copy-Item $ClientDllSource -Destination $ClientModDest -Force
        Write-Host "[TitlesSystemClientMod] Deployed TitlesSystemClientMod.dll" -ForegroundColor Green
    } else {
        Write-Warning "Client DLL not found at $ClientDllSource - skipping client DLL copy."
    }

    Copy-Item (Join-Path $ClientModDir "ModInfo.xml")              -Destination $ClientModDest     -Force
    Copy-Item (Join-Path $ClientModDir "Config\XUi\windows.xml")   -Destination $ClientConfigXUiDest -Force
    Write-Host "[TitlesSystemClientMod] Deployed ModInfo.xml and Config\XUi\windows.xml" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Deploy complete! ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Server mod installed to:"
Write-Host "  $ModDest"
if ($DeployClientMod) {
    Write-Host ""
    Write-Host "Client mod installed to:"
    Write-Host "  $(Join-Path $GameRoot "Mods\TitlesSystemClientMod")"
    Write-Host ""
    Write-Host "In-game, open the Rank Panel with the console command:"
    Write-Host '  rankpanel   (alias: rp)'
}
Write-Host ""
Write-Host "To test ranks, connect to the server console and run:"
Write-Host '  rank set YourSteamName 1000'
Write-Host ""

# --- Optionally start the server ----------------------------------------------

if ($StartServer) {
    $ServerLaunchers = @(
        (Join-Path $ServerRoot "StartDedicatedServer.bat"),
        (Join-Path $ServerRoot "startdedicated.bat"),
        (Join-Path $ServerRoot "7DaysToDieServer.exe")
    )

    $ServerLauncher = $ServerLaunchers | Where-Object { Test-Path $_ } | Select-Object -First 1

    if (-not $ServerLauncher) {
        Write-Warning "No dedicated server launcher found in $ServerRoot - skipping server start."
    } else {
        Write-Host "Starting 7DTD Dedicated Server..." -ForegroundColor Cyan
        Write-Host "  Launcher      : $ServerLauncher"

        $StartedProcess = $null
        if ($ServerLauncher.EndsWith(".bat", [System.StringComparison]::OrdinalIgnoreCase)) {
            # Use /k so the Windows console stays open if the batch exits early with an error.
            $StartedProcess = Start-Process -FilePath "cmd.exe" -ArgumentList @("/k", $ServerLauncher) -WorkingDirectory $ServerRoot -PassThru
        } else {
            $StartedProcess = Start-Process -FilePath $ServerLauncher -WorkingDirectory $ServerRoot -PassThru
        }

        if ($StartedProcess) {
            Write-Host "  Started PID   : $($StartedProcess.Id)"
        }
    }
}
