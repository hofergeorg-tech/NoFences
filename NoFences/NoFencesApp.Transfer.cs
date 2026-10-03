using NoFences.Model;
using NoFences.Util;

namespace NoFences
{
    /// <summary>Export/import of fences (and user styles) as one file.</summary>
    public sealed partial class NoFencesApp
    {
        private void AddTransferItems(ToolStripItemCollection items)
        {
            items.Add(Strings.ExportFences, null, (_, _) => ExportFences());
            items.Add(Strings.ImportFences, null, (_, _) => ImportFences());
        }

        internal void ExportFences()
        {
            using var dialog = new SaveFileDialog
            {
                Filter = Strings.ExportFilter,
                FileName = $"NoFences-{DateTime.Now:yyyy-MM-dd}.nofences.json",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            if (dialog.ShowDialog() != DialogResult.OK)
                return;
            try
            {
                File.WriteAllText(dialog.FileName, FenceExport.Create(Store.Config.Fences, ThemesFolder).ToJson());
                ShowBalloon(Strings.ExportDone(Store.Config.Fences.Count));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "NoFences", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        internal void ImportFences()
        {
            using var dialog = new OpenFileDialog { Filter = Strings.ExportFilter };
            if (dialog.ShowDialog() != DialogResult.OK)
                return;
            try
            {
                var export = FenceExport.FromJson(File.ReadAllText(dialog.FileName));
                if (export.WriteThemes(ThemesFolder) > 0)
                {
                    LoadCustomThemes(report: false);
                    ApplyToAll();
                }
                var fences = export.PrepareForImport();
                foreach (var info in fences)
                {
                    Store.Config.Fences.Add(info);
                    OpenWindow(info); // off-screen positions are moved back onto a screen
                }
                Store.RequestSave();
                ShowBalloon(Strings.ImportDone(fences.Count));
            }
            catch (Exception e)
            {
                MessageBox.Show(Strings.ImportFailed(e.Message), "NoFences", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
