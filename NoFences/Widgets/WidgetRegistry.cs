using NoFences.Util;

namespace NoFences.Widgets
{
    public static class WidgetRegistry
    {
        /// <summary>Widget types offered in the menus, with their display names and default size.</summary>
        public static IReadOnlyList<(string Type, Func<string> Name, Size DefaultSize)> Types { get; } = new (string, Func<string>, Size)[]
        {
            ("clock", () => Strings.WidgetClock, new Size(280, 320)),
            ("system", () => Strings.WidgetSystem, new Size(260, 260)),
            ("drives", () => Strings.WidgetDrives, new Size(280, 220)),
            ("recyclebin", () => Strings.WidgetRecycleBin, new Size(200, 190)),
            ("starcitizen", () => Strings.WidgetStarCitizen, new Size(260, 250)),
        };

        public static FenceWidget? Create(string? type, IFenceHost host) => type switch
        {
            "clock" => new ClockWidget(),
            "system" => new SystemWidget(() => host.FpsEnabled, host.ToggleFps),
            "drives" => new DrivesWidget(),
            "recyclebin" => new RecycleBinWidget(),
            "starcitizen" => new StarCitizenWidget(),
            _ => null
        };
    }
}
