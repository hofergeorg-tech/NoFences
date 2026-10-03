using System.Diagnostics;
using Microsoft.Data.Sqlite;
using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>
    /// Star Citizen playtime from SC Playtime's database (read-only): today, week, month, total and
    /// whether the game is running right now.
    /// </summary>
    public sealed class StarCitizenWidget : FenceWidget
    {
        public const string ProjectUrl = "https://github.com/hofergeorg-tech/sc-playtime";
        private const string Game = "Star Citizen";

        private PlaytimeSummary? summary;
        private bool missing;
        private DateTime lastRead;

        public static string DatabasePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SC-Playtime", "playtime.db");

        public override string Type => "starcitizen";

        // The clock ticks every second while playing; the database is read less often.
        public override int RefreshMs => 1000;

        public override void Refresh()
        {
            var interval = summary?.Live == true ? TimeSpan.FromSeconds(5) : TimeSpan.FromSeconds(30);
            if (summary != null && DateTime.Now - lastRead < interval)
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
                summary = PlaytimeSummary.Compute(ReadSessions(), DateTime.Now);
                missing = false;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"SC Playtime: {e.Message}");
            }
        }

        private static List<PlaySession> ReadSessions()
        {
            // Read-only and shared, so SC Playtime can keep writing while we read.
            using var connection = new SqliteConnection($"Data Source={DatabasePath};Mode=ReadOnly;Cache=Shared");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT channel, start, end FROM sessions WHERE game = $game";
            command.Parameters.AddWithValue("$game", Game);
            using var reader = command.ExecuteReader();
            var list = new List<PlaySession>();
            while (reader.Read())
                list.Add(new PlaySession(reader.IsDBNull(0) ? "" : reader.GetString(0), reader.GetDouble(1), reader.GetDouble(2)));
            return list;
        }

        public override void Draw(WidgetCanvas c)
        {
            if (missing || summary == null)
            {
                var line = c.Label.GetHeight(c.G) + c.Px(2);
                c.Text(Strings.ScPlaytimeMissing, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line * 2));
                c.Text("github.com/hofergeorg-tech/sc-playtime", new RectangleF(c.Area.X, c.Area.Y + line * 2, c.Area.Width, line));
                return;
            }

            var s = summary;
            float y = c.Area.Y;
            if (s.Live)
            {
                // Live time ticks every second between database reads
                var liveFor = s.LiveFor + (DateTime.Now - lastRead);
                var line = c.Label.GetHeight(c.G) + c.Px(2);
                c.Dot(c.Area.X, y + line / 2 - c.Px(4), c.Px(8));
                // The channel (LIVE, PTU, …) doubles as the "running" label; fall back to "LIVE".
                var text = $"{s.LiveChannel ?? "LIVE"}  {liveFor:h\\:mm\\:ss}";
                c.Text(text, new RectangleF(c.Area.X + c.Px(14), y, c.Area.Width - c.Px(14), line));
                y += line + c.Px(4);
            }

            using var big = c.Sized(Math.Min(c.Area.Height * 0.2f, c.Px(36)), FontStyle.Bold);
            var bigHeight = big.GetHeight(c.G);
            c.Text(PlaytimeSummary.Format(s.Today), new RectangleF(c.Area.X, y, c.Area.Width, bigHeight), big);
            y += bigHeight;
            c.Text(Strings.PlaytimeToday, new RectangleF(c.Area.X, y, c.Area.Width, c.Label.GetHeight(c.G)));
            y += c.Label.GetHeight(c.G) + c.Px(10);

            c.Row(ref y, Strings.PlaytimeWeek, PlaytimeSummary.Format(s.Week));
            c.Row(ref y, Strings.PlaytimeMonth, PlaytimeSummary.Format(s.Month));
            c.Row(ref y, Strings.PlaytimeTotal, PlaytimeSummary.Format(s.Total));
        }

        public override void DoubleClick(Point p)
        {
            if (missing)
                try { Process.Start(new ProcessStartInfo(ProjectUrl) { UseShellExecute = true }); } catch { }
        }
    }
}
