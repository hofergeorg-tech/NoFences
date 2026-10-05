using NoFences.Model;
using NoFences.Util;

namespace NoFences
{
    /// <summary>A note's versions, saving it as a template, and export (Markdown, PDF, print).</summary>
    public sealed partial class FenceWindow
    {
        /// <summary>Keeps the current state as a version (before an edit or a restore changes it).</summary>
        private void RememberNoteVersion()
        {
            var encrypted = IsProtectedNote;
            var text = encrypted ? Info.NoteCipher ?? "" : Info.NoteText;
            if (text.Trim().Length == 0)
                return;
            Info.NoteVersions ??= new List<NoteVersion>();
            NoteLists.AddVersion(Info.NoteVersions, new NoteVersion { Time = DateTime.Now, Text = text, Encrypted = encrypted });
        }

        /// <summary>A version's text; encrypted ones only while this note is unlocked with the same password.</summary>
        private string? ReadVersion(NoteVersion version) =>
            !version.Encrypted ? version.Text : notePassword != null ? NoteCrypto.Decrypt(version.Text, notePassword) : null;

        private void ShowNoteVersions()
        {
            if (Info.NoteVersions is not { Count: > 0 } versions || NoteLockedNow)
                return;
            using var dialog = new NoteVersionsDialog(Info.Name, versions, ReadVersion);
            if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Chosen == null)
                return;
            RememberNoteVersion();
            NoteContent = dialog.Chosen;
            app.RequestSave();
            Invalidate();
        }

        private void AddNoteExtrasMenu(ToolStripItemCollection items)
        {
            var locked = NoteLockedNow;
            items.Add(new ToolStripMenuItem(Strings.NoteVersionsMenu, null, (_, _) => ShowNoteVersions())
            {
                Enabled = !locked && Info.NoteVersions is { Count: > 0 }
            });
            items.Add(new ToolStripMenuItem(Strings.TemplateSave, null, (_, _) => SaveAsTemplate()) { Enabled = !locked });
            var export = new ToolStripMenuItem(Strings.NoteExportMenu) { Enabled = !locked };
            export.DropDownItems.Add(Strings.NoteExportMarkdown, null, (_, _) => ExportMarkdown());
            export.DropDownItems.Add(new ToolStripMenuItem(Strings.NoteExportPdf, null, (_, _) => ExportPdf()) { Enabled = NotePrinter.CanSavePdf });
            export.DropDownItems.Add(Strings.NotePrint, null, (_, _) =>
            {
                using var printer = new NotePrinter(Info.Name, NoteContent);
                printer.Print(this);
            });
            items.Add(export);
        }

        private void SaveAsTemplate()
        {
            using var dialog = new InputDialog(Strings.TemplateSave.TrimEnd('…'), Strings.TemplateNamePrompt, Info.Name);
            if (dialog.ShowDialog(this) == DialogResult.OK && dialog.Value.Trim().Length > 0)
                app.SaveNoteTemplate(dialog.Value.Trim(), NoteContent);
        }

        private string? AskExportPath(string filter, string extension)
        {
            var name = string.Concat(Info.Name.Split(Path.GetInvalidFileNameChars())).Trim();
            using var dialog = new SaveFileDialog
            {
                Filter = filter,
                FileName = (name.Length > 0 ? name : "Notiz") + extension,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
        }

        private void ExportMarkdown()
        {
            if (AskExportPath("Markdown (*.md)|*.md|Text (*.txt)|*.txt", ".md") is not { } path)
                return;
            try
            {
                File.WriteAllText(path, NoteLists.ToMarkdown(Info.Name, NoteContent).Replace("\n", "\r\n"));
            }
            catch (Exception e)
            {
                MessageBox.Show(this, e.Message, "NoFences", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ExportPdf()
        {
            if (AskExportPath("PDF (*.pdf)|*.pdf", ".pdf") is not { } path)
                return;
            try
            {
                using var printer = new NotePrinter(Info.Name, NoteContent);
                printer.SavePdf(path);
            }
            catch (Exception e)
            {
                MessageBox.Show(this, e.Message, "NoFences", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
