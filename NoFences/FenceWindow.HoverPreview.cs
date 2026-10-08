using System.Drawing.Drawing2D;
using System.Drawing.Text;
using NoFences.Model;
using NoFences.Util;
using NoFences.Win32;

namespace NoFences
{
    /// <summary>
    /// Resting the mouse on an item shows what's behind it: a folder's contents, or a large preview of
    /// an image, PDF or video. Gone as soon as the mouse moves on.
    /// </summary>
    public sealed partial class FenceWindow
    {
        private readonly System.Windows.Forms.Timer hoverPreviewTimer = new() { Interval = 700 };
        private HoverPopup? hoverPopup;

        private void InitHoverPreview()
        {
            hoverPreviewTimer.Tick += (_, _) =>
            {
                using var _ = UiWatchdog.Activity("Hover preview");
                hoverPreviewTimer.Stop();
                ShowHoverPreview();
            };
        }

        /// <summary>Called whenever the hovered item changes.</summary>
        private void RestartHoverPreview()
        {
            HideHoverPreview();
            if (hoverPath != null && app.HoverPreview && !IsWidget && !IsNote && !collapsed)
                hoverPreviewTimer.Start();
        }

        private void HideHoverPreview()
        {
            hoverPreviewTimer.Stop();
            if (hoverPopup != null)
            {
                hoverPopup.Close();
                hoverPopup.Dispose();
                hoverPopup = null;
            }
        }

        private void ShowHoverPreview()
        {
            var index = entries.FindIndex(e => e.Path == hoverPath);
            if (index < 0 || index >= itemRects.Count || MouseButtons != MouseButtons.None || appMenuOpen || Editing || !Visible)
                return;
            var entry = entries[index];
            if (!entry.IsFolder && !HoverPopup.HasPreview(entry.Path))
                return;
            var itemOnScreen = RectangleToScreen(ToClient(itemRects[index]));
            hoverPopup = new HoverPopup(entry, scale);
            hoverPopup.ShowNear(itemOnScreen);
        }
    }

    /// <summary>The preview window itself: never takes focus or clicks.</summary>
    internal sealed class HoverPopup : Form
    {
        private static readonly string[] PreviewExtensions =
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tif", ".tiff", ".heic", ".ico",
            ".pdf", ".mp4", ".mkv", ".mov", ".avi", ".wmv", ".webm"
        };

        private const int MaxFolderLines = 14;

        private readonly FenceEntry entry;
        private readonly float scale;
        private readonly List<string> children = new();
        private readonly int totalChildren;
        private readonly Font font;
        private readonly Font bold;
        private Rectangle anchor;

        public static bool HasPreview(string path) => PreviewExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

        public HoverPopup(FenceEntry entry, float scale)
        {
            this.entry = entry;
            this.scale = scale;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            BackColor = Color.FromArgb(30, 30, 34);
            var baseFont = SystemFonts.MessageBoxFont ?? Control.DefaultFont;
            font = new Font(baseFont.FontFamily, 9f * scale, GraphicsUnit.Point);
            bold = new Font(font, FontStyle.Bold);
            Font = font;

            if (entry.IsFolder)
                (children, totalChildren) = ReadFolder(entry.Path);
            IconCache.Shared.ImageLoaded += ImageLoaded;
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                const int WS_EX_TOPMOST = 0x8, WS_EX_TRANSPARENT = 0x20, WS_EX_NOACTIVATE = 0x08000000;
                cp.ExStyle |= Native.WS_EX_TOOLWINDOW | WS_EX_TOPMOST | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Native.SetCornerPreference(Handle, 2);
        }

