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
        private RichTextBox? editor;
        private Form? editorHost;
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
                ApplyEditorColors(editor);
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

            // The editor lives in its own small, normal window laid exactly over the note. Inside the
            // fence it can't work: the fence's transparency (glass/clear accent) drops the alpha of
            // classic controls, so text and background come out white/see-through whatever the colors.
            var (back, fore) = theme.EditorColors;
            editor = new RichTextBox
            {
                Multiline = true,
                AcceptsTab = true,
                WordWrap = true,
                DetectUrls = false,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
                Font = noteFont,
                BackColor = back,
                ForeColor = fore,
                Dock = DockStyle.Fill,
                Text = Info.NoteText
            };
            editorHost = new EditorHost { BackColor = back };
            editorHost.Controls.Add(editor);
            editorHost.Deactivate += (_, _) => BeginInvoke(EndEditNote);
            ApplyEditorColors(editor);
            editor.HandleCreated += (_, _) => ApplyEditorColors(editor);
            editor.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    e.SuppressKeyPress = true;
                    EndEditNote();
                }
                else if (e.Control && e.KeyCode == Keys.V || e.Shift && e.KeyCode == Keys.Insert)
                {
                    // Paste as plain text in the note's own font and color.
                    e.SuppressKeyPress = true;
                    if (Clipboard.ContainsText())
                        editor!.SelectedText = Clipboard.GetText().Replace("\r\n", "\n");
                    ApplyEditorColors(editor!);
                }
            };
            LayoutEditor();
            editorHost.Show(this);
            editorHost.Activate();
            Native.SetForegroundWindowSafe(editorHost.Handle);
            editor.Focus();
            editor.SelectionStart = editor.TextLength;
            Invalidate();
        }

        /// <summary>Borderless, opaque window that carries the note editor.</summary>
        private sealed class EditorHost : Form
        {
            public EditorHost()
            {
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.Manual;
                AutoScaleMode = AutoScaleMode.None;
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;
                    cp.ExStyle |= Native.WS_EX_TOOLWINDOW; // no Alt+Tab entry
                    return cp;
                }
            }
        }

        /// <summary>Forces font and colors on the whole text and on what will be typed next.</summary>
        private void ApplyEditorColors(RichTextBox box)
        {
            var (back, fore) = theme.EditorColors;
            box.BackColor = back;
            box.ForeColor = fore;
            var (start, length) = (box.SelectionStart, box.SelectionLength);
            box.SelectAll();
            box.SelectionColor = fore;
            box.SelectionBackColor = back;
            if (noteFont != null)
                box.SelectionFont = noteFont;
            box.Select(start, length);
            box.SelectionColor = fore;
        }

        private void EndEditNote()
        {
            if (editor == null)
                return;
            var text = editor.Text.Replace("\r\n", "\n").TrimEnd();
            var host = editorHost;
            editor = null;
            editorHost = null;
            host?.Close();
            host?.Dispose();

            if (text != Info.NoteText)
            {
                Info.NoteText = text;
                app.RequestSave();
            }
            if (!OnTop)
                Native.SendToBottom(Handle);
            Invalidate();
        }

        /// <summary>Keeps the editor window exactly over the note area (after moving/resizing the fence).</summary>
        private void LayoutEditor()
        {
            if (editorHost == null || !IsHandleCreated)
                return;
            var area = NoteArea;
            editorHost.Bounds = new Rectangle(PointToScreen(area.Location), area.Size);
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
            editorHost?.Dispose();
            noteFont?.Dispose();
            noteFontDone?.Dispose();
            checkFont?.Dispose();
            noteFormat.Dispose();
        }
    }
}
