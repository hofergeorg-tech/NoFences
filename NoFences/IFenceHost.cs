using NoFences.Model;
using NoFences.Themes;

namespace NoFences
{
    /// <summary>What a fence window needs from the application around it.</summary>
    public interface IFenceHost
    {
        bool ShowExtensions { get; }

        FenceTheme ThemeFor(FenceInfo info);

        void RequestSave();

        void CreateFence(FenceKind kind, string? name = null);

        void RemoveFence(FenceWindow window);

        /// <summary>Visible surfaces (screen coordinates) of all other fences, for snapping.</summary>
        IReadOnlyCollection<Rectangle> OtherFenceSurfaces(FenceWindow except);
    }
}
