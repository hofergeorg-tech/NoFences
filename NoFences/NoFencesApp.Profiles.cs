using NoFences.Model;
using NoFences.Util;

namespace NoFences
{
    /// <summary>
    /// Fence profiles ("Work", "Gaming"): each fence can belong to some profiles, and switching the
    /// profile in the tray shows only those (fences without a profile always show).
    /// </summary>
    public sealed partial class NoFencesApp
    {
        public IReadOnlyList<string> Profiles => Store.Config.Profiles;

        public string? ActiveProfile => Store.Config.ActiveProfile;

        public void SwitchProfile(string? profile)
        {
            Store.Config.ActiveProfile = profile != null && Store.Config.Profiles.Contains(profile) ? profile : null;
            Store.RequestSave();
            ApplyVisibility();
            ShowBalloon(Strings.ProfileSwitched(Store.Config.ActiveProfile ?? Strings.ProfileAll));
        }

        /// <summary>Asks for a name and adds the profile; null if cancelled or it exists.</summary>
        public string? NewProfile(IWin32Window? owner)
        {
            using var dialog = new InputDialog(Strings.ProfileNew.TrimEnd('…'), Strings.ProfileNamePrompt, "");
            if (dialog.ShowDialog(owner) != DialogResult.OK)
                return null;
            var name = dialog.Value.Trim();
            if (name.Length == 0 || Store.Config.Profiles.Contains(name))
                return null;
            Store.Config.Profiles.Add(name);
            Store.RequestSave();
            return name;
        }

        public void DeleteProfile(string profile)
        {
            if (MessageBox.Show(Strings.ProfileDeleteConfirm(profile), "NoFences", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            Store.Config.Profiles.Remove(profile);
            foreach (var f in Store.Config.Fences)
            {
                f.Profiles?.Remove(profile);
                if (f.Profiles is { Count: 0 })
                    f.Profiles = null;
            }
            if (Store.Config.ActiveProfile == profile)
                Store.Config.ActiveProfile = null;
            Store.RequestSave();
            ApplyVisibility();
        }

        /// <summary>Adds or removes a fence from a profile.</summary>
        public void ToggleFenceProfile(FenceInfo info, string profile)
        {
            info.Profiles ??= new();
            if (!info.Profiles.Remove(profile))
                info.Profiles.Add(profile);
            if (info.Profiles.Count == 0)
                info.Profiles = null;
            Store.RequestSave();
            ApplyVisibility();
        }

        /// <summary>A fence created while a profile is active belongs to that profile.</summary>
        private void AssignActiveProfile(FenceInfo info)
        {
            if (Store.Config.ActiveProfile != null && info.Profiles is not { Count: > 0 })
                info.Profiles = new() { Store.Config.ActiveProfile };
        }

        /// <summary>Tray: "Profile: Gaming ▸" with all profiles, new and delete.</summary>
        private void AddProfileItems(ToolStripItemCollection items)
        {
            var menu = new ToolStripMenuItem(Strings.ProfileMenu(ActiveProfile ?? Strings.ProfileAll));
            menu.DropDownItems.Add(new ToolStripMenuItem(Strings.ProfileAll, null, (_, _) => SwitchProfile(null)) { Checked = ActiveProfile == null });
            foreach (var p in Profiles.ToList())
                menu.DropDownItems.Add(new ToolStripMenuItem(p, null, (_, _) => SwitchProfile(p)) { Checked = ActiveProfile == p });
            menu.DropDownItems.Add(new ToolStripSeparator());
            menu.DropDownItems.Add(Strings.ProfileNew, null, (_, _) =>
            {
                if (NewProfile(null) is { } name)
                    SwitchProfile(name);
            });
            if (Profiles.Count > 0)
            {
                var delete = new ToolStripMenuItem(Strings.ProfileDelete);
                foreach (var p in Profiles.ToList())
                    delete.DropDownItems.Add(p, null, (_, _) => DeleteProfile(p));
                menu.DropDownItems.Add(delete);
            }
            menu.DropDownItems.Add(new ToolStripSeparator());
            menu.DropDownItems.Add(new ToolStripMenuItem(Strings.ProfileHowTo) { Enabled = false });
            items.Add(menu);
        }

        /// <summary>Fence menu: "Show in profile ▸" with a check per profile.</summary>
        public void AddFenceProfileItems(ToolStripItemCollection items, FenceInfo info, IWin32Window owner)
        {
            var menu = new ToolStripMenuItem(Strings.ProfileFenceMenu);
            foreach (var p in Profiles.ToList())
                menu.DropDownItems.Add(new ToolStripMenuItem(p, null, (_, _) => ToggleFenceProfile(info, p)) { Checked = info.Profiles?.Contains(p) == true });
            if (Profiles.Count > 0)
                menu.DropDownItems.Add(new ToolStripSeparator());
            menu.DropDownItems.Add(Strings.ProfileNew, null, (_, _) =>
            {
                if (NewProfile(owner) is { } name)
                    ToggleFenceProfile(info, name);
            });
            menu.DropDownItems.Add(new ToolStripMenuItem(Strings.ProfileFenceHint) { Enabled = false });
            items.Add(menu);
        }
    }
}
