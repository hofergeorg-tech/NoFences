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

        private readonly ListView list = new() { View = View.Details, CheckBoxes = true, FullRowSelect = true, Width = 640, Height = 300 };
        private readonly ListBox folderList = new() { Width = 520, Height = 76, IntegralHeight = false };
        private readonly ComboBox age = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
        private readonly Label total = new() { AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
        private readonly Button recycle = new() { AutoSize = true };
        private readonly Model.AppConfig config;
        private readonly Action save;
        private List<Entry> entries = new();
        private List<List<Model.DuplicateFinder.FileItem>>? duplicates;
        private bool DuplicateMode => age.SelectedIndex == Ages.Length;
        private int scanVersion;

        public static void Show(Model.AppConfig config, Action save)
        {
            if (open is { IsDisposed: false })
            {
                open.Activate();
                return;
            }
            open = new DownloadsCleaner(config, save);
            ((Form)open).Show();
        }

        /// <summary>The folders to clean up; Downloads until the user chooses others.</summary>
        private List<string> Folders => config.CleanupFolders.Count > 0 ? config.CleanupFolders : new List<string> { DownloadsFolder() };

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

        private DownloadsCleaner(Model.AppConfig config, Action save)
        {
            this.config = config;
            this.save = save;
            Text = Strings.DownloadsTitle;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Padding = new Padding(16);
            Font = SystemFonts.MessageBoxFont ?? Font;

            list.Columns.Add(Strings.Name, 280);
            list.Columns.Add(Strings.Folder, 120);
            list.Columns.Add(Strings.DownloadsSize, 100, HorizontalAlignment.Right);
            list.Columns.Add(Strings.DownloadsAge, 120, HorizontalAlignment.Right);
            list.ItemChecked += (_, _) => UpdateTotal();
            age.Items.AddRange(Ages.Select(d => (object)Strings.DownloadsOlderThan(d)).ToArray());
            age.Items.Add(Strings.DuplicatesMode);
            age.SelectedIndex = 1;
            age.SelectedIndexChanged += (_, _) => Fill();
            recycle.Text = Strings.DownloadsRecycle;
            recycle.Click += (_, _) => Recycle();
            // Opens the folder of the chosen item, else the chosen folder of the list above
            var openFolder = new Button { Text = Strings.OpenFolder, AutoSize = true };
            openFolder.Click += (_, _) =>
            {
                var target = list.SelectedItems.Count > 0 ? Path.GetDirectoryName(((Entry)list.SelectedItems[0].Tag!).Path)
                    : folderList.SelectedItem as string ?? Folders[0];
                if (target != null && Directory.Exists(target))
                    Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            };
            var close = new Button { Text = Strings.Close, AutoSize = true, DialogResult = DialogResult.Cancel };
            CancelButton = close;

            // Folders to look in: Downloads by default, more can be added
            var addFolder = new Button { Text = Strings.CleanupAddFolder, AutoSize = true };
            addFolder.Click += (_, _) => AddFolder();
            var removeFolder = new Button { Text = Strings.RuleRemove, AutoSize = true };
            removeFolder.Click += (_, _) => RemoveFolder();
            var folderButtons = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            folderButtons.Controls.Add(addFolder);
            folderButtons.Controls.Add(removeFolder);
            var folderRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            folderRow.Controls.Add(folderList);
            folderRow.Controls.Add(folderButtons);
            FillFolders();

            var top = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 10, 0, 0) };
            top.Controls.Add(new Label { Text = Strings.DownloadsShow, AutoSize = true, Margin = new Padding(0, 6, 6, 0) });
            top.Controls.Add(age);
            var layout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Dock = DockStyle.Fill, WrapContents = false };
            layout.Controls.Add(new Label { Text = Strings.CleanupFolders, AutoSize = true, Margin = new Padding(0, 0, 0, 4) });
            layout.Controls.Add(folderRow);
            layout.Controls.Add(top);
            layout.Controls.Add(list);
            layout.Controls.Add(total);
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Bottom, Padding = new Padding(0, 12, 0, 0) };
            buttons.Controls.AddRange(new Control[] { close, recycle, openFolder });
            Controls.Add(layout);
            Controls.Add(buttons);
            Shown += async (_, _) => await RescanAsync();
        }

        private void FillFolders()
        {
            folderList.Items.Clear();
            folderList.Items.AddRange(Folders.Cast<object>().ToArray());
        }

        private async void AddFolder()
        {
            using var dialog = new FolderBrowserDialog { Description = Strings.CleanupAddFolder.TrimEnd('…'), UseDescriptionForTitle = true };
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;
            // The first own choice keeps Downloads in the list (it was only implied before)
            if (config.CleanupFolders.Count == 0)
                config.CleanupFolders.Add(DownloadsFolder());
            if (!config.CleanupFolders.Contains(dialog.SelectedPath, StringComparer.OrdinalIgnoreCase))
                config.CleanupFolders.Add(dialog.SelectedPath);
            save();
            FillFolders();
            await RescanAsync();
        }

        private async void RemoveFolder()
        {
            if (folderList.SelectedItem is not string folder)
                return;
            if (config.CleanupFolders.Count == 0)
                config.CleanupFolders.Add(DownloadsFolder());
            config.CleanupFolders.RemoveAll(f => string.Equals(f, folder, StringComparison.OrdinalIgnoreCase));
            save();
            FillFolders();
            await RescanAsync();
        }

        private async Task RescanAsync()
        {
            var version = ++scanVersion;
            total.Text = Strings.WeatherLoading;
            var folders = Folders.ToList();
            duplicates = null;
            var found = await Task.Run(() => folders.SelectMany(Scan).ToList());
            // A newer scan (folder added meanwhile) wins
            if (version != scanVersion || IsDisposed)
                return;
            entries = found;
            Fill();
        }

        private static List<Entry> Scan(string folder)
        {
            var result = new List<Entry>();
            if (!Directory.Exists(folder))
                return result;
            try
            {
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
            }
            catch (Exception) { }
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

        private async void Fill()
        {
            if (DuplicateMode)
            {
                if (duplicates == null)
                {
                    list.Items.Clear();
                    total.Text = Strings.DuplicatesSearching;
                    var version = scanVersion;
                    var folders = Folders.ToList();
                    var found = await Task.Run(() => Model.DuplicateFinder.Find(Model.DuplicateFinder.Scan(folders), Model.DuplicateFinder.Sha256));
                    if (version != scanVersion || IsDisposed || !DuplicateMode)
                        return;
                    duplicates = found;
                }
                FillDuplicates();
                return;
            }
            list.Groups.Clear();
            var days = Ages[Math.Max(0, age.SelectedIndex)];
            var now = DateTime.Now;
            list.BeginUpdate();
            list.Items.Clear();
            foreach (var e in Old(entries, now, days))
            {
                var item = new ListViewItem((e.IsFolder ? "📁 " : "") + e.Name) { Tag = e };
                item.SubItems.Add(Path.GetFileName(Path.GetDirectoryName(e.Path)) ?? "");
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
                    foreach (var group in duplicates ?? new())
                        group.RemoveAll(d => d.Path == e.Path);
                    duplicates?.RemoveAll(g => g.Count < 2);
                }
                catch (Exception ex)
                {
                    Log.Write("Downloads", $"{e.Name}: {Log.Describe(ex)}");
                }
            }
            Fill();
        }

        /// <summary>One list group per set of identical files; the copies are pre-checked, the original (oldest) isn't.</summary>
        private void FillDuplicates()
        {
            list.BeginUpdate();
            list.Items.Clear();
            list.Groups.Clear();
            var now = DateTime.Now;
            foreach (var group in duplicates ?? new())
            {
                var header = new ListViewGroup(Strings.DuplicatesGroup(Path.GetFileName(group[0].Path), group.Count, DrivesWidget.FormatSize(group[0].Size)));
                list.Groups.Add(header);
                for (var i = 0; i < group.Count; i++)
                {
                    var d = group[i];
                    var e = new Entry(d.Path, Path.GetFileName(d.Path), d.Size, d.Modified, false);
                    var item = new ListViewItem((i == 0 ? Strings.DuplicatesOriginal + " " : "") + e.Name, header) { Tag = e, Checked = i > 0 };
                    item.SubItems.Add(Path.GetFileName(Path.GetDirectoryName(e.Path)) ?? "");
                    item.SubItems.Add(DrivesWidget.FormatSize(e.Size));
                    item.SubItems.Add(Strings.CountdownDays((int)(now - e.Modified).TotalDays));
                    list.Items.Add(item);
                }
            }
            list.EndUpdate();
            UpdateTotal();
            if (list.Items.Count == 0)
                total.Text = Strings.DuplicatesNone;
        }

        [DllImport("shell32.dll")]
        private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid id, uint flags, IntPtr token, out IntPtr path);
    }
}
