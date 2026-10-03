using System.Globalization;
using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>
    /// Upcoming appointments from one or more calendar links (.ics: Google, Outlook, iCloud …),
    /// grouped by day for the next two weeks.
    /// </summary>
    public sealed class AgendaWidget : FenceWidget
    {
        private static readonly TimeSpan UpdateEvery = TimeSpan.FromMinutes(15);

        private readonly Func<string?> getOption;
        private readonly Action<string?> setOption;
        private List<CalendarEvent> events = new();
        private string? loadedFor;
        private DateTime nextFetch;
        private bool fetching, failed;
        private float scroll, maxScroll;

        public AgendaWidget(Func<string?> getOption, Action<string?> setOption)
        {
            this.getOption = getOption;
            this.setOption = setOption;
        }

        public override string Type => "agenda";

        public override int RefreshMs => 30_000;

        public static IReadOnlyList<string> Urls(string? option) =>
            (option ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        internal void SetPreview(IEnumerable<CalendarEvent> demo)
        {
            events = demo.ToList();
            loadedFor = getOption();
            nextFetch = DateTime.MaxValue;
        }

        public override void Refresh()
        {
            if (PreviewMode)
                return;
            var option = getOption();
            if (option != loadedFor)
                nextFetch = DateTime.MinValue;
            if (fetching || DateTime.UtcNow < nextFetch || Urls(option).Count == 0)
                return;
            _ = FetchAsync(option);
        }

        private async Task FetchAsync(string? option)
        {
            fetching = true;
            try
            {
                var from = DateTime.Today;
                var to = from.AddDays(15);
                var all = new List<CalendarEvent>();
                var anyFailed = false;
                foreach (var url in Urls(option))
                {
                    try
                    {
                        var ics = await Web.Http.GetStringAsync(Web.NormalizeUrl(url));
                        all.AddRange(IcsCalendar.Parse(ics, from, to));
                    }
                    catch (Exception)
                    {
                        anyFailed = true;
                    }
                }
                events = all.OrderBy(e => e.Start).ToList();
                failed = anyFailed && all.Count == 0;
                loadedFor = option;
                nextFetch = DateTime.UtcNow + (failed ? TimeSpan.FromMinutes(2) : UpdateEvery);
                RequestRedraw();
            }
            finally
            {
                fetching = false;
            }
        }

        public override void Draw(WidgetCanvas c)
        {
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            if (Urls(getOption()).Count == 0)
            {
                c.Text(Strings.AgendaHint, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line * 3));
                return;
            }
            var now = DateTime.Now;
            var upcoming = events.Where(e => e.End > now).ToList();
            if (upcoming.Count == 0)
            {
                var text = fetching && loadedFor == null ? Strings.WeatherLoading : failed ? Strings.AgendaFailed : Strings.AgendaEmpty;
                c.Text(text, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line * 2));
                return;
            }

            var culture = new CultureInfo(Strings.Effective);
            var state = c.G.Save();
            c.G.SetClip(c.Area);
            float y = c.Area.Y - scroll;
            using var bold = c.Sized(c.Label.GetHeight(c.G) * 0.95f, FontStyle.Bold);
            DateTime? day = null;
            var timeWidth = c.Area.Width * 0.27f;
            foreach (var e in upcoming)
            {
                var eventDay = e.Start < now.Date ? now.Date : e.Start.Date;
                if (eventDay != day)
                {
                    day = eventDay;
                    if (y > c.Area.Y - scroll)
                        y += c.Px(4);
                    var heading = eventDay == now.Date ? Strings.PlaytimeToday : eventDay == now.Date.AddDays(1) ? Strings.AgendaTomorrow : eventDay.ToString("dddd, d. MMMM", culture);
                    c.Text(heading, new RectangleF(c.Area.X, y, c.Area.Width, line), bold);
                    y += line + c.Px(2);
                    using var pen = new Pen(Color.FromArgb(70, c.Theme.HintColor));
                    c.G.DrawLine(pen, c.Area.X, y - c.Px(2), c.Area.Right, y - c.Px(2));
                }
                var time = e.AllDay ? Strings.AgendaAllDay : e.Start.ToString("t", culture);
                var running = !e.AllDay && e.Start <= now && e.End > now;
                if (running)
                    c.Dot(c.Area.X, y + line / 2 - c.Px(3), c.Px(6));
                c.Text(time, new RectangleF(c.Area.X + (running ? c.Px(10) : 0), y, timeWidth, line));
                c.Text(e.Title, new RectangleF(c.Area.X + timeWidth, y, c.Area.Width - timeWidth, line));
                y += line + c.Px(1);
            }
            c.G.Restore(state);
            maxScroll = Math.Max(0, y + scroll - c.Area.Bottom);
        }

        public override bool Wheel(int delta)
        {
            if (maxScroll <= 0)
                return false;
            scroll = Math.Clamp(scroll - Math.Sign(delta) * 60, 0, maxScroll);
            return true;
        }

        public override void DoubleClick(Point p) => Edit(null);

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner)
        {
            menu.Add(Strings.AgendaSet, null, (_, _) => Edit(owner));
            menu.Add(Strings.WeatherUpdateNow, null, (_, _) =>
            {
                nextFetch = DateTime.MinValue;
                Refresh();
            });
        }

        private void Edit(IWin32Window? owner)
        {
            var urls = AskUrls(owner, getOption());
            if (urls == null)
                return;
            setOption(urls);
            scroll = 0;
            Refresh();
        }

        /// <summary>Asks for the calendar links (one per line); null if cancelled.</summary>
        public static string? AskUrls(IWin32Window? owner, string? current)
        {
            using var dialog = new TextListDialog(Strings.WidgetAgenda, Strings.AgendaPrompt, current ?? "");
            return dialog.ShowDialog(owner) == DialogResult.OK ? string.Join("\n", Urls(dialog.Value)) : null;
        }
    }

    /// <summary>A prompt with a multi-line text box (one entry per line), e.g. calendar or feed links.</summary>
    internal sealed class TextListDialog : Form
    {
        private readonly TextBox box = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Width = 460, Height = 110, AcceptsReturn = true };

        public string Value => box.Text;

        public TextListDialog(string title, string prompt, string value, IEnumerable<(string Name, string Value)>? presets = null)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(16);
            Font = SystemFonts.MessageBoxFont ?? Font;
            box.Text = value.Replace("\n", Environment.NewLine);

            var layout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Dock = DockStyle.Fill, WrapContents = false };
            layout.Controls.Add(new Label { Text = prompt, AutoSize = true, MaximumSize = new Size(460, 0), Margin = new Padding(0, 0, 0, 8) });
            if (presets != null)
            {
                var row = new FlowLayoutPanel { AutoSize = true, WrapContents = true, MaximumSize = new Size(470, 0) };
                foreach (var (name, url) in presets)
                {
                    var add = new Button { Text = "+ " + name, AutoSize = true };
                    add.Click += (_, _) =>
                    {
                        if (!box.Text.Contains(url))
                            box.AppendText((box.Text.Length > 0 && !box.Text.EndsWith(Environment.NewLine) ? Environment.NewLine : "") + url + Environment.NewLine);
                    };
                    row.Controls.Add(add);
                }
                layout.Controls.Add(row);
            }
            layout.Controls.Add(box);

            var ok = new Button { Text = Strings.Ok, DialogResult = DialogResult.OK, AutoSize = true };
            var cancel = new Button { Text = Strings.Cancel, DialogResult = DialogResult.Cancel, AutoSize = true };
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Bottom, Padding = new Padding(0, 12, 0, 0) };
            buttons.Controls.AddRange(new Control[] { cancel, ok });
            CancelButton = cancel;
            Controls.Add(layout);
            Controls.Add(buttons);
        }
    }
}
