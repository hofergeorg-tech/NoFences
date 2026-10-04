using NoFences.Model;
using NoFences.Util;
using NoFences.Win32;

namespace NoFences
{
    /// <summary>
    /// Fence profiles ("Work", "Gaming"): each fence can belong to some profiles, and switching the
    /// profile in the tray shows only those (fences without a profile always show).
    /// </summary>
    public sealed partial class NoFencesApp
    {
        public IReadOnlyList<string> Profiles => Store.Config.Profiles;

        private readonly List<GlobalHotkey> profileHotkeys = new();

        /// <summary>
        /// Ctrl+Alt+F1…F9 switch to profile 1…9, Ctrl+Alt+F10 shows all fences. (Not Ctrl+Alt+digits:
        /// that's AltGr on many keyboards and would take away { [ ] } and friends.)
        /// </summary>
        internal void UpdateProfileHotkeys()
        {
            foreach (var h in profileHotkeys)
                h.Dispose();
            profileHotkeys.Clear();
            if (!Store.Config.ProfileHotkeys || Store.Config.Profiles.Count == 0)
                return;
            Register(Keys.F10, null);
            for (var i = 0; i < Math.Min(9, Store.Config.Profiles.Count); i++)
                Register(Keys.F1 + i, Store.Config.Profiles[i]);

            void Register(Keys key, string? profile)
            {
                var hotkey = new GlobalHotkey(Native.MOD_CONTROL | Native.MOD_ALT, key);
                if (!hotkey.Registered)
                {
                    hotkey.Dispose();
                    return;
                }
                hotkey.Pressed += (_, _) => SwitchProfile(profile);
                profileHotkeys.Add(hotkey);
            }
        }

        /// <summary>"Ctrl+Alt+F2" for the second profile, for menus.</summary>
        internal string? ProfileHotkeyText(string? profile)
        {
            if (!Store.Config.ProfileHotkeys)
                return null;
            var index = profile == null ? 9 : Store.Config.Profiles.IndexOf(profile);
            return index is >= 0 and <= 9 ? Strings.HotkeyName($"Ctrl+Alt+F{index + 1}") : null;
        }

        private void DisposeProfileHotkeys()
        {
            foreach (var h in profileHotkeys)
                h.Dispose();
            profileHotkeys.Clear();
        }

        public string? ActiveProfile => Store.Config.ActiveProfile;

        /// <param name="automatic">Switched by a profile rule; a manual switch ends what the rule started.</param>
        public void SwitchProfile(string? profile, bool automatic = false)
        {
            if (!automatic)
                ruleActive = false;
            var oldProfile = Store.Config.ActiveProfile;
            Store.Config.ActiveProfile = profile != null && Store.Config.Profiles.Contains(profile) ? profile : null;
            if (oldProfile != Store.Config.ActiveProfile)
                RunProfilePrograms(oldProfile, Store.Config.ActiveProfile);
            Store.RequestSave();
            ApplyVisibility();
            ApplyProfileWallpaper(Store.Config.ActiveProfile);
            ApplyProfilePowerPlan(Store.Config.ActiveProfile);
            var name = Store.Config.ActiveProfile ?? Strings.ProfileAll;
            ShowBalloon(automatic ? Strings.ProfileSwitchedAuto(name) : Strings.ProfileSwitched(name));
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
            UpdateProfileHotkeys();
            Store.RequestSave();
            return name;
        }

        public void DeleteProfile(string profile)
        {
            if (MessageBox.Show(Strings.ProfileDeleteConfirm(profile), "NoFences", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            Store.Config.Profiles.Remove(profile);
            Store.Config.ProfileWallpapers.Remove(profile);
            Store.Config.ProfilePowerPlans.Remove(profile);
            Store.Config.ProfilePrograms.Remove(profile);
            UpdateProfileHotkeys();
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
            menu.DropDownItems.Add(new ToolStripMenuItem(Strings.ProfileAll, null, (_, _) => SwitchProfile(null)) { Checked = ActiveProfile == null, ShortcutKeyDisplayString = ProfileHotkeyText(null) });
            foreach (var p in Profiles.ToList())
                menu.DropDownItems.Add(new ToolStripMenuItem(p, null, (_, _) => SwitchProfile(p)) { Checked = ActiveProfile == p, ShortcutKeyDisplayString = ProfileHotkeyText(p) });
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
            AddWallpaperItems(menu.DropDownItems);
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
