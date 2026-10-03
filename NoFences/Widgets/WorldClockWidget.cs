using System.Globalization;
using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>Times in other time zones, with the difference to here and day/night.</summary>
    public sealed class WorldClockWidget : FenceWidget
    {
        public const string DefaultOption = "Pacific Standard Time|Los Angeles\nEastern Standard Time|New York\nGMT Standard Time|London\nTokyo Standard Time|Tokyo";

        private readonly Func<string?> getOption;
        private readonly Action<string?> setOption;

        public WorldClockWidget(Func<string?> getOption, Action<string?> setOption)
        {
            this.getOption = getOption;
            this.setOption = setOption;
        }

        public override string Type => "worldclock";

        public override int RefreshMs => 10_000;

        public sealed record Clock(TimeZoneInfo Zone, string Label);

        /// <summary>Lines of "ZoneId|Label"; unknown zones are skipped.</summary>
        public static List<Clock> Parse(string? option)
        {
            var list = new List<Clock>();
            foreach (var line in (option ?? DefaultOption).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = line.Split('|', 2);
                try
                {
                    var zone = TimeZoneInfo.FindSystemTimeZoneById(parts[0]);
                    list.Add(new Clock(zone, parts.Length > 1 && parts[1].Length > 0 ? parts[1] : ShortName(zone)));
                }
                catch (Exception) { }
            }
            return list;
        }

        public static string Format(IEnumerable<Clock> clocks) => string.Join("\n", clocks.Select(c => $"{c.Zone.Id}|{c.Label.Replace("|", "/")}"));

        /// <summary>"(UTC+09:00) Osaka, Sapporo, Tokyo" → "Osaka, Sapporo, Tokyo".</summary>
        public static string ShortName(TimeZoneInfo zone)
        {
            var name = zone.DisplayName;
            var close = name.IndexOf(')');
            return (close > 0 && name.StartsWith("(") ? name[(close + 1)..] : name).Trim();
        }

        /// <summary>"+6 h", "−5½ h", "±0" compared to local time.</summary>
        public static string Difference(TimeZoneInfo zone, DateTime utcNow)
        {
            var diff = zone.GetUtcOffset(utcNow) - TimeZoneInfo.Local.GetUtcOffset(utcNow);
            if (diff == TimeSpan.Zero)
                return "±0";
            var hours = Math.Abs(diff.TotalHours);
            var text = hours % 1 == 0 ? $"{hours:0}" : hours % 1 == 0.5 ? $"{Math.Floor(hours):0}½" : $"{hours:0.##}";
            return $"{(diff > TimeSpan.Zero ? "+" : "−")}{text} h";
        }

        public override void Draw(WidgetCanvas c)
        {
            var clocks = Parse(getOption());
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            if (clocks.Count == 0)
            {
                c.TextWrapped(Strings.WorldClockHint, c.Area.X, c.Area.Y, c.Area.Width, c.Label, 3);
                return;
            }
            var culture = new CultureInfo(Strings.Effective);
            var utc = DateTime.UtcNow;
            var today = DateTime.Today;
            using var timeFont = c.Sized(Math.Min(c.Px(26), Math.Max(c.Px(14), (c.Area.Height / (float)clocks.Count) * 0.45f)), FontStyle.Bold);
            using var small = new Font(c.Label.FontFamily, c.Label.Size * 0.84f, FontStyle.Regular, c.Label.Unit);
            var rowHeight = Math.Max(timeFont.GetHeight(c.G), line + small.GetHeight(c.G)) + c.Px(8);
            float y = c.Area.Y;
            foreach (var clock in clocks)
            {
                if (y + rowHeight > c.Area.Bottom + c.Px(6))
                    break;
                var local = TimeZoneInfo.ConvertTimeFromUtc(utc, clock.Zone);
                var day = (local.Date - today).Days;
                var night = local.Hour < 6 || local.Hour >= 21;
                c.Text(clock.Label, new RectangleF(c.Area.X, y, c.Area.Width * 0.6f, line));
                var meta = Difference(clock.Zone, utc) + (day != 0 ? (day > 0 ? $" · {Strings.AgendaTomorrow}" : $" · {Strings.WorldClockYesterday}") : "");
                c.Muted((night ? "☾ " : "") + meta, c.Area.X, y + line, small);
                c.Text(local.ToString("t", culture), new RectangleF(c.Area.X + c.Area.Width * 0.4f, y, c.Area.Width * 0.6f, rowHeight - c.Px(8)),
                    timeFont, StringAlignment.Far, StringAlignment.Center);
                y += rowHeight;
            }
        }

        public override void DoubleClick(Point p) => Edit(null);

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner) =>
            menu.Add(Strings.WorldClockSet, null, (_, _) => Edit(owner));

        private void Edit(IWin32Window? owner)
        {
            using var dialog = new WorldClockDialog(Parse(getOption()));
            if (dialog.ShowDialog(owner) == DialogResult.OK)
                setOption(Format(dialog.Clocks));
            RequestRedraw();
        }
    }

    /// <summary>Picks time zones (with an optional own label) for the world clock.</summary>
    internal sealed class WorldClockDialog : Form
    {
        private readonly ListBox chosen = new() { Width = 380, Height = 130, IntegralHeight = false };
        private readonly ComboBox zones = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 380, MaxDropDownItems = 20 };
        private readonly TextBox label = new() { Width = 200 };
        private readonly List<WorldClockWidget.Clock> clocks;
        private readonly List<TimeZoneInfo> all = TimeZoneInfo.GetSystemTimeZones().ToList();

        public List<WorldClockWidget.Clock> Clocks => clocks;

        public WorldClockDialog(List<WorldClockWidget.Clock> current)
        {
            clocks = current.ToList();
            Text = Strings.WidgetWorldClock;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(16);
            Font = SystemFonts.MessageBoxFont ?? Font;
            TopMost = true;

            zones.Items.AddRange(all.Select(z => (object)z.DisplayName).ToArray());
            zones.SelectedIndex = Math.Max(0, all.FindIndex(z => z.Id == TimeZoneInfo.Local.Id));
            zones.SelectedIndexChanged += (_, _) => label.Text = WorldClockWidget.ShortName(all[zones.SelectedIndex]);
            label.Text = WorldClockWidget.ShortName(all[zones.SelectedIndex]);
            Fill();

            var add = new Button { Text = Strings.WorldClockAdd, AutoSize = true };
            add.Click += (_, _) =>
            {
                clocks.Add(new WorldClockWidget.Clock(all[zones.SelectedIndex], label.Text.Trim().Length > 0 ? label.Text.Trim() : WorldClockWidget.ShortName(all[zones.SelectedIndex])));
                Fill();
            };
            var remove = new Button { Text = Strings.RuleRemove, AutoSize = true };
            remove.Click += (_, _) =>
            {
                if (chosen.SelectedIndex < 0)
                    return;
                clocks.RemoveAt(chosen.SelectedIndex);
                Fill();
            };

            var layout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Dock = DockStyle.Fill, WrapContents = false };
            layout.Controls.Add(chosen);
            layout.Controls.Add(remove);
            layout.Controls.Add(new Label { Text = Strings.WorldClockZone, AutoSize = true, Margin = new Padding(0, 12, 0, 2) });
            layout.Controls.Add(zones);
            var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            row.Controls.Add(new Label { Text = Strings.WorldClockLabel, AutoSize = true, Margin = new Padding(0, 7, 6, 0) });
            row.Controls.Add(label);
            row.Controls.Add(add);
            layout.Controls.Add(row);

            var ok = new Button { Text = Strings.Ok, DialogResult = DialogResult.OK, AutoSize = true };
            var cancel = new Button { Text = Strings.Cancel, DialogResult = DialogResult.Cancel, AutoSize = true };
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Bottom, Padding = new Padding(0, 12, 0, 0) };
            buttons.Controls.AddRange(new Control[] { cancel, ok });
            AcceptButton = ok;
            CancelButton = cancel;
            Controls.Add(layout);
            Controls.Add(buttons);
        }

        private void Fill()
        {
            chosen.Items.Clear();
            chosen.Items.AddRange(clocks.Select(c => (object)$"{c.Label}  –  {c.Zone.DisplayName}").ToArray());
        }
    }
}
