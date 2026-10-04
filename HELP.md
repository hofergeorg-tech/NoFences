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
- **More fences** (menu → More fences):
  - **Templates** (gaming setup, office, minimal) create several matching fences at once.
  - The **shelf** is a folder fence for parking files briefly; what lies there for a week goes to the recycle bin
    (set the time in the fence settings – for any other folder fence too).
  - **Browser bookmarks** shows the bookmarks bar of Chrome, Edge, Brave, Vivaldi or Opera and stays up to date.
  - **Recently opened folders**: the 15 folders you worked in last.

Tip: for a tidy desktop, create a folder such as `Documents\Fences\Work` and use it as a folder fence.

## Working with items

- Double-click opens an item. Drag items to reorder them, onto another fence to move them there, or into Explorer.
- **Ctrl+click** selects several items, **Shift+click** a range; dragging on empty space draws a selection rectangle.
- A clicked fence listens to the keyboard: **Enter** opens, **F2** renames, **Delete** removes (link fences: only the
  link; folder fences: recycle bin), **Ctrl+A**, **Ctrl+C**, arrow keys, **Esc**.
- **Ctrl+Z** undoes the last change: a deleted fence comes back, moved or resized fences return, removed or moved
  links reappear, renamed items and fences get their old name. Also in the tray and fence menu ("Undo: …"). Files that
  went to the recycle bin are restored from there.
- **Groups**: right-click → Group → "New group…" or an existing group. Fences in a group move together when you drag
  one of them, and "Fold group" shrinks them all to their title bars until you unfold them again.
- **Just type** to search in that fence; the search text shows top right, Esc ends it.
- Fence menu → **Sort by**: manual, name, type, date modified, size or **most used first** (NoFences counts how often
  you open something from the fence).
- **Color marks**: Shift+right-click an item → Mark, or **Ctrl+1…6** for the selected items (Ctrl+0 removes) – a
  colored dot on the icon, e.g. red for important things.
- **Preview on hover**: rest the mouse on a folder to see its contents, on pictures, PDFs and videos for a large
  preview (can be turned off in Settings → Desktop).
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
- A **shortcut per fence** (fence settings, Ctrl+Shift+F1…F12) brings just that fence to the front – even from another
  profile.
- **Fade when the mouse is far away** (Settings → Desktop): fences grow more transparent the further away the mouse is.
  Exclude single fences in their settings ("Never fade this fence").
- Tools ▸ **Move all fences to monitor ▸** moves all shown fences to the same place on another screen.

## Notes

- **Double-click** to write; **Esc** or a click outside saves.
- Lines starting with `[ ]` become checkboxes; a click ticks them and strikes the line through.
- Web addresses and paths are underlined and open on click. Text dragged onto a note is appended.
- **Formatting**: `# Heading` (also `##`, `###`), `- item` or `* item` for bullets, `> quote`, `---` for a line,
  `**bold**` and `*italic*`.
- **Ctrl+Alt+N** (changeable in Settings → Desktop) creates a note at the mouse, ready to type – from anywhere.
- Fence menu → **Reminder…**: NoFences plays a sound and shows a notification at that time – once, daily, on weekdays,
  weekly or monthly.
- **Pictures**: Ctrl+V in the editor pastes a picture from the clipboard; it shows right in the note.
- **Record voice note…** (note menu) records from the microphone; the play button in the note plays it.
- **Protect with password…** (note menu): the note is saved encrypted and locks itself after 2 minutes without use;
  double-click unlocks it. **Without the password it can't be opened any more** – not even by NoFences.
- Post-it style in yellow, pink, green, blue and orange.

## Widgets

Tray or fence menu → **New widget**. Widgets with a list scroll with the mouse wheel.

- **Clock & calendar**; days with appointments from an appointments widget carry a dot.
- **System monitor**: CPU, RAM, GPU load and temperature (NVIDIA), a graph of the last two minutes, and **FPS** if
  enabled (see below). If the graphics card stays too hot, NoFences warns you (limit in the menu: 75–90 °C or off).
- **Drives**: fill level and free space; click opens the drive.
- **Recycle bin**: drop files on it to delete them, double-click opens it, the menu empties it.
- **Playtime**: today / this week / this month / total for any game. Double-click and pick the game's exe; NoFences
  records how long it runs.
