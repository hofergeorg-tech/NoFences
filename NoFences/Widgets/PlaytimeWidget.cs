using System.Diagnostics;
using NoFences.Model;
using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>
    /// Playtime of any game: pick its exe, NoFences records while it runs (see NoFencesApp.Playtime)
    /// and shows today, this week, this month, total and whether it is running right now.
    /// </summary>
    public sealed class PlaytimeWidget : FenceWidget
    {
        private readonly Func<string?> getExe;
        private readonly Action<string?> setExe;
        private readonly Func<PlaytimeLog> log;

        private PlaytimeSummary? summary;
        private string? nameFor;
        private string gameName = "";

        public PlaytimeWidget(Func<string?> getExe, Action<string?> setExe, Func<PlaytimeLog> log)
        {
            this.getExe = getExe;
            this.setExe = setExe;
            this.log = log;
        }

        public override string Type => "playtime";

        public override int RefreshMs => 1000;

        public override void Refresh()
        {
            var exe = getExe();
            summary = exe == null ? null : PlaytimeSummary.Compute(log().SessionsOf(exe), DateTime.Now);
            if (exe != null && nameFor != exe)
            {
                nameFor = exe;
                gameName = GameName(exe);
            }
        }

        /// <summary>The product name from the exe ("Elden Ring"), else its file name.</summary>
        public static string GameName(string exe)
        {
            try
            {
                var info = FileVersionInfo.GetVersionInfo(exe);
                var name = string.IsNullOrWhiteSpace(info.ProductName) ? info.FileDescription : info.ProductName;
                if (!string.IsNullOrWhiteSpace(name))
                    return name.Trim();
            }
            catch (Exception)
            {
            }
            return Path.GetFileNameWithoutExtension(exe);
        }

        public override void Draw(WidgetCanvas c)
        {
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            if (getExe() == null || summary == null)
            {
                c.Text(Strings.PlaytimeChooseHint, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line * 2));
                return;
            }

            var s = summary;
            float y = c.Area.Y;
            if (s.Live)
            {
                c.Dot(c.Area.X, y + line / 2 - c.Px(4), c.Px(8));
                c.Text($"{Strings.PlaytimeRunning}  {s.LiveFor:h\\:mm\\:ss}", new RectangleF(c.Area.X + c.Px(14), y, c.Area.Width - c.Px(14), line));
                y += line + c.Px(4);
            }

            using var big = c.Sized(Math.Min(c.Area.Height * 0.2f, c.Px(36)), FontStyle.Bold);
            var bigHeight = big.GetHeight(c.G);
            c.Text(PlaytimeSummary.Format(s.Today), new RectangleF(c.Area.X, y, c.Area.Width, bigHeight), big);
            y += bigHeight;
            c.Text($"{Strings.PlaytimeToday} · {gameName}", new RectangleF(c.Area.X, y, c.Area.Width, line));
            y += line + c.Px(8);

            c.Row(ref y, Strings.PlaytimeWeek, PlaytimeSummary.Format(s.Week));
            c.Row(ref y, Strings.PlaytimeMonth, PlaytimeSummary.Format(s.Month));
            c.Row(ref y, Strings.PlaytimeTotal, PlaytimeSummary.Format(s.Total));
        }

        public override void DoubleClick(Point p) => Choose(null);

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner) =>
            menu.Add(Strings.PlaytimeChoose, null, (_, _) => Choose(owner));

        private void Choose(IWin32Window? owner)
        {
            var exe = ChooseExe(owner, getExe());
            if (exe == null)
                return;
            setExe(exe);
            Refresh();
        }

        /// <summary>File dialog for the game's exe; null if cancelled.</summary>
        public static string? ChooseExe(IWin32Window? owner, string? current)
        {
            using var dialog = new OpenFileDialog
            {
                Title = Strings.PlaytimeChoose.TrimEnd('…'),
                Filter = Strings.PlaytimeExeFilter,
                InitialDirectory = current != null ? Path.GetDirectoryName(current) : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            };
            return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.FileName : null;
        }
    }
}
