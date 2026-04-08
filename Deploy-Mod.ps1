<#
.SYNOPSIS
    Runs local test+deploy flow for TitlesSystem on a Windows 7DTD dedicated server.

.DESCRIPTION
    Executes unit tests, deploys the mod via deploy-windows.ps1, and optionally
    starts or restarts the local dedicated server. Can also tail server logs.

.PARAMETER ServerPath
    Path to the 7 Days to Die dedicated server install.

.PARAMETER Launch
    Start the server after deployment.

.PARAMETER Restart
    Stop running 7DaysToDieServer.exe processes first, then start after deployment.

.PARAMETER SkipTests
    Skip unit tests.

.PARAMETER TailLog
    Follow latest server log and filter by pattern.

.PARAMETER TailLogSeconds
    Duration in seconds for log tailing.

.PARAMETER LogPattern
    Regex filter used while tailing logs.
#>

[CmdletBinding()]
param(
    [string]$ServerPath = "C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server",
    [switch]$Launch,
    [switch]$Restart,
    [switch]$SkipTests,
    [switch]$TailLog,
    [ValidateRange(5, 600)]
    [int]$TailLogSeconds = 30,
    [string]$LogPattern = 'TitlesSystem|\[TitlesSystem\]'
)

function Test-IsWslPowerShell {
    return $PSVersionTable.PSEdition -eq 'Core' -and
           [System.Environment]::GetEnvironmentVariable('WSL_DISTRO_NAME')
}

function Convert-ToWindowsPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ($Path -match '^[A-Za-z]:\\') {
        return $Path
    }

    $wslPath = Get-Command wslpath -ErrorAction SilentlyContinue
    if (-not $wslPath) {
        throw 'wslpath was not found. Install WSL path tools or pass Windows-style paths.'
    }

    return (& $wslPath.Source '-w' $Path).Trim()
}

function Invoke-WindowsSelf {
    $windowsPowerShell = Get-Command powershell.exe -ErrorAction SilentlyContinue
    if (-not $windowsPowerShell) {
        throw 'powershell.exe was not found in WSL. Run this script from Windows PowerShell or install PowerShell bridging in WSL.'
    }

    $windowsScriptPath = Convert-ToWindowsPath -Path $PSCommandPath
    $relayArgs = @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $windowsScriptPath,
        '-ServerPath', (Convert-ToWindowsPath -Path $ServerPath),
        '-TailLogSeconds', $TailLogSeconds,
        '-LogPattern', $LogPattern
    )

    if ($Launch) {
        $relayArgs += '-Launch'
    }
    if ($Restart) {
        $relayArgs += '-Restart'
    }
    if ($SkipTests) {
        $relayArgs += '-SkipTests'
    }
    if ($TailLog) {
        $relayArgs += '-TailLog'
    }

    & $windowsPowerShell.Source @relayArgs
    exit $LASTEXITCODE
}

if (Test-IsWslPowerShell) {
    Invoke-WindowsSelf
}

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step  { param([string]$Msg) Write-Host "`n==> $Msg" -ForegroundColor Cyan }
function Write-Ok    { param([string]$Msg) Write-Host "    [OK]  $Msg" -ForegroundColor Green }
function Write-Fail  { param([string]$Msg) Write-Host "    [ERR] $Msg" -ForegroundColor Red }
function Write-Warn  { param([string]$Msg) Write-Host "    [WARN] $Msg" -ForegroundColor Yellow }

function Resolve-ServerLauncher {
    param([Parameter(Mandatory = $true)][string]$ServerPath)

    $launchers = @(
        (Join-Path $ServerPath 'StartDedicatedServer.bat'),
        (Join-Path $ServerPath 'startdedicated.bat'),
        (Join-Path $ServerPath '7DaysToDieServer.exe')
    )

    return $launchers | Where-Object { Test-Path $_ } | Select-Object -First 1
}

function Stop-DedicatedServer {
    $processes = @(Get-Process -Name '7DaysToDieServer' -ErrorAction SilentlyContinue)
    if ($processes.Count -eq 0) {
        Write-Ok 'No running dedicated server process found.'
        return
    }

    foreach ($process in $processes) {
        Write-Host "    Stopping PID $($process.Id): $($process.Path)"
        Stop-Process -Id $process.Id -Force
    }

    Start-Sleep -Seconds 2
    Write-Ok 'Dedicated server stopped.'
}

