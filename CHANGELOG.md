# Changelog

All notable changes to this fork. Format based on [Keep a Changelog](https://keepachangelog.com/),
versions follow [Semantic Versioning](https://semver.org/).
Deutsch: [CHANGELOG.de.md](CHANGELOG.de.md) · Italiano: [CHANGELOG.it.md](CHANGELOG.it.md) ·
Français : [CHANGELOG.fr.md](CHANGELOG.fr.md) · Español: [CHANGELOG.es.md](CHANGELOG.es.md)

## [2.6.0] - unreleased

### Added
- **Own translations**: Settings → General → "Own translations…" opens the `lang` folder with an English template. A file like `nl.json` adds a language, a `de.json` with single texts changes just those. All texts now live in one JSON file per language.
- **Flags for every language**: the language menu shows real country flags (flag-icons), so own languages get their flag too (by language code, region like `pt-BR`, or `"_flag": "at"` in the language file).
- **Undo (Ctrl+Z)** for deleting, moving, resizing and renaming fences and for removing, moving and renaming items; also "Undo: …" in the tray and fence menu.
- **Fence groups**: right-click → Group. Fences in a group move together and can be folded to their title bars together.
- **Fences**: open folders inside the fence (arrow in the folder's corner, contents indented below), **notes on items** (tooltip), **usage and tidy up** (never opened items as a suggestion), a **background picture** or pattern per fence, and **open files with** a chosen program.
- **Notes** recognize appointments ("Mo 14:00 Dentist", "tomorrow 9:30 …", "12.10. 15:00 …") and offer a reminder 15 minutes before; ringing timers and alarms can be **snoozed** for 5 or 10 minutes. New tools: **password generator** (copies without clipboard history) and **network info** (addresses, Wi-Fi, router; a click copies).

### Changed
- **All data next to NoFences.exe**, sorted into folders (`config`, `backups`, `themes`, `media`, `cache`, `logs`, `lang`). Data of older versions is copied over once; in a non-writable program folder (Program Files, WinGet) the data stays in `%LocalAppData%\NoFences`, sorted the same way.

## [2.5.0] - 2026-10-04

### Added
- **Games**: playtime of every detected game, counted automatically and shown under its cover; sort by most played.
- Widgets **game news** (announcements and patch notes of your Steam games) and **Twitch live** (who's live, with a notification).
- **Steam sales** can be set up: sales, top sellers, new releases or only your wishlist; minimum discount, highest price, count.
- Widgets **timer & alarm** (quick timers, alarms on chosen days, rings even when hidden), **habits** (tick the last 7 days, streaks) and **time progress** (day, week, month, year).
- **Break reminder** after 30–120 minutes of active PC use (Settings → Automation).
- The **clock's calendar** marks days with appointments.
- **Fences**: color marks for items (Shift+right-click → Mark, or Ctrl+1…6), sort by **most used**, **preview on hover** (folder contents, large image/PDF preview).
- **More fences**: **templates** (gaming setup, office, minimal), a **shelf** that empties itself, **browser bookmarks** (Chrome, Edge, Brave, Vivaldi, Opera) and **recently opened folders**.
- Own **shortcut per fence** (Ctrl+Shift+F1…F12) brings it to the front, even from another profile.
- Fences can **fade** when the mouse is far away (Settings → Desktop); single fences can be excluded.
- Tools: **show/hide desktop icons** and **move all fences to another monitor**.
- **Profiles**: start programs with a profile (and close them again when leaving, if wanted); **wallpaper by time of day** (Settings → Automation).
- **Notes**: protect with a password (encrypted, locks itself after 2 minutes), paste **images** with Ctrl+V, record **voice notes**.
- **Clipboard history** also keeps images; **pin** entries (right-click) so they stay on top, even after a restart.
- **System monitor** with a two-minute graph and a **warning when the graphics card gets too hot**; **speed test** in the network widget.
- **Battery widget** also shows **controllers and Bluetooth devices**; new widget **Autostart** (switch Windows startup programs on/off).
- Tools: **QR code** (text or link from the clipboard), **magnifier**; "Clean up folders" finds **duplicate files**.
- **Weather**: rain hint for the next two hours ("Rain in about 20 min"), sunrise and sunset, moon phase.
- **Style designer**: make your own style by clicking (colors, fonts, border, corners) with a live preview; new **high contrast** style with large text.
- New widget **Web page**: a small page (dashboard, status page …) right in a fence, refreshed regularly.
- **Steam sales**: a wishlist that isn't public works via its **share link**.
- **Steam sales** show all current sales (not only the featured ones; more load when scrolling) and every discounted game of your wishlist.

## [2.4.2] - 2026-10-03

### Changed
- **Clean up folders** (was: Clean up Downloads): add more folders than Downloads; all are searched together, with a
  column showing where each item lies. The folder list is kept.

### Fixed
- Scrolling lists (Steam sales, news, appointments, to-dos) stayed scrolled and stuck after making the fence bigger.

## [2.4.1] - 2026-10-03

### Added
- **Search** also finds Start menu apps and Windows settings pages, and calculates (`12*7`, `200*15%`; Enter copies).
- Widgets **screen time** (programs used today / 7 days, stays on this PC), **sound** (volume, mute, microphone,
  switch playback device) and **service status** (RSI, Discord, Epic Games, GitHub … from their status pages).
- **Focus mode**: the focus timer switches to a chosen profile during focus rounds.
- **Profile shortcuts** Ctrl+Alt+F1…F9 (F10: all fences) and a **wallpaper per profile**.
- **Desktop assistant**: sorts desktop icons into new fences by kind (offered on the first start).
- **Screen ruler** in pixels, centimetres or inches (Tools ▸ Screen ruler – in the tray and every fence menu).
- **Color picker** with magnifier (copies #RRGGBB) and **Clean up Downloads** (old files, biggest first, to the recycle bin).
- **Quick note** from anywhere with Ctrl+Alt+N; **formatting in notes** (headings, bullets, quotes, lines, bold, italic).
- **Repeating reminders** (daily, weekdays, weekly, monthly) and a **to-do list** widget with due times.
- Widgets **world clock**, **power plan** (also switched per profile) and **Steam sales** (wishlist first).

### Changed
- **Grouped menus**: New widget ▸ Time & planning / Info & news / System / Games & media; Style ▸ Basic / Gaming & tech /
  Work & everyday / Leisure / Post-it / Own styles.
- **News** look clearer: semibold headlines on up to two lines, the source in the style's accent color, separators.

### Fixed
- The news and prices widgets said "No connection to the weather service" when a feed failed.
- A web page entered as a news feed now finds the feed the page announces, or says clearly that it is not a feed.
- A failed update no longer clears headlines, prices or appointments that were already shown; retries after 30 seconds.
- Problems with online widgets are written to log.txt in the data folder.

## [2.4.0] - 2026-10-03

### Added
- **Widgets**: clock & calendar, system monitor (CPU, RAM, GPU load and temperature, FPS), drives, recycle bin,
  playtime and countdown. Tray or fence menu → New widget.
- **Playtime** for any game: pick its exe, NoFences records how long it runs (today, week, month, total, running now).
- **Countdown** to a date with a title.
- **Weather** (Open-Meteo, no account), **now playing** with media controls, **network** with graph and ping,
  **clipboard history** (in memory only, password managers respected) and **battery** widgets.
- **Games** widget: installed games from Steam (with covers), Epic, GOG and the Xbox app; click to play.
- **Appointments** from calendar links (.ics: Google, Outlook, iCloud), including recurring events.
- **Photo frame**, **focus timer (Pomodoro)**, **news** (RSS/Atom with ready-made feeds) and **prices** (stocks,
  indices, crypto) widgets.
- **Search across all fences** (Ctrl+Alt+F): links, folder contents, tabs and notes.
- **Profiles** like "Work" and "Gaming": switch in the tray, assign fences via right-click → Show in profile.
- **Automation**: switch profiles while a program runs or at set times; hide fences while something runs full screen;
  light/dark default style following Windows or the clock.
- **Several PCs**: keep fences in a shared folder such as OneDrive.
- **Windows accent color** style – 25 styles in total.
- **French and Spanish**; a **language** menu with flags in the tray and in every fence menu.
- **Donate** button (About and Settings → Updates).
- **FPS measurement** (optional, off by default): a small helper with administrator rights counts the frames of the
  program in front; Windows asks once, the settings explain why.
- **Recent files** fence and **quick-launch bar** (icons only, names as tooltips).
- **Tabs** in link fences.
- **Only on this virtual desktop** per fence.
- **Export/import** of fences and own styles, e.g. for another PC.
- **Settings window** (tray → Settings) with everything app-wide; the tray menu is much shorter.
- **Fence settings** redesigned with sections and a live preview.
- **Languages**: English, German, **Italian**, **French** and **Spanish**; automatic (Windows language, English
  otherwise) or chosen in the settings.
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

[2.4.1]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.4.1
[2.4.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.4.0
[2.3.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.3.0
[2.2.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.2.0
[2.1.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.1.0
[2.0.0]: https://github.com/hofergeorg-tech/NoFences/releases/tag/v2.0.0
[1.x]: https://github.com/Twometer/NoFences
