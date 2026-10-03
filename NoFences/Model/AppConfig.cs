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

        public List<FenceInfo> Fences { get; set; } = new();
    }
}
