using System.Globalization;

namespace NoFences.Util
{
    /// <summary>UI texts in German and English, picked from the Windows display language.</summary>
    public static class Strings
    {
        private static readonly bool De = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de";

        private static string T(string en, string de) => De ? de : en;

        public static string NewFence => T("New fence", "Neuer Fence");
        public static string NewFolderFence => T("New folder fence…", "Neuer Ordner-Fence…");
        public static string FirstFence => T("First fence", "Erster Fence");
        public static string ChooseFolder => T("Choose the folder this fence should show", "Ordner wählen, den dieser Fence anzeigen soll");
        public static string Settings => T("Fence settings…", "Fence-Einstellungen…");
        public static string Locked => T("Locked", "Gesperrt");
        public static string AutoCollapse => T("Collapse when not hovered", "Einklappen wenn Maus weg");
        public static string RemoveItem => T("Remove from fence", "Aus Fence entfernen");
        public static string OpenFolder => T("Open folder in Explorer", "Ordner im Explorer öffnen");
        public static string DeleteFence => T("Delete fence", "Fence löschen");
        public static string ReallyDelete(string name) => T($"Really delete the fence \"{name}\"?", $"Fence \"{name}\" wirklich löschen?");
        public static string ReallyDeleteFolderNote => T("The folder and its files are not touched.", "Der Ordner und seine Dateien bleiben unangetastet.");
        public static string ShowFences => T("Show fences", "Fences anzeigen");
        public static string Autostart => T("Start with Windows", "Mit Windows starten");
        public static string ShowExtensions => T("Show file extensions", "Dateiendungen anzeigen");
        public static string ExtFollowExplorer => T("Like Explorer", "Wie im Explorer");
        public static string ExtAlways => T("Always", "Immer");
        public static string ExtNever => T("Never", "Nie");
        public static string Theme => T("Style", "Style");
        public static string ThemeGlobal => T("Default style", "Standard-Style");
        public static string ThemeInherit => T("(use default style)", "(Standard-Style verwenden)");
        public static string OpenDataFolder => T("Open config folder", "Konfigurationsordner öffnen");
        public static string Exit => T("Exit", "Beenden");

        public static string Name => T("Name", "Name");
        public static string Folder => T("Folder", "Ordner");
        public static string Browse => T("Browse…", "Durchsuchen…");
        public static string TitleHeight => T("Title height", "Titelhöhe");
        public static string IconSize => T("Icon size", "Icongröße");
        public static string Background => T("Background", "Hintergrund");
        public static string Opacity => T("Opacity", "Deckkraft");
        public static string Ok => T("OK", "OK");
        public static string Cancel => T("Cancel", "Abbrechen");
        public static string DropHint => T("Drop files or folders here", "Dateien oder Ordner hierher ziehen");
        public static string Kind => T("Type", "Typ");
        public static string KindLinks => T("Links (files stay where they are)", "Verknüpfungen (Dateien bleiben wo sie sind)");
        public static string KindFolder => T("Folder (shows a folder's contents)", "Ordner (zeigt den Inhalt eines Ordners)");
        public static string AutoSort => T("Auto-sort from desktop", "Vom Desktop einsortieren");
        public static string AutoSortHint => T("New desktop files matching these patterns go into this fence, e.g. *.pdf; *.docx",
                                               "Neue Desktop-Dateien, die passen, landen in diesem Fence, z. B. *.pdf; *.docx");
        public static string AddPreset => T("Add preset…", "Vorlage hinzufügen…");
        public static string PresetImages => T("Images", "Bilder");
        public static string PresetDocuments => T("Documents", "Dokumente");
        public static string PresetArchives => T("Archives", "Archive");
        public static string PresetInstallers => T("Installers / programs", "Installer / Programme");
        public static string PresetVideos => T("Videos", "Videos");
        public static string PresetMusic => T("Music", "Musik");
        public static string PresetShortcuts => T("Shortcuts", "Verknüpfungen");
        public static string AutoSortEnabled => T("Auto-sort new desktop files", "Neue Desktop-Dateien automatisch einsortieren");
        public static string SortNow => T("Tidy up desktop now", "Desktop jetzt aufräumen");
        public static string SortNowNoRules => T("No fence has auto-sort patterns yet.\nSet them in a fence's settings.",
                                                 "Noch kein Fence hat Einsortier-Regeln.\nDu legst sie in den Fence-Einstellungen fest.");
        public static string SortNowDone(int n) => n == 1 ? T("1 file sorted into fences.", "1 Datei in Fences einsortiert.")
                                                          : T($"{n} files sorted into fences.", $"{n} Dateien in Fences einsortiert.");
        public static string DoubleClickToggle => T("Double-click desktop to hide fences", "Doppelklick auf Desktop blendet Fences aus");
        public static string FolderMissing(string path) => T($"Folder not found:\n{path}", $"Ordner nicht gefunden:\n{path}");
    }
}
