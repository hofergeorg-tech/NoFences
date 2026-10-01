namespace NoFences
{
    /// <summary>Smooth collapse/expand and the themes' hover animations.</summary>
    public sealed partial class FenceWindow
    {
        private const int CollapseMilliseconds = 140;

        private readonly System.Windows.Forms.Timer animationTimer = new() { Interval = 15 };
        private readonly System.Windows.Forms.Timer hoverTimer = new() { Interval = 33 };
        private DateTime heightAnimationStart;
        private int heightFrom, heightTo;
        private DateTime? hoverSince;

        private void InitAnimations()
        {
            animationTimer.Tick += (_, _) => StepHeightAnimation();
            hoverTimer.Tick += (_, _) => StepHover();
        }

        /// <summary>Changes the height, animated if enabled and visible.</summary>
        private void AnimateHeight(int target)
        {
            if (!app.Animations || !Visible || !IsHandleCreated)
            {
                SetHeightSilently(target);
                return;
            }
            heightFrom = Height;
            heightTo = target;
            heightAnimationStart = DateTime.Now;
            animationTimer.Start();
        }

        private void StepHeightAnimation()
        {
            var p = Math.Clamp((DateTime.Now - heightAnimationStart).TotalMilliseconds / CollapseMilliseconds, 0, 1);
            var eased = 1 - Math.Pow(1 - p, 3); // ease-out
            SetHeightSilently((int)Math.Round(heightFrom + (heightTo - heightFrom) * eased));
            if (p >= 1)
            {
                animationTimer.Stop();
                Relayout();
            }
            Invalidate();
        }

        /// <summary>Starts the hover animation when the mouse comes in; it stops itself when the mouse leaves.</summary>
        private void HoverStarted()
        {
            if (hoverSince != null || !app.Animations || !theme.AnimatesOnHover)
                return;
            hoverSince = DateTime.Now;
            hoverTimer.Start();
        }

        private void StepHover()
        {
            if (!Bounds.Contains(Cursor.Position) || !app.Animations)
            {
                hoverTimer.Stop();
                hoverSince = null;
            }
            Invalidate();
        }

        private void DrawHoverAnimation(Graphics g)
        {
            if (hoverSince is not DateTime since || collapsed)
                return;
            theme.DrawHoverEffect(g, ClientRectangle, titleHeight, (float)(DateTime.Now - since).TotalSeconds, scale);
        }

        private void DisposeAnimations()
        {
            animationTimer.Dispose();
            hoverTimer.Dispose();
        }
    }
}
