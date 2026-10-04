using NoFences.Util;
using NoFences.Win32;

namespace NoFences
{
    /// <summary>Renaming a fence in place: double-click its title.</summary>
    public sealed partial class FenceWindow
    {
        private Form? titleHost;
        private TextBox? titleBox;

        /// <summary>Title area on screen (inside a theme's clear margin, e.g. on the Post-it paper).</summary>
        private Rectangle TitleEditBounds()
        {
            var ins = theme.SurfaceInsets;
            var left = Px(ins.Left) + Px(8);
            var top = Px(ins.Top);
            var height = Math.Max(titleHeight - top, (titleFont?.Height ?? Px(20)) + Px(6));
            var width = Math.Max(Px(60), ClientSize.Width - Px(ins.Left + ins.Right) - Px(16));
            return new Rectangle(PointToScreen(new Point(left, top)), new Size(width, height));
        }

        public void StartEditTitle()
        {
            if (titleHost != null || Editing)
                return;

            var (back, fore) = theme.EditorColors;
            var bounds = TitleEditBounds();
            titleBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = titleFont,
                BackColor = back,
                ForeColor = fore,
                TextAlign = HorizontalAlignment.Center,
                Text = Info.Name,
                Width = bounds.Width
            };
            // Like the note editor, this lives in its own opaque window (classic controls go white inside fences).
            titleHost = new EditorHost { BackColor = back, Bounds = bounds };
            titleBox.Top = Math.Max(0, (bounds.Height - titleBox.Height) / 2);
            titleHost.Controls.Add(titleBox);

            titleBox.KeyDown += (_, e) =>
            {
                if (e.KeyCode is Keys.Enter or Keys.Escape)
                {
                    e.SuppressKeyPress = true;
                    EndEditTitle(save: e.KeyCode == Keys.Enter);
                }
            };
            titleHost.Deactivate += (_, _) => BeginInvoke(() => EndEditTitle(save: true));

            titleHost.Show(this);
            titleHost.Activate();
            Native.SetForegroundWindowSafe(titleHost.Handle);
            titleBox.Focus();
            titleBox.SelectAll();
        }

        private void EndEditTitle(bool save)
        {
            if (titleHost == null || titleBox == null)
                return;
            var name = titleBox.Text.Trim();
            var host = titleHost;
            titleHost = null;
            titleBox = null;
            host.Close();
            host.Dispose();

            if (save && name.Length > 0 && name != Info.Name)
            {
                app.RecordUndo(Strings.UndoRenameFence(Info.Name), new[] { Info.Id });
                Info.Name = name;
                Text = name;
                app.RequestSave();
            }
            if (!OnTop)
                Native.SendToBottom(Handle);
            Invalidate();
        }
    }
}
