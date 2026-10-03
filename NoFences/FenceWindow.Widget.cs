using NoFences.Model;
using NoFences.Widgets;

namespace NoFences
{
    /// <summary>Hosting a widget: create it for the fence, refresh it on its interval, forward clicks and drops.</summary>
    public sealed partial class FenceWindow
    {
        private FenceWidget? widget;
        private readonly System.Windows.Forms.Timer widgetTimer = new();
        private readonly ToolTip toolTip = new() { InitialDelay = 400, ReshowDelay = 100 };

        private bool IsWidget => Info.Kind == FenceKind.Widget;

        /// <summary>Called from ApplySettings: (re)creates the widget when the type changed.</summary>
        private void ApplyWidgetSettings()
        {
            if (!IsWidget)
            {
                DisposeWidget();
                return;
            }
            if (widget?.Type != Info.WidgetType)
            {
                DisposeWidget();
                widget = WidgetRegistry.Create(Info, app);
                if (widget == null)
                    return;
                widgetTimer.Interval = widget.RefreshMs;
                widgetTimer.Tick -= WidgetTick;
                widgetTimer.Tick += WidgetTick;
                widget.Refresh();
            }
            if (IsHandleCreated && Visible)
                widgetTimer.Start();
        }

        private void WidgetTick(object? sender, EventArgs e)
        {
            if (widget == null || !Visible || collapsed)
                return;
            try
            {
                widget.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Widget {widget.Type}: {ex.Message}");
            }
            Invalidate();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (widget == null)
                return;
            if (Visible)
                widgetTimer.Start();
            else
                widgetTimer.Stop();
        }

        private void DrawWidget(Graphics g)
        {
            if (widget == null || labelFont == null || noteFont == null)
                return;
            try
            {
                widget.Draw(new WidgetCanvas { G = g, Area = NoteArea, Theme = theme, Label = labelFont, Big = noteFont, S = scale });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Widget draw: {ex.Message}");
            }
        }

        /// <summary>Tooltip with the item name in compact (icons-only) fences.</summary>
        private void UpdateCompactTooltip(string? path)
        {
            if (!Info.Compact)
                return;
            var entry = path == null ? null : FenceEntry.FromPath(path);
            toolTip.SetToolTip(this, entry?.GetDisplayName(app.ShowExtensions) ?? "");
        }

        /// <summary>Preview renderer: sample twice so rates (CPU load) have a baseline.</summary>
        internal void RefreshWidgetForPreview()
        {
            widget?.Refresh();
            Thread.Sleep(600);
            widget?.Refresh();
        }

        private void DisposeWidget()
        {
            widgetTimer.Stop();
            widget?.Dispose();
            widget = null;
        }
    }
}
