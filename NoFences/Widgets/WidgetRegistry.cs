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
            ("countdown", () => Strings.WidgetCountdown, new Size(280, 200)),
            ("weather", () => Strings.WidgetWeather, new Size(270, 300)),
            ("media", () => Strings.WidgetMedia, new Size(330, 200)),
            ("network", () => Strings.WidgetNetwork, new Size(260, 230)),
            ("clipboard", () => Strings.WidgetClipboard, new Size(280, 300)),
            ("battery", () => Strings.WidgetBattery, new Size(220, 170)),
            ("agenda", () => Strings.WidgetAgenda, new Size(300, 320)),
            ("photos", () => Strings.WidgetPhotos, new Size(360, 260)),
            ("focus", () => Strings.WidgetFocus, new Size(230, 280)),
            ("news", () => Strings.WidgetNews, new Size(340, 360)),
            ("ticker", () => Strings.WidgetTicker, new Size(320, 260)),
            ("screentime", () => Strings.WidgetScreenTime, new Size(300, 300)),
            ("audio", () => Strings.WidgetAudio, new Size(280, 220)),
            ("status", () => Strings.WidgetStatus, new Size(320, 220)),
            ("todo", () => Strings.WidgetTodo, new Size(300, 320)),
            ("worldclock", () => Strings.WidgetWorldClock, new Size(290, 260)),
            ("power", () => Strings.WidgetPower, new Size(280, 170)),
            ("gamenews", () => Strings.WidgetGameNews, new Size(340, 360)),
            ("timer", () => Strings.WidgetTimer, new Size(280, 260)),
            ("habits", () => Strings.WidgetHabits, new Size(330, 230)),
            ("progress", () => Strings.WidgetProgress, new Size(260, 230)),
            ("autostart", () => Strings.WidgetAutostart, new Size(300, 320)),
        };

        /// <summary>
        /// Widgets that were removed in 2.13 (they slowed NoFences down or relied on unofficial services).
        /// Fences of these types are taken out when the config is loaded.
        /// </summary>
        public static readonly string[] Removed = { "system", "drives", "recyclebin", "playtime", "starcitizen", "games", "steamdeals", "twitch", "webpage" };

        /// <summary>Takes fences of removed widgets out of <paramref name="fences"/> and returns them.</summary>
        public static List<FenceInfo> TakeOutRemoved(List<FenceInfo> fences)
        {
            var removed = fences.Where(f => f.Kind == FenceKind.Widget && Removed.Contains(f.WidgetType)).ToList();
            fences.RemoveAll(removed.Contains);
            return removed;
        }

        public enum Group { Time, Info, System, GamesMedia }

        /// <summary>Menu groups, in menu order, with the widget types they contain (in that order too).</summary>
        public static IReadOnlyList<(Group Group, string[] Types)> Groups { get; } = new (Group, string[])[]
        {
            (Group.Time, new[] { "clock", "worldclock", "timer", "todo", "habits", "countdown", "agenda", "focus", "progress", "screentime" }),
            (Group.Info, new[] { "weather", "news", "ticker", "status" }),
            (Group.System, new[] { "audio", "power", "network", "battery", "clipboard", "autostart" }),
            (Group.GamesMedia, new[] { "gamenews", "media", "photos" }),
        };

        public static FenceWidget? Create(FenceInfo info, IFenceHost host)
        {
            void Set(string? option)
            {
                info.WidgetOption = option;
                host.RequestSave();
            }

            return info.WidgetType switch
            {
                "clock" => new ClockWidget(),
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
                "clipboard" => new ClipboardWidget(() => info.WidgetOption, Set),
                "battery" => new BatteryWidget(),
                "agenda" => new AgendaWidget(() => info.WidgetOption, Set),
                "photos" => new PhotoWidget(() => info.WidgetOption, Set),
                "focus" => new FocusWidget(() => info.WidgetOption, Set, host.Notify, host),
                "news" => new NewsWidget(() => info.WidgetOption, Set),
                "ticker" => new TickerWidget(() => info.WidgetOption, Set),
                "audio" => new AudioWidget(),
                "status" => new StatusWidget(() => info.WidgetOption, Set),
                "todo" => new TodoWidget(() => info.WidgetOption, Set),
                "worldclock" => new WorldClockWidget(() => info.WidgetOption, Set),
                "power" => new PowerWidget(),
                "timer" => new TimerWidget(() => info.WidgetOption, Set, host.StopAlarmSound),
                "habits" => new HabitsWidget(() => info.WidgetOption, Set),
                "progress" => new ProgressWidget(),
                "autostart" => new AutostartWidget(),
                "gamenews" =>new NewsWidget(() => info.WidgetOption, Set, gameNews: true),
                "screentime" =>new ScreenTimeWidget(() => info.WidgetOption, Set, () => host.ScreenTime),
                _ => null
            };
        }
    }
}
