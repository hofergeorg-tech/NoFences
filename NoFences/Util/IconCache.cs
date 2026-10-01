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
            try { stamp = File.GetLastWriteTimeUtc(path).Ticks; } catch { }
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

        private static Bitmap? LoadShellImage(string path, int size)
        {
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