        /// <summary>Folders first, then files; at most <see cref="MaxFolderLines"/> names plus the total.</summary>
        private static (List<string>, int) ReadFolder(string folder)
        {
            try
            {
                var all = new DirectoryInfo(folder).EnumerateFileSystemInfos()
                    .Where(f => (f.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0)
                    .Take(1000)
                    .ToList();
                var shown = all.OrderBy(f => f is FileInfo).ThenBy(f => f.Name, StringComparer.CurrentCultureIgnoreCase)
                    .Take(MaxFolderLines).Select(f => f.FullName).ToList();
                return (shown, all.Count);
            }
            catch (Exception)
            {
                return (new(), -1);
            }
        }

        private int Px(float v) => (int)Math.Round(v * scale);

        private int ThumbSize => Px(320);

        private int LineHeight => Px(22);

        public void ShowNear(Rectangle item)
        {
            anchor = item;
            Size = PopupSize();
            Place();
            Show();
        }

        private Size PopupSize() => entry.IsFolder
            ? new Size(Px(300), Px(16) + LineHeight * (1 + Math.Max(1, children.Count) + (totalChildren > children.Count ? 1 : 0)) + Px(4))
            : ThumbnailSize();

        /// <summary>Preview renderer: draws the popup without showing it.</summary>
        internal void RenderTo(Graphics g)
        {
            Size = PopupSize();
            using (var back = new SolidBrush(BackColor))
                g.FillRectangle(back, 0, 0, Width, Height);
            OnPaint(new PaintEventArgs(g, new Rectangle(Point.Empty, Size)));
        }

        private Size ThumbnailSize()
        {
            var thumb = IconCache.Shared.Get(entry.Path, ThumbSize);
            var w = thumb?.Width ?? ThumbSize;
            var h = thumb?.Height ?? ThumbSize * 3 / 4;
            return new Size(Math.Max(w, Px(160)) + Px(16), h + Px(16) + LineHeight * 2);
        }

        /// <summary>Right of the item, or left if there's no room; always on the item's monitor.</summary>
        private void Place()
        {
            var area = Screen.FromRectangle(anchor).WorkingArea;
            var x = anchor.Right + Px(8);
            if (x + Width > area.Right)
                x = anchor.Left - Px(8) - Width;
            var y = Math.Clamp(anchor.Top, area.Top, Math.Max(area.Top, area.Bottom - Height));
            Location = new Point(Math.Max(area.Left, x), y);
        }

        private void ImageLoaded(object? sender, EventArgs e)
        {
            if (IsDisposed)
                return;
            if (!entry.IsFolder)
            {
                var size = ThumbnailSize();
                if (size != Size)
                {
                    Size = size;
                    Place();
                }
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using var border = new Pen(Color.FromArgb(70, 255, 255, 255));
            g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
            using var muted = new SolidBrush(Color.FromArgb(170, 170, 178));
            var pad = Px(8);
            var name = entry.GetDisplayName(true);

            if (entry.IsFolder)
            {
                var y = pad;
                DrawLine(g, entry.Path, name, bold, Brushes.White, y);
                y += LineHeight;
                if (totalChildren <= 0)
                    g.DrawString(totalChildren == 0 ? Strings.FolderEmpty : "–", font, muted, pad + Px(22), y + Px(3));
                foreach (var child in children)
                {
                    DrawLine(g, child, Path.GetFileName(child), font, Brushes.Gainsboro, y);
                    y += LineHeight;
                }
                if (totalChildren > children.Count)
                    g.DrawString(Strings.FolderMore(totalChildren - children.Count), font, muted, pad + Px(22), y + Px(3));
                return;
            }

            var thumb = IconCache.Shared.Get(entry.Path, ThumbSize);
            var imageArea = new Rectangle(pad, pad, Width - 2 * pad, Height - 2 * pad - LineHeight * 2);
            if (thumb != null)
                g.DrawImage(thumb, imageArea.X + (imageArea.Width - thumb.Width) / 2, imageArea.Y + (imageArea.Height - thumb.Height) / 2, thumb.Width, thumb.Height);
            else
                g.DrawString("…", bold, muted, imageArea, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

            var text = new RectangleF(pad, imageArea.Bottom + Px(4), Width - 2 * pad, LineHeight);
            var format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            g.DrawString(name, bold, Brushes.White, text, format);
            text.Y += LineHeight;
            g.DrawString(Details(entry.Path), font, muted, text, format);
        }

        private void DrawLine(Graphics g, string path, string text, Font f, Brush brush, int y)
        {
            var pad = Px(8);
            var icon = IconCache.Shared.Get(path, Px(16));
            if (icon != null)
                g.DrawImage(icon, pad, y + (LineHeight - icon.Height) / 2, icon.Width, icon.Height);
            var rect = new RectangleF(pad + Px(22), y + Px(3), Width - pad * 2 - Px(22), LineHeight);
            g.DrawString(text, f, brush, rect, new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
        }

        /// <summary>"2.4 MB · 03.10.2026 14:12"</summary>
        private static string Details(string path)
        {
            try
            {
                var info = new FileInfo(path);
                return $"{FormatSize(info.Length)} · {info.LastWriteTime:g}";
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static string FormatSize(long bytes) => bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
            < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):0.#} MB",
            _ => $"{bytes / (1024.0 * 1024 * 1024):0.##} GB"
        };

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IconCache.Shared.ImageLoaded -= ImageLoaded;
                font.Dispose();
                bold.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
