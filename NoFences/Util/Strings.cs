using System.Globalization;

namespace NoFences.Util
{
    /// <summary>UI texts in German and English, picked from the Windows display language.</summary>
    public static class Strings
    {
        private static readonly bool De = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de";

        public static string HelpDocument => De ? "HILFE.md" : "HELP.md";
        public static string ChangelogDocument => De ? "CHANGELOG.de.md" : "CHANGELOG.md";
        public static string Help => T("Help", "Hilfe");
        public static string WhatsNew => T("What's new?", "Was ist neu?");
        public static string FirstStartHint => T("Drag files onto the fence. Right-click a fence for options; the tray icon has Help.",
                                                 "Zieh Dateien auf den Fence. Rechtsklick auf einen Fence zeigt die Optionen, im Tray-Icon gibt es die Hilfe.");

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
        public static string SortBy => T("Sort by", "Sortieren nach");
        public static string SortModeName(Model.FenceSortMode mode) => mode switch
        {
            Model.FenceSortMode.Name => T("Name", "Name"),
            Model.FenceSortMode.Type => T("Type", "Typ"),
            Model.FenceSortMode.Modified => T("Date modified (newest first)", "Änderungsdatum (neueste zuerst)"),
            Model.FenceSortMode.Size => T("Size (largest first)", "Größe (größte zuerst)"),
            _ => T("Manual (drag & drop)", "Manuell (Drag & Drop)")
        };

        public static string PeekMenu => T("Bring fences to front", "Fences nach vorne holen");
        public static string PeekHotkey => T("Shortcut", "Tastenkürzel");
        public static string HotkeyName(string hotkey) => hotkey switch
        {
            "Off" => T("Off", "Aus"),
            _ => De ? hotkey.Replace("Ctrl", "Strg").Replace("Space", "Leertaste") : hotkey
        };
        public static string HotkeyTaken(string hotkey) => T($"The shortcut {HotkeyName(hotkey)} is already used by another program. Pick another one in the tray menu.",
                                                             $"Das Tastenkürzel {HotkeyName(hotkey)} wird schon von einem anderen Programm verwendet. Wähle im Tray-Menü ein anderes.");

        public static string CheckForUpdatesAuto => T("Check for updates automatically", "Automatisch nach Updates suchen");
        public static string CheckForUpdatesNow => T("Check for updates now", "Jetzt nach Updates suchen");
        public static string InstallUpdate(Version v) => T($"Install update {v}", $"Update {v} installieren");
        public static string UpdateAvailable(Version v) => T($"NoFences {v} is available. Click here to install it.",
                                                             $"NoFences {v} ist verfügbar. Hier klicken zum Installieren.");
        public static string UpdateAvailableManual(Version v) => T($"NoFences {v} is available. Click here to open the download page.",
                                                                   $"NoFences {v} ist verfügbar. Hier klicken, um die Download-Seite zu öffnen.");
        public static string UpToDate(Version v) => T($"You have the latest version ({v}).", $"Du hast die neueste Version ({v}).");
        public static string UpdateDownloading => T("Downloading update…", "Update wird heruntergeladen…");
        public static string UpdateFailed(string reason) => T($"The update failed: {reason}\nThe download page will open instead.",
                                                              $"Das Update ist fehlgeschlagen: {reason}\nStattdessen öffnet sich die Download-Seite.");
        public static string UpdateCheckFailed => T("Could not reach GitHub to check for updates.", "GitHub war für die Update-Prüfung nicht erreichbar.");
        public static string RestoreBackup => T("Restore backup", "Sicherung wiederherstellen");
        public static string NoBackups => T("No backups yet", "Noch keine Sicherungen");
        public static string ConfirmRestore(DateTime time) => T($"Restore all fences as they were on {time:g}?\nNoFences restarts; the current state is kept as a backup too.",
                                                                $"Alle Fences auf den Stand vom {time:g} zurücksetzen?\nNoFences startet neu; der aktuelle Stand wird vorher ebenfalls gesichert.");
        public static string Rename => T("Rename", "Umbenennen");
        public static string NewName => T("New name:", "Neuer Name:");
        public static string RenameFailed(string reason) => T($"Could not rename: {reason}", $"Umbenennen nicht möglich: {reason}");
        public static string Search => T("Search", "Suche");
        public static string Reminder => T("Reminder…", "Erinnerung…");
        public static string ReminderTitle => T("Remind me", "Erinnern");
        public static string ReminderIn1h => T("In 1 hour", "In 1 Stunde");
        public static string ReminderTonight => T("Today 6 pm", "Heute 18:00");
        public static string ReminderTomorrow => T("Tomorrow 9 am", "Morgen 9:00");
        public static string ReminderRemove => T("Remove", "Entfernen");
        public static string ReminderDue(string name) => T($"Reminder: {name}", $"Erinnerung: {name}");
        public static string AlwaysOnTop => T("Always on top", "Immer im Vordergrund");
        public static string NewNote => T("New note", "Neue Notiz");
        public static string NoteName => T("Note", "Notiz");
        public static string EditNote => T("Edit note", "Notiz bearbeiten");
        public static string NoteHint => T("Double-click to write.\nLines starting with [ ] become checkboxes.",
                                           "Doppelklick zum Schreiben.\nZeilen mit [ ] am Anfang werden zu Kästchen.");
        public static string KindNote => T("Note (sticky note with text)", "Notiz (Post-it mit Text)");
        public static string FolderMissing(string path) => T($"Folder not found:\n{path}", $"Ordner nicht gefunden:\n{path}");
    }
}
