using System.Drawing.Printing;
using NoFences.Model;
using NoFences.Util;

namespace NoFences
{
    /// <summary>Earlier states of a note: pick one, see it, bring it back.</summary>
    internal sealed class NoteVersionsDialog : Form
    {
        private readonly ListBox list = new() { Width = 200, Height = 300, IntegralHeight = false };
        private readonly TextBox preview = new() { Width = 380, Height = 300, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, WordWrap = true };

        /// <summary>The chosen version's text (already decrypted), or null.</summary>
        public string? Chosen { get; private set; }

        public NoteVersionsDialog(string noteName, IReadOnlyList<NoteVersion> versions, Func<NoteVersion, string?> read)
        {
            Text = Strings.NoteVersionsTitle(noteName);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(14);
            Font = SystemFonts.MessageBoxFont ?? Font;

            var newestFirst = versions.Reverse().ToList();
            var culture = new System.Globalization.CultureInfo(Strings.Effective);
            foreach (var v in newestFirst)
                list.Items.Add(v.Time.ToString("g", culture));
            var restore = new Button { Text = Strings.NoteVersionRestore, AutoSize = true, Enabled = false };
            var close = new Button { Text = Strings.Close, AutoSize = true, DialogResult = DialogResult.Cancel };
            CancelButton = close;
            list.SelectedIndexChanged += (_, _) =>
            {
                var text = list.SelectedIndex < 0 ? null : read(newestFirst[list.SelectedIndex]);
                preview.Text = text?.Replace("\n", "\r\n") ?? Strings.NoteVersionLocked;
                restore.Enabled = text != null;
            };
            restore.Click += (_, _) =>
            {
                Chosen = read(newestFirst[list.SelectedIndex]);
                DialogResult = DialogResult.OK;
            };

            var columns = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            columns.Controls.Add(list);
            columns.Controls.Add(preview);
            var layout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            layout.Controls.Add(new Label { Text = Strings.NoteVersionsHint, AutoSize = true, MaximumSize = new Size(590, 0) });
            layout.Controls.Add(columns);
            var buttons = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
            buttons.Controls.AddRange(new Control[] { restore, close });
            layout.Controls.Add(buttons);
            Controls.Add(layout);
            if (list.Items.Count > 0)
                list.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// Prints a note (or saves it as PDF with Windows' "Microsoft Print to PDF"): headings, checkboxes,
    /// bullets, priority, formatting and markers, tables and pictures, over as many pages as needed.
    /// </summary>
    internal sealed class NotePrinter : IDisposable
    {
        private const string PdfPrinter = "Microsoft Print to PDF";

        private readonly string title;
        private readonly string[] lines;
        private readonly PrintDocument document = new();
        private int next;          // first line of the next page
        private Font? body, bold, h1, h2, h3, check;

        public NotePrinter(string title, string text)
        {
            lines = text.Split('\n');
            // A note that starts with its own heading needs no extra title
            this.title = text.TrimStart().StartsWith("# ") ? "" : title;
            document.DocumentName = title;
            document.BeginPrint += (_, _) => next = -1; // -1 = the title comes first
            document.PrintPage += PrintPage;
        }

        public static bool CanSavePdf => PrinterSettings.InstalledPrinters.Cast<string>().Contains(PdfPrinter);

        public void SavePdf(string path)
        {
            document.PrinterSettings.PrinterName = PdfPrinter;
            document.PrinterSettings.PrintToFile = true;
            document.PrinterSettings.PrintFileName = path;
            document.PrintController = new StandardPrintController(); // no "printing page 1" window
            document.Print();
        }

        public void Print(IWin32Window owner)
        {
            using var dialog = new PrintDialog { Document = document, UseEXDialog = true };
            if (dialog.ShowDialog(owner) == DialogResult.OK)
                document.Print();
        }

        private void Fonts(float size)
        {
            if (body != null)
                return;
            body = new Font("Segoe UI", size, FontStyle.Regular, GraphicsUnit.Point);
            bold = new Font(body, FontStyle.Bold);
            h1 = new Font("Segoe UI", size * 1.6f, FontStyle.Bold, GraphicsUnit.Point);
            h2 = new Font("Segoe UI", size * 1.3f, FontStyle.Bold, GraphicsUnit.Point);
            h3 = new Font("Segoe UI", size * 1.1f, FontStyle.Bold, GraphicsUnit.Point);
            check = new Font("Segoe UI Symbol", size, FontStyle.Regular, GraphicsUnit.Point);
        }

        private void PrintPage(object? sender, PrintPageEventArgs e)
        {
            var g = e.Graphics!;
            Fonts(11);
            var area = e.MarginBounds;
            float y = area.Top;
            if (next < 0)
            {
                if (title.Length > 0)
                {
                    g.DrawString(title, h1!, Brushes.Black, area.Left, y);
                    y += h1!.GetHeight(g) + 10;
                }
                next = 0;
            }
            while (next < lines.Length)
            {
                // Tables are printed as a whole block
                if (NoteLists.IsTableLine(lines[next]))
                {
                    var rows = new List<string>();
                    var end = next;
                    while (end < lines.Length && NoteLists.IsTableLine(lines[end]))
                        rows.Add(lines[end++]);
                    var tableHeight = Table(g, rows, area.Left, y, area.Width, false);
                    if (y + tableHeight > area.Bottom && y > area.Top)
                        break;
                    Table(g, rows, area.Left, y, area.Width, true);
                    y += tableHeight;
                    next = end;
                    continue;
                }
                var height = Line(g, lines[next], area.Left, y, area.Width, false);
                if (y + height > area.Bottom && y > area.Top)
                    break;
                Line(g, lines[next], area.Left, y, area.Width, true);
                y += height;
                next++;
            }
            e.HasMorePages = next < lines.Length;
        }

        /// <summary>Measures (draw = false) or draws one note line; returns its height.</summary>
        private float Line(Graphics g, string raw, float x, float y, float width, bool draw)
        {
            var font = body!;
            var right = x + width;
            var indent = NoteLists.Indent(raw);
            x += indent * 18;
            var line = raw.TrimStart();
            if (NoteText.Media(line) is { } media)
                return Picture(g, media, x, y, width - indent * 18, draw);
            var m = NoteText.CheckboxPrefix().Match(line);
            var done = false;
            if (m.Success)
            {
                done = m.Groups[2].Value != " ";
                if (draw)
                    g.DrawString(done ? "☑" : "☐", check!, Brushes.Black, x, y);
                x += g.MeasureString("☐ ", check!).Width;
                line = line[m.Length..];
            }
            else
            {
                var (kind, content) = NoteText.ParseLine(line);
                switch (kind)
                {
                    case NoteText.LineKind.Rule:
                        if (draw)
                            g.DrawLine(Pens.Gray, x, y + font.GetHeight(g) / 2, x + width, y + font.GetHeight(g) / 2);
                        return font.GetHeight(g);
                    case NoteText.LineKind.Heading1: font = h1!; line = content; break;
                    case NoteText.LineKind.Heading2: font = h2!; line = content; break;
                    case NoteText.LineKind.Heading3: font = h3!; line = content; break;
                    case NoteText.LineKind.Bullet:
                        if (draw)
                            g.DrawString("•", font, Brushes.Black, x, y);
                        x += g.MeasureString("• ", font).Width;
                        line = content;
                        break;
                    case NoteText.LineKind.Quote:
                        if (draw)
                            g.FillRectangle(Brushes.Gray, x, y, 3, font.GetHeight(g));
                        x += 10;
                        line = content;
                        break;
                }
            }
            var (priority, rest) = NoteText.Priority(line);
            if (priority > 0)
            {
                var marks = new string('!', priority);
                var size = g.MeasureString(marks, bold!);
                if (draw)
                {
                    using var fill = new SolidBrush(FenceWindow.PriorityColor(priority));
                    g.FillRectangle(fill, x, y + 1, size.Width + 4, font.GetHeight(g) - 2);
                    g.DrawString(marks, bold!, Brushes.White, x + 2, y);
                }
                x += size.Width + 10;
                line = rest;
            }
            if (NoteLists.TryCalculate(line, out var result))
                line = line.TrimEnd() + " **" + result + "**";
            var runs = NoteText.Runs(line);
            if (done)
                runs = runs.Select(r => r with { Strike = true }).ToList();
            return Runs(g, runs, font, x, y, Math.Max(20, right - x), draw);
        }

        /// <summary>Word-wrapped formatted text in black (markers as colored backgrounds); returns the height.</summary>
        private static float Runs(Graphics g, List<NoteText.Run> runs, Font font, float x, float y, float width, bool draw)
        {
            var format = StringFormat.GenericTypographic;
            var lineHeight = font.GetHeight(g);
            var space = g.MeasureString(" ", font, PointF.Empty, format).Width + font.Size * 0.3f;
            float cx = x, cy = y;
            var right = x + width;
            foreach (var run in runs)
            {
                var style = font.Style | (run.Bold ? FontStyle.Bold : 0) | (run.Italic ? FontStyle.Italic : 0)
                    | (run.Underline ? FontStyle.Underline : 0) | (run.Strike ? FontStyle.Strikeout : 0);
                using var styled = new Font(font, style);
                var words = run.Text.Split(' ');
                for (var w = 0; w < words.Length; w++)
                {
                    if (words[w].Length > 0)
                    {
                        var size = g.MeasureString(words[w], styled, PointF.Empty, format);
                        if (cx + size.Width > right && cx > x)
                        {
                            cx = x;
                            cy += lineHeight;
                        }
                        if (draw)
                        {
                            if (run.Highlight is char c)
                            {
                                using var marker = new SolidBrush(FenceWindow.HighlightColor(c));
                                g.FillRectangle(marker, cx, cy, size.Width + (w < words.Length - 1 ? space : 0), lineHeight);
                            }
                            g.DrawString(words[w], styled, Brushes.Black, cx, cy, format);
                        }
                        cx += size.Width;
                    }
                    if (w < words.Length - 1)
                        cx += space;
                }
            }
            return cy - y + lineHeight + 2;
        }

        private float Table(Graphics g, List<string> rows, float x, float y, float width, bool draw)
        {
            var cells = rows.Select(NoteLists.Cells).OfType<List<string>>().ToList();
            if (cells.Count == 0)
                return 0;
            var header = rows.Count > 1 && NoteLists.Cells(rows[1]) == null;
            var columns = cells.Max(c => c.Count);
            var columnWidth = Math.Min(200, width / columns);
            var top = y;
            for (var r = 0; r < cells.Count; r++)
            {
                var font = header && r == 0 ? bold! : body!;
                var height = cells[r].Select(c => Runs(g, NoteText.Runs(c), font, 0, 0, columnWidth - 8, false)).DefaultIfEmpty(font.GetHeight(g)).Max() + 4;
                if (draw)
                {
                    for (var c = 0; c < columns; c++)
                    {
                        g.DrawRectangle(Pens.Gray, x + c * columnWidth, y, columnWidth, height);
                        if (c < cells[r].Count)
                            Runs(g, NoteText.Runs(cells[r][c]), font, x + c * columnWidth + 4, y + 2, columnWidth - 8, true);
                    }
                }
                y += height;
            }
            return y - top + 6;
        }

        private static float Picture(Graphics g, NoteText.MediaLine media, float x, float y, float width, bool draw)
        {
            if (media.IsAudio)
                return 0;
            var path = AppData.Resolve(media.Path);
            if (!File.Exists(path))
                return 0;
            try
            {
                using var image = Image.FromFile(path);
                var w = Math.Min(width, image.Width * 0.75f); // pixels at 96 dpi → 1/100 inch
                var h = image.Height * w / image.Width;
                if (draw)
                    g.DrawImage(image, x, y, w, h);
                return h + 6;
            }
            catch (Exception e) when (e is OutOfMemoryException or IOException)
            {
                return 0;
            }
        }

        public void Dispose()
        {
            document.Dispose();
            foreach (var f in new[] { body, bold, h1, h2, h3, check })
                f?.Dispose();
        }
    }
}
