using NoFences.Model;
using NoFences.Util;
using NoFences.Win32;

namespace NoFences
{
    /// <summary>A global shortcut (Ctrl+Alt+F) opens a search across everything in all fences.</summary>
    public sealed partial class NoFencesApp
    {
        private GlobalHotkey? searchHotkey;

        private void InitSearch() => UpdateSearchHotkey(notifyIfTaken: true);

        internal void UpdateSearchHotkey(bool notifyIfTaken)
        {
            searchHotkey?.Dispose();
            searchHotkey = null;
            var (modifiers, key) = ParseHotkey(Store.Config.SearchHotkey);
            if (key == Keys.None)
                return;
            var candidate = new GlobalHotkey(modifiers, key);
            if (!candidate.Registered)
            {
                candidate.Dispose();
                if (notifyIfTaken)
                    ShowBalloon(Strings.HotkeyTaken(Store.Config.SearchHotkey), timeout: 6000);
                return;
            }
            candidate.Pressed += (_, _) => OpenSearch();
            searchHotkey = candidate;
        }

        public void OpenSearch() => SearchWindow.ShowSingle(this);

        /// <summary>Opens a result: the file, or for a note, brings the fences to the front.</summary>
        internal void OpenSearchResult(SearchItem item)
        {
            switch (item.Kind)
            {
                case SearchKind.Calculation:
                    try { Clipboard.SetText(item.Path ?? ""); } catch (System.Runtime.InteropServices.ExternalException) { }
                    return;
                case SearchKind.Setting:
                    try { OpenUrl(item.Path!); } catch (Exception) { }
                    return;
                case SearchKind.Note:
                    StartPeek();
                    windows.FirstOrDefault(w => w.Info == item.Fence)?.Activate();
                    return;
                default:
                    if (item.Path != null)
                        FenceEntry.FromPath(item.Path)?.Open();
                    return;
            }
        }

        /// <summary>Start menu shortcuts, read once in the background and kept for later searches.</summary>
        private static Task<List<SearchItem>>? startMenuApps;

        internal static Task<List<SearchItem>> StartMenuAppsAsync() => startMenuApps ??= Task.Run(FenceSearch.StartMenuApps);

        private ToolStripMenuItem SearchItem()
        {
            var search = new ToolStripMenuItem(Strings.SearchMenu, null, (_, _) => OpenSearch());
            if (Store.Config.SearchHotkey != "Off")
                search.ShortcutKeyDisplayString = Strings.HotkeyName(Store.Config.SearchHotkey);
            return search;
        }

        /// <summary>"Tools ▸": search, screen ruler, desktop assistant, tidy up – in the tray and every fence menu.</summary>
        public void AddToolItems(ToolStripItemCollection items)
        {
            var tools = new ToolStripMenuItem(Strings.ToolsMenu);
            tools.DropDownItems.Add(SearchItem());
            tools.DropDownItems.Add(Strings.RulerMenu, null, (_, _) => RulerWindow.Toggle());
            tools.DropDownItems.Add(Strings.ColorPickerMenu, null, (_, _) => PickColor());
            tools.DropDownItems.Add(Strings.DownloadsMenu, null, (_, _) => DownloadsCleaner.Show(Store.Config, Store.RequestSave));
            tools.DropDownItems.Add(new ToolStripSeparator());
            AddDesktopToolItems(tools.DropDownItems);
            tools.DropDownItems.Add(Strings.AssistantMenu, null, (_, _) => RunDesktopAssistant());
            tools.DropDownItems.Add(Strings.SortNow, null, (_, _) => SortDesktopNow());
            items.Add(tools);
        }

        /// <summary>Color picker: the picked value goes to the clipboard and is shown in a notification.</summary>
        public void PickColor()
        {
            // Let the menu that started it disappear before the screens are captured
            var wait = new System.Windows.Forms.Timer { Interval = 250 };
            wait.Tick += (_, _) =>
            {
                wait.Dispose();
                ColorPicker.Start(value =>
                {
                    try { Clipboard.SetText(value); } catch (System.Runtime.InteropServices.ExternalException) { }
                    ShowBalloon(Strings.ColorCopied(value));
                });
            };
            wait.Start();
        }

        private void DisposeSearch() => searchHotkey?.Dispose();
    }

    /// <summary>Search box with live results; Enter opens, arrows choose, Esc or clicking elsewhere closes.</summary>
    internal sealed class SearchWindow : Form
    {
        private static SearchWindow? open;
        private readonly NoFencesApp app;
        private readonly TextBox box = new() { BorderStyle = BorderStyle.None, Dock = DockStyle.Top };
        private readonly ListBox results = new() { BorderStyle = BorderStyle.None, Dock = DockStyle.Fill, DrawMode = DrawMode.OwnerDrawFixed, IntegralHeight = false };
        private readonly Label hint = new() { Dock = DockStyle.Bottom, AutoSize = false, ForeColor = SystemColors.GrayText, TextAlign = ContentAlignment.MiddleLeft };
        private readonly List<SearchItem> all;
        private readonly int fenceItemCount;
        private List<SearchItem> shown = new();

        public static void ShowSingle(NoFencesApp app)
        {
            if (open is { IsDisposed: false })
            {
                open.Activate();
                return;
            }
            open = new SearchWindow(app);
            open.Show();
            // Allowed: the hotkey message gives this process the right to take the foreground
            Native.SetForegroundWindowSafe(open.Handle);
            open.Activate();
            open.box.Focus();
        }