function Get-ServerLogFiles {
    param(
        [Parameter(Mandatory = $true)][string]$ServerPath,
        [datetime]$NotBefore = [datetime]::MinValue
    )

    $files = New-Object System.Collections.Generic.List[object]

    $candidateDirectories = @(
        $ServerPath,
        (Join-Path $ServerPath '7DaysToDieServer_Data'),
        (Join-Path $env:APPDATA '7DaysToDie\logs')
    )

    foreach ($directory in $candidateDirectories) {
        if (-not [string]::IsNullOrWhiteSpace($directory) -and (Test-Path $directory)) {
            Get-ChildItem -Path $directory -File -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -match 'output_log|Player|server|log|dedi' } |
                ForEach-Object {
                    $priority = 4
                    if ($_.Name -match '^output_log_dedi') {
                        $priority = 0
                    } elseif ($_.DirectoryName -eq $ServerPath) {
                        $priority = 1
                    } elseif ($_.DirectoryName -like '*7DaysToDieServer_Data*') {
                        $priority = 2
                    } elseif ($_.Name -match 'dedi|dedicated|server') {
                        $priority = 3
                    }

                    $isFresh = $_.LastWriteTime -ge $NotBefore
                    [void]$files.Add([pscustomobject]@{
                        FileInfo = $_
                        Priority = $priority
                        IsFresh  = $isFresh
                    })
                }
        }
    }

    $freshFiles = $files | Where-Object { $_.IsFresh }
    if ($freshFiles) {
        return $freshFiles |
            Sort-Object -Property Priority, @{ Expression = { $_.FileInfo.LastWriteTime }; Descending = $true }, @{ Expression = { $_.FileInfo.FullName }; Descending = $false } |
            ForEach-Object { $_.FileInfo }
    }

    return $files |
        Sort-Object -Property Priority, @{ Expression = { $_.FileInfo.LastWriteTime }; Descending = $true }, @{ Expression = { $_.FileInfo.FullName }; Descending = $false } |
        ForEach-Object { $_.FileInfo }
}

function Get-LatestServerLogFile {
    param(
        [Parameter(Mandatory = $true)][string]$ServerPath,
        [datetime]$NotBefore = [datetime]::MinValue
    )

    return Get-ServerLogFiles -ServerPath $ServerPath -NotBefore $NotBefore | Select-Object -First 1
}

function Follow-ServerLog {
    param(
        [Parameter(Mandatory = $true)][string]$ServerPath,
        [Parameter(Mandatory = $true)][string]$Pattern,
        [Parameter(Mandatory = $true)][int]$DurationSeconds,
        [datetime]$NotBefore = [datetime]::MinValue
    )

    $logFile = Get-LatestServerLogFile -ServerPath $ServerPath -NotBefore $NotBefore
    if (-not $logFile) {
        Write-Warn 'No server log file was found to follow.'
        return
    }

    Write-Step 'Following server log'
    Write-Host "    File: $($logFile.FullName)"
    Write-Host "    Filter: $Pattern"
    Write-Host "    Duration: $DurationSeconds seconds"

    foreach ($line in Get-Content -Path $logFile.FullName -Tail 20 -ErrorAction SilentlyContinue) {
        if ($line -match $Pattern) {
            Write-Host $line
        }
    }

    $deadline = (Get-Date).AddSeconds($DurationSeconds)
    $stream = [System.IO.File]::Open($logFile.FullName, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)

    try {
        $reader = New-Object System.IO.StreamReader($stream)
        [void]$stream.Seek(0, [System.IO.SeekOrigin]::End)

        while ((Get-Date) -lt $deadline) {
            while (-not $reader.EndOfStream) {
                $line = $reader.ReadLine()
                if ($line -match $Pattern) {
                    Write-Host $line
                }
            }

            Start-Sleep -Milliseconds 500
        }
    }
    finally {
        $reader.Dispose()
        $stream.Dispose()
    }

    Write-Ok "Stopped log tail after $DurationSeconds seconds."
}

if ($Restart) {
    $Launch = $true
}

$ServerLaunchTime = [datetime]::MinValue
$RepoRoot = $PSScriptRoot
$DeployScript = Join-Path $RepoRoot 'deploy-windows.ps1'
$TestProject = Join-Path $RepoRoot 'TitlesSystem.Tests\TitlesSystem.Tests.csproj'

Write-Step 'Checking prerequisites'

if (-not (Test-Path $DeployScript)) {
    Write-Fail "Expected script not found: $DeployScript"
    exit 1
}

if (-not (Test-Path $ServerPath)) {
    Write-Fail "Dedicated server path not found: $ServerPath"
    exit 1
}
Write-Ok "Server path: $ServerPath"

$ServerLauncher = Resolve-ServerLauncher -ServerPath $ServerPath
if (($Launch -or $Restart) -and -not $ServerLauncher) {
    Write-Fail "No dedicated server launcher found in: $ServerPath"
    exit 1
}

if (-not $SkipTests) {
    Write-Step 'Running unit tests'

    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnet) {
        Write-Fail 'dotnet not found in PATH. Install .NET SDK 8+ to run tests.'
        exit 1
    }

    & $dotnet.Source test $TestProject --verbosity normal --nologo
    if ($LASTEXITCODE -ne 0) {
        Write-Fail 'Unit tests failed.'
        exit $LASTEXITCODE
    }

    Write-Ok 'Unit tests passed.'
}

if ($Restart) {
    Write-Step 'Restarting dedicated server'
    Stop-DedicatedServer
}

Write-Step 'Deploying TitlesSystem mod'
$deployArgs = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $DeployScript, '-ServerRoot', $ServerPath)
if ($Launch) {
    $deployArgs += '-StartServer'
    $ServerLaunchTime = Get-Date
}

& powershell.exe @deployArgs
if ($LASTEXITCODE -ne 0) {
    Write-Fail 'Deployment failed.'
    exit $LASTEXITCODE
}
Write-Ok 'Deployment completed.'

if ($TailLog) {
    Follow-ServerLog -ServerPath $ServerPath -Pattern $LogPattern -DurationSeconds $TailLogSeconds -NotBefore $ServerLaunchTime
}

Write-Host "`nDone." -ForegroundColor Green
