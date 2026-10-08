using NoFences.Model;
using NoFences.Util;

namespace NoFences
{
    /// <summary>Export/import of fences (and user styles) as one file.</summary>
    public sealed partial class NoFencesApp
    {

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
                var network = export.NetworkPaths();
                if (network.Count > 0)
                {
                    switch (AskAboutNetworkPaths(network))
                    {
                        case null:
                            return;
                        case false:
                            export.RemoveNetworkPaths();
                            break;
                    }
                }
                if (export.WriteThemes(ThemesFolder) > 0)
                {
                    LoadCustomThemes(report: false);
                    ApplyToAll();
                }
                var fences = export.PrepareForImport();
                foreach (var info in fences)
                {
                    // Profiles from the other PC become profiles here
                    foreach (var profile in info.Profiles ?? new())
                    {
                        if (!Store.Config.Profiles.Contains(profile))
                            Store.Config.Profiles.Add(profile);
                    }
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

        /// <summary>
        /// The file points to other computers: take it without those entries (default), as it is, or not at
        /// all (null). Showing them would make Windows log on to those servers with the user's credentials.
        /// </summary>
        private static bool? AskAboutNetworkPaths(List<(string Fence, string Path)> found)
        {
            const int shown = 8;
            var list = string.Join("\n", found.Take(shown).Select(f => $"• {f.Fence}: {f.Path}"));
            if (found.Count > shown)
                list += "\n" + Strings.ImportNetworkMore(found.Count - shown);
            var without = new TaskDialogButton(Strings.ImportWithoutNetwork);
            var anyway = new TaskDialogButton(Strings.ImportAnyway);
            var page = new TaskDialogPage
            {
                Caption = "NoFences",
                Heading = Strings.ImportNetworkHeading(found.Count),
                Text = Strings.ImportNetworkText + "\n\n" + list,
                Icon = TaskDialogIcon.Warning,
                Buttons = { without, anyway, TaskDialogButton.Cancel },
                DefaultButton = without
            };
            var result = TaskDialog.ShowDialog(page);
            return result == without ? false : result == anyway ? true : null;
        }
    }
}