- **Countdown**: days and hours until a date; double-click to set it.
- **Weather**: current weather and three days ahead for a place you search for (data: Open-Meteo, no account needed),
  plus a **rain hint** for the next two hours ("Rain in about 20 min"), sunrise and sunset and the **moon phase**
  (tooltip with its name).
- **Now playing**: title, artist and cover of what Spotify, a browser or a media player is playing, with
  previous / play-pause / next.
- **Network**: download and upload rate with a one-minute graph, and the ping; a **speed test** with one click (measures
  about 12 seconds via speed.cloudflare.com).
- **Clipboard history**: the last 15 copied texts and **pictures**; click one to copy it again. Right-click an entry →
  **pin**: pinned entries stay on top, even after a restart. The rest is kept only while NoFences runs; passwords from
  password managers are skipped.
- **Battery**: charge, charging or not, time left (laptops), plus the battery of **controllers** (Xbox/XInput) and
  **Bluetooth devices** that report it to Windows (headsets, many mice and keyboards).
- **Games**: your installed games from Steam (with covers), Epic, GOG and the Xbox app; recently played first. Under
  each cover is the **playtime** NoFences counts automatically. Click starts a game. The menu hides games, sorts by name
  or playtime, or searches again.
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
- **To-do list**: double-click to add a to-do, optionally with a due time and repetition; click the circle to tick it
  off (repeating ones move to their next date). Due to-dos are announced even while the widget is hidden.
- **World clock**: the time in other places, with the difference to yours; double-click to choose time zones.
- **Power plan**: switch between Balanced, High performance and others with one click.
- **Steam sales**: all current sales, top sellers or popular new releases (more load when scrolling); every discounted
  game on your **wishlist** comes first. Menu → Settings: list, minimum discount, highest price, games per page and the
  account (empty = the one signed in on this PC). If the wishlist isn't public, paste its **share link** (Steam:
  wishlist → Share). Click opens the store page in Steam.
- **Game news**: announcements and patch notes of your installed Steam games.
- **Twitch live**: which of your streamers are live, with game and title; a notification when someone goes live.
- **Timer & alarm**: quick timers (buttons +1, +5, +10, +15, +30 minutes) and alarms on chosen days; they ring even
  while the widget is hidden.
- **Habits**: tick off the last 7 days, with a streak counter.
- **Time progress**: how much of the day, week, month and year has passed.
- **Autostart**: programs that start with Windows, each with a switch (like Task Manager). Entries for all users are
  dimmed, they need admin rights.
- **Web page**: a small page (dashboard, status page …) right in a fence, refreshed every 10 seconds to 15 minutes.
  Click opens it in the browser, the wheel scrolls; address, interval and zoom in the menu.

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

## More tools

- Tools ▸ **Color picker**: the screen freezes and a magnifier follows the mouse; a click copies the color as `#RRGGBB`
  (Shift+click: `rgb(…)`), Esc cancels.
- Tools ▸ **Clean up folders…**: lists what has been lying untouched for a week, month, three months or a year, biggest
  first, and moves the chosen items to the recycle bin (restorable). It starts with Downloads; **Add folder…** adds more
  (desktop, videos, a game folder …), and the list is kept. The choice **Duplicate files** finds files with the same
  content; the oldest copy counts as the original, the others are already ticked.
- Tools ▸ **QR code…**: shows a text or link (prefilled from the clipboard) as a QR code to scan with your phone; copy it
  or save it as a picture.
- Tools ▸ **Magnifier**: a round lens follows the mouse; **+/–** changes the zoom (2× to 8×), Esc or a click closes it.
- Tools ▸ **Show desktop icons**: hides and shows the Windows desktop icons with one click.

## Profiles

Group fences into profiles like "Work" and "Gaming" and switch between them in the tray (**Profile ▸**) or in
**Settings → Desktop**. Right-click a fence → **Show in profile** to assign it; a fence without a profile shows in
every profile. Fences created while a profile is active belong to it. **Ctrl+Alt+F1…F9** switch to profile 1…9,
**Ctrl+Alt+F10** shows all fences. With a profile active, tray → Profile ▸ **Wallpaper for "…"** gives it its own
wallpaper; your usual wallpaper comes back in profiles without one. **Power plan for "…" ▸** in the same menu switches
the power plan along with the profile (e.g. High performance for Gaming). **Programs for "…" ▸** starts programs when
switching to the profile (e.g. Steam and Discord for Gaming); with "Close when leaving the profile" NoFences closes them
again on the next switch – but only if it started them itself.

