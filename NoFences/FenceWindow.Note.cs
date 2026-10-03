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
        // Markdown-style formatting: bold, italic and three heading sizes
        private Font? noteBold, noteItalic, heading1, heading2, heading3;
        private RichTextBox? editor;
        private Form? editorHost;
        private readonly List<(RectangleF box, int line)> checkboxes = new(); // content coordinates
        private readonly List<(RectangleF rect, string target)> links = new(); // content coordinates
        private readonly StringFormat noteFormat = new() { Alignment = StringAlignment.Near, Trimming = StringTrimming.None };

        private static Regex CheckboxPrefix() => NoteText.CheckboxPrefix();

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
            foreach (var f in new[] { noteBold, noteItalic, heading1, heading2, heading3 })
                f?.Dispose();
            noteBold = new Font(noteFont, FontStyle.Bold);
            noteItalic = new Font(noteFont, FontStyle.Italic);
            heading1 = new Font(noteFont.FontFamily, noteFont.Size * 1.45f, FontStyle.Bold, noteFont.Unit);
            heading2 = new Font(noteFont.FontFamily, noteFont.Size * 1.2f, FontStyle.Bold, noteFont.Unit);
            heading3 = new Font(noteFont.FontFamily, noteFont.Size * 1.05f, FontStyle.Bold, noteFont.Unit);
            // Without tab stops GDI+ draws tabs differently than the editor; use the same grid in both.
            noteFormat.SetTabStops(0, new[] { TabWidth });
            if (editor != null)
            {
                editor.Font = noteFont;
                ApplyEditorColors(editor);
            }
        }

        /// <summary>One tab = about six average characters of the note font (pixels).</summary>
        private float TabWidth => (noteFont?.Size ?? 16) * 3f;

        private void ApplyEditorTabs(RichTextBox box)
        {
            var (start, length) = (box.SelectionStart, box.SelectionLength);
            box.SelectAll();
            // RichTextBox allows 32 tab stops; repeat the same width as the painted text.
            box.SelectionTabs = Enumerable.Range(1, 32).Select(i => (int)Math.Round(i * TabWidth)).ToArray();
            box.Select(start, length);
        }

        private Rectangle NoteArea => new(
            Px(12 + theme.ContentInset),
            titleHeight + Px(8),
            Math.Max(Px(20), ClientSize.Width - Px(24 + 2 * theme.ContentInset)),
            Math.Max(Px(20), ViewHeight - Px(16 + theme.BottomInset)));

        private void DrawNote(Graphics g, Rectangle view)
        {
            checkboxes.Clear();
            links.Clear();
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

            // Keep long notes on the paper, out of margins/shadows the theme draws around it.
            g.SetClip(Rectangle.Intersect(view, new Rectangle(0, area.Top - Px(4), ClientSize.Width, area.Height + Px(8))));

            var y = (float)area.Y - scrollOffset;
            (float X, float Y)? quoteBar = null;
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
                else
                {
                    // Markdown-style lines: headings, bullets, quotes, rules
                    var (kind, content) = NoteText.ParseLine(line);
                    switch (kind)
                    {
                        case NoteText.LineKind.Rule:
                            using (var pen = new Pen(Color.FromArgb(120, theme.HintColor), Math.Max(1, scale)))
                                g.DrawLine(pen, x, y + font.Height * 0.45f, area.Right, y + font.Height * 0.45f);
                            y += font.Height * 0.9f;
                            continue;
                        case NoteText.LineKind.Heading1:
                        case NoteText.LineKind.Heading2:
                        case NoteText.LineKind.Heading3:
                            font = kind == NoteText.LineKind.Heading1 ? heading1! : kind == NoteText.LineKind.Heading2 ? heading2! : heading3!;
                            if (i > 0)
                                y += Px(4);
                            line = content;
                            break;
                        case NoteText.LineKind.Bullet:
                            var bullet = g.MeasureString("• ", font);
                            theme.DrawLabel(g, "•", new RectangleF(x, y, bullet.Width, bullet.Height), font, noteFormat, scale);
                            x += bullet.Width;
                            line = content;
                            break;
                        case NoteText.LineKind.Quote:
                            quoteBar = (x, y);
                            x += Px(10);
                            font = noteItalic!;
                            line = content;
                            break;
                    }
                }

                var width = area.Right - x;
                float height;
                if (NoteText.HasInlineFormatting(line) && font == noteFont)
                {
                    // **bold** / *italic* runs: laid out word by word (links aren't underlined here)
                    var visible = y + font.Height * 3 >= view.Top && y <= view.Bottom;
                    height = DrawRuns(g, NoteText.Runs(line), x, y, width, visible);
                }
                else
                {
                    height = line.Length == 0 ? font.Height : g.MeasureString(line, font, (int)Math.Max(1, width), noteFormat).Height;
                    var layout = new RectangleF(x, y, width, height + 2);
                    if (y + height >= view.Top && y <= view.Bottom)
                    {
                        theme.DrawLabel(g, line, layout, font, noteFormat, scale);
                        MarkLinks(g, line, layout, font);
                    }
                }
                if (quoteBar is (float qx, float qy))
                {
                    using var bar = new SolidBrush(Color.FromArgb(150, theme.Accent));
                    g.FillRectangle(bar, qx, qy + Px(2), Px(3), height - Px(2));
                    quoteBar = null;
                }
                y += height;
            }

            // Content coordinates are relative to the top of the view, like the item layout.
            contentHeight = (int)(y + scrollOffset - titleHeight) + Px(8);
        }

        /// <summary>Draws bold/italic runs with word wrapping; returns the height used.</summary>
        private float DrawRuns(Graphics g, List<NoteText.Run> runs, float x, float y, float width, bool draw)
        {
            var format = StringFormat.GenericTypographic;
            var lineHeight = noteFont!.GetHeight(g);
            var cx = x;
            var cy = y;
            var space = g.MeasureString(" ", noteFont, PointF.Empty, format).Width + noteFont.Size * 0.25f;
            foreach (var run in runs)
            {
                var font = run.Bold ? noteBold! : run.Italic ? noteItalic! : noteFont;
                var words = run.Text.Split(' ');
                for (var w = 0; w < words.Length; w++)
                {
                    var word = words[w];
                    if (word.Length > 0)
                    {
                        var size = g.MeasureString(word, font, PointF.Empty, format);
                        if (cx + size.Width > x + width && cx > x)
                        {
                            cx = x;
                            cy += lineHeight;
                        }
                        if (draw)
                            theme.DrawLabel(g, word, new RectangleF(cx, cy, size.Width + 2, lineHeight + 2), font, format, scale);
                        cx += size.Width;
                    }
                    if (w < words.Length - 1)
                        cx += space;
                }
            }
            return cy - y + lineHeight;
        }

        /// <summary>Underlines web addresses and paths in a drawn line and remembers where they are.</summary>
        private void MarkLinks(Graphics g, string line, RectangleF layout, Font font)
        {
            var found = NoteText.FindLinks(line).Take(32).ToList(); // GDI+ measures at most 32 ranges at once
            if (found.Count == 0)
                return;

            using var format = (StringFormat)noteFormat.Clone();
            format.SetMeasurableCharacterRanges(found.Select(l => new CharacterRange(l.Start, l.Length)).ToArray());
            var regions = g.MeasureCharacterRanges(line, font, layout, format);
            using var pen = new Pen(Color.FromArgb(220, theme.HintColor), Math.Max(1, scale));
            using var identity = new System.Drawing.Drawing2D.Matrix();
            for (var i = 0; i < regions.Length; i++)
            {
                using var region = regions[i];
                // A link wrapped over several lines yields one rectangle per line.
                foreach (var r in region.GetRegionScans(identity))
                {
                    g.DrawLine(pen, r.Left, r.Bottom - Px(2), r.Right, r.Bottom - Px(2));
                    links.Add((new RectangleF(r.X, r.Y + scrollOffset - titleHeight, r.Width, r.Height), found[i].Target));
                }
            }
        }

        private string? LinkAt(Point client)
        {
            var p = new PointF(client.X, client.Y - titleHeight + scrollOffset);
            return links.FirstOrDefault(l => l.rect.Contains(p)).target;
        }

        /// <summary>Handles a left click in a note; returns true if it toggled a checkbox or opened a link.</summary>
        private bool NoteClick(Point client)
        {
            if (Editing || collapsed)
                return false;
            var p = new PointF(client.X, client.Y - titleHeight + scrollOffset);
            foreach (var (box, line) in checkboxes)
            {
                if (!RectangleF.Inflate(box, Px(3), Px(3)).Contains(p))
                    continue;
                Info.NoteText = NoteText.ToggleCheckbox(Info.NoteText, line);
                app.RequestSave();
                Invalidate();
                return true;
            }

            var target = LinkAt(client);
            if (target != null)
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(target) { UseShellExecute = true });
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "NoFences", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return true;
            }
            return false;
        }

        /// <summary>Small "⏰ 18:00" in the title row while a reminder is set.</summary>
        private void DrawReminderBadge(Graphics g)
        {
            if (Info.ReminderAt is not DateTime at || labelFont == null)
                return;
            var when = at.Date == DateTime.Today ? at.ToString("HH:mm") : at.ToString("dd.MM. HH:mm");
            using var brush = new SolidBrush(Color.FromArgb(230, theme.HintColor));
            using var format = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Far };
            var inset = Px(12 + theme.ContentInset);
            g.DrawString((Info.ReminderRepeat != Repeat.None ? "↻ " : "⏰ ") + when, labelFont, brush, new RectangleF(inset, 0, ClientSize.Width - 2 * inset, titleHeight + Px(4)), format);
        }

        private void EditReminder()
        {
            using var dialog = new ReminderDialog(Info.ReminderAt, Info.ReminderRepeat);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;
            Info.ReminderAt = dialog.Result;
            Info.ReminderRepeat = dialog.Result == null ? Repeat.None : dialog.ResultRepeat;
            app.RequestSave();
            Invalidate();
        }

        /// <summary>Hand cursor over links and checkboxes.</summary>
        private void UpdateNoteCursor(Point client)
        {
            var p = new PointF(client.X, client.Y - titleHeight + scrollOffset);
            var clickable = LinkAt(client) != null || checkboxes.Any(c => RectangleF.Inflate(c.box, Px(3), Px(3)).Contains(p));
            Cursor = clickable ? Cursors.Hand : Cursors.Default;
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
            ApplyEditorTabs(box);
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
            foreach (var f in new[] { noteBold, noteItalic, heading1, heading2, heading3 })
                f?.Dispose();
            noteFormat.Dispose();
        }
    }
}
