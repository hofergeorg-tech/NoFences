using System.Drawing.Drawing2D;
using NoFences.Model;
using NoFences.Util;

namespace NoFences
{
    /// <summary>
    /// Checklist extras of notes: fold arrows for sub-items, counters "[3/8]", tags "#work", tables,
    /// the progress in the title, finished items at the end or hidden, recurring reset and text size.
    /// </summary>
    public sealed partial class FenceWindow
    {
        private readonly List<(RectangleF rect, int line, int index)> counters = new(); // content coordinates
        private readonly List<(RectangleF rect, string key)> foldArrows = new();         // content coordinates

        #region Fold arrows

        private void DrawFoldArrow(Graphics g, float x, float y, Font font, bool folded, string key, Rectangle view)
        {
            var h = font.GetHeight(g);
            var size = Px(10);
            var r = new RectangleF(x, y + (h - size) / 2, size, size);
            foldArrows.Add((RectangleF.Inflate(new RectangleF(r.X, r.Y + scrollOffset - titleHeight, r.Width, r.Height), Px(4), Px(4)), key));
            if (y + h < view.Top || y > view.Bottom)
                return;
            using var brush = new SolidBrush(Color.FromArgb(200, theme.HintColor));
            var points = folded
                ? new[] { new PointF(r.Left + size * 0.2f, r.Top), new PointF(r.Right, r.Top + size / 2), new PointF(r.Left + size * 0.2f, r.Bottom) }
                : new[] { new PointF(r.Left, r.Top + size * 0.2f), new PointF(r.Right, r.Top + size * 0.2f), new PointF(r.Left + size / 2, r.Bottom) };
            var mode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.FillPolygon(brush, points);
            g.SmoothingMode = mode;
        }

        private void ToggleFold(string key)
        {
            Info.NoteFolded ??= new List<string>();
            if (!Info.NoteFolded.Remove(key))
                Info.NoteFolded.Add(key);
            if (Info.NoteFolded.Count == 0)
                Info.NoteFolded = null;
            app.RequestSave();
            Invalidate();
        }

        #endregion

        #region Counters and tags

        private float CounterWidth(Graphics g, string text, Font font) =>
            g.MeasureString(text.Trim('[', ']'), font, PointF.Empty, StringFormat.GenericTypographic).Width + Px(14);

        /// <summary>A pill "3/8" filled up to the count; a click counts up, Shift+click down.</summary>
        private void DrawCounter(Graphics g, System.Text.RegularExpressions.Match counter, RectangleF cell, Font font, int line, int index)
        {
            var value = int.Parse(counter.Groups[1].Value);
            var goal = Math.Max(1, int.Parse(counter.Groups[2].Value));
            var h = cell.Height * 0.86f;
            var r = new RectangleF(cell.X, cell.Y + (cell.Height - h) / 2, cell.Width - Px(4), h);
            var mode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = RoundedRect(r, h / 2))
            {
                using (var track = new SolidBrush(Color.FromArgb(60, theme.HintColor)))
                    g.FillPath(track, path);
                var state = g.Save();
                g.SetClip(path, CombineMode.Intersect);
                using (var fill = new SolidBrush(Color.FromArgb(value >= goal ? 200 : 110, value >= goal ? Color.FromArgb(70, 170, 90) : theme.Accent)))
                    g.FillRectangle(fill, r.X, r.Y, r.Width * Math.Min(1f, value / (float)goal), r.Height);
                g.Restore(state);
            }
            g.SmoothingMode = mode;
            using (var format = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                theme.DrawLabel(g, $"{value}/{goal}", r, font, format, scale);
            counters.Add((new RectangleF(r.X, r.Y + scrollOffset - titleHeight, r.Width, r.Height), line, index));
        }

        private float TagWidth(Graphics g, string tag, Font font)
        {
            using var small = new Font(font.FontFamily, font.Size * 0.85f, FontStyle.Bold, font.Unit);
            return g.MeasureString(tag, small, PointF.Empty, StringFormat.GenericTypographic).Width + Px(12);
        }

