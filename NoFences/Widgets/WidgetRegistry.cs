using NoFences.Model;
using NoFences.Util;

namespace NoFences.Widgets
{
    public static class WidgetRegistry
    {
        /// <summary>Widget types offered in the menus, with their display names and default size.</summary>
        public static IReadOnlyList<(string Type, Func<string> Name, Size DefaultSize)> Types { get; } = new (string, Func<string>, Size)[]
        {
            ("clock", () => Strings.WidgetClock, new Size(280, 320)),
            ("system", () => Strings.WidgetSystem, new Size(260, 300)),
            ("drives", () => Strings.WidgetDrives, new Size(280, 220)),
            ("recyclebin", () => Strings.WidgetRecycleBin, new Size(200, 190)),
            ("playtime", () => Strings.WidgetPlaytime, new Size(270, 260)),
        };

        public static FenceWidget? Create(FenceInfo info, IFenceHost host)
        {
            // Fences created as "Star Citizen playtime" before the widget became generic
            if (info.WidgetType == "starcitizen")
            {
                info.WidgetType = "playtime";
                info.WidgetOption ??= "Star Citizen";
                host.RequestSave();
            }

            return info.WidgetType switch
            {
                "clock" => new ClockWidget(),
                "system" => new SystemWidget(() => host.FpsEnabled, host.ToggleFps),
                "drives" => new DrivesWidget(),
                "recyclebin" => new RecycleBinWidget(),
                "playtime" => new PlaytimeWidget(() => info.WidgetOption, game =>
                {
                    info.WidgetOption = game;
                    host.RequestSave();
                }),
                _ => null
            };
        }
    }
}
