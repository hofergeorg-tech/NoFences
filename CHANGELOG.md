# Changelog

All notable changes to this fork. Format based on [Keep a Changelog](https://keepachangelog.com/),
versions follow [Semantic Versioning](https://semver.org/). German version: [CHANGELOG.de.md](CHANGELOG.de.md).

## [2.0.0] - 2026-10-01

First release of this fork. Rewritten on .NET 10; based on
[Twometer/NoFences](https://github.com/Twometer/NoFences) 1.x by Twometer and contributors.

### Added
- **Folder fences**: show the live contents of a folder. Dropping files moves them into it (Ctrl = copy),
  so the icons really leave the desktop.
- **Auto-sort**: wildcard patterns per fence (e.g. `*.pdf; *.docx`) with presets for images, documents,
  archives, installers, videos, music and shortcuts. New desktop files and finished browser downloads are
  sorted automatically; "Tidy up desktop now" in the tray sorts existing files.
- **Double-click empty desktop space** to hide/show all fences.
- **11 styles**, globally or per fence: Standard (glass), Star Citizen (HUD), Retro-Arcade,
  Hardware (circuit board), Nerd (terminal), Hobby (pinboard), Work (business), Family, Gaming (RGB),
  Finance (trading desk) and Social.
- `--preview <folder>` renders all styles with sample items into PNGs.
- **Tray icon** with new fence, show/hide fences, start with Windows, file extensions, open config folder, exit.
- "Start with Windows" also in every fence's context menu.
- **Help** and **What's new?** in the tray and fence menus, in English and German (follows the Windows
  language). "What's new?" opens once by itself after an update.
- Drag & drop: reorder items, move items between fences, drag items out to Explorer.
- Shell thumbnails for images, videos, PDFs etc.; icon size 24–96 px.
- One settings dialog per fence: name, type, folder, style, title height, icon size, color, opacity,
  auto-sort, lock, auto-collapse.
- Portable mode (`portable.txt` next to the exe).
- App icon, self-contained single-file exe, GitHub release workflow with build provenance
  (SignPath code signing prepared).

### Changed
- Ported from .NET Framework 4.8 to .NET 10.
- Configuration is a single `fences.json` with atomic saves. Fences from 1.x are migrated automatically.
- Per-monitor DPI awareness; fences that end up off-screen are moved back.
- UI languages: German and English (follows the Windows display language).

### Fixed
- Laggy scrolling and redrawing with many items (icons are now cached and loaded in the background).
- GDI resource leaks while painting.
- Thumbnails stopped loading after a few broken images.
- A missing or broken fence file prevented the app from starting.
- Last window position/size could be lost when closing shortly after moving.
- Fences sporadically appeared on top of other windows.
- Opening documents failed with "UseShellExecute" after the .NET port.
- There was no way to exit the app other than deleting all fences.
- Long single-word names were broken mid-word; they are now shortened with "…".

### Removed
- Chinese and Czech translations of the 1.x dialogs (the dialogs were replaced).

## [1.x]

Original NoFences by Twometer, see [Twometer/NoFences](https://github.com/Twometer/NoFences).

[2.0.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.0.0
[1.x]: https://github.com/Twometer/NoFences