        /// <summary>A colored pill per tag (always the same color for the same tag); a click searches for it.</summary>
        private void DrawTag(Graphics g, string tag, RectangleF cell, Font font)
        {
            using var small = new Font(font.FontFamily, font.Size * 0.85f, FontStyle.Bold, font.Unit);
            var h = cell.Height * 0.82f;
            var r = new RectangleF(cell.X, cell.Y + (cell.Height - h) / 2, cell.Width - Px(4), h);
            var mode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = RoundedRect(r, h / 2))
            using (var fill = new SolidBrush(FromHue(NoteLists.TagHue(tag))))
                g.FillPath(fill, path);
            g.SmoothingMode = mode;
            using (var format = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(tag, small, Brushes.White, r, format);
            links.Add((new RectangleF(r.X, r.Y + scrollOffset - titleHeight, r.Width, r.Height), "tag:" + tag));
        }

        /// <summary>A medium-dark color of the given hue (white text stays readable on it).</summary>
        private static Color FromHue(int hue)
        {
            double h = hue / 60.0, c = 0.55, x = c * (1 - Math.Abs(h % 2 - 1)), m = 0.22;
            var (r, g, b) = (int)h switch
            {
                0 => (c, x, 0d), 1 => (x, c, 0d), 2 => (0d, c, x), 3 => (0d, x, c), 4 => (x, 0d, c), _ => (c, 0d, x)
            };
            return Color.FromArgb(225, (int)((r + m) * 255), (int)((g + m) * 255), (int)((b + m) * 255));
        }

        #endregion

        #region Tables

        /// <summary>Draws "| a | b |" lines as a table (the line after the first "|---|" makes the first row a header); returns the height.</summary>
        private float DrawTable(Graphics g, List<string> rows, float x, float y, float width, Rectangle view)
        {
            var font = noteFont!;
            var cells = new List<(List<string> Cells, bool Header)>();
            for (var r = 0; r < rows.Count; r++)
            {
                var parsed = NoteLists.Cells(rows[r]);
                if (parsed == null)
                {
                    if (cells.Count == 1)
                        cells[0] = (cells[0].Cells, true);
                    continue;
                }
                cells.Add((parsed, false));
            }
            if (cells.Count == 0)
                return 0;
            var columns = cells.Max(c => c.Cells.Count);
            var pad = Px(6);
            var format = StringFormat.GenericTypographic;
            // Column widths: as wide as the widest cell, shrunk together if the table is too wide
            var widths = new float[columns];
            foreach (var (row, header) in cells)
                for (var c = 0; c < row.Count; c++)
                {
                    var text = string.Concat(NoteText.Runs(row[c]).Select(run => run.Text));
                    widths[c] = Math.Max(widths[c], g.MeasureString(text, header ? Styled(font, FontStyle.Bold) : font, PointF.Empty, format).Width + 2 * pad);
                }
            var total = widths.Sum();
            if (total > width)
                for (var c = 0; c < columns; c++)
                    widths[c] *= width / total;

            var top = y;
            using var grid = new Pen(Color.FromArgb(110, theme.HintColor), Math.Max(1, scale));
            using var headerFill = new SolidBrush(Color.FromArgb(40, theme.HintColor));
            foreach (var (row, header) in cells)
            {
                // Row height: the tallest cell (cells wrap inside their column)
                var heights = new float[columns];
                for (var c = 0; c < row.Count; c++)
                    heights[c] = DrawRuns(g, Header(row[c], header), 0, 0, Math.Max(1, widths[c] - 2 * pad), false, font);
                var h = Math.Max(font.GetHeight(g), heights.Max()) + Px(4);
                var visible = y + h >= view.Top && y <= view.Bottom;
                if (visible)
                {
                    if (header)
                        g.FillRectangle(headerFill, x, y, widths.Sum(), h);
                    var cx = x;
                    for (var c = 0; c < columns; c++)
                    {
                        if (c < row.Count)
                            DrawRuns(g, Header(row[c], header), cx + pad, y + Px(2), Math.Max(1, widths[c] - 2 * pad), true, font);
                        g.DrawRectangle(grid, cx, y, widths[c], h);
                        cx += widths[c];
                    }
                }
                y += h;
            }
            return y - top + Px(4);

            static List<NoteText.Run> Header(string text, bool header) =>
                header ? NoteText.Runs(text).Select(r => r with { Bold = true }).ToList() : NoteText.Runs(text);
        }

        #endregion

        #region Progress in the title

