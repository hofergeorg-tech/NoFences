using System.Drawing.Drawing2D;
using System.Text.Json;
using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>
    /// Installed games (Steam, Epic, GOG, Xbox) as a grid of covers; click starts the game. Games
    /// without a cover show their icon and name. Recently played Steam games come first.
    /// </summary>
    public sealed class GamesWidget : FenceWidget
    {
        public sealed class Options
        {
            public List<string> Hidden { get; set; } = new();
            public bool SortByName { get; set; }
        }

        private static readonly TimeSpan RescanEvery = TimeSpan.FromMinutes(30);

        private readonly Func<string?> getOption;
        private readonly Action<string?> setOption;
        private List<GameInfo> games = new();
        private readonly Dictionary<string, Bitmap?> covers = new();
        private readonly List<(RectangleF Rect, GameInfo Game)> tiles = new();
        private DateTime nextScan = DateTime.MinValue;
        private bool scanning;
        private float scroll, maxScroll;
        private GameInfo? hovered;

        public GamesWidget(Func<string?> getOption, Action<string?> setOption)
        {
            this.getOption = getOption;
            this.setOption = setOption;
            IconCache.Shared.ImageLoaded += IconsLoaded;
        }

        public override string Type => "games";

        public override int RefreshMs => 10_000;

        private void IconsLoaded(object? sender, EventArgs e) => RequestRedraw();

        private Options Settings
        {
            get
            {
                try { return JsonSerializer.Deserialize<Options>(getOption() ?? "") ?? new(); }
                catch (JsonException) { return new(); }
            }
            set => setOption(JsonSerializer.Serialize(value));
        }

        /// <summary>For the preview renderer: made-up games instead of this PC's library.</summary>
        internal void SetPreview(IEnumerable<(GameInfo Game, Bitmap? Cover)> demo)
        {
            games = demo.Select(d => d.Game).ToList();
            foreach (var (game, cover) in demo)
                covers[game.Id] = cover;
            nextScan = DateTime.MaxValue;
        }

        public override void Refresh()
        {
            if (PreviewMode)
                return;
            if (scanning || DateTime.UtcNow < nextScan)
                return;
            _ = ScanAsync();
        }

        private async Task ScanAsync()
        {
            scanning = true;
            nextScan = DateTime.UtcNow + RescanEvery;
            try
            {
                var found = await Task.Run(GameLibrary.Scan);
                // Load covers off the UI thread, shrunk to what a tile needs
                var missing = found.Where(g => g.CoverPath != null && !covers.ContainsKey(g.Id)).ToList();
                var loaded = await Task.Run(() => missing.Select(g => (g.Id, Cover: LoadCover(g.CoverPath!))).ToList());
                foreach (var (id, cover) in loaded)
                    covers[id] = cover;
                games = found;
                RequestRedraw();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"Games: {e.Message}");
            }
            finally
            {
                scanning = false;
            }
        }

        private static Bitmap? LoadCover(string path)
        {
            try
            {
                using var image = Image.FromFile(path);
                var bitmap = new Bitmap(240, 360);
                using var g = Graphics.FromImage(bitmap);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, 0, 0, 240, 360);
                return bitmap;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Visible games in display order.</summary>
        public static List<GameInfo> Arrange(IEnumerable<GameInfo> games, Options options) =>
            (options.SortByName
                ? games.OrderBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase)
                : games.OrderByDescending(g => g.LastPlayed).ThenBy(g => g.Name, StringComparer.CurrentCultureIgnoreCase))
            .Where(g => !options.Hidden.Contains(g.Id))
            .ToList();

        public override void Draw(WidgetCanvas c)
        {
            tiles.Clear();
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            var list = Arrange(games, Settings);
            if (list.Count == 0)
            {
                c.Text(scanning || games.Count == 0 && nextScan == DateTime.MinValue ? Strings.GamesSearching : Strings.GamesNone,
                    new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line * 3));
                return;
            }

            var gap = c.Px(8);
            var columns = Math.Max(1, (int)((c.Area.Width + gap) / (c.Px(84) + gap)));
            var tileWidth = (c.Area.Width - gap * (columns - 1)) / columns;
            var tileHeight = tileWidth * 1.5f;
            var rows = (list.Count + columns - 1) / columns;
            maxScroll = Math.Max(0, rows * (tileHeight + gap) - gap - c.Area.Height);
            scroll = Math.Clamp(scroll, 0, maxScroll);

            var state = c.G.Save();
            c.G.SetClip(c.Area);
            c.G.InterpolationMode = InterpolationMode.HighQualityBicubic;
            for (var i = 0; i < list.Count; i++)
            {
                var game = list[i];
                var rect = new RectangleF(c.Area.X + i % columns * (tileWidth + gap), c.Area.Y + i / columns * (tileHeight + gap) - scroll, tileWidth, tileHeight);
                if (rect.Bottom < c.Area.Top || rect.Top > c.Area.Bottom)
                    continue;
                tiles.Add((rect, game));
                DrawTile(c, rect, game, game == hovered);
            }
            c.G.Restore(state);

            if (maxScroll > 0)
            {
                // Thin scroll indicator on the right
                var track = c.Area.Height;
                var thumb = Math.Max(c.Px(20), track * c.Area.Height / (c.Area.Height + maxScroll));
                var top = c.Area.Y + (track - thumb) * (scroll / maxScroll);
                using var brush = new SolidBrush(Color.FromArgb(140, c.Theme.Accent));
                c.G.FillRectangle(brush, c.Area.Right + c.Px(3), top, c.Px(3), thumb);
            }
        }

        private void DrawTile(WidgetCanvas c, RectangleF rect, GameInfo game, bool hover)
        {
            if (covers.TryGetValue(game.Id, out var cover) && cover != null)
            {
                c.G.DrawImage(cover, rect);
            }
            else
            {
                using (var back = new SolidBrush(Color.FromArgb(70, c.Theme.HintColor)))
                    c.G.FillRectangle(back, rect);
                var iconSize = Math.Min(rect.Width * 0.5f, c.Px(48));
                var icon = game.IconPath != null ? IconCache.Shared.Get(game.IconPath, 64) : null;
                var iconRect = new RectangleF(rect.X + (rect.Width - iconSize) / 2, rect.Y + rect.Height * 0.22f, iconSize, iconSize);
                if (icon != null)
                    c.G.DrawImage(icon, iconRect);
                using var font = c.Sized(Math.Max(c.Px(10), rect.Width / 8f), FontStyle.Bold);
                using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.EllipsisWord };
                c.Theme.DrawLabel(c.G, game.Name, new RectangleF(rect.X + c.Px(4), iconRect.Bottom + c.Px(8), rect.Width - c.Px(8), rect.Bottom - iconRect.Bottom - c.Px(10)), font, format, c.S);
            }
            if (hover)
            {
                using var pen = new Pen(c.Theme.Accent, Math.Max(2, 2 * c.S));
                c.G.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
            }
        }

        private GameInfo? GameAt(Point p) => tiles.FirstOrDefault(t => t.Rect.Contains(p)).Game;

        public override bool IsClickable(Point p)
        {
            var game = GameAt(p);
            if (game != hovered)
            {
                hovered = game;
                RequestRedraw();
            }
            return game != null;
        }

        public override string? TooltipAt(Point p) => GameAt(p) is { } g ? $"{g.Name} ({SourceName(g.Source)})" : null;

        private static string SourceName(GameSource source) => source switch
        {
            GameSource.Steam => "Steam",
            GameSource.Epic => "Epic Games",
            GameSource.Gog => "GOG",
            _ => "Xbox"
        };

        public override bool Click(Point p)
        {
            if (GameAt(p) is not { } game)
                return false;
            game.Start();
            return true;
        }

        public override bool Wheel(int delta)
        {
            if (maxScroll <= 0)
                return false;
            scroll = Math.Clamp(scroll - Math.Sign(delta) * 80, 0, maxScroll);
            return true;
        }

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner)
        {
            var options = Settings;
            if (hovered is { } game)
            {
                menu.Add(Strings.GamesHide(game.Name), null, (_, _) =>
                {
                    options.Hidden.Add(game.Id);
                    Settings = options;
                    hovered = null;
                    RequestRedraw();
                });
            }
            if (options.Hidden.Count > 0)
            {
                menu.Add(Strings.GamesShowHidden(options.Hidden.Count), null, (_, _) =>
                {
                    options.Hidden.Clear();
                    Settings = options;
                    RequestRedraw();
                });
            }
            menu.Add(new ToolStripMenuItem(Strings.GamesSortByName, null, (_, _) =>
            {
                options.SortByName = !options.SortByName;
                Settings = options;
                scroll = 0;
                RequestRedraw();
            }) { Checked = options.SortByName });
            menu.Add(Strings.GamesRescan, null, (_, _) =>
            {
                nextScan = DateTime.MinValue;
                Refresh();
            });
        }

        public override void Dispose()
        {
            IconCache.Shared.ImageLoaded -= IconsLoaded;
            foreach (var cover in covers.Values)
                cover?.Dispose();
            covers.Clear();
        }
    }
}
