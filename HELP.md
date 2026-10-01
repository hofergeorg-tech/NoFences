# NoFences Help

NoFences puts boxes ("fences") on your desktop that keep your icons organized. German version: [HILFE.md](HILFE.md).

## Getting started

- After the first start there is one empty fence. Drag files or folders onto it.
- **Right-click a fence** (title or empty space) for its menu: settings, style, lock, new fence, delete.
- **Right-click an item** for the normal Explorer menu. Shift + right-click shows the fence menu instead.
- The **tray icon** (bottom right, maybe behind the ^ arrow) has the app-wide options and "Exit".

## Two kinds of fences

- **Link fence** (default): shows links to files and folders. The files stay where they are, so they are
  still on the desktop too. Deleting the original removes it from the fence as well.
- **Folder fence**: shows the contents of a folder. Dropping files onto it **moves** them into that folder
  (hold Ctrl to copy), so they really leave the desktop. Create one via "New folder fence…".

Tip: for a tidy desktop, create a folder such as `Documents\Fences\Work` and use it as a folder fence.

## Working with items

- Double-click opens an item.
- Drag items to reorder them, onto another fence to move them there, or into Explorer.
- Mouse wheel scrolls when a fence is full.
- "Remove from fence" (link fences) only removes the link, never the file.

## Moving and resizing

- Drag the title bar to move a fence, drag the edges to resize.
- **Locked** fences can't be moved, resized or dropped onto.
- **Collapse when not hovered** shrinks the fence to its title bar until you point at it.

## Auto-sort

In a fence's settings, enter patterns under "Auto-sort from desktop", e.g. `*.pdf; *.docx`, or pick a preset
(images, documents, archives, installers, videos, music, shortcuts).

- New files that land on the desktop and match are moved into that folder fence (or linked in a link fence).
- Downloads are sorted once they are finished.
- Tray → **Tidy up desktop now** sorts the files that are already on the desktop.
- Tray → **Auto-sort new desktop files** switches it off and on.
- Only your own desktop is watched, not the shared "Public" desktop.

## Hiding fences

- **Double-click an empty spot on the desktop** to hide all fences, double-click again to show them.
  Switch this off in the tray menu.
- Tray → **Show fences** does the same; double-clicking the tray icon too.

## Styles

11 styles: Standard (glass), Star Citizen (HUD), Retro-Arcade, Hardware, Nerd, Hobby, Work, Family, Gaming,
Finance and Social.

- Tray → **Default style** sets the style for all fences.
- Fence menu → **Style** overrides it for one fence.
- Color and opacity are set in the fence settings (color only for the Standard style).

## Settings per fence

Right-click → **Fence settings…**: name, type (links/folder), folder, style, title height, icon size, background
color, opacity, auto-sort patterns, locked, collapse.

## App options (tray menu)

- **Start with Windows** (also in every fence menu).
- **Show file extensions**: like Explorer, always or never.
- **Open config folder**: where the settings are stored.
- **Exit**.

## FAQ

**Can I delete the original after dragging an icon into a fence?**
In a link fence: no, the fence only links to it. In a folder fence the file was moved, so there is nothing left to delete.

**Windows shows a SmartScreen warning when starting NoFences.**
The exe is not code-signed yet. Click "More info" → "Run anyway". Signing is planned.

**Where are my settings?**
`%LocalAppData%\NoFences\fences.json`. With an empty `portable.txt` next to `NoFences.exe` they are kept next to the exe instead.

**I lost a fence off-screen.**
Restart NoFences; fences outside all screens are moved back automatically.

**How do I uninstall?**
Tray → uncheck "Start with Windows", tray → Exit, then delete `NoFences.exe` and the folder `%LocalAppData%\NoFences`.
Files in folder fences stay in their folders.