        /// <summary>"3/7" and a small bar in the title row of notes with checkboxes.</summary>
        private void DrawNoteProgress(Graphics g)
        {
            if (labelFont == null || NoteLockedNow)
                return;
            var (done, total) = NoteLists.Progress(NoteContent);
            if (total == 0)
                return;
            // Inside the theme's surface (the Post-it's paper starts below its tape margin)
            var ins = theme.SurfaceInsets;
            var inset = Px(ins.Left + 10 + theme.ContentInset);
            var top = Px(ins.Top);
            var middle = top + (titleHeight - top) / 2f;
            var text = $"{done}/{total}";
            var size = g.MeasureString(text, labelFont);
            var barWidth = Px(34);
            var y = middle - size.Height / 2 + Px(1);
            var bar = new RectangleF(inset + size.Width + Px(2), middle - Px(2), barWidth, Px(4));
            using (var brush = new SolidBrush(Color.FromArgb(230, theme.HintColor)))
                g.DrawString(text, labelFont, brush, inset, y);
            using (var track = new SolidBrush(Color.FromArgb(60, theme.HintColor)))
                g.FillRectangle(track, bar);
            using (var fill = new SolidBrush(done == total ? Color.FromArgb(70, 170, 90) : theme.Accent))
                g.FillRectangle(fill, bar.X, bar.Y, bar.Width * done / total, bar.Height);
        }

        #endregion

        #region Clicks

        /// <summary>Counters, fold arrows and tags; true if the click was one of them.</summary>
        private bool NoteListClick(PointF content)
        {
            foreach (var (rect, line, index) in counters)
            {
                if (!rect.Contains(content))
                    continue;
                var delta = (ModifierKeys & Keys.Shift) != 0 ? -1 : 1;
                NoteContent = NoteLists.StepCounter(NoteContent, line, index, delta);
                app.RequestSave();
                Invalidate();
                return true;
            }
            foreach (var (rect, key) in foldArrows)
            {
                if (!rect.Contains(content))
                    continue;
                ToggleFold(key);
                return true;
            }
            return false;
        }

        #endregion

        #region Menu

        private void AddChecklistItems(ToolStripItemCollection items)
        {
            var menu = new ToolStripMenuItem(Strings.ChecklistMenu);
            foreach (var mode in Enum.GetValues<NoteDoneMode>())
            {
                var m = mode;
                menu.DropDownItems.Add(new ToolStripMenuItem(Strings.NoteDoneName(mode), null, (_, _) =>
                {
                    Info.NoteDone = m;
                    app.RequestSave();
                    Invalidate();
                }) { Checked = Info.NoteDone == mode });
            }
            menu.DropDownItems.Add(new ToolStripSeparator());
            var reset = new ToolStripMenuItem(Strings.ChecklistReset);
            foreach (var repeat in new[] { Repeat.None, Repeat.Daily, Repeat.Weekdays, Repeat.Weekly, Repeat.Monthly })
            {
                var r = repeat;
                reset.DropDownItems.Add(new ToolStripMenuItem(repeat == Repeat.None ? Strings.ChecklistResetNever : Strings.RepeatName(repeat), null, (_, _) =>
                {
                    Info.NoteResetRepeat = r;
                    Info.NoteLastReset = DateTime.Now; // from the next period on
                    app.RequestSave();
                }) { Checked = Info.NoteResetRepeat == repeat });
            }
            menu.DropDownItems.Add(reset);
            menu.DropDownItems.Add(Strings.ChecklistClearNow, null, (_, _) => ResetChecklist());
            items.Add(menu);
        }

        /// <summary>Unticks everything and sets counters to 0 (also the daily/weekly reset).</summary>
        internal void ResetChecklist()
        {
            if (NoteLockedNow || Editing)
                return;
            var reset = NoteLists.Reset(NoteContent);
            Info.NoteLastReset = DateTime.Now;
            if (reset != NoteContent)
            {
                NoteContent = reset;
                Invalidate();
            }
            app.RequestSave();
        }

        #endregion

        #region Text size

        /// <summary>Ctrl+mouse wheel over a note: bigger or smaller text (remembered per note).</summary>
        private bool ZoomNote(int delta)
        {
            if (!IsNote || (ModifierKeys & Keys.Control) == 0)
                return false;
            var zoom = Math.Clamp(Info.NoteZoom + Math.Sign(delta) * 10, 60, 250);
            if (zoom != Info.NoteZoom)
            {
                Info.NoteZoom = zoom;
                ApplySettings();
                app.RequestSave();
            }
            return true;
        }

        #endregion
    }
}
