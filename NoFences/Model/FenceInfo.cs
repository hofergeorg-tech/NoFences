namespace NoFences.Model
{
    public enum FenceKind
    {
        /// <summary>Shows a hand-picked list of links to files and folders anywhere on disk.</summary>
        Links,

        /// <summary>Mirrors the live contents of a folder; dropping files moves them into it.</summary>
        Folder,

        /// <summary>A sticky note: free text, lines starting with "[ ]" become checkboxes.</summary>
        Note,

        /// <summary>Live content instead of files: clock, system monitor, weather, media, playtime and more (see WidgetRegistry).</summary>
        Widget
    }

    public enum FenceSortMode
    {
        /// <summary>Order chosen by drag & drop.</summary>
        Manual,
        Name,
        Type,
        /// <summary>Newest first.</summary>
        Modified,
        /// <summary>Largest first.</summary>
        Size
    }

    /*
     * Property names are part of the on-disk format (JSON, and the legacy XML migration).
     * Do not rename them.
     */
    public class FenceInfo
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = "";

        public FenceKind Kind { get; set; } = FenceKind.Links;

        /// <summary>Only used for <see cref="FenceKind.Folder"/>.</summary>
        public string? FolderPath { get; set; }

        /// <summary>Window position in device pixels.</summary>
        public int PosX { get; set; }

        public int PosY { get; set; }

        /// <summary>Expanded window size in device pixels.</summary>
        public int Width { get; set; } = 300;

        public int Height { get; set; } = 300;

        public bool Locked { get; set; }

        /// <summary>
        /// Position and size per monitor setup (key: the screens' bounds). When monitors change,
        /// the fence returns to where it was the last time this setup was used.
        /// </summary>
        public Dictionary<string, int[]> Layouts { get; set; } = new();

        /// <summary>Stay above other windows instead of sitting on the desktop.</summary>
        public bool AlwaysOnTop { get; set; }

        /// <summary>Collapse to the title bar while the mouse is elsewhere.</summary>
        public bool CanMinify { get; set; }

        /// <summary>Title bar height in logical (96 dpi) pixels.</summary>
        public int TitleHeight { get; set; } = 35;

        /// <summary>Icon edge length in logical pixels.</summary>
        public int IconSize { get; set; } = 32;

        /// <summary>Background color as RGB; alpha comes from <see cref="BackgroundAlpha"/>.</summary>
        public int BackgroundColor { get; set; } = 0x000000;

        public int BackgroundAlpha { get; set; } = 100;

        /// <summary>Theme id; null = use the global default theme.</summary>
        public string? Theme { get; set; }

        /// <summary>
        /// Wildcards separated by ';' (e.g. "*.pdf; *.docx"). New desktop files matching them are
        /// moved into a folder fence, or linked into a links fence. Empty = no auto-sorting.
        /// </summary>
        public string? AutoSortPatterns { get; set; }

        public FenceSortMode SortMode { get; set; } = FenceSortMode.Manual;

        /// <summary>Widget fences: which widget ("clock", "system", "drives", "recyclebin", "starcitizen").</summary>
        public string? WidgetType { get; set; }

        /// <summary>Widget-specific choice, e.g. the game shown by the playtime widget (null = default).</summary>
        public string? WidgetOption { get; set; }

        /// <summary>Icons only, no names (quick-launch bar); names show as tooltips.</summary>
        public bool Compact { get; set; }

        /// <summary>Nothing can be dropped in, removed or renamed (e.g. "Recent files").</summary>
        public bool ReadOnly { get; set; }

        /// <summary>Show at most this many entries (0 = all), e.g. the 20 most recent files.</summary>
        public int MaxItems { get; set; }

        /// <summary>
        /// Links fences with tabs. The active tab's links always live in <see cref="Files"/>; the copy in
        /// <c>Tabs[ActiveTab]</c> is only brought up to date when switching tabs.
        /// </summary>
        public List<FenceTab> Tabs { get; set; } = new();

        public int ActiveTab { get; set; }

        /// <summary>Only on this virtual desktop (null = on all desktops).</summary>
        public Guid? VirtualDesktop { get; set; }

        /// <summary>How the note's reminder repeats after it fired.</summary>
        public Repeat ReminderRepeat { get; set; }

        /// <summary>Profiles this fence belongs to ("Work", "Gaming"); null or empty = shown in every profile.</summary>
        public List<string>? Profiles { get; set; }

        /// <summary>Whether the fence shows while <paramref name="activeProfile"/> is active (null = all fences).</summary>
        public bool InProfile(string? activeProfile) =>
            activeProfile == null || Profiles is not { Count: > 0 } || Profiles.Contains(activeProfile);

        /// <summary>Only used for <see cref="FenceKind.Note"/>; lines separated by '\n'.</summary>
        public string NoteText { get; set; } = "";

        /// <summary>Notes only: local time at which to remind the user; cleared once shown.</summary>
        public DateTime? ReminderAt { get; set; }

        /// <summary>
        /// Links fence: the entries. Folder fence: the user's preferred order of the folder's
        /// contents; entries missing from this list are appended alphabetically.
        /// </summary>
        public List<string> Files { get; set; } = new();
    }

    public class FenceTab
    {
        public string Name { get; set; } = "";

        public List<string> Files { get; set; } = new();
    }
}
