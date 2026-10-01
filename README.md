# NoFences

Free, open-source desktop fences for Windows 10/11.

> **This is a fork of [Twometer/NoFences](https://github.com/Twometer/NoFences)**, created by
> [Twometer](https://github.com/Twometer) and its contributors. All credit for the original idea and
> implementation goes to them. This fork ports the app to .NET 10 and adds the features listed below.
> See [Credits](#credits).

![screenshot](screenshot.png)

## Features

- **Link fences**: drag files/folders from anywhere in; they stay where they are.
- **Folder fences**: show the live contents of a folder. Dropping files moves them into it
  (Ctrl = copy), so the icons actually leave your desktop.
- **Auto-sort**: give a fence patterns like `*.pdf; *.docx` (or pick a preset) and new desktop files
  matching them are moved into it automatically. Finished browser downloads are picked up too.
  Tray → "Tidy up desktop now" sorts what is already there.
- **Double-click empty desktop space** to hide/show all fences.
- Reorder items by drag & drop, drag items between fences or out to Explorer.
- Shell thumbnails (images, videos, PDFs …) and selectable icon size (24–96 px).
- Styles: **Standard (glass)**, **Star Citizen (HUD)**, **Retro-Arcade**, globally or per fence.
- Auto-collapse to the title bar, lock, background color/opacity, title height.
- Tray icon: new fence, show/hide all fences, autostart, file extensions, exit.
- Per-monitor DPI aware; fences that end up off-screen are moved back.

Right-click a fence for its menu; right-click an item for the Explorer menu (Shift + right-click for the fence menu).

See [CHANGELOG.md](CHANGELOG.md) ([Deutsch](CHANGELOG.de.md)) for all changes.

## Build

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```
dotnet build NoFences/NoFences.csproj -c Release
```

`.\publish.ps1` creates a self-contained single `dist\NoFences.exe` that runs without a .NET install.

Pushing a tag like `v2.0.0` (must match `<Version>` in the csproj) makes the GitHub Action build the exe,
attach a build-provenance attestation and create the release. Code signing via SignPath Foundation kicks in
automatically once the secret `SIGNPATH_API_TOKEN` and the variable `SIGNPATH_ORGANIZATION_ID` are set.

## Configuration

Stored in `%LocalAppData%\NoFences\fences.json`. Fences from NoFences 1.x are migrated automatically.

**Portable mode:** put an empty `portable.txt` next to `NoFences.exe`; the config is then kept next to the exe.

## Credits

- **[Twometer](https://github.com/Twometer)**: original author of NoFences ([Twometer/NoFences](https://github.com/Twometer/NoFences)).
- Contributors to the original project: Birol Capa, damianb53, Daniel Lerch, GordnCZ, lucarnosky, QIVD, Tim.
- Shell context menu (`Win32/ShellContextMenu.cs`): Andreas Johansson, based on FileBrowser from CodeProject.
- Fork maintained by [hofergeorg-tech](https://github.com/hofergeorg-tech).

## License

MIT, see [LICENSE](LICENSE). The original copyright notice by Twometer is kept as required.
