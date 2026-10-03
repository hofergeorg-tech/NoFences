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

        void CreateFence(FenceKind kind, string? name = null);

        void RemoveFence(FenceWindow window);

        /// <summary>Adds "New widget ▸", "Recent files" and "Quick-launch bar" to a menu.</summary>
        void AddCreateExtrasItems(ToolStripItemCollection items);

        /// <summary>Visible surfaces (screen coordinates) of all other fences, for snapping.</summary>
        IReadOnlyCollection<Rectangle> OtherFenceSurfaces(FenceWindow except);
    }
}
