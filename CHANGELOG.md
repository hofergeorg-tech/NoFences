# Changelog

All notable changes to this fork. Format based on [Keep a Changelog](https://keepachangelog.com/),
versions follow [Semantic Versioning](https://semver.org/). German version: [CHANGELOG.de.md](CHANGELOG.de.md).

## [2.4.0] - 2026-10-03

### Added
- **Widgets**: clock & calendar, system monitor (CPU, RAM, GPU load and temperature, FPS), drives, recycle bin,
  playtime and countdown. Tray or fence menu → New widget.
- **Playtime** for any game: pick its exe, NoFences records how long it runs (today, week, month, total, running now).
- **Countdown** to a date with a title.
- **Weather** (Open-Meteo, no account), **now playing** with media controls, **network** with graph and ping,
  **clipboard history** (in memory only, password managers respected) and **battery** widgets.
- **Profiles** like "Work" and "Gaming": switch in the tray, assign fences via right-click → Show in profile.
- **Language** menu in the tray and in every fence menu.
- **FPS measurement** (optional, off by default): a small helper with administrator rights counts the frames of the
  program in front; Windows asks once, the settings explain why.
- **Recent files** fence and **quick-launch bar** (icons only, names as tooltips).
- **Tabs** in link fences.
- **Only on this virtual desktop** per fence.
- **Export/import** of fences and own styles, e.g. for another PC.
- **Settings window** (tray → Settings) with everything app-wide; the tray menu is much shorter.
- **Fence settings** redesigned with sections and a live preview.
- **Languages**: English, German and **Italian**; automatic (Windows language, English otherwise) or chosen in the settings.
- **About window** with version, credits and links.
- **8 new styles**: Documents, Multimedia, Music, Sport, Photos, Travel, Cooking, Nature – 24 styles in total; every
  style has an accent color for widgets.

### Fixed
- OK in the settings of a link fence removed all of its links.
- Opening the settings of a widget crashed.
- Post-it items spilled over the paper's shadow at the bottom.
- The peek shortcut was stuck to the menu text without a space.
- The selected page in the settings' side bar became unreadable.

## [2.3.0] - 2026-10-02

### Added
- **Multi-select**: Ctrl/Shift-click and rubber band selection; drag, reorder and the Explorer menu work on
  several items.
- **Keyboard**: Enter opens, Delete removes links (folder fences: recycle bin), F2 renames, Ctrl+A, Ctrl+C,
  arrow keys, Esc. Clicked fences take the keyboard focus but stay behind other windows.
- **Search**: just type while a fence is focused; the search shows top right, Esc clears it.
- **Snapping** to screen edges and other fences while moving or resizing; hold Alt to place freely.
- **Layouts per monitor setup**: fences go back to where they were for the current screens (e.g. docked/undocked).
- **Backups** of the configuration every 12 hours (last 10 kept); tray → "Restore backup".
- **Note reminders** (fence menu → Reminder…) as a notification with sound; the time shows in the title.
- **Links in notes** (web addresses, paths) are underlined and open on click.
- **Post-it colors**: yellow, pink, green, blue and orange.
- **Animations**: smooth collapsing; hover effects for Star Citizen, Gaming and Retro-Arcade. Tray → Animations.
- **Own styles** as JSON files in the `themes` folder (an example is created); tray → Own styles.
- Automatic tests (45) run on every push; winget manifests prepared in `packaging/winget`.

- **Rename in place**: double-click a fence's title (or fence menu → Rename).

### Fixed
- Moving fences stuttered: position is now saved once when you let go, hover animations pause while
  dragging, and snapping is less sticky.
- Tabs in notes are drawn with the same tab stops as in the editor.
- A collapsed Post-it kept only a thin line instead of its title.
- Post-its could not be resized (the handles were on the clear margin, which doesn't receive clicks).

## [2.2.0] - 2026-10-01

### Added
- **Sticky notes**: a new fence type "Note" (tray or fence menu → "New note"). Double-click to write, Esc or a
  click outside saves. Lines starting with `[ ]` become checkboxes you can tick with a click.
  Text dragged onto a note is appended.
- **Post-it style**: a yellow note taped onto the desktop: clear background around the paper, two strips of
  translucent tape, soft shadow with lifted corners, curled corner and handwriting. New notes use it by default.
  Styles can now opt out of the frosted glass and draw free shapes.
- **Always on top** per fence (fence menu), e.g. for a note next to a game in borderless window mode.

### Changed
- New fences and notes appear next to the mouse, on the monitor you are working on, instead of always on the
  primary monitor (where a game may cover them).

## [2.1.0] - 2026-10-01

### Added
- **Updates**: NoFences checks GitHub for new releases (shortly after start, then every 6 hours) and installs
  them with one click on the notification or tray → "Install update". Can be switched off; "Check for updates now"
  in the tray.
- **Bring fences to front** with a shortcut (default Ctrl+Alt+D; Ctrl+Alt+Space, Ctrl+Shift+D or off in the tray).
  Fences stay on top until you press the shortcut again, press Esc, click outside them or open an item.
- **Sorting per fence**: manual (drag & drop), name, type, date modified or size. Fence menu → "Sort by" or the
  fence settings. Folders always come first.

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
- Electron apps (e.g. RSI Launcher, Discord) did not start from a fence when NoFences itself had been
  started from VS Code or another Electron app (inherited `ELECTRON_RUN_AS_NODE`).

### Removed
- Chinese and Czech translations of the 1.x dialogs (the dialogs were replaced).

## [1.x]

Original NoFences by Twometer, see [Twometer/NoFences](https://github.com/Twometer/NoFences).

[2.4.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.4.0
[2.3.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.3.0
[2.2.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.2.0
[2.1.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.1.0
[2.0.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.0.0
[1.x]: https://github.com/Twometer/NoFences
