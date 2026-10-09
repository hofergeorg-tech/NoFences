using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using NoFences.Win32;

namespace NoFences.Util
{
    /// <summary>
    /// Loads shell icons/thumbnails (the same images Explorer shows, incl. previews for images,
    /// videos, PDFs, ...) on a background STA thread and caches them. Lookups never block the UI:
    /// a miss returns null and <see cref="ImageLoaded"/> fires once the image is ready.
    /// </summary>
    public sealed class IconCache : IDisposable
    {
        public static IconCache Shared { get; } = new();

        private readonly Dictionary<string, Bitmap?> cache = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> pending = new(StringComparer.OrdinalIgnoreCase);
        private readonly BlockingCollection<(string key, string path, int size)> queue = new();
        private readonly SynchronizationContext ui;
        private readonly Thread worker;

        public event EventHandler? ImageLoaded;

        private IconCache()
        {
            ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
            worker = new Thread(WorkerLoop) { IsBackground = true, Name = "IconCache" };
            worker.SetApartmentState(ApartmentState.STA);
            worker.Start();
        }

        /// <summary>Must be called on the UI thread.</summary>
        public Bitmap? Get(string path, int size)
        {
            var key = MakeKey(path, size);
            if (cache.TryGetValue(key, out var bmp))
                return bmp;

            if (pending.Add(key))
                queue.Add((key, path, size));
            return null;
        }

        /// <summary>Drops all cached sizes of a path, e.g. after the file changed.</summary>
        public void Invalidate(string path)
        {
            var prefix = path + "|";
            foreach (var key in cache.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList())
            {
                cache[key]?.Dispose();
                cache.Remove(key);
            }
        }

        private static string MakeKey(string path, int size)
        {
            long stamp = 0;
            // Network paths: no file access (it would log on to that server), see TypeIcon
            if (!Model.NetworkPath.IsNetworkPath(path))
            {
                try { stamp = File.GetLastWriteTimeUtc(path).Ticks; } catch { }
            }
            return $"{path}|{size}|{stamp}";
        }

        private void WorkerLoop()
        {
            foreach (var (key, path, size) in queue.GetConsumingEnumerable())
            {
                Bitmap? bmp = null;
                try
                {
                    bmp = LoadShellImage(path, size);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Icon for {path} failed: {e.Message}");
                }

                ui.Post(_ =>
                {
                    pending.Remove(key);
                    // The same file at this size with an older timestamp: that image is outdated now
                    var stale = key[..(key.LastIndexOf('|') + 1)];
                    foreach (var old in cache.Keys.Where(k => k != key && k.StartsWith(stale, StringComparison.OrdinalIgnoreCase)).ToList())
                    {
                        cache[old]?.Dispose();
                        cache.Remove(old);
                    }
                    cache[key] = bmp;
                    ImageLoaded?.Invoke(this, EventArgs.Empty);
                }, null);
            }
        }

        #region Shell interop

        [ComImport, Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemImageFactory
        {
            [PreserveSig]
            int GetImage(SIZE size, int flags, out IntPtr phbm);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE
        {
            public int cx, cy;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(string path, IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory factory);

        private const int SIIGBF_RESIZETOFIT = 0x0;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHGetFileInfo(string path, uint attributes, ref SHFILEINFO info, uint size, uint flags);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr icon);

        private const uint SHGFI_ICON = 0x100, SHGFI_LARGEICON = 0x0, SHGFI_USEFILEATTRIBUTES = 0x10;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10, FILE_ATTRIBUTE_NORMAL = 0x80;

        /// <summary>
        /// The icon for the item's type (folder, .pdf …) without opening it: for network paths, where any
        /// access would make Windows log on to that server.
        /// </summary>
        internal static Bitmap? TypeIcon(string path, int size)
        {
            var isFolder = path.EndsWith('\\') || !Path.HasExtension(path);
            var info = new SHFILEINFO();
            // Only the name matters with USEFILEATTRIBUTES; a neutral one keeps the shell from looking at the server
            var name = isFolder ? "folder" : "file" + Path.GetExtension(path);
            if (SHGetFileInfo(name, isFolder ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL, ref info,
                    (uint)Marshal.SizeOf<SHFILEINFO>(), SHGFI_ICON | SHGFI_LARGEICON | SHGFI_USEFILEATTRIBUTES) == IntPtr.Zero
                || info.hIcon == IntPtr.Zero)
                return null;
            try
            {
                using var icon = Icon.FromHandle(info.hIcon);
                using var bmp = icon.ToBitmap();
                var result = new Bitmap(size, size, PixelFormat.Format32bppPArgb);
                using (var g = Graphics.FromImage(result))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(bmp, 0, 0, size, size);
                }
                return result;
            }
            finally
            {
                DestroyIcon(info.hIcon);
            }
        }

        private static Bitmap? LoadShellImage(string path, int size)
        {
            if (Model.NetworkPath.IsNetworkPath(path))
                return TypeIcon(path, size);
            SHCreateItemFromParsingName(path, IntPtr.Zero, typeof(IShellItemImageFactory).GUID, out var factory);
            try
            {
                if (factory.GetImage(new SIZE { cx = size, cy = size }, SIIGBF_RESIZETOFIT, out var hbm) != 0 || hbm == IntPtr.Zero)
                    return null;
                try
                {
                    return HBitmapToArgb(hbm);
                }
                finally
                {
                    Native.DeleteObject(hbm);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(factory);
            }
        }

        /// <summary>
        /// Image.FromHbitmap drops the alpha channel, so copy the raw (premultiplied) pixels.
        /// Thumbnails without alpha (all zero) are treated as opaque.
        /// </summary>
        private static Bitmap HBitmapToArgb(IntPtr hbm)
        {
            using var src = Image.FromHbitmap(hbm);
            if (Image.GetPixelFormatSize(src.PixelFormat) < 32)
                return new Bitmap(src);

            var rect = new Rectangle(0, 0, src.Width, src.Height);
            var data = src.LockBits(rect, ImageLockMode.ReadOnly, src.PixelFormat);
            try
            {
                var hasAlpha = false;
                unsafe
                {
                    for (var y = 0; y < data.Height && !hasAlpha; y++)
                    {
                        var row = (byte*)data.Scan0 + y * data.Stride;
                        for (var x = 0; x < data.Width; x++)
                        {
                            if (row[x * 4 + 3] != 0)
                            {
                                hasAlpha = true;
                                break;
                            }
                        }
                    }
                }

                var format = hasAlpha ? PixelFormat.Format32bppPArgb : PixelFormat.Format32bppRgb;
                using var view = new Bitmap(data.Width, data.Height, data.Stride, format, data.Scan0);
                var result = new Bitmap(data.Width, data.Height, PixelFormat.Format32bppPArgb);
                using (var g = Graphics.FromImage(result))
                    g.DrawImageUnscaled(view, 0, 0);
                return result;
            }
            finally
            {
                src.UnlockBits(data);
            }
        }

        #endregion

        public void Dispose()
        {
            queue.CompleteAdding();
        }
    }
}
