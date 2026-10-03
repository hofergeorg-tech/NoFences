using System.Diagnostics;
using NoFences.Themes;
using NoFences.Util;

namespace NoFences
{
    /// <summary>User styles from the "themes" folder and the animation switch.</summary>
    public sealed partial class NoFencesApp
    {
        internal string ThemesFolder => Path.Combine(Store.DataDirectory, "themes");

        /// <summary>Loads user styles and reports problems (or the count) in a notification.</summary>
        internal void LoadCustomThemes(bool report)
        {
            var (count, errors) = LoadCustomThemesQuiet();
            if (errors.Count > 0)
                ShowBalloon(Strings.ThemeErrors(string.Join("\n", errors)), timeout: 10_000);
            else if (report)
                ShowBalloon(Strings.ThemesLoaded(count));
        }

        /// <summary>Loads user styles; creates the folder with an example on first use. Safe before the tray exists.</summary>
        private (int Count, List<string> Errors) LoadCustomThemesQuiet()
        {
            try
            {
                if (!Directory.Exists(ThemesFolder))
                {
                    Directory.CreateDirectory(ThemesFolder);
                    File.WriteAllText(Path.Combine(ThemesFolder, "beispiel-mocha.json"), JsonTheme.ExampleJson);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Themes folder: {e.Message}");
            }

            var (themes, errors) = JsonTheme.LoadFolder(ThemesFolder);
            ThemeRegistry.SetCustom(themes);
            return (themes.Count, errors);
        }

    }
}
