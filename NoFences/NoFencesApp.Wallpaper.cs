using System.Runtime.InteropServices;
using System.Text;
using NoFences.Util;

namespace NoFences
{
    /// <summary>
    /// A wallpaper per profile. The wallpaper from before is remembered and comes back in profiles that
    /// don't have their own (and for "all fences").
    /// </summary>
    public sealed partial class NoFencesApp
    {
        private const int SPI_GETDESKWALLPAPER = 0x73, SPI_SETDESKWALLPAPER = 0x14, SPIF_UPDATE_AND_SEND = 0x3;

        private void ApplyProfileWallpaper(string? profile)
        {
            var c = Store.Config;
            if (profile != null && c.ProfileWallpapers.TryGetValue(profile, out var image) && File.Exists(image))
            {
                var current = CurrentWallpaper();
                if (string.Equals(current, image, StringComparison.OrdinalIgnoreCase))
                    return;
                // Remember the user's own wallpaper the first time NoFences replaces it
                c.OriginalWallpaper ??= current;
                Store.RequestSave();
                SetWallpaper(image);
            }
            else if (c.OriginalWallpaper != null)
            {
                var original = c.OriginalWallpaper;
                c.OriginalWallpaper = null;
                Store.RequestSave();
                if (File.Exists(original))
                    SetWallpaper(original);
            }
        }

        internal void ChooseProfileWallpaper(string profile, IWin32Window? owner)
        {
            using var dialog = new OpenFileDialog
            {
                Title = Strings.WallpaperChoose(profile),
                Filter = Strings.WallpaperFilter,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };
            if (dialog.ShowDialog(owner) != DialogResult.OK)
                return;
            Store.Config.ProfileWallpapers[profile] = dialog.FileName;
            Store.RequestSave();
            if (ActiveProfile == profile)
                ApplyProfileWallpaper(profile);
        }

        internal void RemoveProfileWallpaper(string profile)
        {
            Store.Config.ProfileWallpapers.Remove(profile);
            Store.RequestSave();
            if (ActiveProfile == profile)
                ApplyProfileWallpaper(profile);
        }

        /// <summary>Tray: "Wallpaper for 'Gaming'…" and remove it again.</summary>
        private void AddWallpaperItems(ToolStripItemCollection items)
        {
            if (ActiveProfile is not { } profile)
                return;
            items.Add(Strings.WallpaperChoose(profile) + "…", null, (_, _) => ChooseProfileWallpaper(profile, null));
            if (Store.Config.ProfileWallpapers.ContainsKey(profile))
                items.Add(Strings.WallpaperRemove, null, (_, _) => RemoveProfileWallpaper(profile));
        }

        private static string? CurrentWallpaper()
        {
            var path = new StringBuilder(520);
            return SystemParametersInfo(SPI_GETDESKWALLPAPER, path.Capacity, path, 0) && path.Length > 0 ? path.ToString() : null;
        }

        /// <summary>Changing the wallpaper can take a moment; done in the background.</summary>
        private static void SetWallpaper(string path) =>
            Task.Run(() => SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, new StringBuilder(path), SPIF_UPDATE_AND_SEND));

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SystemParametersInfo(int action, int param, StringBuilder value, int winIni);
    }
}
