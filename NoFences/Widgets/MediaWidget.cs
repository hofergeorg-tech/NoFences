using NoFences.Util;
using Windows.Media.Control;
using MediaManager = Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager;
using MediaSession = Windows.Media.Control.GlobalSystemMediaTransportControlsSession;

namespace NoFences.Widgets
{
    /// <summary>
    /// What's playing right now (Spotify, browsers, media player – anything that shows up in Windows'
    /// media flyout), with cover, progress and previous / play-pause / next.
    /// </summary>
    public sealed class MediaWidget : FenceWidget
    {
        private enum Button { Previous, PlayPause, Next }

        private MediaManager? manager;
        private bool connecting;
        private MediaSession? session;
        private string title = "", artist = "", source = "";
        private Image? cover;
        private string? propertiesFor;
        private DateTime nextProperties;
        private bool loadingProperties;
        private bool playing;
        private TimeSpan position, duration;
        private readonly List<(RectangleF Rect, Button Button)> buttons = new();

        public override string Type => "media";

        public override int RefreshMs => 1000;

        /// <summary>For the preview renderer: fixed content instead of the real media session.</summary>
        internal void SetPreview(string title, string artist, string source, Image? cover, TimeSpan position, TimeSpan duration)
        {
            (this.title, this.artist, this.source, this.cover, this.position, this.duration) = (title, artist, source, cover, position, duration);
            playing = true;
            preview = true;
        }

        private bool preview;

        public override void Refresh()
        {
            // The preview must never show what's really playing on this PC
            if (preview)
                return;
            if (manager == null)
            {
                if (!connecting)
                    _ = ConnectAsync();
                return;
            }
            try
            {
                var current = manager.GetCurrentSession();
                if (current?.SourceAppUserModelId != session?.SourceAppUserModelId)
                    nextProperties = DateTime.MinValue;
                session = current;
                if (session == null)
                {
                    Clear();
                    return;
                }
                source = AppName(session.SourceAppUserModelId);
                playing = session.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
                var timeline = session.GetTimelineProperties();
                duration = timeline.EndTime - timeline.StartTime;
                position = timeline.Position;
                // Apps only report the position now and then; extrapolate while playing
                if (playing && timeline.LastUpdatedTime.Year > 2000)
                    position += DateTimeOffset.Now - timeline.LastUpdatedTime;
                if (duration > TimeSpan.Zero)
                    position = TimeSpan.FromTicks(Math.Clamp(position.Ticks, 0, duration.Ticks));

                if (!loadingProperties && DateTime.UtcNow >= nextProperties)
                    _ = LoadPropertiesAsync(session);
            }
            catch (Exception)
            {
                // The session can vanish between calls (app closed)
                session = null;
                Clear();
            }
        }

        private async Task ConnectAsync()
        {
            connecting = true;
            try
            {
                manager = await MediaManager.RequestAsync();
            }
            catch (Exception)
            {
                // Media controls unavailable (e.g. N editions without the media feature pack)
            }
        }

