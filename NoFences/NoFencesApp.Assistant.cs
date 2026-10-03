using NoFences.Model;
using NoFences.Util;

namespace NoFences
{
    /// <summary>
    /// Desktop assistant: sorts what's on the desktop into new link fences by kind (games, programs,
    /// documents, pictures …). Nothing is moved; the fences only link to the files.
    /// </summary>
    public sealed partial class NoFencesApp
    {
        /// <summary>Desktop items that aren't in any fence yet, by category.</summary>
        private Dictionary<DesktopCategory, List<string>> ScanDesktop()
        {
            var inFences = new HashSet<string>(Store.Config.Fences.SelectMany(f => f.Files.Concat(f.Tabs.SelectMany(t => t.Files))), StringComparer.OrdinalIgnoreCase);
            var result = new Dictionary<DesktopCategory, List<string>>();
            foreach (var root in new[] { Environment.SpecialFolder.DesktopDirectory, Environment.SpecialFolder.CommonDesktopDirectory })
            {
                var folder = Environment.GetFolderPath(root);
                if (!Directory.Exists(folder))
                    continue;
                foreach (var info in new DirectoryInfo(folder).EnumerateFileSystemInfos())
                {
                    if ((info.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0 || inFences.Contains(info.FullName))
                        continue;
                    var target = info.Extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase) ? ShortcutTarget(info.FullName)
                        : info.Extension.Equals(".url", StringComparison.OrdinalIgnoreCase) ? DesktopSorter.ReadUrlShortcut(info.FullName)
                        : null;
                    var category = DesktopSorter.Categorize(info.FullName, info is DirectoryInfo, target);
                    if (!result.TryGetValue(category, out var list))
                        result[category] = list = new List<string>();
                    list.Add(info.FullName);
                }
            }
            return result;
        }

        /// <summary>Where a .lnk points (via the Windows Script Host), or null.</summary>
        private static string? ShortcutTarget(string lnk)
        {
            try
            {
                var type = Type.GetTypeFromProgID("WScript.Shell");
                if (type == null)
                    return null;
                dynamic shell = Activator.CreateInstance(type)!;
                try
                {
                    dynamic shortcut = shell.CreateShortcut(lnk);
                    return (string)shortcut.TargetPath + " " + (string)shortcut.Arguments;
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void RunDesktopAssistant()
        {
            var scan = ScanDesktop();
            if (scan.Count == 0)
            {
                MessageBox.Show(Strings.AssistantNothing, "NoFences", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var dialog = new DesktopAssistantDialog(scan);
            if (dialog.ShowDialog() != DialogResult.OK || dialog.Chosen.Count == 0)
                return;

            // One link fence per chosen category, in columns from the top left of the main screen
            var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1040);
            const int width = 320, gap = 20;
            int x = area.X + gap, y = area.Y + gap;
            foreach (var category in dialog.Chosen)
            {
                var files = scan[category];
                var rows = (files.Count + 2) / 3;
                var height = Math.Clamp(45 + rows * 95, 160, 460);
                if (y + height > area.Bottom - gap)
                {
                    y = area.Y + gap;
                    x += width + gap;
                }
                var info = new FenceInfo
                {
                    Name = Strings.CategoryName(category),
                    Kind = FenceKind.Links,
                    Files = files.ToList(),
                    Theme = DesktopSorter.ThemeFor(category),
                    PosX = x,
                    PosY = y,
                    Width = width,
                    Height = height
                };
                AssignActiveProfile(info);
                Store.Config.Fences.Add(info);
                OpenWindow(info);
                y += height + gap;
            }
            Store.RequestSave();
            ShowBalloon(Strings.AssistantDone(dialog.Chosen.Count), timeout: 8000);
        }
    }

    /// <summary>Which categories become fences; shows how many items each has.</summary>
    internal sealed class DesktopAssistantDialog : Form
    {
        private readonly CheckedListBox list = new() { Width = 360, Height = 190, CheckOnClick = true, IntegralHeight = false };
        private readonly List<DesktopCategory> categories;

        public List<DesktopCategory> Chosen => list.CheckedIndices.Cast<int>().Select(i => categories[i]).ToList();

        public DesktopAssistantDialog(Dictionary<DesktopCategory, List<string>> scan)
        {
            Text = Strings.AssistantTitle;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(16);
            Font = SystemFonts.MessageBoxFont ?? Font;
            TopMost = true;

            categories = Enum.GetValues<DesktopCategory>().Where(scan.ContainsKey).ToList();
            foreach (var c in categories)
                list.Items.Add($"{Strings.CategoryName(c)}  ({scan[c].Count})", true);

            var layout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Dock = DockStyle.Fill, WrapContents = false };
            layout.Controls.Add(new Label { Text = Strings.AssistantIntro, AutoSize = true, MaximumSize = new Size(370, 0), Margin = new Padding(0, 0, 0, 10) });
            layout.Controls.Add(list);
            layout.Controls.Add(new Label { Text = Strings.AssistantNote, AutoSize = true, MaximumSize = new Size(370, 0), ForeColor = SystemColors.GrayText, Margin = new Padding(0, 8, 0, 0) });

            var ok = new Button { Text = Strings.AssistantCreate, DialogResult = DialogResult.OK, AutoSize = true };
            var cancel = new Button { Text = Strings.Cancel, DialogResult = DialogResult.Cancel, AutoSize = true };
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Bottom, Padding = new Padding(0, 12, 0, 0) };
            buttons.Controls.AddRange(new Control[] { cancel, ok });
            AcceptButton = ok;
            CancelButton = cancel;
            Controls.Add(layout);
            Controls.Add(buttons);
        }
    }
}
