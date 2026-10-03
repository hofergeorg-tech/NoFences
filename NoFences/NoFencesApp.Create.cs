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
            var widgets = new ToolStripMenuItem(Strings.NewWidget);
            foreach (var (type, name, _) in WidgetRegistry.Types)
                widgets.DropDownItems.Add(name(), null, (_, _) => CreateWidget(type));
            items.Add(widgets);
            items.Add(Strings.NewRecent, null, (_, _) => CreateRecentFence());
            items.Add(Strings.NewQuickLaunch, null, (_, _) => CreateQuickLaunch());
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
