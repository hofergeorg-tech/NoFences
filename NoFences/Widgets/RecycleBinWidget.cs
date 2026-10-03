using System.Diagnostics;
using System.Runtime.InteropServices;
using NoFences.Util;
using NoFences.Win32;

namespace NoFences.Widgets
{
    /// <summary>
    /// The recycle bin: full/empty icon with item count and size. Drop files on it to delete them,
    /// double-click opens it, the menu empties it.
    /// </summary>
    public sealed class RecycleBinWidget : FenceWidget
    {
        private long items, bytes;
        private Bitmap? fullIcon, emptyIcon;

        public override string Type => "recyclebin";

        public override int RefreshMs => 3000;

        public override void Refresh()
        {
            var info = new SHQUERYRBINFO { cbSize = Marshal.SizeOf<SHQUERYRBINFO>() };
            if (SHQueryRecycleBin(null, ref info) == 0)
            {
                items = info.i64NumItems;
                bytes = info.i64Size;
            }
        }

        public override void Draw(WidgetCanvas c)
        {
            var size = Math.Min(c.Px(64), Math.Min(c.Area.Width, c.Area.Height) * 0.55f);
            var icon = items > 0 ? (fullIcon ??= StockIcon(SIID_RECYCLERFULL)) : (emptyIcon ??= StockIcon(SIID_RECYCLER));
            var x = c.Area.X + (c.Area.Width - size) / 2;
            if (icon != null)
                c.G.DrawImage(icon, x, c.Area.Y, size, size);

            var y = c.Area.Y + size + c.Px(6);
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            var text = items == 0 ? Strings.RecycleEmptyState : Strings.RecycleItems(items, DrivesWidget.FormatSize(bytes));
            c.Text(text, new RectangleF(c.Area.X, y, c.Area.Width, line), align: StringAlignment.Center);
            c.Text(Strings.RecycleDropHint, new RectangleF(c.Area.X, y + line, c.Area.Width, line), align: StringAlignment.Center);
        }

        public override void DoubleClick(Point p)
        {
            try { Process.Start(new ProcessStartInfo("explorer.exe", "shell:RecycleBinFolder") { UseShellExecute = true }); } catch { }
        }

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner)
        {
            menu.Add(new ToolStripMenuItem(Strings.RecycleEmptyAction, null, (_, _) =>
            {
                // Windows asks for confirmation itself.
                SHEmptyRecycleBin(owner.Handle, null, 0);
                Refresh();
            }) { Enabled = items > 0 });
        }

        public override bool AcceptsDrop(IDataObject data) => data.GetDataPresent(DataFormats.FileDrop);

        public override void Drop(IDataObject data, IWin32Window owner)
        {
            if (data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                ShellFileOps.Recycle(owner, files);
            Refresh();
        }

        private static Bitmap? StockIcon(uint id)
        {
            var info = new SHSTOCKICONINFO { cbSize = (uint)Marshal.SizeOf<SHSTOCKICONINFO>() };
            if (SHGetStockIconInfo(id, SHGSI_ICON | SHGSI_LARGEICON, ref info) != 0 || info.hIcon == IntPtr.Zero)
                return null;
            try
            {
                using var icon = Icon.FromHandle(info.hIcon);
                return icon.ToBitmap();
            }
            finally
            {
                Native.DestroyIcon(info.hIcon);
            }
        }

        public override void Dispose()
        {
            fullIcon?.Dispose();
            emptyIcon?.Dispose();
        }

        #region Native

        private const uint SIID_RECYCLER = 31, SIID_RECYCLERFULL = 32;
        private const uint SHGSI_ICON = 0x100, SHGSI_LARGEICON = 0x0;

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        private struct SHQUERYRBINFO
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHSTOCKICONINFO
        {
            public uint cbSize;
            public IntPtr hIcon;
            public int iSysIconIndex;
            public int iIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szPath;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHQueryRecycleBin(string? rootPath, ref SHQUERYRBINFO info);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? rootPath, uint flags);

        [DllImport("shell32.dll")]
        private static extern int SHGetStockIconInfo(uint siid, uint flags, ref SHSTOCKICONINFO info);

        #endregion
    }
}
