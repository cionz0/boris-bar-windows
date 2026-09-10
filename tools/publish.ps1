#Requires -Version 5.1
<#
.SYNOPSIS
    Publish self-contained Boris Bar builds (and optionally an Inno Setup installer).

.EXAMPLE
    .\tools\publish.ps1
    .\tools\publish.ps1 -All -Installer
    .\tools\publish.ps1 -Runtime win-x64 -Version 0.2.2
#>
[CmdletBinding()]
param(
    [string[]]$Runtime,
    [switch]$All,
    [switch]$Installer,
    [string]$Version = "0.2.2",
    [string]$OutputRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputRoot) {
    $OutputRoot = Join-Path $repoRoot "publish"
}

if ($All) {
    $Runtime = @("win-x64", "win-arm64")
}
elseif (-not $Runtime) {
    if ($env:PROCESSOR_ARCHITECTURE -eq "ARM64") {
        $Runtime = @("win-arm64")
    }
    else {
        $Runtime = @("win-x64")
    }
}

function Find-InnoCompiler {
    $named = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
    if ($named) {
        return $named.Source
    }

    $paths = @(
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe")
    )
    foreach ($path in $paths) {
        if ($path -and (Test-Path -LiteralPath $path)) {
            return $path
        }
    }
    return $null
}

function Test-HasAudioFiles {
    param([string]$Directory)

    if (-not (Test-Path -LiteralPath $Directory)) {
        return $false
    }

    $extensions = @(".mp3", ".mp4", ".m4a", ".wav", ".aiff", ".aif", ".caf", ".ogg")
    $hit = Get-ChildItem -LiteralPath $Directory -File -ErrorAction SilentlyContinue |
        Where-Object { $extensions -contains $_.Extension.ToLowerInvariant() } |
        Select-Object -First 1
    return $null -ne $hit
}

function Copy-ClipsToPublishDir {
    param(
        [string]$SourceDir,
        [string]$PublishDir
    )

    $dest = Join-Path $PublishDir "assets\clips"
    New-Item -ItemType Directory -Path $dest -Force | Out-Null
    $extensions = @(".mp3", ".mp4", ".m4a", ".wav", ".aiff", ".aif", ".caf", ".ogg")
    Get-ChildItem -LiteralPath $SourceDir -File -ErrorAction SilentlyContinue |
        Where-Object { $extensions -contains $_.Extension.ToLowerInvariant() } |
        ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $dest $_.Name) -Force
        }
}

New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

$clipsStaging = Join-Path $repoRoot "assets\clips"
if (-not (Test-HasAudioFiles $clipsStaging)) {
    Write-Host "Packing built-in clips from the original macOS DMG (build time only; not a runtime download)"
    & (Join-Path $PSScriptRoot "import-audio-from-dmg.ps1") -TargetDirectory $clipsStaging
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to fetch built-in clips for the installer."
    }
}

if (-not (Test-HasAudioFiles $clipsStaging)) {
    Write-Error "No built-in audio clips found to include in the installer."
}

foreach ($rid in $Runtime) {
    $outDir = Join-Path $OutputRoot $rid
    if (Test-Path -LiteralPath $outDir) {
        Remove-Item -LiteralPath $outDir -Recurse -Force
    }

    Write-Host "Publishing $rid"
    & dotnet publish (Join-Path $repoRoot "src\BorisBar\BorisBar.csproj") `
        -c Release `
        -r $rid `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -p:Version=$Version `
        -o $outDir
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet publish failed for $rid (exit code $LASTEXITCODE)."
    }

    Copy-ClipsToPublishDir -SourceDir $clipsStaging -PublishDir $outDir

    $zipPath = Join-Path $OutputRoot "BorisBar-$Version-$rid.zip"
    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }
    Compress-Archive -Path (Join-Path $outDir "*") -DestinationPath $zipPath
    Write-Host "Wrote $zipPath"
}

if ($Installer) {
    $iscc = Find-InnoCompiler
    if (-not $iscc) {
        Write-Error "Inno Setup 6 is required for -Installer. Install from https://jrsoftware.org/isinfo.php"
    }

    foreach ($rid in $Runtime) {
        Write-Host "Building installer for $rid"
        & $iscc `
            /Q `
            "/DMyAppVersion=$Version" `
            "/DRid=$rid" `
            (Join-Path $PSScriptRoot "installer.iss")
        if ($LASTEXITCODE -ne 0) {
            Write-Error "ISCC failed for $rid (exit code $LASTEXITCODE)."
        }
    }
}
