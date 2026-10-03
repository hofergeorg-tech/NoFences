using NoFences.Model;
using NoFences.Themes;

namespace NoFences
{
    /// <summary>What a fence window needs from the application around it.</summary>
    public interface IFenceHost
    {
        bool ShowExtensions { get; }

        bool Animations { get; }

        /// <summary>The opt-in FPS helper (needs admin rights) is enabled.</summary>
        bool FpsEnabled { get; }

        /// <summary>Turns the FPS helper on (after explaining the admin rights) or off.</summary>
        void ToggleFps();

        FenceTheme ThemeFor(FenceInfo info);

        void RequestSave();

        /// <summary>Tray notification (e.g. "Break time!" from the focus timer).</summary>
        void Notify(string text);

        /// <summary>Playtime recorded by NoFences for the playtime widgets.</summary>
        PlaytimeLog Playtime { get; }

        /// <summary>Screen time per program, recorded by NoFences for the screen time widget.</summary>
        UsageLog ScreenTime { get; }

        void CreateFence(FenceKind kind, string? name = null);

        void RemoveFence(FenceWindow window);

        /// <summary>Id of the current virtual desktop, null if unknown.</summary>
        Guid? CurrentVirtualDesktop { get; }

        void TogglePinToDesktop(FenceInfo info);

        /// <summary>Adds the app's "Settings…" and "Language ▸" to a menu.</summary>
        void AddAppSettingsItems(ToolStripItemCollection items);

        IReadOnlyList<string> Profiles { get; }

        string? ActiveProfile { get; }

        /// <summary>Switches the profile (null = all fences); automatic = by a rule or the focus timer.</summary>
        void SwitchProfile(string? profile, bool automatic = false);

        /// <summary>Adds "Show in profile ▸" for this fence to a menu.</summary>
        void AddFenceProfileItems(ToolStripItemCollection items, FenceInfo info, IWin32Window owner);

        /// <summary>Adds "New widget ▸", "Recent files" and "Quick-launch bar" to a menu.</summary>
        void AddCreateExtrasItems(ToolStripItemCollection items);

        /// <summary>Visible surfaces (screen coordinates) of all other fences, for snapping.</summary>
        IReadOnlyCollection<Rectangle> OtherFenceSurfaces(FenceWindow except);
    }
}
