# Boris Bar (Windows)

> **NON-COMMERCIAL FAN PROJECT**  
> This is a free, amateur, open-source, non-profit fan project inspired by the Italian TV series *Boris*. It is not affiliated with, sponsored by, approved by, or connected to RAI, Wildside, Sky, Mediaset, Disney+, or the authors, directors, cast, or rights holders of the series.  
> This repository **does not distribute audio files**. It only provides a tray player and global hotkeys. Obtaining clips and checking that local use is lawful is **the user's sole responsibility**.  
> **No revenue, donations, advertising, or monetization** is associated with this project.

Windows 10/11 tray app at [cionz0/boris-bar-windows](https://github.com/cionz0/boris-bar-windows), based on Emilio Coppa's [GNOME Shell extension](https://github.com/ercoppa/boris-bar-gnome-extension). The original *Boris Bar* concept comes from [Andrea Ricciotti's macOS app](https://github.com/andrearicciotti1/boris-bar).

Zero extra NuGet dependencies. .NET 8, WinForms tray icon, Windows Media Foundation for playback.

## Features

- Goldfish icon in the notification area (system tray)
- 9 built-in clips with global hotkeys `Win+Alt+1` … `Win+Alt+9`
- Custom sounds in `%LOCALAPPDATA%\boris-bar\custom\`
- Max duration 30 seconds; press the same hotkey again (or the same menu item) to stop
- Optional start with Windows (parity with a GNOME extension that loads at login)

## Requirements

- Windows 10 version 1809 (build 17763) or later, or Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) to build (`dotnet run` / `dotnet publish`)
- For the optional DMG import: [7-Zip](https://www.7-zip.org/) and `curl.exe` (already on Windows 10 1803+)

x64 and ARM64 guests are both fine. On VMware Fusion on Apple Silicon, use an ARM64 Windows VM and run `dotnet run` inside the guest (it targets the VM architecture automatically).

## Run from the Fusion shared folder

This repo is meant to live on the Mac host and appear in the Windows VM via a VMware Fusion shared folder. With `/Users/cionzo/Desktop/boris` mapped to `Z:\`, the project is `Z:\boris-bar-windows`.

1. Install the .NET 8 SDK in the VM.
2. In a normal PowerShell window inside the VM:

```powershell
cd Z:\boris-bar-windows
dotnet run --project src\BorisBar
```

3. Import clips (see below).
4. Left- or right-click the goldfish in the tray. On Windows 11, pin the icon in the taskbar corner overflow if it is hidden.

Do not run `git` inside the VM on the shared folder. Commit from macOS.

If `dotnet restore` or `dotnet build` is flaky on the `Z:` share, copy the tree to `C:\src\boris-bar-windows` and build there, or set a local output path:

```powershell
dotnet run --project src\BorisBar -p:BaseOutputPath=C:\build\boris-bar\
```

## Import audio from the original macOS release

Built-in clips are loaded from:

`%LOCALAPPDATA%\boris-bar\builtin`

The bundled script downloads the original macOS DMG from:

`https://github.com/andrearicciotti1/boris-bar/releases/download/v1.0/BorisBar-1.0.dmg`

and copies audio files into that folder. The script and the app only import and play; legality of the download and local use stays with the user.

```powershell
cd Z:\boris-bar-windows
Set-ExecutionPolicy -Scope Process Bypass
.\tools\import-audio-from-dmg.ps1
```

Custom URL:

```powershell
.\tools\import-audio-from-dmg.ps1 "https://example.com/BorisBar.dmg"
```

Windows Media Foundation plays MP3, M4A, WAV, and most MP4 audio reliably. `.caf` / `.ogg` / `.aiff` may fail unless extra codecs are installed. If a clip is silent, convert it to `.m4a` or `.mp3` in the `builtin` folder.

## Custom sounds

Put `mp3`, `wav`, `m4a`, or other supported files in:

`%LOCALAPPDATA%\boris-bar\custom\`

Use **Aggiungi suono personalizzato…** or **Apri cartella suoni**. The file name (without extension) is the menu label.

## Default hotkeys

| Shortcut     | Clip |
|--------------|------|
| `Win+Alt+1` | Fai uno sforzo |
| `Win+Alt+2` | Tutti basiti |
| `Win+Alt+3` | A cazzo di cane |
| `Win+Alt+4` | F4 |
| `Win+Alt+5` | Fiano Romano |
| `Win+Alt+6` | Però sei molto italiano |
| `Win+Alt+7` | Thank you for being so not italian |
| `Win+Alt+8` | Io la mollo questa serie |
| `Win+Alt+9` | Vuoi una pompa |

They work without opening the menu. Pressing the same shortcut again stops playback.

In VMware Fusion, click the VM so it owns the keyboard; otherwise the Mac host eats `Win`/`Cmd`.

## Release packages (permanent)

GitHub **Actions artifacts** expire (often after 90 days). **GitHub Release** assets do not: they stay on
[Releases](https://github.com/cionz0/boris-bar-windows/releases) until someone deletes them.

Push a SemVer tag from macOS (not from the VM share):

```bash
git tag v0.1.0
git push origin v0.1.0
```

The [Release workflow](.github/workflows/release.yml) builds self-contained `win-x64` and `win-arm64` builds, an Inno Setup installer for each, and attaches them to that tag. No audio files are included.

Locally, inside the VM (optional, same script the workflow uses):

```powershell
cd Z:\boris-bar-windows
Set-ExecutionPolicy -Scope Process Bypass
.\tools\publish.ps1 -All
.\tools\publish.ps1 -All -Installer   # needs Inno Setup 6
```

If the `Z:` share is slow, write output to the VM disk:

```powershell
.\tools\publish.ps1 -All -OutputRoot C:\build\boris-bar
```

Use the **x64** installer on typical Intel/AMD PCs. Use **arm64** on Windows on ARM (including this Fusion VM).

## Stack

- .NET 8 / WinForms (`NotifyIcon`)
- Low-level keyboard hook for `Win+Alt+1–9` (`RegisterHotKey` cannot take the Win key)
- `Windows.Media.Playback.MediaPlayer` (Media Foundation)
- `%LOCALAPPDATA%\boris-bar\` for user clips (same layout as `~/.local/share/boris-bar` on GNOME)

## License and disclaimer

### Source code

[MIT](LICENSE) for the **code** and non-audio assets.

### Audio (excerpts from *Boris*)

- This project does **not** redistribute audio clips.
- Clips are not our property. Rights belong to RAI, Wildside, Sky, Mediaset, Disney+, and the authors/performers.
- Use is tribute / illustration / commentary only, as described in `DISCLAIMER.txt`.

Rights holders who want a takedown can open a GitHub issue on [this repository](https://github.com/cionz0/boris-bar-windows/issues).

> *"Basito."*
