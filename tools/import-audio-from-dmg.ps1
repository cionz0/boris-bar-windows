#Requires -Version 5.1
<#
.SYNOPSIS
    Import built-in Boris Bar audio clips from the original macOS DMG.

.DESCRIPTION
    Downloads the original Boris Bar macOS release DMG, extracts bundled audio
    clips, and installs them into:

      %LOCALAPPDATA%\boris-bar\builtin

    Override the source DMG by passing a different URL as the first argument.

    Requirements:
      - curl.exe (bundled with Windows 10 1803+)
      - 7-Zip (7z.exe)
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$DmgUrl = "https://github.com/andrearicciotti1/boris-bar/releases/download/v1.0/BorisBar-1.0.dmg"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($DmgUrl -eq "-h" -or $DmgUrl -eq "--help") {
    Write-Host @"
Usage: tools/import-audio-from-dmg.ps1 [DMG_URL]

Downloads the original Boris Bar macOS release DMG, extracts bundled audio
clips, and installs them into:

  %LOCALAPPDATA%\boris-bar\builtin

Override the source DMG by passing a different URL as the first argument.

Requirements:
  - curl.exe
  - 7-Zip (7z.exe)
"@
    exit 0
}

function Find-SevenZip {
    $named = Get-Command "7z.exe" -ErrorAction SilentlyContinue
    if ($named) {
        return $named.Source
    }

    $named = Get-Command "7zz.exe" -ErrorAction SilentlyContinue
    if ($named) {
        return $named.Source
    }

    $paths = @(
        (Join-Path $env:ProgramFiles "7-Zip\7z.exe")
    )
    if (${env:ProgramFiles(x86)}) {
        $paths += (Join-Path ${env:ProgramFiles(x86)} "7-Zip\7z.exe")
    }

    foreach ($path in $paths) {
        if (Test-Path -LiteralPath $path) {
            return $path
        }
    }

    return $null
}

function Expand-SevenZipArchive {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Archive,
        [Parameter(Mandatory = $true)]
        [string]$OutDir
    )

    New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

    # macOS DMGs contain an Applications symlink. Creating it on Windows
    # requires SeCreateSymbolicLinkPrivilege and 7-Zip then exits 2.
    & $sevenZip x -y "-o$OutDir" "-snl-" "-xr!Applications" $Archive 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 7) {
        & $sevenZip x -y "-o$OutDir" "-xr!Applications" $Archive 2>&1 | Out-Null
    }
}

$curl = Get-Command "curl.exe" -ErrorAction SilentlyContinue
if (-not $curl) {
    Write-Error "curl.exe is required (included with Windows 10 1803+)."
}

$sevenZip = Find-SevenZip
if (-not $sevenZip) {
    Write-Error "Install 7-Zip (https://www.7-zip.org/) so 7z.exe can extract the DMG."
}

$targetDir = Join-Path $env:LOCALAPPDATA "boris-bar\builtin"
$workDir = Join-Path ([System.IO.Path]::GetTempPath()) ("boris-bar-import-" + [guid]::NewGuid().ToString("N"))
$dmgPath = Join-Path $workDir "BorisBar.dmg"
$extractDir = Join-Path $workDir "extracted"

try {
    New-Item -ItemType Directory -Path $extractDir -Force | Out-Null

    Write-Host "Downloading $DmgUrl"
    & curl.exe -L --fail --output $dmgPath $DmgUrl
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Download failed with exit code $LASTEXITCODE"
    }

    Write-Host "Extracting DMG"
    Expand-SevenZipArchive -Archive $dmgPath -OutDir $extractDir

    $innerImages = @(Get-ChildItem -LiteralPath $extractDir -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Extension -match '^\.(hfs|img|iso)$' })
    $innerIndex = 0
    foreach ($image in $innerImages) {
        $innerIndex++
        $innerOut = Join-Path $extractDir ("volume-" + $innerIndex)
        Write-Host "Extracting inner volume $($image.Name)"
        Expand-SevenZipArchive -Archive $image.FullName -OutDir $innerOut
    }

    $extensions = @(".mp3", ".mp4", ".m4a", ".wav", ".aiff", ".aif", ".caf", ".ogg")
    $files = @(Get-ChildItem -LiteralPath $extractDir -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $extensions -contains $_.Extension.ToLowerInvariant() })

    if ($files.Count -eq 0) {
        Write-Error "No audio clips were found in the extracted DMG."
    }

    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    $copied = 0
    foreach ($file in $files) {
        Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $targetDir $file.Name) -Force
        $copied++
    }

    Write-Host "Imported $copied audio clips into $targetDir"
}
finally {
    if (Test-Path -LiteralPath $workDir) {
        Remove-Item -LiteralPath $workDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
