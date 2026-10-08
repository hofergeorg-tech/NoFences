using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>
    /// The last copied texts and images; click one to copy it again. Pinned entries stay on top and
    /// survive a restart, the rest is kept in memory only. Entries marked by password managers as
    /// "don't record" are skipped.
    /// </summary>
    public sealed class ClipboardWidget : FenceWidget
    {
        public const int MaxEntries = 15;
        private const string PinFolder = "clipboard";

        private readonly ClipboardHistory history = new(MaxEntries);
        private readonly Listener listener;
        private readonly Func<string?> getOption;
        private readonly Action<string?> setOption;
        private readonly SynchronizationContext? ui = SynchronizationContext.Current;
        private readonly List<(RectangleF Rect, ClipItem Item)> rows = new();
        private ClipItem? justCopied;
        private DateTime justCopiedUntil;
        private ClipItem? hovered;

        public ClipboardWidget(Func<string?> getOption, Action<string?> setOption)
        {
            this.getOption = getOption;
            this.setOption = setOption;
            listener = new Listener(OnClipboardChanged);
            settle.Tick += UiWatchdog.Named("Clipboard", (_, _) => StartRead());
            LoadPinned();
        }

        public override string Type => "clipboard";

        public override int RefreshMs => 1000;

        internal ClipboardHistory History => history;

        /// <summary>A read that takes longer than this (the source program hangs) no longer blocks new ones.</summary>
        private static readonly TimeSpan ReadGivesUpAfter = TimeSpan.FromSeconds(10);

        private readonly System.Windows.Forms.Timer settle = new() { Interval = 150 };
        private DateTime? readingSince;
        private bool readAgain;

        private void OnClipboardChanged()
        {
            if (PreviewMode)
                return;
            // Programs often update the clipboard several times in a row: read once it settled
            settle.Stop();
            settle.Start();
        }

        /// <summary>
        /// Reads the clipboard on its own STA thread. Many programs render the copied data only when
        /// someone asks for it, and big screenshots take a moment to convert – the fences must not wait for that.
        /// </summary>
        private void StartRead()
        {
            settle.Stop();
            if (readingSince is { } since && DateTime.Now - since < ReadGivesUpAfter)
            {
                readAgain = true;
                return;
            }
            readingSince = DateTime.Now;
            var thread = new Thread(() =>
            {
                var result = ReadClipboard();
                Post(() =>
                {
                    if (result is string text)
                        history.Add(text);
                    else if (result is ClipItem image)
                        history.AddImage(image);
                    readingSince = null;
                    if (readAgain)
                    {
                        readAgain = false;
                        StartRead();
                    }
                });
            }) { IsBackground = true, Name = "Clipboard" };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        /// <summary>The copied text, an image ready for the list, or null if there's nothing to record.</summary>
        private static object? ReadClipboard()
        {
            try
            {
                var data = Clipboard.GetDataObject();
                if (data == null || ClipboardHistory.IsExcluded(data.GetFormats()) || ClipboardHistory.HistoryForbidden(data))
                    return null;
                if (data.GetDataPresent(DataFormats.UnicodeText) && data.GetData(DataFormats.UnicodeText) is string text)
                    return text;
                if (data.GetDataPresent(DataFormats.Bitmap) && Clipboard.GetImage() is Bitmap image)
                {
                    using (image)
                        return ClipItem.FromImage(image);
                }
            }
            catch (ExternalException)
            {
                // Clipboard is busy (another app holds it); this copy is skipped
            }
            catch (Exception e)
            {
                // Never let a strange clipboard format take the app down from this thread
                Log.Write("Clipboard", Log.Describe(e));
            }
            return null;
        }

        private void Post(Action action)
        {
            if (ui == null)
                action();
            else
                ui.Post(_ =>
                {
                    action();
                    RequestRedraw();
                }, null);
        }

        #region Pinned entries

        private sealed record PinnedEntry(string? Text, string? Image);

        private void LoadPinned()
        {
            if (getOption() is not { Length: > 0 } json)
                return;
            try
            {
                foreach (var p in JsonSerializer.Deserialize<List<PinnedEntry>>(json) ?? new())
                {
                    if (p.Text != null)
                        history.AddPinned(new ClipItem { Text = p.Text });
                    else if (p.Image != null && File.Exists(AppData.Resolve(p.Image)))
                    {
                        using var image = new Bitmap(new MemoryStream(File.ReadAllBytes(AppData.Resolve(p.Image))));
                        var item = ClipItem.FromImage(image);
                        item.File = p.Image;
                        history.AddPinned(item);
                    }
                }
            }
            catch (Exception e) when (e is JsonException or IOException or ArgumentException)
            {
                Log.Write("Clipboard", Log.Describe(e));
            }
        }

        private void SavePinned()
        {
            var pinned = new List<PinnedEntry>();
            foreach (var item in history.Items.Where(i => i.Pinned))
            {
                if (item.Text != null)
                {
                    pinned.Add(new PinnedEntry(item.Text, null));
                }
                else if (item.Png != null)
                {
                    // Images are kept as files next to the config
                    if (item.File == null && AppData.Folder != null)
                    {
                        var dir = Path.Combine(AppData.Folder, PinFolder);
                        Directory.CreateDirectory(dir);
                        item.File = $"{PinFolder}/{item.Hash}.png";
                        File.WriteAllBytes(AppData.Resolve(item.File), item.Png);
                    }
                    if (item.File != null)
                        pinned.Add(new PinnedEntry(null, item.File));
                }
            }
            setOption(pinned.Count == 0 ? null : JsonSerializer.Serialize(pinned));
        }

        private void TogglePin(ClipItem item)
        {
            if (item.Pinned)
            {
                history.Unpin(item);
                if (item.File != null)
                {
                    try { File.Delete(AppData.Resolve(item.File)); } catch (IOException) { }
                    item.File = null;
                }
            }
            else
            {
                history.Pin(item);
            }
            SavePinned();
            RequestRedraw();
        }

        #endregion

        public override void Draw(WidgetCanvas c)
        {
            rows.Clear();
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            if (history.Items.Count == 0)
            {
                c.Text(Strings.ClipboardHint, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line * 3));
                return;
            }

            float y = c.Area.Y;
            foreach (var item in history.Items)
            {
                var height = item.Thumb != null ? c.Px(52) : line + c.Px(4);
                if (y + height > c.Area.Bottom)
                    break;
                var rect = new RectangleF(c.Area.X, y, c.Area.Width, height);
                if (item == justCopied && DateTime.UtcNow < justCopiedUntil)
                {
                    using var mark = new SolidBrush(Color.FromArgb(60, c.Theme.Accent));
                    c.G.FillRectangle(mark, rect);
                }
                var textWidth = rect.Width - c.Px(4) - (item.Pinned ? c.Px(16) : 0);
                if (item.Thumb != null)
                {
                    var thumbHeight = height - c.Px(6);
                    var thumbWidth = Math.Min(c.Px(90), thumbHeight * item.Thumb.Width / (float)item.Thumb.Height);
                    c.G.DrawImage(item.Thumb, rect.X + c.Px(2), y + c.Px(3), thumbWidth, thumbHeight);
                    c.Text($"{Strings.NoteImage} {item.Width} × {item.Height}", new RectangleF(rect.X + thumbWidth + c.Px(8), y + (height - line) / 2, textWidth - thumbWidth - c.Px(6), line));
                }
                else
                {
                    c.Text(ClipboardHistory.OneLine(item.Text ?? ""), new RectangleF(rect.X + c.Px(2), y + c.Px(2), textWidth, line));
                }
                if (item.Pinned)
                    DrawPin(c, rect.Right - c.Px(12), y + height / 2);
                rows.Add((rect, item));
                y += rect.Height + c.Px(2);
                using var pen = new Pen(Color.FromArgb(40, c.Theme.HintColor));
                c.G.DrawLine(pen, rect.X, y - c.Px(1), rect.Right, y - c.Px(1));
            }
        }

        /// <summary>A small drawn pin (no emoji: GDI+ shows those as boxes).</summary>
        private static void DrawPin(WidgetCanvas c, float x, float y)
        {
            using var brush = new SolidBrush(c.Theme.Accent);
            using var pen = new Pen(c.Ink, Math.Max(1, c.S * 1.5f));
            var r = c.Px(4);
            c.G.FillEllipse(brush, x - r, y - r - c.Px(3), 2 * r, 2 * r);
            c.G.DrawLine(pen, x, y + r - c.Px(3), x, y + c.Px(6));
        }

        private ClipItem? ItemAt(Point p) => rows.FirstOrDefault(r => r.Rect.Contains(p)).Item;

        public override bool IsClickable(Point p)
        {
            hovered = ItemAt(p);
            return hovered != null;
        }

        public override string? TooltipAt(Point p) => ItemAt(p) is { Text: { } text } ? (text.Length > 400 ? text[..400] + " …" : text) : null;

        public override bool Click(Point p)
        {
            var item = ItemAt(p);
            if (item == null)
                return false;
            try
            {
                if (item.Text != null)
                    Clipboard.SetText(item.Text);
                else if (item.Png != null)
                {
                    using var image = new Bitmap(new MemoryStream(item.Png));
                    Clipboard.SetImage(image);
                }
                justCopied = item;
                justCopiedUntil = DateTime.UtcNow.AddSeconds(1.5);
            }
            catch (ExternalException)
            {
            }
            return true;
        }

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner)
        {
            // The entry under the mouse when the menu opened
            if (hovered is { } item && history.Items.Contains(item))
                menu.Add(item.Pinned ? Strings.ClipboardUnpin : Strings.ClipboardPin, null, (_, _) => TogglePin(item));
            menu.Add(Strings.ClipboardClear, null, (_, _) => history.Clear());
        }

        public override void Dispose()
        {
            settle.Dispose();
            listener.Dispose();
            history.Clear(includingPinned: true);
        }

        /// <summary>Hidden window that gets WM_CLIPBOARDUPDATE.</summary>
        private sealed class Listener : NativeWindow, IDisposable
        {
            private const int WM_CLIPBOARDUPDATE = 0x031D;
            private readonly Action changed;

            public Listener(Action changed)
            {
                this.changed = changed;
                // Message-only window
                CreateHandle(new CreateParams { Parent = new IntPtr(-3) });
                AddClipboardFormatListener(Handle);
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_CLIPBOARDUPDATE)
                    changed();
                base.WndProc(ref m);
            }

            public void Dispose()
            {
                if (Handle == IntPtr.Zero)
                    return;
                RemoveClipboardFormatListener(Handle);
                DestroyHandle();
            }

            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool AddClipboardFormatListener(IntPtr hwnd);

            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
        }
    }

    /// <summary>A copied text or image.</summary>
    public sealed class ClipItem
    {
        public string? Text { get; init; }

        /// <summary>Images: the full picture as PNG, a small thumbnail and its size.</summary>
        public byte[]? Png { get; init; }

        public Bitmap? Thumb { get; init; }

        public int Width { get; init; }

        public int Height { get; init; }

        /// <summary>Identifies an image (the same screenshot copied twice is listed once).</summary>
        public string? Hash { get; init; }

        public bool Pinned { get; set; }

        /// <summary>Pinned images: where the PNG is stored (relative to the data folder).</summary>
        public string? File { get; set; }

        public static ClipItem FromImage(Image image)
        {
            using var stream = new MemoryStream();
            image.Save(stream, ImageFormat.Png);
            var png = stream.ToArray();
            var scale = Math.Min(1f, Math.Min(160f / image.Width, 96f / image.Height));
            var thumb = new Bitmap(Math.Max(1, (int)(image.Width * scale)), Math.Max(1, (int)(image.Height * scale)));
            using (var g = Graphics.FromImage(thumb))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, 0, 0, thumb.Width, thumb.Height);
            }
            return new ClipItem { Png = png, Thumb = thumb, Width = image.Width, Height = image.Height, Hash = Convert.ToHexString(SHA1.HashData(png))[..16] };
        }

        public bool SameContent(ClipItem other) => Text != null ? Text == other.Text : Hash != null && Hash == other.Hash;
    }

    /// <summary>Newest-first list of copied texts and images without duplicates; pinned ones first.</summary>
    public sealed class ClipboardHistory
    {
        private readonly int max;
        private readonly List<ClipItem> items = new();

        public ClipboardHistory(int max) => this.max = max;

        public IReadOnlyList<ClipItem> Items => items;

        /// <summary>The texts, in display order (images left out).</summary>
        public IEnumerable<string> Texts => items.Where(i => i.Text != null).Select(i => i.Text!);

        public void Add(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;
            Insert(new ClipItem { Text = text });
        }

        public void AddImage(ClipItem image) => Insert(image);

        public void AddPinned(ClipItem item)
        {
            item.Pinned = true;
            items.Insert(items.Count(i => i.Pinned), item);
        }

        private void Insert(ClipItem item)
        {
            // Pinned already: stays where it is
            if (items.Any(i => i.Pinned && i.SameContent(item)))
            {
                item.Thumb?.Dispose();
                return;
            }
            // Copying something again moves it to the top instead of listing it twice
            foreach (var old in items.Where(i => !i.Pinned && i.SameContent(item)).ToList())
                Remove(old);
            items.Insert(items.Count(i => i.Pinned), item);
            var unpinned = items.Where(i => !i.Pinned).ToList();
            foreach (var extra in unpinned.Skip(max))
                Remove(extra);
        }

        public void Pin(ClipItem item)
        {
            if (!items.Remove(item))
                return;
            AddPinned(item);
        }

        public void Unpin(ClipItem item)
        {
            if (!items.Remove(item))
                return;
            item.Pinned = false;
            items.Insert(items.Count(i => i.Pinned), item);
        }

        private void Remove(ClipItem item)
        {
            items.Remove(item);
            item.Thumb?.Dispose();
        }

        /// <summary>Forgets the history; pinned entries stay unless asked.</summary>
        public void Clear(bool includingPinned = false)
        {
            foreach (var item in items.Where(i => includingPinned || !i.Pinned).ToList())
                Remove(item);
        }

        /// <summary>Formats that password managers and Windows set for content that must not be recorded.</summary>
        public static bool IsExcluded(IEnumerable<string> formats) => formats.Any(f =>
            f is "ExcludeClipboardContentFromMonitorProcessing" or "Clipboard Viewer Ignore");

        /// <summary>"CanIncludeInClipboardHistory" = 0 also means: don't keep this (e.g. passwords).</summary>
        public static bool HistoryForbidden(IDataObject data)
        {
            if (!data.GetDataPresent("CanIncludeInClipboardHistory"))
                return false;
            return data.GetData("CanIncludeInClipboardHistory") is MemoryStream { Length: >= 4 } stream
                && BitConverter.ToInt32(stream.ToArray(), 0) == 0;
        }

        /// <summary>First line, whitespace collapsed, with "…" if there was more.</summary>
        public static string OneLine(string text)
        {
            var trimmed = text.Trim();
            var newline = trimmed.IndexOfAny(new[] { '\r', '\n' });
            var first = newline < 0 ? trimmed : trimmed[..newline].TrimEnd() + " …";
            return string.Join(' ', first.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
