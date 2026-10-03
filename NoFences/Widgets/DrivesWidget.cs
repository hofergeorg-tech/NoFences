using System.Diagnostics;
using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>Drives with fill bars; USB sticks appear and disappear by themselves. Click opens the drive.</summary>
    public sealed class DrivesWidget : FenceWidget
    {
        private List<(string Root, string Name, long Free, long Total)> drives = new();
        private readonly List<(RectangleF Rect, string Root)> rows = new();

        public override string Type => "drives";

        public override int RefreshMs => 5000;

        public override void Refresh()
        {
            var list = new List<(string, string, long, long)>();
            foreach (var d in DriveInfo.GetDrives())
            {
                try
                {
                    if (!d.IsReady || d.DriveType is not (DriveType.Fixed or DriveType.Removable or DriveType.Network))
                        continue;
                    var label = string.IsNullOrWhiteSpace(d.VolumeLabel) ? Strings.DriveDefaultName(d.DriveType) : d.VolumeLabel;
                    list.Add((d.RootDirectory.FullName, $"{d.Name.TrimEnd('\\')}  {label}", d.AvailableFreeSpace, d.TotalSize));
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            drives = list;
        }

        public override void Draw(WidgetCanvas c)
        {
            rows.Clear();
            if (drives.Count == 0)
                Refresh();
            float y = c.Area.Y;
            foreach (var (root, name, free, total) in drives)
            {
                if (y > c.Area.Bottom)
                    break;
                var used = total == 0 ? 0 : 1.0 - (double)free / total;
                var rect = c.Row(ref y, name, Strings.FreeSpace(FormatSize(free)), used);
                rows.Add((rect, root));
            }
        }

        public static string FormatSize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double value = bytes;
            var unit = 0;
            while (value >= 1024 && unit < units.Length - 1)
            {
                value /= 1024;
                unit++;
            }
            return value >= 100 || unit == 0 ? $"{value:0} {units[unit]}" : $"{value:0.#} {units[unit]}";
        }

        public override bool IsClickable(Point p) => rows.Any(r => r.Rect.Contains(p));

        public override bool Click(Point p)
        {
            var row = rows.FirstOrDefault(r => r.Rect.Contains(p));
            if (row.Root == null)
                return false;
            try { Process.Start(new ProcessStartInfo(row.Root) { UseShellExecute = true }); } catch { }
            return true;
        }
    }
}
