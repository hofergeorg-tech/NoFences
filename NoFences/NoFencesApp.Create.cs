using NoFences.Model;
using NoFences.Util;
using NoFences.Widgets;

namespace NoFences
{
    /// <summary>Creating widgets and the special fences (recent files, quick-launch bar).</summary>
    public sealed partial class NoFencesApp
    {
        /// <summary>"New widget ▸", "Recent files", "Quick-launch bar" – used in the tray and fence menus.</summary>
        public void AddCreateExtrasItems(ToolStripItemCollection items)
        {
            // New widget ▸ Time & planning ▸ …, Info ▸ …, System ▸ …, Games & media ▸ …
            var widgets = new ToolStripMenuItem(Strings.NewWidget);
            foreach (var (group, types) in WidgetRegistry.Groups)
            {
                var sub = new ToolStripMenuItem(Strings.WidgetGroupName(group));
                foreach (var type in types)
                {
                    var name = WidgetRegistry.Types.First(t => t.Type == type).Name();
                    sub.DropDownItems.Add(name, null, (_, _) => CreateWidget(type));
                }
                widgets.DropDownItems.Add(sub);
            }
            items.Add(widgets);
            items.Add(Strings.NewRecent, null, (_, _) => CreateRecentFence());
            items.Add(Strings.NewQuickLaunch, null, (_, _) => CreateQuickLaunch());
            AddMoreFenceItems(items);
        }

        public void CreateWidget(string type)
        {
            var (_, name, size) = WidgetRegistry.Types.First(t => t.Type == type);
            // Playtime needs the game first; its name becomes the fence title.
            string? option = null;
            var title = name();
            if (type == "playtime")
            {
                option = PlaytimeWidget.ChooseExe(null, null);
                if (option != null)
                    title = PlaytimeWidget.GameName(option);
            }
            else if (type == "weather" && WeatherPlaceDialog.Choose(null, null) is { } place)
            {
                option = place.ToOption();
                title = $"{name()} {place.Name}";
            }
            else if (type == "agenda")
            {
                option = AgendaWidget.AskUrls(null, null);
            }
            else if (type == "news")
            {
                option = NewsWidget.AskFeeds(null, null);
            }
            else if (type == "ticker")
            {
                option = TickerWidget.DefaultSymbols;
            }
            else if (type == "photos")
            {
                using var dialog = new FolderBrowserDialog
                {
                    Description = Strings.PhotosChoose.TrimEnd('…'),
                    UseDescriptionForTitle = true,
                    SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                };
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    option = PhotoWidget.Format(60, dialog.SelectedPath);
                    title = Path.GetFileName(dialog.SelectedPath.TrimEnd('\\'));
                }
            }
            AddFence(new FenceInfo
            {
                Name = title,
                WidgetOption = option,
                Kind = FenceKind.Widget,
                WidgetType = type,
                Width = size.Width,
                Height = size.Height
            });
        }

        /// <summary>A read-only view of Windows' "Recent" folder: the 20 most recently opened files.</summary>
        public void CreateRecentFence()
        {
            AddFence(new FenceInfo
            {
                Name = Strings.RecentName,
                Kind = FenceKind.Folder,
                FolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Recent),
                SortMode = FenceSortMode.Modified,
                ReadOnly = true,
                MaxItems = 20,
                Width = 340,
                Height = 300
            });
        }

        /// <summary>A slim, icons-only links fence for launchers and games.</summary>
        public void CreateQuickLaunch()
        {
            AddFence(new FenceInfo
            {
                Name = Strings.QuickLaunchName,
                Kind = FenceKind.Links,
                Compact = true,
                IconSize = 48,
                Width = 420,
                Height = 110
            });
        }

        private void AddFence(FenceInfo info)
        {
            PlaceNearCursor(info);
            AssignActiveProfile(info);
            Store.Config.Fences.Add(info);
            Store.RequestSave();
            OpenWindow(info);
        }
    }
}
