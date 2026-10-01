using System.Runtime.InteropServices;
using NoFences.Util;
using NoFences.Win32;

namespace NoFences
{
    /// <summary>Snapping while moving/resizing, and positions remembered per monitor setup.</summary>
    public sealed partial class FenceWindow
    {
        private const int SnapThreshold = 12;
        private const int SnapGap = 8;

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
            if ((ModifierKeys & Keys.Alt) != 0)
                return;
            var r = Marshal.PtrToStructure<Native.RECT>(lParam);
            var (l, t, ri, b) = SurfaceInsetPx();
            var surface = Rectangle.FromLTRB(r.Left + l, r.Top + t, r.Right - ri, r.Bottom - b);
            var offset = Snapper.SnapMove(surface, app.OtherFenceSurfaces(this), ScreenAreas(), Px(SnapThreshold), Px(SnapGap));
            r.Left += offset.X;
            r.Right += offset.X;
            r.Top += offset.Y;
            r.Bottom += offset.Y;
            Marshal.StructureToPtr(r, lParam, false);
        }

        /// <summary>WM_SIZING: snaps only the edges that are being dragged.</summary>
        private void SnapSizing(int edge, IntPtr lParam)
        {
            if ((ModifierKeys & Keys.Alt) != 0)
                return;
            var r = Marshal.PtrToStructure<Native.RECT>(lParam);
            var (l, t, ri, b) = SurfaceInsetPx();
            var surface = Rectangle.FromLTRB(r.Left + l, r.Top + t, r.Right - ri, r.Bottom - b);
            var fences = app.OtherFenceSurfaces(this);
            var screens = ScreenAreas();
            int Snap(int value, bool horizontal) => Snapper.SnapEdge(value, horizontal, surface, fences, screens, Px(SnapThreshold), Px(SnapGap));

            if (edge is Native.WMSZ_LEFT or Native.WMSZ_TOPLEFT or Native.WMSZ_BOTTOMLEFT)
                r.Left = Snap(surface.Left, true) - l;
            if (edge is Native.WMSZ_RIGHT or Native.WMSZ_TOPRIGHT or Native.WMSZ_BOTTOMRIGHT)
                r.Right = Snap(surface.Right, true) + ri;
            if (edge is Native.WMSZ_TOP or Native.WMSZ_TOPLEFT or Native.WMSZ_TOPRIGHT)
                r.Top = Snap(surface.Top, false) - t;
            if (edge is Native.WMSZ_BOTTOM or Native.WMSZ_BOTTOMLEFT or Native.WMSZ_BOTTOMRIGHT)
                r.Bottom = Snap(surface.Bottom, false) + b;
            Marshal.StructureToPtr(r, lParam, false);
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
