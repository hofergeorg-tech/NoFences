using System.Globalization;
using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>Current weather and a three-day forecast for a chosen place (Open-Meteo, no account needed).</summary>
    public sealed class WeatherWidget : FenceWidget
    {
        private static readonly TimeSpan UpdateEvery = TimeSpan.FromMinutes(20);
        private static readonly TimeSpan RetryAfter = TimeSpan.FromMinutes(2);

        private readonly Func<string?> getOption;
        private readonly Action<string?> setOption;
        private WeatherReport? report;
        private string? reportFor;
        private DateTime nextFetch;
        private bool fetching, failed;

        public WeatherWidget(Func<string?> getOption, Action<string?> setOption)
        {
            this.getOption = getOption;
            this.setOption = setOption;
        }

        public override string Type => "weather";

        public override int RefreshMs => 5000;

        /// <summary>For the preview renderer: show a report without going online.</summary>
        internal void SetReport(WeatherReport value)
        {
            report = value;
            reportFor = getOption();
            nextFetch = DateTime.MaxValue;
        }

        public override void Refresh()
        {
            if (PreviewMode)
                return;
            var option = getOption();
            if (option != reportFor)
            {
                report = null;
                failed = false;
                nextFetch = DateTime.MinValue;
            }
            if (fetching || DateTime.UtcNow < nextFetch || WeatherPlace.FromOption(option) is not { } place)
                return;
            _ = FetchAsync(place, option);
        }

        private async Task FetchAsync(WeatherPlace place, string? option)
        {
            fetching = true;
            try
            {
                report = await WeatherService.FetchAsync(place);
                reportFor = option;
                failed = false;
                nextFetch = DateTime.UtcNow + UpdateEvery;
            }
            catch (Exception e)
            {
                Log.Write("Weather", Log.Describe(e));
                // Offline or service down: keep the last report and try again soon
                failed = true;
                reportFor = option;
                nextFetch = DateTime.UtcNow + RetryAfter;
            }
            finally
            {
                fetching = false;
            }
        }

        public static string KindName(WeatherKind kind) => Strings.WeatherKindName(kind);

        public override void Draw(WidgetCanvas c)
        {
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            var place = WeatherPlace.FromOption(getOption());
            if (place == null)
            {
                c.Text(Strings.WeatherHint, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line * 2));
                return;
            }
            if (report == null)
            {
                c.Text(failed ? Strings.WeatherOffline : Strings.WeatherLoading, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line));
                c.Text(place.Name, new RectangleF(c.Area.X, c.Area.Y + line, c.Area.Width, line));
                return;
            }

            var r = report;
            float y = c.Area.Y;
            var iconSize = Math.Min(c.Area.Width * 0.36f, Math.Min(c.Area.Height * 0.36f, c.Px(76)));
            WeatherIcon.Draw(c.G, new RectangleF(c.Area.X, y, iconSize, iconSize), r.Kind, r.IsDay, c.Ink, c.Theme.Accent);

            var textX = c.Area.X + iconSize + c.Px(10);
            var textWidth = c.Area.Right - textX;
            using var big = c.Sized(Math.Min(iconSize * 0.62f, c.Px(44)), FontStyle.Bold);
            var bigHeight = big.GetHeight(c.G);
            c.Text($"{Math.Round(r.Temperature):0}°", new RectangleF(textX, y, textWidth, bigHeight), big);
            c.Text(KindName(r.Kind), new RectangleF(textX, y + bigHeight, textWidth, line));
            y += Math.Max(iconSize, bigHeight + line) + c.Px(4);

            c.Text(place.Name, new RectangleF(c.Area.X, y, c.Area.Width, line));
            y += line;
            c.Text(Strings.WeatherDetails(Math.Round(r.FeelsLike), Math.Round(r.Wind)), new RectangleF(c.Area.X, y, c.Area.Width, line));
            y += line;

            // "Rain in about 20 min" – the most useful line when it applies, so it comes first
            if (RainForecast.Outlook(r.Precipitation, r.PlaceNow) is { } rain)
            {
                var drop = line * 0.62f;
                WeatherIcon.Drop(c.G, new RectangleF(c.Area.X + c.Px(1), y + (line - drop) / 2, drop * 0.75f, drop), c.Theme.Accent);
                c.Text(rain.Starts ? Strings.RainStarts(rain.Minutes) : Strings.RainStops(rain.Minutes), new RectangleF(c.Area.X + drop + c.Px(4), y, c.Area.Width - drop - c.Px(4), line));
                y += line;
            }

            // Sunrise/sunset and the moon
            if (r.Sunrise is DateTime rise && r.Sunset is DateTime set)
            {
                var phase = MoonPhase.Of(DateTime.UtcNow);
                var moon = line * 0.7f;
                c.Text(Strings.SunTimes(rise.ToString("HH:mm"), set.ToString("HH:mm")), new RectangleF(c.Area.X, y, c.Area.Width - moon - c.Px(44), line));
                var moonText = $"{MoonPhase.Illumination(phase) * 100:0} %";
                var textWidth2 = c.G.MeasureString(moonText, c.Label).Width;
                WeatherIcon.MoonPhaseIcon(c.G, new RectangleF(c.Area.Right - textWidth2 - moon - c.Px(4), y + (line - moon) / 2, moon, moon), phase, c.Ink);
                c.Text(moonText, new RectangleF(c.Area.Right - textWidth2, y, textWidth2, line), align: StringAlignment.Far);
                y += line;
            }
            y += c.Px(8);

            // Forecast: today and the next days, one row each
            var culture = new CultureInfo(Strings.Effective);
            var small = line - c.Px(2);
            foreach (var day in r.Days.Take(4))
            {
                if (y + line > c.Area.Bottom)
                    break;
                var name = day.Date.Date == DateTime.Today ? Strings.PlaytimeToday : day.Date.ToString("ddd", culture);
                c.Text(name, new RectangleF(c.Area.X, y, c.Area.Width * 0.4f, line));
                WeatherIcon.Draw(c.G, new RectangleF(c.Area.X + c.Area.Width * 0.4f, y, small, small), day.Kind, true, c.Ink, c.Theme.Accent);
                c.Text($"{Math.Round(day.Max):0}° / {Math.Round(day.Min):0}°", new RectangleF(c.Area.X + c.Area.Width * 0.5f, y, c.Area.Width * 0.5f, line), align: StringAlignment.Far);
                y += line + c.Px(3);
            }
        }

        public override string? TooltipAt(Point p)
        {
            var phase = MoonPhase.Of(DateTime.UtcNow);
            return report?.Sunrise != null ? Strings.MoonTooltip(Strings.MoonPhaseName(MoonPhase.Index(phase)), (int)Math.Round(MoonPhase.Illumination(phase) * 100)) : null;
        }

        public override void DoubleClick(Point p) => ChoosePlace(null);

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner)
        {
            menu.Add(Strings.WeatherChoose, null, (_, _) => ChoosePlace(owner));
            menu.Add(Strings.WeatherUpdateNow, null, (_, _) =>
            {
                nextFetch = DateTime.MinValue;
                Refresh();
            });
        }

        private void ChoosePlace(IWin32Window? owner)
        {
            var place = WeatherPlaceDialog.Choose(owner, WeatherPlace.FromOption(getOption())?.Name);
            if (place == null)
                return;
            setOption(place.ToOption());
            Refresh();
        }
    }

    /// <summary>Search a place by name and pick one of the matches.</summary>
    internal sealed class WeatherPlaceDialog : Form
    {
        private readonly TextBox query = new() { Width = 260 };
        private readonly ListBox results = new() { Width = 360, Height = 160, IntegralHeight = false };
        private readonly Label status = new() { AutoSize = true, ForeColor = SystemColors.GrayText };

        public WeatherPlace? Selected => results.SelectedItem as WeatherPlace;

        public static WeatherPlace? Choose(IWin32Window? owner, string? current)
        {
            using var dialog = new WeatherPlaceDialog(current ?? "");
            return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.Selected : null;
        }

        private WeatherPlaceDialog(string current)
        {
            Text = Strings.WidgetWeather;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(16);
            Font = SystemFonts.MessageBoxFont ?? Font;

            query.Text = current;
            var search = new Button { Text = Strings.WeatherSearch, AutoSize = true };
            search.Click += async (_, _) => await SearchAsync();
            var ok = new Button { Text = Strings.Ok, DialogResult = DialogResult.OK, AutoSize = true, Enabled = false };
            var cancel = new Button { Text = Strings.Cancel, DialogResult = DialogResult.Cancel, AutoSize = true };
            results.SelectedIndexChanged += (_, _) => ok.Enabled = Selected != null;
            results.DoubleClick += (_, _) =>
            {
                if (Selected == null)
                    return;
                DialogResult = DialogResult.OK;
                Close();
            };
            // Enter in the search box searches; Enter elsewhere confirms
            query.KeyDown += async (_, e) =>
            {
                if (e.KeyCode != Keys.Enter)
                    return;
                e.SuppressKeyPress = true;
                await SearchAsync();
            };
            CancelButton = cancel;

            var layout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Dock = DockStyle.Fill, WrapContents = false };
            layout.Controls.Add(new Label { Text = Strings.WeatherPlaceLabel, AutoSize = true });
            var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
            row.Controls.Add(query);
            row.Controls.Add(search);
            layout.Controls.Add(row);
            layout.Controls.Add(results);
            layout.Controls.Add(status);
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Bottom, Padding = new Padding(0, 12, 0, 0) };
            buttons.Controls.AddRange(new Control[] { cancel, ok });
            Controls.Add(layout);
            Controls.Add(buttons);

            Shown += async (_, _) =>
            {
                query.Focus();
                query.SelectAll();
                if (current.Length > 0)
                    await SearchAsync();
            };
        }

        private async Task SearchAsync()
        {
            var text = query.Text.Trim();
            if (text.Length < 2)
                return;
            status.Text = Strings.WeatherLoading;
            results.Items.Clear();
            try
            {
                var places = await WeatherService.SearchAsync(text);
                results.Items.AddRange(places.Cast<object>().ToArray());
                status.Text = places.Count == 0 ? Strings.WeatherNoPlace : Strings.WeatherCredit;
                if (places.Count > 0)
                    results.SelectedIndex = 0;
            }
            catch (Exception)
            {
                status.Text = Strings.WeatherOffline;
            }
        }
    }
}
