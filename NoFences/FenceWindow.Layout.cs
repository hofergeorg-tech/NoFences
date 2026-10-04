using System.Runtime.InteropServices;
using NoFences.Util;
using NoFences.Win32;

namespace NoFences
{
    /// <summary>Snapping while moving/resizing, and positions remembered per monitor setup.</summary>
    public sealed partial class FenceWindow
    {
        private const int SnapThreshold = 8;
        private const int SnapGap = 8;

        /// <summary>True while the user drags or resizes the fence (Windows' move/size loop).</summary>
        private bool inSizeMove;

        // Snap targets are collected once per drag, not on every mouse move.
        private IReadOnlyCollection<Rectangle> snapFences = Array.Empty<Rectangle>();
        private IReadOnlyCollection<Rectangle> snapScreens = Array.Empty<Rectangle>();

        // Where the drag started. Windows continues each move from the rectangle we returned last time,
        // so a snapped position would "stick" (each small step got pulled back to the same edge).
        // Instead the unsnapped position is always recomputed from the mouse movement since the start.
        private Rectangle dragStartBounds;
        private Point dragStartCursor;

        // The rest of the group follows a move; all of them can be put back with Ctrl+Z.
        private List<(FenceWindow Window, Point Start)> groupStart = new();
        private List<string> moveSnapshots = new();

        private void BeginSizeMove()
        {
            inSizeMove = true;
            groupStart = app.GroupMembers(this).Select(w => (w, w.Location)).ToList();
            var others = groupStart.Select(g => g.Window).ToHashSet();
            snapFences = app.OtherFenceSurfaces(this).Where(r => !others.Any(o => o.SurfaceOnScreen == r)).ToList();
            snapScreens = ScreenAreas();
            dragStartBounds = Bounds;
            dragStartCursor = Cursor.Position;
            moveSnapshots = new[] { Info }.Concat(others.Select(o => o.Info)).Select(Model.UndoStack.Snapshot).ToList();
        }

        /// <summary>While the fence is dragged (not resized): its group moves along.</summary>
        private void MoveGroupAlong()
        {
            if (!inSizeMove || groupStart.Count == 0 || Size != dragStartBounds.Size)
                return;
            var dx = Left - dragStartBounds.Left;
            var dy = Top - dragStartBounds.Top;
            foreach (var (window, start) in groupStart)
            {
                if (!window.IsDisposed)
                    window.Location = new Point(start.X + dx, start.Y + dy);
            }
        }

        private Size DragDelta => new(Cursor.Position.X - dragStartCursor.X, Cursor.Position.Y - dragStartCursor.Y);

        /// <summary>Saves position/size once, when the user lets go (not on every pixel).</summary>
        private void EndSizeMove()
        {
            inSizeMove = false;
            if (Bounds != dragStartBounds && moveSnapshots.Count > 0)
            {
                var description = Size == dragStartBounds.Size || collapsed ? Strings.UndoMoveFence(Info.Name) : Strings.UndoResizeFence(Info.Name);
                app.RecordUndo(description, moveSnapshots);
            }
            moveSnapshots = new List<string>();
            groupStart = new();
            if (Bounds != dragStartBounds)
                app.DockMemberChanged(this, dragStartBounds);
            Info.PosX = Left;
            Info.PosY = Top;
            if (!collapsed)
            {
                Info.Width = Width;
                Info.Height = Height;
            }
            RememberLayout();
            app.RequestSave();
            Relayout();
            Invalidate();
        }

        /// <summary>The visible part of the fence in screen coordinates (without a theme's clear margin).</summary>
        public Rectangle SurfaceOnScreen
        {
            get
            {
                var ins = theme.SurfaceInsets;
                return Rectangle.FromLTRB(Left + Px(ins.Left), Top + Px(ins.Top), Right - Px(ins.Right), Bottom - Px(ins.Bottom));
            }
        }

        /// <summary>Offset of the surface inside the window, per side (device px).</summary>
        private (int l, int t, int r, int b) SurfaceInsetPx()
        {
            var ins = theme.SurfaceInsets;
            return (Px(ins.Left), Px(ins.Top), Px(ins.Right), Px(ins.Bottom));
        }

        private static IReadOnlyCollection<Rectangle> ScreenAreas() => Screen.AllScreens.Select(s => s.WorkingArea).ToList();

        /// <summary>WM_MOVING: shifts the proposed window rectangle so the surface snaps. Alt disables snapping.</summary>
        private void SnapMoving(IntPtr lParam)
        {
            var r = Marshal.PtrToStructure<Native.RECT>(lParam);
            // Unsnapped position = start + mouse movement (independent of earlier snapping).
            var raw = dragStartBounds;
            raw.Offset(DragDelta.Width, DragDelta.Height);
            r.Left = raw.Left;
            r.Top = raw.Top;
            r.Right = raw.Left + dragStartBounds.Width;
            r.Bottom = raw.Top + dragStartBounds.Height;
            if ((ModifierKeys & Keys.Alt) != 0)
            {
                Marshal.StructureToPtr(r, lParam, false);
                return;
            }

            var (l, t, ri, b) = SurfaceInsetPx();
            var surface = Rectangle.FromLTRB(r.Left + l, r.Top + t, r.Right - ri, r.Bottom - b);
            var offset = Snapper.SnapMove(surface, snapFences, snapScreens, Px(SnapThreshold), Px(SnapGap));
            r.Left += offset.X;
            r.Right += offset.X;
            r.Top += offset.Y;
            r.Bottom += offset.Y;
            Marshal.StructureToPtr(r, lParam, false);
        }

