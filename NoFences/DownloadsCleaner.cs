using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using NoFences.Util;
using NoFences.Widgets;

namespace NoFences
{
    /// <summary>
    /// Shows what has been lying in the Downloads folder for a while, biggest first, and moves the chosen
    /// items to the recycle bin (so nothing is lost for good).
    /// </summary>
    public sealed class DownloadsCleaner : Form
    {
        public sealed record Entry(string Path, string Name, long Size, DateTime Modified, bool IsFolder);

        private static DownloadsCleaner? open;
        private static readonly int[] Ages = { 7, 30, 90, 365 };

        private readonly ListView list = new() { View = View.Details, CheckBoxes = true, FullRowSelect = true, Width = 560, Height = 300 };
        private readonly ComboBox age = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
        private readonly Label total = new() { AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
        private readonly Button recycle = new() { AutoSize = true };
        private readonly string folder = DownloadsFolder();
        private List<Entry> entries = new();

        public static new void Show()
        {
            if (open is { IsDisposed: false })
            {
                open.Activate();
                return;
            }
            open = new DownloadsCleaner();
            ((Form)open).Show();
        }

        /// <summary>The user's Downloads folder (also when it was moved to another drive).</summary>
        public static string DownloadsFolder()
        {
            try
            {
                if (SHGetKnownFolderPath(new Guid("374DE290-123F-4565-9164-39C4925E467B"), 0, IntPtr.Zero, out var path) == 0)
                {
                    var result = Marshal.PtrToStringUni(path);
                    Marshal.FreeCoTaskMem(path);
                    if (!string.IsNullOrEmpty(result))
                        return result;
                }
            }
            catch (Exception) { }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }

        /// <summary>Items not changed for at least <paramref name="days"/> days, biggest first.</summary>
        public static List<Entry> Old(IEnumerable<Entry> all, DateTime now, int days) =>
            all.Where(e => now - e.Modified >= TimeSpan.FromDays(days)).OrderByDescending(e => e.Size).ToList();

        private DownloadsCleaner()
        {
            Text = Strings.DownloadsTitle;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Padding = new Padding(16);
            Font = SystemFonts.MessageBoxFont ?? Font;

            list.Columns.Add(Strings.Name, 300);
            list.Columns.Add(Strings.DownloadsSize, 100, HorizontalAlignment.Right);
            list.Columns.Add(Strings.DownloadsAge, 130, HorizontalAlignment.Right);
            list.ItemChecked += (_, _) => UpdateTotal();
            age.Items.AddRange(Ages.Select(d => (object)Strings.DownloadsOlderThan(d)).ToArray());
            age.SelectedIndex = 1;
            age.SelectedIndexChanged += (_, _) => Fill();
            recycle.Text = Strings.DownloadsRecycle;
            recycle.Click += (_, _) => Recycle();
            var openFolder = new Button { Text = Strings.OpenFolder, AutoSize = true };
            openFolder.Click += (_, _) => Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
            var close = new Button { Text = Strings.Close, AutoSize = true, DialogResult = DialogResult.Cancel };
            CancelButton = close;

            var top = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            top.Controls.Add(new Label { Text = Strings.DownloadsShow, AutoSize = true, Margin = new Padding(0, 6, 6, 0) });
            top.Controls.Add(age);
            var layout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Dock = DockStyle.Fill, WrapContents = false };
            layout.Controls.Add(new Label { Text = folder, AutoSize = true, ForeColor = SystemColors.GrayText, Margin = new Padding(0, 0, 0, 6) });
            layout.Controls.Add(top);
            layout.Controls.Add(list);
            layout.Controls.Add(total);
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Bottom, Padding = new Padding(0, 12, 0, 0) };
            buttons.Controls.AddRange(new Control[] { close, recycle, openFolder });
            Controls.Add(layout);
            Controls.Add(buttons);
            Shown += async (_, _) =>
            {
                total.Text = Strings.WeatherLoading;
                entries = await Task.Run(Scan);
                Fill();
            };
        }

        private List<Entry> Scan()
        {
            var result = new List<Entry>();
            if (!Directory.Exists(folder))
                return result;
            foreach (var info in new DirectoryInfo(folder).EnumerateFileSystemInfos())
            {
                try
                {
                    if ((info.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0)
                        continue;
                    var size = info is FileInfo f ? f.Length : FolderSize((DirectoryInfo)info);
                    result.Add(new Entry(info.FullName, info.Name, size, info.LastWriteTime, info is DirectoryInfo));
                }
                catch (Exception) { }
            }
            return result;
        }

        private static long FolderSize(DirectoryInfo dir)
        {
            long size = 0;
            try
            {
                foreach (var f in dir.EnumerateFiles("*", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true }))
                    size += f.Length;
            }
            catch (Exception) { }
            return size;
        }

        private void Fill()
        {
            var days = Ages[Math.Max(0, age.SelectedIndex)];
            var now = DateTime.Now;
            list.BeginUpdate();
            list.Items.Clear();
            foreach (var e in Old(entries, now, days))
            {
                var item = new ListViewItem((e.IsFolder ? "📁 " : "") + e.Name) { Tag = e };
                item.SubItems.Add(DrivesWidget.FormatSize(e.Size));
                item.SubItems.Add(Strings.CountdownDays((int)(now - e.Modified).TotalDays));
                list.Items.Add(item);
            }
            list.EndUpdate();
            UpdateTotal();
        }

        private void UpdateTotal()
        {
            var chosen = list.CheckedItems.Cast<ListViewItem>().Select(i => (Entry)i.Tag!).ToList();
            total.Text = list.Items.Count == 0
                ? Strings.DownloadsNothing
                : Strings.DownloadsSelected(chosen.Count, DrivesWidget.FormatSize(chosen.Sum(e => e.Size)), list.Items.Count);
            recycle.Enabled = chosen.Count > 0;
        }

        private void Recycle()
        {
            var chosen = list.CheckedItems.Cast<ListViewItem>().Select(i => (Entry)i.Tag!).ToList();
            if (chosen.Count == 0)
                return;
            if (MessageBox.Show(this, Strings.DownloadsConfirm(chosen.Count, DrivesWidget.FormatSize(chosen.Sum(e => e.Size))), Text,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            foreach (var e in chosen)
            {
                try
                {
                    // Recycle bin, not a final delete
                    if (e.IsFolder)
                        Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(e.Path, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                    else
                        Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(e.Path, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                    entries.Remove(e);
                }
                catch (Exception ex)
                {
                    Log.Write("Downloads", $"{e.Name}: {Log.Describe(ex)}");
                }
            }
            Fill();
        }

        [DllImport("shell32.dll")]
        private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid id, uint flags, IntPtr token, out IntPtr path);
    }
}
