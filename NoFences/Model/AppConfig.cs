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

        public List<FenceInfo> Fences { get; set; } = new();
    }
}
