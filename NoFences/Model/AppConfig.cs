namespace NoFences.Model
{
    public class AppConfig
    {
        public int Version { get; set; } = 2;

        /// <summary>null = follow the Explorer setting "File name extensions".</summary>
        public bool? ShowExtensions { get; set; }

        /// <summary>Default theme id for fences that don't set their own.</summary>
        public string Theme { get; set; } = "default";

        /// <summary>Double-clicking empty desktop space hides/shows all fences.</summary>
        public bool DesktopDoubleClickToggle { get; set; } = true;

        /// <summary>Master switch for the per-fence auto-sort rules.</summary>
        public bool AutoSortEnabled { get; set; } = true;

        /// <summary>App version whose changelog the user has seen; a newer version shows "What's new" once.</summary>
        public string? LastSeenVersion { get; set; }

        /// <summary>Look for new releases on GitHub at startup and every few hours.</summary>
        public bool CheckForUpdates { get; set; } = true;

        /// <summary>Smooth collapsing and the styles' hover effects.</summary>
        public bool Animations { get; set; } = true;

        /// <summary>Opt-in: run the elevated FPS helper (off by default, user is told about admin rights first).</summary>
        public bool FpsHelperEnabled { get; set; }

        /// <summary>UI language: "auto" (Windows language, English if not German/Italian), "en", "de" or "it".</summary>
        public string Language { get; set; } = "auto";

        /// <summary>Shortcut that brings all fences to the front: see <see cref="PeekHotkeys"/>.</summary>
        public string PeekHotkey { get; set; } = "Ctrl+Alt+D";

        public static readonly IReadOnlyList<string> PeekHotkeys = new[] { "Ctrl+Alt+D", "Ctrl+Alt+Space", "Ctrl+Shift+D", "Off" };

        /// <summary>Fence profiles like "Work" or "Gaming", switched in the tray menu.</summary>
        public List<string> Profiles { get; set; } = new();

        /// <summary>The active profile; null = show all fences.</summary>
        public string? ActiveProfile { get; set; }

        /// <summary>Ctrl+Alt+F1…F9 switch profiles, Ctrl+Alt+F10 shows all fences.</summary>
        public bool ProfileHotkeys { get; set; } = true;

        /// <summary>Wallpaper per profile (image path); profiles without one keep the current wallpaper.</summary>
        public Dictionary<string, string> ProfileWallpapers { get; set; } = new();

        /// <summary>Wallpaper by time of day (morning, day, evening …); a profile's own wallpaper wins.</summary>
        public List<TimedWallpaper> TimedWallpapers { get; set; } = new();

        /// <summary>Programs started with a profile (profile name → programs).</summary>
        public Dictionary<string, List<ProfileProgram>> ProfilePrograms { get; set; } = new();

        /// <summary>The wallpaper from before NoFences changed it, restored for profiles without their own.</summary>
        public string? OriginalWallpaper { get; set; }

        /// <summary>Automatic profile switching (program running, time of day).</summary>
        public List<ProfileRule> ProfileRules { get; set; } = new();

        /// <summary>Hide fences on a monitor while a program runs full screen there (games, videos).</summary>
        public bool HideOnFullscreen { get; set; } = true;

        /// <summary>Switch the default style between <see cref="LightTheme"/> and <see cref="DarkTheme"/> automatically.</summary>
        public AutoThemeMode AutoTheme { get; set; }

        public string LightTheme { get; set; } = "postit";

        public string DarkTheme { get; set; } = "default";

        public string DarkFrom { get; set; } = "19:00";

        public string DarkTo { get; set; } = "07:00";

        /// <summary>Break reminder after this many minutes of active use; 0 = off.</summary>
        public int BreakReminderMinutes { get; set; }

        public static readonly int[] BreakReminderChoices = { 0, 30, 45, 60, 90, 120 };

        /// <summary>Own text for the break reminder ("Drink some water"); empty = the standard one.</summary>
        public string? BreakReminderText { get; set; }

        /// <summary>Folders the clean-up tool looks in; empty = the Downloads folder.</summary>
        public List<string> CleanupFolders { get; set; } = new();

        /// <summary>Shortcut for a new note at the mouse.</summary>
        public string QuickNoteHotkey { get; set; } = "Ctrl+Alt+N";

        public static readonly IReadOnlyList<string> QuickNoteHotkeys = new[] { "Ctrl+Alt+N", "Ctrl+Alt+Q", "Off" };

        /// <summary>Power plan (scheme GUID) per profile, switched along with the profile.</summary>
        public Dictionary<string, Guid> ProfilePowerPlans { get; set; } = new();

        /// <summary>The power plan from before a profile changed it, restored for profiles without one.</summary>
        public Guid? OriginalPowerPlan { get; set; }

        /// <summary>Shortcut for the search across all fences.</summary>
        public string SearchHotkey { get; set; } = "Ctrl+Alt+F";

        public static readonly IReadOnlyList<string> SearchHotkeys = new[] { "Ctrl+Alt+F", "Ctrl+Shift+F", "Ctrl+Alt+S", "Off" };

        /// <summary>Fences grow transparent the further away the mouse is.</summary>
        public bool FadeFences { get; set; }

        /// <summary>Hovering a folder shows its contents, hovering an image or PDF a large preview.</summary>
        public bool HoverPreview { get; set; } = true;

        /// <summary>Fence groups docked to a screen edge (sidebars and bars).</summary>
        public List<DockBar> Docks { get; set; } = new();

        public List<FenceInfo> Fences { get; set; } = new();
    }
}
