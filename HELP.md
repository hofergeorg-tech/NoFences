# NoFences Help

NoFences puts boxes ("fences") on your desktop that keep your icons organized, plus sticky notes and widgets.
Deutsch: [HILFE.md](HILFE.md) · Italiano: [AIUTO.md](AIUTO.md) · Français : [AIDE.md](AIDE.md) · Español: [AYUDA.md](AYUDA.md)

## Getting started

- After the first start there is one empty fence. Drag files or folders onto it.
- **Right-click a fence** (title or empty space) for its menu: settings, style, rename, new fence or widget, delete.
- **Right-click an item** for the normal Explorer menu. Shift + right-click shows the fence menu instead.
- The **tray icon** (bottom right, maybe behind the ^ arrow) creates fences, shows/hides them, switches profiles and
  opens the **Settings** for everything app-wide. **Language** is in the tray and in every fence menu.

## Kinds of fences

- **Link fence** (default): links to files and folders. The files stay where they are, so they are still on the
  desktop too. Deleting the original removes it from the fence as well.
- **Folder fence**: shows the contents of a folder. Dropping files onto it **moves** them into that folder
  (hold Ctrl to copy), so they really leave the desktop.
- **Note**: a sticky note with text, see below.
- **Widget**: live content – clock, weather, games, appointments and more, see below.
- **Recent files**: the 20 files you opened last (read-only).
- **Quick-launch bar**: a slim link fence with icons only; names show as tooltips.

Tip: for a tidy desktop, create a folder such as `Documents\Fences\Work` and use it as a folder fence.

## Working with items

- Double-click opens an item. Drag items to reorder them, onto another fence to move them there, or into Explorer.
- **Ctrl+click** selects several items, **Shift+click** a range; dragging on empty space draws a selection rectangle.
- A clicked fence listens to the keyboard: **Enter** opens, **F2** renames, **Delete** removes (link fences: only the
  link; folder fences: recycle bin), **Ctrl+A**, **Ctrl+C**, arrow keys, **Esc**.
- **Just type** to search in that fence; the search text shows top right, Esc ends it.
- Fence menu → **Sort by**: manual, name, type, date modified or size.
- **Tabs** (link fences): fence menu → Add tab. Click a tab to switch, double-click to rename, drag items onto a tab
  to move them there.

## Search across all fences

**Ctrl+Alt+F** (or right-click the tray icon or a fence → Tools ▸ Search fences…) opens a search box. It finds everything in your fences – links, folder
contents, tabs and note texts – even letters in order ("ffx" finds Firefox) – and also Start menu apps and Windows
settings pages ("bluetooth", "sound"). Type a calculation like `12*7` or `200*15%` and Enter copies the result.
**Enter** opens the result, **↑↓** choose, **Esc** closes. The shortcut can be changed in Settings → Desktop.

## Moving, sizing, renaming

- Drag the title bar to move a fence, drag the edges to resize. **Double-click the title** to rename it.
- Fences **snap** to screen edges and other fences; hold **Alt** to place freely.
- Positions are remembered **per monitor setup**: unplug a monitor and plug it back in, and the fences return.
- **Locked** fences can't be moved or changed. **Collapse when not hovered** shrinks a fence to its title bar.
- **Always on top** keeps a fence above all windows (over games only in "borderless window" mode).
- **Only on this virtual desktop** shows a fence only on the current virtual desktop (Win+Ctrl+arrows).
- **Ctrl+Alt+D** brings all fences in front of the open windows; Esc or a click elsewhere sends them back.

## Notes

- **Double-click** to write; **Esc** or a click outside saves.
- Lines starting with `[ ]` become checkboxes; a click ticks them and strikes the line through.
- Web addresses and paths are underlined and open on click. Text dragged onto a note is appended.
- Fence menu → **Reminder…**: NoFences plays a sound and shows a notification at that time.
- Post-it style in yellow, pink, green, blue and orange.

## Widgets

Tray or fence menu → **New widget**. Widgets with a list scroll with the mouse wheel.

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
- **Games**: your installed games from Steam (with covers), Epic, GOG and the Xbox app; recently played first.
  Click starts a game. The menu hides games, sorts by name or searches again.
- **Appointments**: the next two weeks from calendar links (.ics). Google: calendar settings → "Secret address in iCal
  format"; Outlook: Settings → Calendar → Shared calendars → Publish → ICS; iCloud: share the calendar publicly.
  Several calendars: one link per line. Recurring events are supported.
- **Photo frame**: a slideshow of a picture folder (subfolders too), every 10 s to 15 min. Click shows the next
  picture, double-click opens it.
- **Focus timer (Pomodoro)**: 25 minutes focus, 5 minutes break, a long break after four rounds (or 50/10, 15/3).
  A sound and a notification mark each change.
- **News**: headlines from RSS or Atom feeds (ready-made buttons for Tagesschau, ORF, heise, BBC and more); click
  opens the article.
- **Prices**: stocks, indices and crypto with the change since yesterday and a chart of the day, using Yahoo Finance
  symbols such as `AAPL`, `^GDAXI` (DAX), `^ATX`, `BTC-EUR`. Updated every five minutes; for information only.
