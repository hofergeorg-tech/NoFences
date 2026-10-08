using NoFences.Model;
using NoFences.Themes;

namespace NoFences
{
    /// <summary>What a fence window needs from the application around it.</summary>
    public interface IFenceHost
    {
        bool ShowExtensions { get; }

        bool Animations { get; }

        FenceTheme ThemeFor(FenceInfo info);

        void RequestSave();

        /// <summary>Tray notification (e.g. "Break time!" from the focus timer).</summary>
        void Notify(string text);

        /// <summary>Tray notification that does something when clicked.</summary>
        void Offer(string text, Action onClick);

        /// <summary>Stops a ringing timer or alarm.</summary>
        void StopAlarmSound();

        /// <summary>Playtime recorded by NoFences for the playtime widgets.</summary>
        PlaytimeLog Playtime { get; }

        /// <summary>Screen time per program, recorded by NoFences for the screen time widget.</summary>
        UsageLog ScreenTime { get; }

        void CreateFence(FenceKind kind, string? name = null);

        void RemoveFence(FenceWindow window);

        /// <summary>Id of the current virtual desktop, null if unknown.</summary>
        Guid? CurrentVirtualDesktop { get; }

        void TogglePinToDesktop(FenceInfo info);

        /// <summary>Adds "Tools ▸" (search, screen ruler, desktop assistant, tidy up) to a menu.</summary>
        void AddToolItems(ToolStripItemCollection items);

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

        /// <summary>The fence's settings dialog was confirmed (shortcut, shelf, … may have changed).</summary>
        void FenceSettingsChanged(FenceInfo info);

        /// <summary>Opens the style designer; with a fence, "Save and use" applies the style to it.</summary>
        void OpenStyleDesigner(FenceInfo? info);

        /// <summary>Hovering items shows a folder's contents or a large image preview.</summary>
        bool HoverPreview { get; }

        /// <summary>Visible surfaces (screen coordinates) of all other fences, for snapping.</summary>
        IReadOnlyCollection<Rectangle> OtherFenceSurfaces(FenceWindow except);

        /// <summary>Remembers these fences as they are now, so Ctrl+Z can bring them back.</summary>
        void RecordUndo(string description, IEnumerable<Guid> fences, Action? reverse = null);

        /// <summary>Remembers fences copied earlier (see <see cref="Model.UndoStack.Snapshot"/>).</summary>
        void RecordUndo(string description, IReadOnlyList<string> snapshots);

        /// <summary>Undoes the last change (Ctrl+Z).</summary>
        void Undo();

        /// <summary>Saves a note's text as an own template.</summary>
        void SaveNoteTemplate(string name, string text);

        /// <summary>Opens the search across all fences with this text (e.g. a tag).</summary>
        void SearchFor(string text);

        /// <summary>Puts this fence above the other fences on the desktop (e.g. while it unfolds over them).</summary>
        void RaiseAboveOtherFences(FenceWindow window);

        /// <summary>Adds "Undo: …" to a menu when there is something to undo.</summary>
        void AddUndoItem(ToolStripItemCollection items);

        /// <summary>What Ctrl+Z would undo, or null.</summary>
        string? UndoDescription { get; }

        /// <summary>A fence of a docked bar was dragged or resized (<paramref name="before"/> = its bounds before).</summary>
        void DockMemberChanged(FenceWindow window, Rectangle before);

        /// <summary>The other fences in this fence's group (empty without a group).</summary>
        IReadOnlyList<FenceWindow> GroupMembers(FenceWindow window);

        /// <summary>Adds "Group ▸" (join, new group, leave, fold) for this fence to a menu.</summary>
        void AddGroupItems(ToolStripItemCollection items, FenceWindow window);
    }
}