        private async Task LoadPropertiesAsync(MediaSession s)
        {
            loadingProperties = true;
            nextProperties = DateTime.UtcNow.AddSeconds(3);
            try
            {
                var props = await s.TryGetMediaPropertiesAsync();
                var key = $"{s.SourceAppUserModelId}|{props.Title}|{props.Artist}";
                title = props.Title ?? "";
                artist = string.IsNullOrEmpty(props.Artist) ? props.AlbumArtist ?? "" : props.Artist;
                if (key != propertiesFor)
                {
                    propertiesFor = key;
                    var old = cover;
                    cover = props.Thumbnail == null ? null : await LoadCoverAsync(props.Thumbnail);
                    old?.Dispose();
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                loadingProperties = false;
            }
        }

        private static async Task<Image?> LoadCoverAsync(Windows.Storage.Streams.IRandomAccessStreamReference reference)
        {
            try
            {
                using var stream = await reference.OpenReadAsync();
                using var net = stream.AsStreamForRead();
                using var copy = new MemoryStream();
                await net.CopyToAsync(copy);
                copy.Position = 0;
                using var image = Image.FromStream(copy);
                return new Bitmap(image);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void Clear()
        {
            title = artist = source = "";
            playing = false;
            position = duration = TimeSpan.Zero;
            propertiesFor = null;
            cover?.Dispose();
            cover = null;
        }

        /// <summary>A readable name from the app's id: "Spotify.exe" → "Spotify", "MSEdge" → "Edge".</summary>
        public static string AppName(string? id)
        {
            if (string.IsNullOrEmpty(id))
                return "";
            var name = id;
            if (name.Contains('!'))
                name = name[(name.LastIndexOf('!') + 1)..];
            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                name = name[..^4];
            if (name.Contains('.'))
                name = name[(name.LastIndexOf('.') + 1)..];
            return name.ToLowerInvariant() switch
            {
                "msedge" => "Edge",
                "chrome" => "Chrome",
                "firefox" or "308046b0af4a39cb" => "Firefox",
                "zunemusic" => "Media Player",
                "spotify" => "Spotify",
                "vlc" => "VLC",
                _ => name.Length > 0 && name.All(char.IsAsciiHexDigit) ? "" : name
            };
        }

        public static string FormatTime(TimeSpan t) =>
            t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes}:{t.Seconds:00}";

        public override void Draw(WidgetCanvas c)
        {
            buttons.Clear();
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            if (title.Length == 0 && artist.Length == 0)
            {
                c.Text(Strings.MediaNothing, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line * 2));
                return;
            }

            float y = c.Area.Y;
            var controlsHeight = c.Px(30);
            var coverSize = Math.Min(c.Area.Width * 0.38f, Math.Max(0, c.Area.Height - controlsHeight - line - c.Px(16)));
            coverSize = Math.Min(coverSize, c.Px(110));
            float textX = c.Area.X;
            if (coverSize >= c.Px(32))
            {
                var coverRect = new RectangleF(c.Area.X, y, coverSize, coverSize);
                if (cover != null)
                {
                    var oldMode = c.G.InterpolationMode;
                    c.G.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    c.G.DrawImage(cover, Fit(cover.Size, coverRect));
                    c.G.InterpolationMode = oldMode;
                }
                else
                {
                    using var back = new SolidBrush(Color.FromArgb(50, c.Theme.HintColor));
                    c.G.FillRectangle(back, coverRect);
                    using var note = c.Sized(coverSize * 0.5f);
                    c.Text("♪", coverRect, note, StringAlignment.Center, StringAlignment.Center);
                }
                textX += coverSize + c.Px(10);
            }

            var textWidth = c.Area.Right - textX;
            using (var bold = c.Sized(c.Label.GetHeight(c.G) * 1.15f, FontStyle.Bold))
            {
                var h = bold.GetHeight(c.G) + c.Px(2);
                c.Text(title, new RectangleF(textX, y, textWidth, h), bold);
                y += h;
            }
            c.Text(artist, new RectangleF(textX, y, textWidth, line));
            y += line;
            if (source.Length > 0)
                c.Text(source, new RectangleF(textX, y, textWidth, line));
            y = Math.Max(y + line, c.Area.Y + coverSize) + c.Px(8);

            if (duration > TimeSpan.Zero)
            {
                c.Bar(new RectangleF(c.Area.X, y, c.Area.Width, c.Px(4)), position.TotalSeconds / duration.TotalSeconds);
                y += c.Px(6);
                c.Text(FormatTime(position), new RectangleF(c.Area.X, y, c.Area.Width / 2f, line));
                c.Text(FormatTime(duration), new RectangleF(c.Area.X + c.Area.Width / 2f, y, c.Area.Width / 2f, line), align: StringAlignment.Far);
                y += line;
            }

            // Previous · play/pause · next, centered
            var size = Math.Min(controlsHeight, Math.Max(c.Px(18), c.Area.Bottom - y));
            var gap = size * 0.9f;
            var center = c.Area.X + c.Area.Width / 2f;
            var top = Math.Min(y + c.Px(2), c.Area.Bottom - size);
            DrawButton(c, new RectangleF(center - size * 1.5f - gap, top, size, size), Button.Previous);
            DrawButton(c, new RectangleF(center - size / 2f, top, size, size), Button.PlayPause);
            DrawButton(c, new RectangleF(center + size / 2f + gap, top, size, size), Button.Next);
        }

        private static RectangleF Fit(Size image, RectangleF box)
        {
            var scale = Math.Min(box.Width / image.Width, box.Height / image.Height);
            var w = image.Width * scale;
            var h = image.Height * scale;
            return new RectangleF(box.X + (box.Width - w) / 2, box.Y + (box.Height - h) / 2, w, h);
        }

        private void DrawButton(WidgetCanvas c, RectangleF r, Button button)
        {
            buttons.Add((r, button));
            var oldMode = c.G.SmoothingMode;
            c.G.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(button == Button.PlayPause ? c.Theme.Accent : c.Ink);
            var cx = r.X + r.Width / 2;
            var cy = r.Y + r.Height / 2;
            var u = r.Width / 2;
            switch (button)
            {
                case Button.PlayPause when playing:
                    c.G.FillRectangle(brush, cx - u * 0.55f, cy - u * 0.6f, u * 0.38f, u * 1.2f);
                    c.G.FillRectangle(brush, cx + u * 0.17f, cy - u * 0.6f, u * 0.38f, u * 1.2f);
                    break;
                case Button.PlayPause:
                    c.G.FillPolygon(brush, new[] { new PointF(cx - u * 0.45f, cy - u * 0.65f), new PointF(cx + u * 0.65f, cy), new PointF(cx - u * 0.45f, cy + u * 0.65f) });
                    break;
                case Button.Previous:
                    c.G.FillRectangle(brush, cx - u * 0.6f, cy - u * 0.5f, u * 0.18f, u);
                    c.G.FillPolygon(brush, new[] { new PointF(cx + u * 0.55f, cy - u * 0.5f), new PointF(cx - u * 0.38f, cy), new PointF(cx + u * 0.55f, cy + u * 0.5f) });
                    break;
                case Button.Next:
                    c.G.FillRectangle(brush, cx + u * 0.42f, cy - u * 0.5f, u * 0.18f, u);
                    c.G.FillPolygon(brush, new[] { new PointF(cx - u * 0.55f, cy - u * 0.5f), new PointF(cx + u * 0.38f, cy), new PointF(cx - u * 0.55f, cy + u * 0.5f) });
                    break;
            }
            c.G.SmoothingMode = oldMode;
        }

        public override bool IsClickable(Point p) => session != null && buttons.Any(b => b.Rect.Contains(p));

        public override bool Click(Point p)
        {
            var hit = buttons.FirstOrDefault(b => b.Rect.Contains(p));
            if (session == null || hit.Rect.IsEmpty)
                return false;
            _ = Send(session, hit.Button);
            return true;
        }

        private async Task Send(MediaSession s, Button button)
        {
            try
            {
                _ = button switch
                {
                    Button.Previous => await s.TrySkipPreviousAsync(),
                    Button.Next => await s.TrySkipNextAsync(),
                    _ => await s.TryTogglePlayPauseAsync()
                };
                nextProperties = DateTime.MinValue;
            }
            catch (Exception)
            {
            }
        }

        public override void Dispose()
        {
            cover?.Dispose();
            cover = null;
        }
    }
}
