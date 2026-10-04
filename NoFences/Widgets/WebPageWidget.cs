using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>
    /// A small web page in a fence (a dashboard, a status page, a webcam …). The page is loaded in an
    /// invisible browser (Edge WebView2) and shown as a picture that is refreshed regularly; a click
    /// opens it in the normal browser, the wheel scrolls it. A real browser window inside the
    /// transparent fence would lose its colors, like any classic control there.
    /// </summary>
    public sealed class WebPageWidget : FenceWidget
    {
        public static readonly int[] IntervalChoices = { 10, 30, 60, 300, 900 };

        public sealed class Settings
        {
            public string Url { get; set; } = "";

            /// <summary>Seconds between refreshes.</summary>
            public int Seconds { get; set; } = 60;

            /// <summary>Page zoom in percent (small fences show more with 75 %).</summary>
            public int Zoom { get; set; } = 100;
        }

        private readonly Func<string?> getOption;
        private readonly Action<string?> setOption;
        private Form? host;
        private WebView2? view;
        private Bitmap? picture;
        private Size wantedSize;
        private DateTime nextRefresh;
        private bool busy, failed, missingRuntime;
        private int scrollY;
        private string? lastError;

        public WebPageWidget(Func<string?> getOption, Action<string?> setOption)
        {
            this.getOption = getOption;
            this.setOption = setOption;
        }

        public override string Type => "webpage";

        public override int RefreshMs => 1000;

        public static Settings Parse(string? option)
        {
            if (string.IsNullOrWhiteSpace(option))
                return new Settings();
            try
            {
                if (option.TrimStart().StartsWith('{'))
                    return JsonSerializer.Deserialize<Settings>(option) ?? new Settings();
            }
            catch (JsonException)
            {
            }
            return new Settings { Url = option.Trim() };
        }

        /// <summary>"example.com" → "https://example.com"; null if it isn't a web address.</summary>
        public static string? NormalizeUrl(string text)
        {
            text = text.Trim();
            if (text.Length == 0)
                return null;
            if (!text.Contains("://"))
                text = "https://" + text;
            return Uri.TryCreate(text, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) ? uri.ToString() : null;
        }

        private Settings Current => Parse(getOption());

        private void Save(Settings s) => setOption(JsonSerializer.Serialize(s));

        internal void SetPreview(Bitmap demo)
        {
            picture = demo;
            nextRefresh = DateTime.MaxValue;
        }

        /// <summary>Asks for the address (on creation and from the menu); null if cancelled.</summary>
        public static string? AskUrl(IWin32Window? owner, string? current)
        {
            using var dialog = new InputDialog(Strings.WidgetWebPage, Strings.WebPagePrompt, current ?? "https://");
            while (dialog.ShowDialog(owner) == DialogResult.OK)
            {
                if (NormalizeUrl(dialog.Value) is { } url)
                    return JsonSerializer.Serialize(new Settings { Url = url });
                MessageBox.Show(owner, Strings.WebPageInvalid, Strings.WidgetWebPage, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return null;
        }

        public override void Refresh()
        {
            if (PreviewMode || busy || missingRuntime || DateTime.Now < nextRefresh || Current.Url.Length == 0 || wantedSize.Width < 20)
                return;
            nextRefresh = DateTime.Now.AddSeconds(Math.Max(5, Current.Seconds));
            _ = CaptureAsync(reload: view != null);
        }

        private async Task CaptureAsync(bool reload)
        {
            busy = true;
            try
            {
                var settings = Current;
                if (view == null)
                    await CreateViewAsync();
                if (view?.CoreWebView2 == null)
                    return;
                if (host!.ClientSize != wantedSize)
                    host.ClientSize = wantedSize;
                view.ZoomFactor = Math.Clamp(settings.Zoom, 25, 300) / 100.0;
                if (!reload || view.Source?.ToString() != settings.Url)
                {
                    scrollY = 0;
                    await NavigateAsync(settings.Url);
                }
                else
                {
                    await NavigateAsync(null); // reload
                }
                // Give scripts a moment to draw (charts, dashboards)
                await Task.Delay(1500);
                if (scrollY > 0)
                    await view.CoreWebView2.ExecuteScriptAsync($"window.scrollTo(0, {scrollY})");
                await TakePictureAsync();
                failed = false;
            }
            catch (Exception e)
            {
                lastError = Log.Describe(e);
                Log.Write("Web page", lastError);
                failed = true;
            }
            finally
            {
                busy = false;
                RequestRedraw();
            }
        }

        private async Task CreateViewAsync()
        {
            try
            {
                CoreWebView2Environment.GetAvailableBrowserVersionString();
            }
            catch (WebView2RuntimeNotFoundException)
            {
                missingRuntime = true;
                return;
            }
            // A normal, never-activated window far off screen: WebView2 only renders in a visible window
            host = new OffscreenHost { ClientSize = wantedSize };
            view = new WebView2 { Dock = DockStyle.Fill };
            host.Controls.Add(view);
            host.Show();
            // The browser cache lives with NoFences' local data, not next to the exe
            var cache = Path.Combine(AppData.Cache, "WebView2");
            // Chromium pauses windows it thinks nobody sees; this one is never seen, but must keep drawing
            var options = new CoreWebView2EnvironmentOptions("--disable-features=CalculateNativeWinOcclusion --disable-background-timer-throttling --disable-renderer-backgrounding");
            var environment = await CoreWebView2Environment.CreateAsync(null, cache, options);
            await view.EnsureCoreWebView2Async(environment);
            view.CoreWebView2.IsMuted = true;
            view.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            // Pop-ups and new windows open in the normal browser
            view.CoreWebView2.NewWindowRequested += (_, e) => e.Handled = true;
        }

        private async Task NavigateAsync(string? url)
        {
            var done = new TaskCompletionSource<bool>();
            void Completed(object? s, CoreWebView2NavigationCompletedEventArgs e) => done.TrySetResult(e.IsSuccess);
            view!.CoreWebView2.NavigationCompleted += Completed;
            try
            {
                if (url == null)
                    view.CoreWebView2.Reload();
                else
                    view.CoreWebView2.Navigate(url);
                var finished = await Task.WhenAny(done.Task, Task.Delay(TimeSpan.FromSeconds(30)));
                if (finished != done.Task)
                    throw new TimeoutException("The page took longer than 30 seconds.");
            }
            finally
            {
                view.CoreWebView2.NavigationCompleted -= Completed;
            }
        }

        private async Task TakePictureAsync()
        {
            using var stream = new MemoryStream();
            await view!.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
            stream.Position = 0;
            var old = picture;
            using (var loaded = Image.FromStream(stream))
                picture = new Bitmap(loaded);
            old?.Dispose();
        }

        public override void Draw(WidgetCanvas c)
        {
            // The browser renders at the fence's size; a resize brings a fresh picture
            var size = new Size(Math.Max(1, c.Area.Width), Math.Max(1, c.Area.Height));
            if (size != wantedSize)
            {
                var first = wantedSize.Width < 20;
                wantedSize = size;
                if (!first)
                    nextRefresh = DateTime.Now.AddSeconds(1);
            }
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            var settings = Current;
            if (settings.Url.Length == 0 && picture == null)
            {
                c.TextWrapped(Strings.WebPageHint, c.Area.X, c.Area.Y, c.Area.Width, c.Label, 4);
                return;
            }
            if (missingRuntime)
            {
                c.TextWrapped(Strings.WebPageNoRuntime, c.Area.X, c.Area.Y, c.Area.Width, c.Label, 5);
                return;
            }
            if (picture == null)
            {
                c.Text(failed ? Strings.NewsFailed : Strings.WeatherLoading, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line));
                c.Muted(new Uri(settings.Url).Host, c.Area.X, c.Area.Y + line, c.Label);
                return;
            }
            var state = c.G.Save();
            c.G.SetClip(c.Area);
            c.G.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            var scale = Math.Min((float)c.Area.Width / picture.Width, (float)c.Area.Height / picture.Height);
            c.G.DrawImage(picture, c.Area.X, c.Area.Y, picture.Width * scale, picture.Height * scale);
            c.G.Restore(state);
            if (busy)
                c.Dot(c.Area.Right - c.Px(10), c.Area.Y + c.Px(4), c.Px(6));
        }

        public override bool IsClickable(Point p) => Current.Url.Length > 0;

        public override bool Click(Point p)
        {
            if (Current.Url.Length == 0)
                return false;
            NoFencesApp.OpenUrl(Current.Url);
            return true;
        }

        public override string? TooltipAt(Point p) => Current.Url.Length > 0 ? Strings.WebPageTooltip(Current.Url) : null;

        public override bool Wheel(int delta)
        {
            if (view?.CoreWebView2 == null || busy)
                return false;
            scrollY = Math.Max(0, scrollY - Math.Sign(delta) * Math.Max(60, wantedSize.Height / 3));
            _ = ScrollAndCaptureAsync();
            return true;
        }

        private async Task ScrollAndCaptureAsync()
        {
            busy = true;
            try
            {
                var result = await view!.CoreWebView2.ExecuteScriptAsync($"window.scrollTo(0, {scrollY}); window.scrollY");
                // At the end of the page the browser stops; remember where it really is
                if (double.TryParse(result, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var actual))
                    scrollY = (int)actual;
                await Task.Delay(150);
                await TakePictureAsync();
            }
            catch (Exception e)
            {
                Log.Write("Web page", Log.Describe(e));
            }
            finally
            {
                busy = false;
                RequestRedraw();
            }
        }

        public override void DoubleClick(Point p) => ChangeUrl(null);

        public override void AddMenuItems(ToolStripItemCollection items, IWin32Window owner)
        {
            items.Add(Strings.WebPageChange, null, (_, _) => ChangeUrl(owner));
            items.Add(Strings.WeatherUpdateNow, null, (_, _) => nextRefresh = DateTime.MinValue);
            var every = new ToolStripMenuItem(Strings.WebPageEvery);
            foreach (var seconds in IntervalChoices)
            {
                var value = seconds;
                every.DropDownItems.Add(new ToolStripMenuItem(Strings.Interval(value), null, (_, _) =>
                {
                    var s = Current;
                    s.Seconds = value;
                    Save(s);
                    nextRefresh = DateTime.Now.AddSeconds(value);
                }) { Checked = Current.Seconds == value });
            }
            items.Add(every);
            var zoom = new ToolStripMenuItem(Strings.WebPageZoom);
            foreach (var percent in new[] { 50, 67, 75, 90, 100, 125 })
            {
                var value = percent;
                zoom.DropDownItems.Add(new ToolStripMenuItem($"{value} %", null, (_, _) =>
                {
                    var s = Current;
                    s.Zoom = value;
                    Save(s);
                    nextRefresh = DateTime.MinValue;
                }) { Checked = Current.Zoom == value });
            }
            items.Add(zoom);
        }

        private void ChangeUrl(IWin32Window? owner)
        {
            if (AskUrl(owner, Current.Url) is not { } option)
                return;
            var s = Current;
            s.Url = Parse(option).Url;
            Save(s);
            picture?.Dispose();
            picture = null;
            nextRefresh = DateTime.MinValue;
            RequestRedraw();
        }

        /// <summary>Loads a page the way the widget does and saves the picture (developer check).</summary>
        internal static void Webshot(string url, string file)
        {
            // In the app Application.Run provides this; awaits must come back to this thread
            SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
            var widget = new WebPageWidget(() => JsonSerializer.Serialize(new Settings { Url = url }), _ => { }) { wantedSize = new Size(480, 360) };
            var done = false;
            widget.Invalidated = () =>
            {
                if (widget.busy)
                    return;
                if (widget.picture != null)
                    widget.picture.Save(file, System.Drawing.Imaging.ImageFormat.Png);
                else
                    Console.Error.WriteLine(widget.missingRuntime ? "WebView2 runtime missing" : "No picture: " + widget.lastError);
                done = true;
            };
            widget.Refresh();
            var until = DateTime.Now.AddSeconds(60);
            while (!done && DateTime.Now < until)
            {
                Application.DoEvents();
                Thread.Sleep(20);
            }
            widget.Dispose();
        }

        public override void Dispose()
        {
            view?.Dispose();
            host?.Dispose();
            picture?.Dispose();
        }

        /// <summary>Invisible for the user: off screen, no taskbar button, never activated.</summary>
        private sealed class OffscreenHost : Form
        {
            public OffscreenHost()
            {
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.Manual;
                Location = new Point(-32000, -32000);
            }

            protected override bool ShowWithoutActivation => true;

            protected override CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;
                    cp.ExStyle |= Win32.Native.WS_EX_TOOLWINDOW | 0x08000000; // WS_EX_NOACTIVATE
                    return cp;
                }
            }
        }
    }
}