- **Screen time**: which programs you used how long today or in the last 7 days (click "today ⇄" to switch).
  Recorded only while the widget exists and you're at the PC; stays on this PC.
- **Sound**: volume of the current playback device (click the bar or use the mouse wheel), mute for speakers and
  microphone, and one click to switch to another device (headset ↔ speakers).
- **Service status**: whether RSI, Discord, Epic Games, GitHub and others have problems right now, from their
  public status pages; click a line to open the page.

Every widget's menu has its own settings. The focus timer's menu also has **focus mode**: it switches to a profile you
choose (e.g. "Focus" with only work fences) while a focus round runs, and back in breaks.

## Desktop assistant

Tray or fence menu → Tools ▸ **Desktop assistant…** (also offered on the first start) sorts what's on your desktop into new fences – games,
programs, documents, pictures, music & videos, archives, folders – each in a fitting style. Nothing is moved; the fences
link to the files. To hide the originals: right-click the desktop → View → Show desktop icons.

## Screen ruler

Tray or fence menu → Tools ▸ **Screen ruler** puts a ruler above everything: drag to move, drag the end to change the length, double-click or
space turns it, arrow keys nudge it (Shift: 10 px), U or the menu switches between pixels, centimetres and inches
(real size, from the size your monitor reports). A red line follows the mouse and shows the distance. Esc closes it.

## Profiles

Group fences into profiles like "Work" and "Gaming" and switch between them in the tray (**Profile ▸**) or in
**Settings → Desktop**. Right-click a fence → **Show in profile** to assign it; a fence without a profile shows in
every profile. Fences created while a profile is active belong to it. **Ctrl+Alt+F1…F9** switch to profile 1…9,
**Ctrl+Alt+F10** shows all fences. With a profile active, tray → Profile ▸ **Wallpaper for "…"** gives it its own
wallpaper; your usual wallpaper comes back in profiles without one.

## Automation (Settings → Automation)

- **Switch profiles automatically**: "Gaming" while a certain program runs, "Work" on weekdays from 8 to 17 and so
  on. A running program wins over a time rule; when no rule applies any more, the previous profile comes back.
  Switching by hand ends what a rule started.
- **Full screen**: while a game, video or presentation fills a monitor, the fences on that monitor are hidden.
- **Light and dark style**: the default style changes with Windows' light/dark mode or at set times – for example
  Post-it during the day and glass at night. Fences with their own style keep it.
- The **Windows accent color** style takes its color from Settings → Personalization → Colors.

## Several PCs

Settings → Data & styles → **Choose shared folder…**, e.g. in OneDrive. Fences, notes, playtime and own styles then
live there, and every PC pointed at the same folder shows the same fences. Positions are kept per monitor setup, so a
laptop and a desktop PC can arrange them differently. When another PC saves, NoFences reloads after a few seconds.
"Stop sharing" copies everything back to this PC.

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
preview). There are 25 styles – glass, Windows accent color, Star Citizen HUD, Retro-Arcade, Hardware, Nerd, Hobby,
Work, Family, Gaming, Finance, Social, Documents, Multimedia, Music, Sport, Photos, Travel, Cooking, Nature and Post-it
in five colors.

**Own styles**: Settings → Data & styles → Open styles folder. Copy `beispiel-mocha.json`, change the colors
(`#RRGGBB` or `#RRGGBBAA`) and reload. Own styles carry a ★.

## Settings (tray → Settings)

- **General**: language (automatic, English, Deutsch, Italiano, Français, Español), start with Windows, file
  extensions, default style, animations.
- **Desktop**: double-click on the desktop hides/shows fences; shortcut to bring fences to the front (Ctrl+Alt+D);
  profiles; search shortcut (Ctrl+Alt+F); auto-sort.
- **Automation**: profile rules, full screen, light and dark style (see above).
- **Updates**: NoFences checks GitHub and installs new versions with one click; donate.
- **FPS measurement**: see above.
- **Data & styles**: export/import fences, restore a backup (made every 12 hours), shared folder, folders.

## FAQ

**Can I delete the original after dragging an icon into a fence?**
In a link fence: no, the fence only links to it. In a folder fence the file was moved, so there is nothing left to delete.

**Windows shows a SmartScreen warning when starting NoFences.**
The exe is not code-signed yet. Click "More info" → "Run anyway".

**What goes online?**
Only what you set up: update checks (GitHub), weather (Open-Meteo), your calendar links, news feeds and prices
(Yahoo Finance). Nothing else is sent anywhere.

**Where are my settings?**
`%LocalAppData%\NoFences\fences.json` (backups next to it), or in the shared folder if you chose one. With an empty
`portable.txt` next to `NoFences.exe` they are kept next to the exe instead.

**How do I uninstall?**
Settings → General: uncheck "Start with Windows"; turn off FPS measurement if used; tray → Exit; delete `NoFences.exe`
and the folder `%LocalAppData%\NoFences`. Files in folder fences stay in their folders.
