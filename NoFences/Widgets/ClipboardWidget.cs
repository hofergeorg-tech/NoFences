using System.Runtime.InteropServices;
using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>
    /// The last copied texts; click one to copy it again. Kept in memory only (gone after a restart),
    /// and entries marked by password managers as "don't record" are skipped.
    /// </summary>
    public sealed class ClipboardWidget : FenceWidget
    {
        public const int MaxEntries = 15;

        private readonly ClipboardHistory history = new(MaxEntries);
        private readonly Listener listener;
        private readonly List<(RectangleF Rect, string Text)> rows = new();
        private string? justCopied;
        private DateTime justCopiedUntil;

        public ClipboardWidget()
        {
            listener = new Listener(OnClipboardChanged);
        }

        public override string Type => "clipboard";

        public override int RefreshMs => 1000;

        internal ClipboardHistory History => history;

        private void OnClipboardChanged()
        {
            if (PreviewMode)
                return;
            try
            {
                var data = Clipboard.GetDataObject();
                if (data == null || !data.GetDataPresent(DataFormats.UnicodeText) || ClipboardHistory.IsExcluded(data.GetFormats()) || ClipboardHistory.HistoryForbidden(data))
                    return;
                if (data.GetData(DataFormats.UnicodeText) is string text)
                    history.Add(text);
            }
            catch (ExternalException)
            {
                // Clipboard is busy (another app holds it); this copy is skipped
            }
        }

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
            foreach (var text in history.Items)
            {
                if (y + line > c.Area.Bottom)
                    break;
                var rect = new RectangleF(c.Area.X, y, c.Area.Width, line + c.Px(4));
                if (text == justCopied && DateTime.UtcNow < justCopiedUntil)
                {
                    using var mark = new SolidBrush(Color.FromArgb(60, c.Theme.Accent));
                    c.G.FillRectangle(mark, rect);
                }
                c.Text(ClipboardHistory.OneLine(text), new RectangleF(rect.X + c.Px(2), y + c.Px(2), rect.Width - c.Px(4), line));
                rows.Add((rect, text));
                y += rect.Height + c.Px(2);
                using var pen = new Pen(Color.FromArgb(40, c.Theme.HintColor));
                c.G.DrawLine(pen, rect.X, y - c.Px(1), rect.Right, y - c.Px(1));
            }
        }

        public override bool IsClickable(Point p) => rows.Any(r => r.Rect.Contains(p));

        public override bool Click(Point p)
        {
            var row = rows.FirstOrDefault(r => r.Rect.Contains(p));
            if (row.Text == null)
                return false;
            try
            {
                Clipboard.SetText(row.Text);
                justCopied = row.Text;
                justCopiedUntil = DateTime.UtcNow.AddSeconds(1.5);
            }
            catch (ExternalException)
            {
            }
            return true;
        }

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner) =>
            menu.Add(Strings.ClipboardClear, null, (_, _) => history.Clear());

        public override void Dispose() => listener.Dispose();

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

    /// <summary>Newest-first list of copied texts without duplicates.</summary>
    public sealed class ClipboardHistory
    {
        private readonly int max;
        private readonly List<string> items = new();

        public ClipboardHistory(int max) => this.max = max;

        public IReadOnlyList<string> Items => items;

        public void Add(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;
            // Copying something again moves it to the top instead of listing it twice
            items.Remove(text);
            items.Insert(0, text);
            if (items.Count > max)
                items.RemoveRange(max, items.Count - max);
        }

        public void Clear() => items.Clear();

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
