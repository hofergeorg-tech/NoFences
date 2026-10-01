using System.Text.RegularExpressions;
using NoFences.Util;

namespace NoFences
{
    /// <summary>
    /// Shows one of the embedded Markdown documents (help, changelog) in the UI language.
    /// Renders the small Markdown subset those files use: headings, bullets, **bold**, `code`, [links](…).
    /// </summary>
    public sealed partial class DocumentViewer : Form
    {
        private static readonly Dictionary<string, DocumentViewer> Open = new();

        private readonly RichTextBox box = new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            DetectUrls = false,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            ShortcutsEnabled = true
        };

        private readonly Font body;
        private readonly Font bold;
        private readonly Font code;
        private readonly Font h1;
        private readonly Font h2;
        private readonly Font h3;

        /// <summary>Shows the document, or brings an already open copy to the front.</summary>
        /// <param name="resource">Embedded resource name, e.g. "HELP.md".</param>
        public static void ShowDocument(string resource, string title)
        {
            if (Open.TryGetValue(resource, out var existing) && !existing.IsDisposed)
            {
                existing.Activate();
                return;
            }
            var viewer = new DocumentViewer(resource, title);
            Open[resource] = viewer;
            viewer.FormClosed += (_, _) => Open.Remove(resource);
            viewer.Show();
            viewer.Activate();
        }

        private DocumentViewer(string resource, string title)
        {
            Text = $"NoFences – {title}";
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(760, 640);
            MinimumSize = new Size(420, 300);
            KeyPreview = true;
            KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
            try
            {
                using var stream = typeof(DocumentViewer).Assembly.GetManifestResourceStream("NoFences.ico");
                if (stream != null)
                    Icon = new Icon(stream);
            }
            catch { }

            body = new Font("Segoe UI", 10f);
            bold = new Font("Segoe UI Semibold", 10f);
            code = new Font("Cascadia Mono", 9.5f);
            if (code.Name != "Cascadia Mono")
            {
                code.Dispose();
                code = new Font("Consolas", 10f);
            }
            h1 = new Font("Segoe UI Semibold", 18f);
            h2 = new Font("Segoe UI Semibold", 13.5f);
            h3 = new Font("Segoe UI Semibold", 11f);

            var dark = SystemSettings.AppsUseDarkTheme;
            BackColor = box.BackColor = dark ? Color.FromArgb(32, 32, 32) : Color.White;
            box.ForeColor = dark ? Color.FromArgb(232, 232, 232) : Color.FromArgb(30, 30, 30);

            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 16, 12, 16), BackColor = box.BackColor };
            panel.Controls.Add(box);
            Controls.Add(panel);

            Render(LoadMarkdown(resource));
            box.SelectionStart = 0;
        }

        private static string LoadMarkdown(string resource)
        {
            using var stream = typeof(DocumentViewer).Assembly.GetManifestResourceStream(resource);
            if (stream == null)
                return $"# {resource}\n\n(not found)";
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        [GeneratedRegex(@"^\[[^\]]+\]:\s")]
        private static partial Regex LinkDefinition();

        [GeneratedRegex(@"\*\*(.+?)\*\*|`([^`]+)`|\[([^\]]+)\]\([^)]*\)|\[([^\]]+)\]")]
        private static partial Regex Inline();

        private void Render(string markdown)
        {
            foreach (var block in ToBlocks(markdown))
            {
                if (block.StartsWith("### "))
                    AppendHeading(block[4..], h3, 10, 2);
                else if (block.StartsWith("## "))
                    AppendHeading(block[3..], h2, 16, 4);
                else if (block.StartsWith("# "))
                    AppendHeading(block[2..], h1, 0, 6);
                else if (block.StartsWith("- "))
                    AppendParagraph(block[2..], bullet: true);
                else
                    AppendParagraph(block, bullet: false);
            }
        }

        /// <summary>Joins wrapped lines into blocks (headings, bullets, paragraphs) and drops link definitions.</summary>
        private static IEnumerable<string> ToBlocks(string markdown)
        {
            var current = "";
            foreach (var raw in markdown.Replace("\r", "").Split('\n'))
            {
                var line = raw.TrimEnd();
                var trimmed = line.TrimStart();
                var startsBlock = trimmed.Length == 0 || trimmed.StartsWith('#') || trimmed.StartsWith("- ") || LinkDefinition().IsMatch(trimmed)
                                  || (trimmed.StartsWith("**") && trimmed.EndsWith("**") && current.Length > 0);
                if (startsBlock && current.Length > 0)
                {
                    yield return current;
                    current = "";
                }
                if (trimmed.Length == 0 || LinkDefinition().IsMatch(trimmed))
                    continue;
                // A line that is entirely bold (FAQ question) keeps its own line within the paragraph.
                var boldLine = current.StartsWith("**") && current.EndsWith("**");
                current = current.Length == 0 ? trimmed : current + (boldLine ? "\n" : " ") + trimmed;
                if (trimmed.StartsWith('#'))
                {
                    yield return current;
                    current = "";
                }
            }
            if (current.Length > 0)
                yield return current;
        }

        private void AppendHeading(string text, Font font, int spaceBefore, int spaceAfter)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionBullet = false;
            box.SelectionIndent = 0;
            box.BulletIndent = 0;
            if (box.TextLength > 0 && spaceBefore > 0)
                AppendRun("\n", body);
            AppendInline(text, font, font, code);
            AppendRun("\n", body);
            if (spaceAfter > 4)
                AppendRun("\n", body);
        }

        private void AppendParagraph(string text, bool bullet)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionBullet = bullet;
            box.SelectionIndent = bullet ? 8 : 0;
            box.BulletIndent = bullet ? 14 : 0;
            AppendInline(text, body, bold, code);
            AppendRun("\n", body);
            box.SelectionBullet = false;
            if (!bullet)
                AppendRun("\n", body);
        }

        private void AppendInline(string text, Font normal, Font strong, Font mono)
        {
            var pos = 0;
            foreach (Match m in Inline().Matches(text))
            {
                if (m.Index > pos)
                    AppendRun(text[pos..m.Index], normal);
                if (m.Groups[1].Success)
                    AppendRun(m.Groups[1].Value, strong);
                else if (m.Groups[2].Success)
                    AppendRun(m.Groups[2].Value, mono);
                else if (m.Groups[3].Success)
                    AppendRun(m.Groups[3].Value, normal);
                else
                    AppendRun(m.Groups[4].Value, normal);
                pos = m.Index + m.Length;
            }
            if (pos < text.Length)
                AppendRun(text[pos..], normal);
        }

        private void AppendRun(string text, Font font)
        {
            // RichEdit switches to a fallback font for symbols like "→" and keeps it for the rest of
            // the run, so append symbols separately and re-apply our font afterwards.
            var start = 0;
            for (var i = 0; i <= text.Length; i++)
            {
                if (i < text.Length && text[i] < 0x2000)
                    continue;
                if (i > start)
                    AppendPiece(text[start..i], font);
                if (i < text.Length)
                    AppendPiece(text[i].ToString(), font);
                start = i + 1;
            }
        }

        private void AppendPiece(string text, Font font)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;
            box.SelectionFont = font;
            box.SelectionColor = box.ForeColor;
            box.AppendText(text);
            // Re-apply in case RichEdit substituted the font while inserting.
            box.Select(box.TextLength - text.Length, text.Length);
            if (text[0] < 0x2000)
                box.SelectionFont = font;
            box.Select(box.TextLength, 0);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                body.Dispose();
                bold.Dispose();
                code.Dispose();
                h1.Dispose();
                h2.Dispose();
                h3.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
