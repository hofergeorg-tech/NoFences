using System.Diagnostics;
using NoFences.Util;

namespace NoFences
{
    /// <summary>Own translations: language files in the data folder's lang folder.</summary>
    public sealed partial class NoFencesApp
    {
        private const string TemplateFile = "TEMPLATE.en.json";

        /// <summary>Reads the lang folder again; problems are shown as a notification.</summary>
        internal void ReloadLanguages()
        {
            var errors = Strings.LoadFolder(Store.Folder.Lang);
            if (errors.Count > 0)
                ShowBalloon(Strings.LanguageFileErrors(string.Join("\n", errors)), timeout: 10_000);
        }

        /// <summary>Opens the lang folder with an English template and a short explanation.</summary>
        internal void OpenLanguageFolder()
        {
            var folder = Store.Folder.Lang;
            try
            {
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, TemplateFile), Strings.BuiltInJson(Strings.Fallback));
                File.WriteAllText(Path.Combine(folder, "README.txt"), Strings.LanguageFolderReadme(TemplateFile));
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "NoFences");
            }
        }
    }
}
