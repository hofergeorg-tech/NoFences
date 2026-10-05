using NoFences.Model;
using NoFences.Util;

namespace NoFences
{
    /// <summary>
    /// The formatting bar above the note editor: bold, italic, underline, strikeout, marker colors and
    /// priority. It writes the same markup that can be typed (**, *, __, ~~, ==, !!!), so notes stay text.
    /// Shortcuts: Ctrl+B, Ctrl+I, Ctrl+U, Ctrl+H (marker).
    /// </summary>
    public sealed partial class FenceWindow
    {
        /// <summary>A button that never takes the focus, so the editor keeps its selection.</summary>
        private sealed class BarButton : Button
        {
            public BarButton()
            {
                SetStyle(ControlStyles.Selectable, false);
                TabStop = false;
                FlatStyle = FlatStyle.Flat;
                FlatAppearance.BorderSize = 0;
                Margin = new Padding(1);
                Padding = Padding.Empty;
                UseVisualStyleBackColor = false;
                Cursor = Cursors.Hand;
            }
        }

        private Control CreateFormatBar(RichTextBox box)
        {
            var (back, fore) = theme.EditorColors;
            var barBack = Shade(back, 0.08f);
            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                BackColor = barBack,
                Padding = new Padding(Px(3), Px(2), Px(3), Px(2))
            };
            var tips = new ToolTip();
            bar.Disposed += (_, _) => tips.Dispose();
            var size = new Size(Px(26), Px(24));
            var baseFont = new Font("Segoe UI", 9.5f * scale, FontStyle.Regular, GraphicsUnit.Pixel);
            bar.Disposed += (_, _) => baseFont.Dispose();

            BarButton Add(string text, FontStyle style, string tip, Action action, Color? fill = null, Color? ink = null)
            {
                var b = new BarButton
                {
                    Text = text,
                    Size = size,
                    BackColor = fill ?? barBack,
                    ForeColor = ink ?? fore,
                    Font = style == FontStyle.Regular ? baseFont : new Font(baseFont, style)
                };
                b.FlatAppearance.MouseOverBackColor = fill == null ? Shade(barBack, 0.12f) : Shade(fill.Value, 0.12f);
                b.Click += (_, _) =>
                {
                    action();
                    box.Focus();
                };
                tips.SetToolTip(b, tip);
                bar.Controls.Add(b);
                return b;
            }

            void Gap() => bar.Controls.Add(new Label { Width = Px(6), Height = size.Height, Margin = Padding.Empty });

            Add(Strings.FormatBoldShort, FontStyle.Bold, Strings.FormatBold, () => Wrap(box, "**"));
            Add(Strings.FormatItalicShort, FontStyle.Italic, Strings.FormatItalic, () => Wrap(box, "*"));
            Add(Strings.FormatUnderlineShort, FontStyle.Underline, Strings.FormatUnderline, () => Wrap(box, "__"));
            Add("ab", FontStyle.Strikeout, Strings.FormatStrike, () => Wrap(box, "~~"));
            Gap();
            foreach (var color in NoteText.HighlightColors)
            {
                var c = color;
                Add("", FontStyle.Regular, Strings.FormatHighlight(Strings.MarkerColorName(c)), () => Highlight(box, c), HighlightColor(c));
            }
            Add("✕", FontStyle.Regular, Strings.FormatNoHighlight, () => Highlight(box, null));
            Gap();
            for (var level = 3; level >= 1; level--)
            {
                var l = level;
                Add(new string('!', level), FontStyle.Bold, Strings.PriorityName(level), () => SetLinePriority(box, l), PriorityColor(level), Color.White);
            }
            Add("–", FontStyle.Bold, Strings.PriorityName(0), () => SetLinePriority(box, 0));
            return bar;
        }

        private static Color Shade(Color c, float amount)
        {
            // Darker for light colors, lighter for dark ones
            var light = c.GetBrightness() > 0.5f;
            int Mix(int v) => (int)Math.Clamp(light ? v * (1 - amount) : v + (255 - v) * amount, 0, 255);
            return Color.FromArgb(Mix(c.R), Mix(c.G), Mix(c.B));
        }

        /// <summary>Ctrl+B/I/U/H in the editor; true if handled.</summary>
        private bool FormatShortcut(RichTextBox box, KeyEventArgs e)
        {
            if (!e.Control || e.Alt || e.Shift)
                return false;
            switch (e.KeyCode)
            {
                case Keys.B: Wrap(box, "**"); return true;
                case Keys.I: Wrap(box, "*"); return true;
                case Keys.U: Wrap(box, "__"); return true;
                case Keys.H: Highlight(box, 'y'); return true;
                default: return false;
            }
        }

        private void Wrap(RichTextBox box, string marker) =>
            Replace(box, NoteText.ToggleWrap(EditorText(box), box.SelectionStart, box.SelectionLength, marker));

        private void Highlight(RichTextBox box, char? color) =>
            Replace(box, NoteText.SetHighlight(EditorText(box), box.SelectionStart, box.SelectionLength, color));

        private void SetLinePriority(RichTextBox box, int level)
        {
            var (text, caret) = NoteText.SetPriority(EditorText(box), box.SelectionStart, level);
            Replace(box, (text, caret, 0));
        }

        /// <summary>The editor's text as the selection positions count it (line breaks as one character).</summary>
        private static string EditorText(RichTextBox box) => box.Text.Replace("\r\n", "\n");

        private void Replace(RichTextBox box, (string Text, int Start, int Length) result)
        {
            if (result.Text != EditorText(box))
            {
                // Through the selection, so Ctrl+Z in the editor can take it back
                box.SelectAll();
                box.SelectedText = result.Text;
                ApplyEditorColors(box);
            }
            box.Select(Math.Min(result.Start, box.TextLength), Math.Min(result.Length, Math.Max(0, box.TextLength - result.Start)));
            lastNoteActivity = DateTime.Now;
        }
    }
}
