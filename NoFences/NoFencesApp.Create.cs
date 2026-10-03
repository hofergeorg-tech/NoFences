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
            AddFence(new FenceInfo
            {
                Name = name(),
                Kind = FenceKind.Widget,
                WidgetType = type,
                Width = size.Width,
                Height = size.Height,
                // The Star Citizen widget looks best in the HUD style
                Theme = type == "starcitizen" ? "starcitizen" : null
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
            Store.Config.Fences.Add(info);
            Store.RequestSave();
            OpenWindow(info);
        }
    }
}
