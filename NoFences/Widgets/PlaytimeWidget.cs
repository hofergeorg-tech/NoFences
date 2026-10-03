using System.Diagnostics;
using Microsoft.Data.Sqlite;
using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>
    /// Playtime of a game from SC Playtime's database (read-only): today, week, month, total and
    /// whether it is running right now. Works for every game SC Playtime records; the game is chosen
    /// in the widget's menu (default: the most recently played one).
    /// </summary>
    public sealed class PlaytimeWidget : FenceWidget
    {
        public const string ProjectUrl = "https://github.com/hofergeorg-tech/sc-playtime";

        private readonly Func<string?> getGame;
        private readonly Action<string?> setGame;

        private PlaytimeSummary? summary;
        private string? shownGame;
        private List<string> games = new(); // most recently played first
        private bool missing;
        private DateTime lastRead;

        /// <param name="getGame">The chosen game, null = most recently played.</param>
        public PlaytimeWidget(Func<string?> getGame, Action<string?> setGame)
        {
            this.getGame = getGame;
            this.setGame = setGame;
        }

        public static string DatabasePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SC-Playtime", "playtime.db");

        public override string Type => "playtime";

        // The live clock ticks every second; the database is read less often.
        public override int RefreshMs => 1000;

        public override void Refresh() => Refresh(force: false);

        private void Refresh(bool force)
        {
            var interval = summary?.Live == true ? TimeSpan.FromSeconds(5) : TimeSpan.FromSeconds(30);
            if (!force && summary != null && DateTime.Now - lastRead < interval)
                return;
            lastRead = DateTime.Now;

            if (!File.Exists(DatabasePath))
            {
                missing = true;
                summary = null;
                return;
            }
            try
            {
                using var connection = new SqliteConnection($"Data Source={DatabasePath};Mode=ReadOnly;Cache=Shared");
                connection.Open();
                games = ReadGames(connection);
                shownGame = getGame() ?? games.FirstOrDefault();
                summary = shownGame == null ? null : PlaytimeSummary.Compute(ReadSessions(connection, shownGame), DateTime.Now);
                missing = false;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"SC Playtime: {e.Message}");
            }
        }

        private static List<string> ReadGames(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT game FROM sessions GROUP BY game ORDER BY MAX(end) DESC";
            using var reader = command.ExecuteReader();
            var list = new List<string>();
            while (reader.Read())
                list.Add(reader.GetString(0));
            return list;
        }

        private static List<PlaySession> ReadSessions(SqliteConnection connection, string game)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT channel, start, end FROM sessions WHERE game = $game";
            command.Parameters.AddWithValue("$game", game);
            using var reader = command.ExecuteReader();
            var list = new List<PlaySession>();
            while (reader.Read())
                list.Add(new PlaySession(reader.IsDBNull(0) ? "" : reader.GetString(0), reader.GetDouble(1), reader.GetDouble(2)));
            return list;
        }

        public override void Draw(WidgetCanvas c)
        {
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            if (missing)
            {
                c.Text(Strings.PlaytimeMissing, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line * 2));
                c.Text("github.com/hofergeorg-tech/sc-playtime", new RectangleF(c.Area.X, c.Area.Y + line * 2, c.Area.Width, line));
                return;
            }
            if (summary == null || shownGame == null)
            {
                c.Text(Strings.PlaytimeNoSessions, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line));
                return;
            }

            var s = summary;
            float y = c.Area.Y;
            if (s.Live)
            {
                // Live time ticks every second between database reads
                var liveFor = s.LiveFor + (DateTime.Now - lastRead);
                c.Dot(c.Area.X, y + line / 2 - c.Px(4), c.Px(8));
                c.Text($"{s.LiveChannel ?? "LIVE"}  {liveFor:h\\:mm\\:ss}", new RectangleF(c.Area.X + c.Px(14), y, c.Area.Width - c.Px(14), line));
                y += line + c.Px(4);
            }

            using var big = c.Sized(Math.Min(c.Area.Height * 0.2f, c.Px(36)), FontStyle.Bold);
            var bigHeight = big.GetHeight(c.G);
            c.Text(PlaytimeSummary.Format(s.Today), new RectangleF(c.Area.X, y, c.Area.Width, bigHeight), big);
            y += bigHeight;
            c.Text($"{Strings.PlaytimeToday} · {shownGame}", new RectangleF(c.Area.X, y, c.Area.Width, line));
            y += line + c.Px(8);

            c.Row(ref y, Strings.PlaytimeWeek, PlaytimeSummary.Format(s.Week));
            c.Row(ref y, Strings.PlaytimeMonth, PlaytimeSummary.Format(s.Month));
            c.Row(ref y, Strings.PlaytimeTotal, PlaytimeSummary.Format(s.Total));
        }

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner)
        {
            var choose = new ToolStripMenuItem(Strings.PlaytimeGame);
            choose.DropDownItems.Add(new ToolStripMenuItem(Strings.PlaytimeLastPlayed, null, (_, _) => Choose(null)) { Checked = getGame() == null });
            foreach (var game in games)
                choose.DropDownItems.Add(new ToolStripMenuItem(game, null, (_, _) => Choose(game)) { Checked = getGame() == game });
            menu.Add(choose);
        }

        private void Choose(string? game)
        {
            setGame(game);
            Refresh(force: true);
        }

        public override void DoubleClick(Point p)
        {
            if (missing)
                try { Process.Start(new ProcessStartInfo(ProjectUrl) { UseShellExecute = true }); } catch { }
        }
    }
}
