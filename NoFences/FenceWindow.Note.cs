using System.Text.RegularExpressions;
using NoFences.Model;
using NoFences.Util;
using NoFences.Win32;

namespace NoFences
{
    /// <summary>
    /// Note fences: the text is painted by the theme (so it sits on the glass/paper look); double-click
    /// swaps in a real text box for editing. Lines starting with "[ ]" or "[x]" are clickable checkboxes.
    /// </summary>
    public sealed partial class FenceWindow
    {
        private Font? noteFont;
        private Font? noteFontDone;
        private Font? checkFont;
        private TextBox? editor;
        private readonly List<(RectangleF box, int line)> checkboxes = new(); // content coordinates
        private readonly StringFormat noteFormat = new() { Alignment = StringAlignment.Near, Trimming = StringTrimming.None };

        [GeneratedRegex(@"^(\s*(?:[-*]\s+)?)\[([ xX])\]\s?")]
        private static partial Regex CheckboxPrefix();

        private bool IsNote => Info.Kind == FenceKind.Note;

        private bool Editing => editor != null;

        private void ApplyNoteSettings()
        {
            noteFont?.Dispose();
            noteFontDone?.Dispose();
            checkFont?.Dispose();
            noteFont = theme.CreateNoteFont(scale);
            noteFontDone = new Font(noteFont, FontStyle.Strikeout);
            checkFont = new Font("Segoe UI Symbol", noteFont.Size, FontStyle.Regular, GraphicsUnit.Pixel);
            if (editor != null)
            {
                editor.Font = noteFont;
                (editor.BackColor, editor.ForeColor) = theme.EditorColors;
            }
        }

        private Rectangle NoteArea => new(Px(12 + theme.ContentInset), titleHeight + Px(8), ClientSize.Width - Px(24 + 2 * theme.ContentInset), Math.Max(0, ViewHeight - Px(16)));

        private void DrawNote(Graphics g, Rectangle view)
        {
            checkboxes.Clear();
            if (noteFont == null || noteFontDone == null || checkFont == null || Editing)
                return;

            var area = NoteArea;
            if (string.IsNullOrWhiteSpace(Info.NoteText))
            {
                using var hint = new SolidBrush(theme.HintColor);
                using var center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(Strings.NoteHint, labelFont!, hint, area, center);
                contentHeight = 0;
                return;
            }

            var y = (float)area.Y - scrollOffset;
            var lines = Info.NoteText.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var x = (float)area.X;
                var font = noteFont;
                var match = CheckboxPrefix().Match(line);
                if (match.Success)
                {
                    var done = match.Groups[2].Value != " ";
                    var glyph = done ? "☑" : "☐";
                    var boxSize = g.MeasureString(glyph, checkFont);
                    theme.DrawLabel(g, glyph, new RectangleF(x, y, boxSize.Width, boxSize.Height), checkFont, noteFormat, scale);
                    checkboxes.Add((new RectangleF(x, y + scrollOffset - titleHeight, boxSize.Width, boxSize.Height), i));
                    x += boxSize.Width + Px(2);
                    line = line[match.Length..];
                    if (done)
                        font = noteFontDone;
                }

                var width = area.Right - x;
                var height = line.Length == 0 ? font.Height : g.MeasureString(line, font, (int)Math.Max(1, width), noteFormat).Height;
                if (y + height >= view.Top && y <= view.Bottom)
                    theme.DrawLabel(g, line, new RectangleF(x, y, width, height + 2), font, noteFormat, scale);
                y += height;
            }

            // Content coordinates are relative to the top of the view, like the item layout.
            contentHeight = (int)(y + scrollOffset - titleHeight) + Px(8);
        }

        /// <summary>Handles a left click in a note; returns true if it toggled a checkbox.</summary>
        private bool NoteClick(Point client)
        {
            if (Editing || collapsed)
                return false;
            var p = new PointF(client.X, client.Y - titleHeight + scrollOffset);
            foreach (var (box, line) in checkboxes)
            {
                if (!RectangleF.Inflate(box, Px(3), Px(3)).Contains(p))
                    continue;
                var lines = Info.NoteText.Split('\n');
                var m = CheckboxPrefix().Match(lines[line]);
                if (!m.Success)
                    return false;
                var state = m.Groups[2];
                var toggled = state.Value == " " ? "x" : " ";
                lines[line] = lines[line][..state.Index] + toggled + lines[line][(state.Index + state.Length)..];
                Info.NoteText = string.Join('\n', lines);
                app.RequestSave();
                Invalidate();
                return true;
            }
            return false;
        }

        public void StartEditNote()
        {
            if (!IsNote || Editing)
                return;
            if (collapsed)
                Expand();

            var (back, fore) = theme.EditorColors;
            editor = new TextBox
            {
                Multiline = true,
                AcceptsReturn = true,
                AcceptsTab = true,
                WordWrap = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
                Font = noteFont,
                BackColor = back,
                ForeColor = fore,
                Bounds = NoteArea,
                Text = Info.NoteText.Replace("\n", "\r\n")
            };
            editor.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    e.SuppressKeyPress = true;
                    EndEditNote();
                }
            };
            editor.LostFocus += (_, _) => BeginInvoke(EndEditNote);
            Controls.Add(editor);

            // Fences normally refuse focus; while editing they must take it.
            Activate();
            Native.SetForegroundWindowSafe(Handle);
            editor.Focus();
            editor.SelectionStart = editor.TextLength;
            Invalidate();
        }

        private void EndEditNote()
        {
            if (editor == null)
                return;
            var text = editor.Text.Replace("\r\n", "\n").TrimEnd();
            var box = editor;
            editor = null;
            Controls.Remove(box);
            box.Dispose();

            if (text != Info.NoteText)
            {
                Info.NoteText = text;
                app.RequestSave();
            }
            if (!Peeking)
                Native.SendToBottom(Handle);
            Invalidate();
        }

        private void LayoutEditor()
        {
            if (editor != null)
                editor.Bounds = NoteArea;
        }

        /// <summary>Text dropped onto a note is appended as a new line.</summary>
        private void AppendDroppedText(string text)
        {
            text = text.Replace("\r\n", "\n").Trim();
            if (text.Length == 0)
                return;
            Info.NoteText = string.IsNullOrEmpty(Info.NoteText) ? text : Info.NoteText.TrimEnd() + "\n" + text;
            app.RequestSave();
            Invalidate();
        }

        private void DisposeNote()
        {
            editor?.Dispose();
            noteFont?.Dispose();
            noteFontDone?.Dispose();
            checkFont?.Dispose();
            noteFormat.Dispose();
        }
    }
}