        /// <summary>WM_SIZING: snaps only the edges that are being dragged.</summary>
        private void SnapSizing(int edge, IntPtr lParam)
        {
            var r = Marshal.PtrToStructure<Native.RECT>(lParam);
            // Unsnapped edges = start + mouse movement, only for the edges being dragged.
            var d = DragDelta;
            var s0 = dragStartBounds;
            bool left = edge is Native.WMSZ_LEFT or Native.WMSZ_TOPLEFT or Native.WMSZ_BOTTOMLEFT;
            bool right = edge is Native.WMSZ_RIGHT or Native.WMSZ_TOPRIGHT or Native.WMSZ_BOTTOMRIGHT;
            bool top = edge is Native.WMSZ_TOP or Native.WMSZ_TOPLEFT or Native.WMSZ_TOPRIGHT;
            bool bottom = edge is Native.WMSZ_BOTTOM or Native.WMSZ_BOTTOMLEFT or Native.WMSZ_BOTTOMRIGHT;
            if (left) r.Left = s0.Left + d.Width;
            if (right) r.Right = s0.Right + d.Width;
            if (top) r.Top = s0.Top + d.Height;
            if (bottom) r.Bottom = s0.Bottom + d.Height;
            if ((ModifierKeys & Keys.Alt) != 0)
            {
                ClampMinimumSize(ref r, left, top);
                Marshal.StructureToPtr(r, lParam, false);
                return;
            }

            var (l, t, ri, b) = SurfaceInsetPx();
            var surface = Rectangle.FromLTRB(r.Left + l, r.Top + t, r.Right - ri, r.Bottom - b);
            var fences = snapFences;
            var screens = snapScreens;
            int Snap(int value, bool horizontal) => Snapper.SnapEdge(value, horizontal, surface, fences, screens, Px(SnapThreshold), Px(SnapGap));

            if (edge is Native.WMSZ_LEFT or Native.WMSZ_TOPLEFT or Native.WMSZ_BOTTOMLEFT)
                r.Left = Snap(surface.Left, true) - l;
            if (edge is Native.WMSZ_RIGHT or Native.WMSZ_TOPRIGHT or Native.WMSZ_BOTTOMRIGHT)
                r.Right = Snap(surface.Right, true) + ri;
            if (edge is Native.WMSZ_TOP or Native.WMSZ_TOPLEFT or Native.WMSZ_TOPRIGHT)
                r.Top = Snap(surface.Top, false) - t;
            if (edge is Native.WMSZ_BOTTOM or Native.WMSZ_BOTTOMLEFT or Native.WMSZ_BOTTOMRIGHT)
                r.Bottom = Snap(surface.Bottom, false) + b;
            ClampMinimumSize(ref r, left, top);
            Marshal.StructureToPtr(r, lParam, false);
        }

        /// <summary>Keeps a usable minimum size, moving only the edge that is being dragged.</summary>
        private void ClampMinimumSize(ref Native.RECT r, bool draggingLeft, bool draggingTop)
        {
            var minW = Px(100);
            var minH = titleHeight + Px(40);
            if (r.Right - r.Left < minW)
            {
                if (draggingLeft) r.Left = r.Right - minW;
                else r.Right = r.Left + minW;
            }
            if (!collapsed && r.Bottom - r.Top < minH)
            {
                if (draggingTop) r.Top = r.Bottom - minH;
                else r.Bottom = r.Top + minH;
            }
        }

        #region Layout per monitor setup

        /// <summary>Identifies the current monitor arrangement, e.g. "0,0,2560,1440|-2560,0,2560,1440".</summary>
        public static string ScreenSetupKey() =>
            string.Join("|", Screen.AllScreens.Select(s => s.Bounds).OrderBy(b => b.X).ThenBy(b => b.Y).Select(b => $"{b.X},{b.Y},{b.Width},{b.Height}"));

        /// <summary>Called whenever position or (expanded) size changes.</summary>
        private void RememberLayout()
        {
            Info.Layouts[ScreenSetupKey()] = new[] { Info.PosX, Info.PosY, Info.Width, Info.Height };
        }

        /// <summary>After monitors changed: go back to where the fence was in this setup, or at least onto a screen.</summary>
        public void ApplyLayoutForCurrentScreens()
        {
            if (Info.Layouts.TryGetValue(ScreenSetupKey(), out var l) && l.Length == 4)
            {
                suppressBoundsSave = true;
                Info.PosX = l[0];
                Info.PosY = l[1];
                Info.Width = l[2];
                Info.Height = l[3];
                Bounds = new Rectangle(l[0], l[1], l[2], collapsed ? CollapsedHeight : l[3]);
                suppressBoundsSave = false;
            }
            EnsureOnScreen();
        }

        #endregion
    }
}
