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
            ("countdown", () => Strings.WidgetCountdown, new Size(280, 200)),
            ("weather", () => Strings.WidgetWeather, new Size(270, 300)),
            ("media", () => Strings.WidgetMedia, new Size(330, 200)),
            ("network", () => Strings.WidgetNetwork, new Size(260, 230)),
            ("clipboard", () => Strings.WidgetClipboard, new Size(280, 300)),
            ("battery", () => Strings.WidgetBattery, new Size(220, 170)),
        };

        public static FenceWidget? Create(FenceInfo info, IFenceHost host)
        {
            // Fences created as "Star Citizen playtime" before the widget became generic
            if (info.WidgetType == "starcitizen")
            {
                info.WidgetType = "playtime";
                // The old widget stored a game name, the new one needs the game's exe
                info.WidgetOption = null;
                host.RequestSave();
            }

            return info.WidgetType switch
            {
                "clock" => new ClockWidget(),
                "system" => new SystemWidget(() => host.FpsEnabled, host.ToggleFps),
                "drives" => new DrivesWidget(),
                "recyclebin" => new RecycleBinWidget(),
                "playtime" => new PlaytimeWidget(() => info.WidgetOption, exe =>
                {
                    info.WidgetOption = exe;
                    host.RequestSave();
                }, () => host.Playtime),
                "countdown" => new CountdownWidget(() => info.WidgetOption, option =>
                {
                    info.WidgetOption = option;
                    host.RequestSave();
                }),
                "weather" => new WeatherWidget(() => info.WidgetOption, option =>
                {
                    info.WidgetOption = option;
                    // The place's name is a better title than "Weather"
                    if (WeatherPlace.FromOption(option) is { } place && (string.IsNullOrEmpty(info.Name) || info.Name == Strings.WidgetWeather || info.Name.StartsWith(Strings.WidgetWeather + " ")))
                        info.Name = $"{Strings.WidgetWeather} {place.Name}";
                    host.RequestSave();
                }),
                "media" => new MediaWidget(),
                "network" => new NetworkWidget(),
                "clipboard" => new ClipboardWidget(),
                "battery" => new BatteryWidget(),
                _ => null
            };
        }
    }
}
