# NoFences

Free, open-source desktop fences for Windows 10/11 – plus sticky notes and widgets.
English · Deutsch · Italiano · Français · Español

> **This is a fork of [Twometer/NoFences](https://github.com/Twometer/NoFences)**, created by
> [Twometer](https://github.com/Twometer) and its contributors. All credit for the original idea and
> implementation goes to them. This fork ports the app to .NET 10 and adds the features listed below.
> See [Credits](#credits).

![Styles](docs/styles.png)

## Features

**Fences**
- **Link fences**: drag files/folders in from anywhere; they stay where they are.
- **Folder fences**: show the live contents of a folder; dropping files moves them in, so the icons really leave the desktop.
- **Auto-sort**: patterns like `*.pdf; *.docx` move new desktop files (and finished downloads) into a fence automatically.
- **Tabs**, **recent files**, an icons-only **quick-launch bar**, sorting (manual, name, type, date, size, most used).
- **More fences**: templates (gaming, office, minimal), a self-emptying **shelf**, **browser bookmarks** (Chrome, Edge,
  Brave, Vivaldi, Opera) and **recently opened folders**.
- **Color marks** for items, a **preview on hover** (folder contents, large picture/PDF preview).
- Multi-select, keyboard (Enter, Delete, F2, Ctrl+A/C, arrows) and type-to-search.
- Snapping, positions per monitor setup, per virtual desktop, always on top, collapse to the title bar, optional
  fading when the mouse is far away, a **shortcut per fence**.
- Double-click empty desktop space to hide all fences; **Ctrl+Alt+D** brings them in front of all windows.

![Preview on hover](docs/hover-preview.png)

**Sticky notes** – free text, clickable `[ ]` checkboxes, links, reminders, a taped-on Post-it look in five colors;
pasted **pictures**, **voice notes** and **password-protected** (encrypted) notes.

![Sticky notes](docs/notes.png)
![Pictures, voice notes and a protected note](docs/notes-media.png)

**Widgets** – clock & calendar, system monitor (CPU, RAM, GPU, optional FPS), drives, recycle bin,
**playtime** of any game (pick its exe, NoFences records how long it runs), a **countdown**, **weather**
(Open-Meteo, no account), **now playing** with media controls, **network** rate and ping, **clipboard history** and
**battery**, your installed **games** (Steam with covers, Epic, GOG, Xbox), **appointments** from calendar links
(.ics), a **photo frame**, a **focus timer** (Pomodoro), **news** (RSS/Atom) and **prices** (stocks, crypto).

![Widgets](docs/widgets-docs.png)
![More widgets](docs/widgets-more-docs.png)
![Even more widgets](docs/widgets-extra-docs.png)

Also **screen time**, **sound** (volume, mute, switch playback device) and **service status** (RSI, Discord, Epic …):

![Tool widgets](docs/widgets-tools-docs.png)

**Search** – Ctrl+Alt+F searches everything in all fences (links, folder contents, tabs, notes), Start menu apps and
Windows settings, and calculates (`12*7`).

Plus a **to-do list** with due times and repetition, a **world clock**, **power plans** and **Steam sales** (all sales,
your whole wishlist – also private ones via the share link):

![Planning widgets](docs/widgets-planning-docs.png)

**Timer & alarm**, **habits**, **time progress**, **Twitch live** and **game news**; the games widget shows the playtime
of every game:

![Time widgets](docs/widgets-time-docs.png)

The system monitor has a graph and a **temperature warning**, the network widget a **speed test**, the battery widget
shows **controllers and Bluetooth devices**, and there is an **autostart** manager. The weather adds a **rain hint**,
sunrise/sunset and the moon phase. A **web page** widget shows a dashboard or status page:

![System widgets](docs/widgets-system-docs.png)
![Web page widget and high contrast style](docs/widgets-look-docs.png)

**Notes** understand simple formatting – `# headings`, `- bullets`, `> quotes`, `**bold**`, `*italic*` – and Ctrl+Alt+N
creates one from anywhere. Reminders can repeat.

**Tools** – a desktop assistant that sorts your desktop icons into fences, a screen ruler (px, cm, in), a color picker,
a magnifier, QR codes, desktop icons on/off, all fences to another monitor, and a folder clean-up (old and duplicate
files, to the recycle bin).

![Screen ruler](docs/ruler.png)

**Profiles & automation** – group fences into profiles like "Work" and "Gaming"; switch by hand or automatically while
a program runs or at set times; a profile can bring its own wallpaper, power plan and programs. Fences hide while
something runs full screen, the default style can follow Windows' light/dark mode or the clock, the wallpaper can change
with the time of day, and a break reminder nudges you after a long session.

**Several PCs** – keep fences in a shared folder (e.g. OneDrive); positions stay per monitor setup.

**Styles** – 26 built-in styles (glass, Windows accent color, high contrast, Star Citizen HUD, Retro-Arcade, Hardware,
Nerd, Hobby, Work, Family, Gaming, Finance, Social, Documents, Multimedia, Music, Sport, Photos, Travel, Cooking, Nature,
Post-it ×5) plus your own – made in the **style designer** with a live preview, or as JSON files; optional animations.

![Style designer](docs/style-designer.png)

**App** – settings window, five languages (English, Deutsch, Italiano, Français, Español), built-in updates with one
click, export/import, automatic backups, help and changelog inside the app.

## Download

Get `NoFences.exe` from the [latest release](https://github.com/hofergeorg-tech/NoFences/releases/latest) – a single
file, no installation, no .NET needed. Windows may show a SmartScreen warning because the exe isn't code-signed yet:
"More info" → "Run anyway".

**Help:** [English](HELP.md) · [Deutsch](HILFE.md) · [Italiano](AIUTO.md) · [Français](AIDE.md) · [Español](AYUDA.md)
– also in the app (tray → Help).
**Changes:** [English](CHANGELOG.md) · [Deutsch](CHANGELOG.de.md) · [Italiano](CHANGELOG.it.md) ·
[Français](CHANGELOG.fr.md) · [Español](CHANGELOG.es.md)

**Online:** only what you set up – update checks (GitHub), weather (Open-Meteo), your calendar links, news feeds and
prices (Yahoo Finance; for information only).

## FPS measurement

Optional and off by default. Windows only provides frame-rate events (ETW) to processes with administrator rights,
so NoFences starts a small helper (`NoFences.exe --fps-helper`) elevated – the main app never runs as administrator.
The helper only counts presented frames per process and writes the frame rate of the program in front to a small file;
it reads no screen content and no input. Enabling it in the settings explains this and asks Windows once (UAC); a
Task Scheduler task then starts it without asking. Disabling removes the task.

## Build

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```
dotnet build NoFences/NoFences.csproj -c Release
dotnet test NoFences.Tests/NoFences.Tests.csproj
```

- `.\publish.ps1` creates the self-contained single `dist\NoFences.exe`.
- `NoFences.exe --preview <folder>` renders all styles, notes, widgets and dialogs into PNGs (used for the images above).
- Pushing a tag like `v2.4.0` (must match `<Version>` in the csproj and have a changelog section) builds the exe, attaches
  a build-provenance attestation and creates the release with the changelog in three languages. Code signing via SignPath
  kicks in once `SIGNPATH_API_TOKEN` and `SIGNPATH_ORGANIZATION_ID` are set.
- winget: see [packaging/winget](packaging/winget/README.md).

## Configuration

Stored next to `NoFences.exe`, sorted into `config\` (`fences.json`, `playtime.json`, `usage.json`), `backups\`,
`themes\`, `media\`, `cache\`, `logs\` and `lang\`. If the exe's folder isn't writable (Program Files, a WinGet package
folder), the same layout is used in `%LocalAppData%\NoFences\`. Data of 2.5 and older (flat in `%LocalAppData%\NoFences\`)
is copied over once; the old folder gets a `moved.txt`. Fences from NoFences 1.x are migrated automatically. With a
shared folder (Settings → Data & styles), fences, playtime and styles live there and `config\sync-folder.txt` points to it.

## Credits

- **[Twometer](https://github.com/Twometer)**: original author of NoFences ([Twometer/NoFences](https://github.com/Twometer/NoFences)).
- Contributors to the original project: Birol Capa, damianb53, Daniel Lerch, GordnCZ, lucarnosky, QIVD, Tim.
- Shell context menu (`Win32/ShellContextMenu.cs`): Andreas Johansson, based on FileBrowser from CodeProject.
- Language flags: [flag-icons](https://github.com/lipis/flag-icons) by Panayiotis Lipiridis (MIT, `NoFences/Flags/LICENSE.txt`),
  drawn with [SVG.NET](https://github.com/svg-net/SVG) (MS-PL).
- Fork maintained by Georg Hofer – [www.georg-hofer.com](https://www.georg-hofer.com).

## Support

NoFences is free. If you like it, you can buy me a coffee:
[donate via PayPal](https://www.paypal.com/donate/?business=USLSACVSEY8YW&no_recurring=0&item_name=Coffee+donation&currency_code=EUR)
– also in the app under About and Settings → Updates. Thank you!

## License

MIT, see [LICENSE](LICENSE). The original copyright notice by Twometer is kept as required.
