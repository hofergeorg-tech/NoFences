# NoFences Help

NoFences puts boxes ("fences") on your desktop that keep your icons organized, plus sticky notes and widgets.
Deutsch: [HILFE.md](HILFE.md) · Italiano: [AIUTO.md](AIUTO.md)

## Getting started

- After the first start there is one empty fence. Drag files or folders onto it.
- **Right-click a fence** (title or empty space) for its menu: settings, style, rename, new fence or widget, delete.
- **Right-click an item** for the normal Explorer menu. Shift + right-click shows the fence menu instead.
- The **tray icon** (bottom right, maybe behind the ^ arrow) creates fences, shows/hides them and opens the
  **Settings** for everything app-wide.

## Kinds of fences

- **Link fence** (default): links to files and folders. The files stay where they are, so they are still on the
  desktop too. Deleting the original removes it from the fence as well.
- **Folder fence**: shows the contents of a folder. Dropping files onto it **moves** them into that folder
  (hold Ctrl to copy), so they really leave the desktop.
- **Note**: a sticky note with text, see below.
- **Widget**: live content – clock, system monitor, drives, recycle bin, playtime, countdown, see below.
- **Recent files**: the 20 files you opened last (read-only).
- **Quick-launch bar**: a slim link fence with icons only; names show as tooltips.

Tip: for a tidy desktop, create a folder such as `Documents\Fences\Work` and use it as a folder fence.

## Working with items

- Double-click opens an item. Drag items to reorder them, onto another fence to move them there, or into Explorer.
- **Ctrl+click** selects several items, **Shift+click** a range; dragging on empty space draws a selection rectangle.
- A clicked fence listens to the keyboard: **Enter** opens, **F2** renames, **Delete** removes (link fences: only the
  link; folder fences: recycle bin), **Ctrl+A**, **Ctrl+C**, arrow keys, **Esc**.
- **Just type** to search; the search text shows top right, Esc ends it.
- Fence menu → **Sort by**: manual, name, type, date modified or size.
- **Tabs** (link fences): fence menu → Add tab. Click a tab to switch, double-click to rename, drag items onto a tab
  to move them there.

## Moving, sizing, renaming

- Drag the title bar to move a fence, drag the edges to resize. **Double-click the title** to rename it.
- Fences **snap** to screen edges and other fences; hold **Alt** to place freely.
- Positions are remembered **per monitor setup**: unplug a monitor and plug it back in, and the fences return.
- **Locked** fences can't be moved or changed. **Collapse when not hovered** shrinks a fence to its title bar.
- **Always on top** keeps a fence above all windows (over games only in "borderless window" mode).
- **Only on this virtual desktop** shows a fence only on the current virtual desktop (Win+Ctrl+arrows).

## Notes

- **Double-click** to write; **Esc** or a click outside saves.
- Lines starting with `[ ]` become checkboxes; a click ticks them and strikes the line through.
- Web addresses and paths are underlined and open on click. Text dragged onto a note is appended.
- Fence menu → **Reminder…**: NoFences plays a sound and shows a notification at that time.
- Post-it style in yellow, pink, green, blue and orange.

## Widgets

Tray or fence menu → **New widget**:

- **Clock & calendar**.
- **System monitor**: CPU, RAM, GPU load and temperature (NVIDIA), and **FPS** if enabled (see below).
- **Drives**: fill level and free space; click opens the drive.
- **Recycle bin**: drop files on it to delete them, double-click opens it, the menu empties it.
- **Playtime**: today / this week / this month / total for any game. Double-click and pick the game's exe; NoFences
  records how long it runs.
- **Countdown**: days and hours until a date; double-click to set it.
- **Weather**: current weather and three days ahead for a place you search for (data: Open-Meteo, no account needed).
- **Now playing**: title, artist and cover of what Spotify, a browser or a media player is playing, with
  previous / play-pause / next.
- **Network**: download and upload rate with a one-minute graph, and the ping.
- **Clipboard history**: the last 15 copied texts; click one to copy it again. Kept only while NoFences runs, and
  passwords from password managers are skipped.
- **Battery**: charge, charging or not, time left (laptops).

## Profiles

Group fences into profiles like "Work" and "Gaming" and switch between them in the tray (**Profile ▸**) or in
**Settings → Desktop**. Right-click a fence → **Show in profile** to assign it; a fence without a profile shows in
every profile. Fences created while a profile is active belong to it.

## FPS measurement (optional)

Windows only gives the frame-rate events to programs with administrator rights. NoFences therefore uses a small
helper process that runs as administrator – NoFences itself does not. It only counts frames, no screen content,
no input. Turn it on in **Settings → FPS measurement**; Windows asks once, after that a Task Scheduler task starts the
helper without asking. Turning it off removes the task again.

## Auto-sort from the desktop

In a fence's settings, enter patterns under "Auto-sort from the desktop", e.g. `*.pdf; *.docx`, or add a preset.
New desktop files that match move into that fence (finished downloads too). **Tidy up desktop now** (tray or settings)
sorts what is already there.

## Styles

Pick the default in **Settings → General**, or one per fence (fence menu → Style, or the fence settings with a live
preview). There are 24 styles – glass, Star Citizen HUD, Retro-Arcade, Hardware, Nerd, Hobby, Work, Family, Gaming,
Finance, Social, Documents, Multimedia, Music, Sport, Photos, Travel, Cooking, Nature and Post-it in five colors.

**Own styles**: Settings → Data & styles → Open styles folder. Copy `beispiel-mocha.json`, change the colors
(`#RRGGBB` or `#RRGGBBAA`) and reload. Own styles carry a ★.

## Settings (tray → Settings)

- **General**: language (automatic, English, Deutsch, Italiano), start with Windows, file extensions, default style, animations.
- **Desktop**: double-click on the desktop hides/shows fences; shortcut to bring fences to the front (Ctrl+Alt+D); auto-sort.
- **Updates**: NoFences checks GitHub and installs new versions with one click.
- **FPS measurement**: see above.
- **Data & styles**: export/import fences (e.g. for another PC), restore a backup (made every 12 hours), folders.

## FAQ

**Can I delete the original after dragging an icon into a fence?**
In a link fence: no, the fence only links to it. In a folder fence the file was moved, so there is nothing left to delete.

**Windows shows a SmartScreen warning when starting NoFences.**
The exe is not code-signed yet. Click "More info" → "Run anyway".

**Where are my settings?**
`%LocalAppData%\NoFences\fences.json` (backups next to it). With an empty `portable.txt` next to `NoFences.exe` they
are kept next to the exe instead.

**How do I uninstall?**
Settings → General: uncheck "Start with Windows"; turn off FPS measurement if used; tray → Exit; delete `NoFences.exe`
and the folder `%LocalAppData%\NoFences`. Files in folder fences stay in their folders.
