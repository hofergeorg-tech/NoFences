using System.Diagnostics;
using System.Drawing.Drawing2D;
using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>Slideshow of the pictures in a folder (and its subfolders), shuffled; click shows the next one.</summary>
    public sealed class PhotoWidget : FenceWidget
    {
        private static readonly string[] Extensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tif", ".tiff" };
        public static readonly int[] Intervals = { 10, 30, 60, 300, 900 };

        private readonly Func<string?> getOption;
        private readonly Action<string?> setOption;
        private List<string> files = new();
        private string? filesFor;
        private int index = -1;
        private Bitmap? current;
        private string? currentPath;
        private DateTime nextSwitch;
        private bool loading;

        public PhotoWidget(Func<string?> getOption, Action<string?> setOption)
        {
            this.getOption = getOption;
            this.setOption = setOption;
        }

        public override string Type => "photos";

        public override int RefreshMs => 1000;

        /// <summary>Stored as "seconds|folder".</summary>
        public static (int Seconds, string? Folder) Parse(string? option)
        {
            if (string.IsNullOrEmpty(option))
                return (60, null);
            var sep = option.IndexOf('|');
            if (sep > 0 && int.TryParse(option[..sep], out var s))
                return (s, option[(sep + 1)..]);
            return (60, option);
        }

        public static string Format(int seconds, string folder) => $"{seconds}|{folder}";

        internal void SetPreview(Bitmap image)
        {
            current = image;
            nextSwitch = DateTime.MaxValue;
            filesFor = getOption();
        }

        public override void Refresh()
        {
            if (PreviewMode)
                return;
            var (seconds, folder) = Parse(getOption());
            if (folder == null || loading)
                return;
            if (folder != filesFor)
            {
                filesFor = folder;
                _ = ScanAsync(folder);
                return;
            }
            if (DateTime.UtcNow >= nextSwitch && files.Count > 0)
                Next(seconds);
        }

        private async Task ScanAsync(string folder)
        {
            loading = true;
            try
            {
                files = await Task.Run(() =>
                {
                    try
                    {
                        var found = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
                            .Where(f => Extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                            .Take(5000)
                            .ToList();
                        return found.OrderBy(_ => Random.Shared.Next()).ToList();
                    }
                    catch (Exception)
                    {
                        return new List<string>();
                    }
                });
                index = -1;
            }
            finally
            {
                loading = false;
            }
            Next(Parse(getOption()).Seconds);
        }

        private void Next(int seconds)
        {
            nextSwitch = DateTime.UtcNow.AddSeconds(seconds);
            if (files.Count == 0 || loading)
            {
                RequestRedraw();
                return;
            }
            index = (index + 1) % files.Count;
            _ = LoadAsync(files[index]);
        }

        private async Task LoadAsync(string path)
        {
            loading = true;
            try
            {
                var image = await Task.Run(() => LoadScaled(path, 1200));
                if (image != null)
                {
                    var old = current;
                    current = image;
                    currentPath = path;
                    old?.Dispose();
                }
            }
            finally
            {
                loading = false;
            }
            RequestRedraw();
        }

        /// <summary>Loads a picture shrunk to at most <paramref name="max"/> px, turned upright by its EXIF orientation.</summary>
        private static Bitmap? LoadScaled(string path, int max)
        {
            try
            {
                using var stream = File.OpenRead(path);
                using var image = Image.FromStream(stream, false, false);
                Orient(image);
                var scale = Math.Min(1f, (float)max / Math.Max(image.Width, image.Height));
                var bitmap = new Bitmap(Math.Max(1, (int)(image.Width * scale)), Math.Max(1, (int)(image.Height * scale)));
                using var g = Graphics.FromImage(bitmap);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, 0, 0, bitmap.Width, bitmap.Height);
                return bitmap;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Photo {path}: {e.Message}");
                return null;
            }
        }

        private static void Orient(Image image)
        {
            const int OrientationId = 0x0112;
            if (!image.PropertyIdList.Contains(OrientationId))
                return;
            var flip = image.GetPropertyItem(OrientationId)?.Value?[0] switch
            {
                3 => RotateFlipType.Rotate180FlipNone,
                6 => RotateFlipType.Rotate90FlipNone,
                8 => RotateFlipType.Rotate270FlipNone,
                _ => RotateFlipType.RotateNoneFlipNone
            };
            if (flip != RotateFlipType.RotateNoneFlipNone)
                image.RotateFlip(flip);
        }

        public override void Draw(WidgetCanvas c)
        {
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            var (_, folder) = Parse(getOption());
            if (folder == null)
            {
                c.Text(Strings.PhotosHint, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line * 2));
                return;
            }
            if (current == null)
            {
                c.Text(loading ? Strings.WeatherLoading : Strings.PhotosNone, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line * 2));
                return;
            }
            // Fill the area, cropping the overflow (like a photo frame)
            var area = c.Area;
            var scale = Math.Max((float)area.Width / current.Width, (float)area.Height / current.Height);
            var w = current.Width * scale;
            var h = current.Height * scale;
            var state = c.G.Save();
            c.G.SetClip(area);
            c.G.InterpolationMode = InterpolationMode.HighQualityBicubic;
            c.G.DrawImage(current, area.X + (area.Width - w) / 2, area.Y + (area.Height - h) / 2, w, h);
            c.G.Restore(state);
        }

        public override bool IsClickable(Point p) => current != null;

        public override bool Click(Point p)
        {
            if (files.Count == 0)
                return false;
            Next(Parse(getOption()).Seconds);
            return true;
        }

        public override string? TooltipAt(Point p) => currentPath != null ? Path.GetFileName(currentPath) : null;

        public override void DoubleClick(Point p)
        {
            if (Parse(getOption()).Folder == null)
                ChooseFolder(null);
            else if (currentPath != null)
                try { Process.Start(new ProcessStartInfo(currentPath) { UseShellExecute = true }); } catch { }
        }

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner)
        {
            var (seconds, folder) = Parse(getOption());
            menu.Add(Strings.PhotosChoose, null, (_, _) => ChooseFolder(owner));
            var interval = new ToolStripMenuItem(Strings.PhotosInterval);
            foreach (var s in Intervals)
            {
                interval.DropDownItems.Add(new ToolStripMenuItem(s < 60 ? $"{s} s" : $"{s / 60} min", null, (_, _) =>
                {
                    if (folder != null)
                        setOption(Format(s, folder));
                    nextSwitch = DateTime.UtcNow.AddSeconds(s);
                }) { Checked = s == seconds });
            }
            menu.Add(interval);
            if (currentPath != null)
                menu.Add(Strings.PhotosShowFile, null, (_, _) =>
                    Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{currentPath}\"")));
        }

        private void ChooseFolder(IWin32Window? owner)
        {
            var (seconds, folder) = Parse(getOption());
            using var dialog = new FolderBrowserDialog
            {
                Description = Strings.PhotosChoose.TrimEnd('…'),
                UseDescriptionForTitle = true,
                SelectedPath = folder ?? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };
            if (dialog.ShowDialog(owner) != DialogResult.OK)
                return;
            setOption(Format(seconds, dialog.SelectedPath));
            Refresh();
        }

        public override void Dispose()
        {
            current?.Dispose();
            current = null;
        }
    }
}