## Automation (Settings → Automation)

- **Switch profiles automatically**: "Gaming" while a certain program runs, "Work" on weekdays from 8 to 17 and so
  on. A running program wins over a time rule; when no rule applies any more, the previous profile comes back.
  Switching by hand ends what a rule started.
- **Full screen**: while a game, video or presentation fills a monitor, the fences on that monitor are hidden.
- **Light and dark style**: the default style changes with Windows' light/dark mode or at set times – for example
  Post-it during the day and glass at night. Fences with their own style keep it.
- The **Windows accent color** style takes its color from Settings → Personalization → Colors.
- **Break reminder**: after 30 to 120 minutes of active use, a reminder to take a break (own text possible, e.g. "Drink
  some water"). Being away for a while starts over.
- **Wallpaper by time of day**: pictures with a start time (e.g. bright from 7:00, dark from 19:00); each stays until
  the next one starts. A profile's own wallpaper takes precedence.

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
preview). There are 26 styles – glass, Windows accent color, **high contrast** (black, large bold text), Star Citizen
HUD, Retro-Arcade, Hardware, Nerd, Hobby, Work, Family, Gaming, Finance, Social, Documents, Multimedia, Music, Sport,
Photos, Travel, Cooking, Nature and Post-it in five colors.

**Own styles**: fence menu → Style ▸ **Design your own style…** (or Settings → Data & styles) opens the **style
designer**: colors, fonts, title bar, border and corners by clicking, with a live preview. "Save and use for this
fence" applies it right away. It is saved as JSON in the styles folder, where styles can also be edited by hand
(`#RRGGBB` or `#RRGGBBAA`) and shared. Own styles carry a ★.

## Settings (tray → Settings)

- **General**: language (automatic, English, Deutsch, Italiano, Français, Español), start with Windows, file
  extensions, default style, animations. **Own translations…** opens the `lang` folder with an English template: copy
  it to e.g. `nl.json` and translate it for a new language, or put single texts into `de.json` to change them. Choose
  the language again to load the files.
- **Desktop**: double-click on the desktop hides/shows fences; shortcut to bring fences to the front (Ctrl+Alt+D);
  fading far from the mouse; preview on hover; profiles; search shortcut (Ctrl+Alt+F); auto-sort.
- **Automation**: profile rules, break reminder, full screen, light and dark style, wallpaper by time of day (see above).
- **Updates**: NoFences checks GitHub and installs new versions with one click; donate.
- **FPS measurement**: see above.
- **Data & styles**: export/import fences, restore a backup (made every 12 hours), shared folder, folders.

## FAQ

**Can I delete the original after dragging an icon into a fence?**
In a link fence: no, the fence only links to it. In a folder fence the file was moved, so there is nothing left to delete.

**Windows shows a SmartScreen warning when starting NoFences.**
The exe is not code-signed yet. Click "More info" → "Run anyway".

**What goes online?**
Only what you set up: update checks (GitHub), weather (Open-Meteo), your calendar links, news feeds, prices (Yahoo
Finance), Steam sales and news (Steam), Twitch status (decapi.me), the speed test (Cloudflare) and pages in the web page
widget. Nothing else is sent anywhere.

**Where are my settings?**
Next to `NoFences.exe`, sorted into folders: `config` (fences.json, playtime), `backups`, `themes`, `media` (note
pictures, voice notes, shelf), `cache`, `logs` and `lang` (own translations). If the program folder can't be written to
(e.g. Program Files), they are in `%LocalAppData%\NoFences` instead. With a shared folder, fences and styles are there.
Settings → Data & styles → "Open data folder" shows the folder. Data of older versions is moved automatically.

**How do I uninstall?**
Settings → General: uncheck "Start with Windows"; turn off FPS measurement if used; tray → Exit; delete `NoFences.exe`
and its data folders (see above). Files in folder fences stay in their folders.