        private SearchWindow(NoFencesApp app)
        {
            this.app = app;
            all = FenceSearch.Collect(app.Store.Config.Fences, app.ShowExtensions);
            fenceItemCount = all.Count;
            all.AddRange(SettingsPages.All());
            // Start menu apps join as soon as they are read (the first time takes a moment)
            var apps = NoFencesApp.StartMenuAppsAsync();
            if (apps.IsCompleted)
                all.AddRange(apps.Result);
            else
                apps.ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully && !IsDisposed)
                        BeginInvoke(() =>
                        {
                            all.AddRange(t.Result);
                            Update();
                        });
                }, TaskScheduler.Default);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            ControlBox = false;
            Text = "";
            ShowInTaskbar = false;
            TopMost = true;
            KeyPreview = true;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = SystemFonts.MessageBoxFont ?? Font;
            Padding = new Padding(12);
            BackColor = SystemColors.Window;

            var area = Screen.FromPoint(Cursor.Position).WorkingArea;
            var scale = DeviceDpi / 96f;
            Size = new Size((int)(560 * scale), (int)(420 * scale));
            StartPosition = FormStartPosition.Manual;
            Location = new Point(area.X + (area.Width - Width) / 2, area.Y + area.Height / 5);

            box.Font = new Font(Font.FontFamily, Font.Size * 1.6f);
            box.PlaceholderText = Strings.SearchPlaceholder;
            results.Font = Font;
            results.ItemHeight = (int)(40 * scale);
            hint.Height = (int)(24 * scale);
            hint.Text = Strings.SearchFooter(fenceItemCount);

            var gap = new Panel { Dock = DockStyle.Top, Height = (int)(10 * scale) };
            Controls.Add(results);
            Controls.Add(gap);
            Controls.Add(box);
            Controls.Add(hint);

            box.TextChanged += (_, _) => Update();
            results.DrawItem += DrawResult;
            results.DoubleClick += (_, _) => OpenSelected();
            Deactivate += (_, _) => BeginInvoke(Close);
            IconCache.Shared.ImageLoaded += IconsLoaded;
        }

        private void IconsLoaded(object? sender, EventArgs e) => results.Invalidate();

        private new void Update()
        {
            if (IsDisposed)
                return;
            shown = FenceSearch.Find(all, box.Text);
            // A calculation comes first: "12*7" → "= 84"
            if (FenceSearch.Calculation(box.Text) is { } calc)
                shown.Insert(0, calc);
            results.BeginUpdate();
            results.Items.Clear();
            results.Items.AddRange(shown.Cast<object>().ToArray());
            if (shown.Count > 0)
                results.SelectedIndex = 0;
            results.EndUpdate();
            hint.Text = box.Text.Trim().Length == 0 ? Strings.SearchFooter(fenceItemCount)
                : shown.Count == 0 ? Strings.SearchNothing
                : shown[0].Kind == SearchKind.Calculation ? Strings.SearchCopyResult
                : Strings.SearchKeys;
        }

        private void DrawResult(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= shown.Count)
                return;
            e.DrawBackground();
            var item = shown[e.Index];
            var selected = (e.State & DrawItemState.Selected) != 0;
            var iconSize = e.Bounds.Height - 8;
            var icon = item.Kind is SearchKind.FenceItem or SearchKind.App && item.Path != null ? IconCache.Shared.Get(item.Path, 32) : null;
            var iconRect = new Rectangle(e.Bounds.X + 6, e.Bounds.Y + 4, iconSize, iconSize);
            if (icon != null)
            {
                e.Graphics.DrawImage(icon, iconRect);
            }
            else
            {
                var glyph = item.Kind switch { SearchKind.Note => "📝", SearchKind.Setting => "⚙", SearchKind.Calculation => "=", _ => "" };
                using var glyphFont = new Font(Font.FontFamily, Font.Size * 1.4f);
                TextRenderer.DrawText(e.Graphics, glyph, glyphFont, iconRect, e.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }

            var textX = iconRect.Right + 10;
            var half = e.Bounds.Height / 2;
            using var bold = new Font(Font, FontStyle.Bold);
            TextRenderer.DrawText(e.Graphics, item.Name, bold, new Rectangle(textX, e.Bounds.Y + 2, e.Bounds.Right - textX - 6, half),
                e.ForeColor, TextFormatFlags.Bottom | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            var detail = item.Kind switch
            {
                SearchKind.Note => Strings.SearchInNote(item.FenceName),
                SearchKind.App => Strings.SearchApp,
                SearchKind.Setting => Strings.SearchWindowsSettings,
                SearchKind.Calculation => item.FenceName,
                _ => Strings.SearchInFence(item.FenceName)
            };
            TextRenderer.DrawText(e.Graphics, detail, Font, new Rectangle(textX, e.Bounds.Y + half, e.Bounds.Right - textX - 6, half - 2),
                selected ? e.ForeColor : SystemColors.GrayText, TextFormatFlags.Top | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    Close();
                    break;
                case Keys.Enter:
                    OpenSelected();
                    break;
                case Keys.Down when results.Items.Count > 0:
                    results.SelectedIndex = Math.Min(results.Items.Count - 1, results.SelectedIndex + 1);
                    break;
                case Keys.Up when results.Items.Count > 0:
                    results.SelectedIndex = Math.Max(0, results.SelectedIndex - 1);
                    break;
                default:
                    return;
            }
            e.Handled = e.SuppressKeyPress = true;
        }

        private void OpenSelected()
        {
            if (results.SelectedIndex < 0 || results.SelectedIndex >= shown.Count)
                return;
            var item = shown[results.SelectedIndex];
            Close();
            app.OpenSearchResult(item);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            IconCache.Shared.ImageLoaded -= IconsLoaded;
            base.OnFormClosed(e);
        }
    }
}
