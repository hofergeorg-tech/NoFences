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
        public static string OnlyThisDesktop => T("Only on this virtual desktop", "Nur auf diesem virtuellen Desktop");
        public static string ExportFences => T("Export fences…", "Fences exportieren…");
        public static string ImportFences => T("Import fences…", "Fences importieren…");
        public static string ExportFilter => T("NoFences export (*.nofences.json)|*.nofences.json|JSON (*.json)|*.json", "NoFences-Export (*.nofences.json)|*.nofences.json|JSON (*.json)|*.json");
        public static string ExportDone(int n) => T($"{n} fences exported.", $"{n} Fences exportiert.");
        public static string ImportDone(int n) => T($"{n} fences imported.", $"{n} Fences importiert.");
        public static string ImportFailed(string reason) => T($"Import failed: {reason}", $"Import fehlgeschlagen: {reason}");
        public static string AddTab => T("Add tab", "Reiter hinzufügen");
        public static string RenameTab => T("Rename tab", "Reiter umbenennen");
        public static string RemoveTab => T("Remove tab (keeps its links)", "Reiter entfernen (Verknüpfungen bleiben)");
        public static string TabDefaultName(int n) => T($"Tab {n}", $"Reiter {n}");
        public static string NewWidget => T("New widget", "Neues Widget");
        public static string NewRecent => T("New \"Recent files\" fence", "Neuer Fence „Zuletzt verwendet“");
        public static string RecentName => T("Recent files", "Zuletzt verwendet");
        public static string NewQuickLaunch => T("New quick-launch bar", "Neue Schnellstart-Leiste");
        public static string QuickLaunchName => T("Quick launch", "Schnellstart");
        public static string CompactMode => T("Icons only (compact)", "Nur Icons (kompakt)");
        public static string WidgetClock => T("Clock & calendar", "Uhr & Kalender");
        public static string WidgetSystem => T("System monitor (CPU, RAM, GPU, FPS)", "System-Monitor (CPU, RAM, GPU, FPS)");
        public static string WidgetDrives => T("Drives", "Laufwerke");
        public static string WidgetRecycleBin => T("Recycle bin", "Papierkorb");
        public static string WidgetPlaytime => T("Playtime (SC Playtime)", "Spielzeit (SC Playtime)");
        public static string PlaytimeGame => T("Game", "Spiel");
        public static string PlaytimeLastPlayed => T("Most recently played", "Zuletzt gespieltes Spiel");
        public static string PlaytimeNoSessions => T("No playtime recorded yet.", "Noch keine Spielzeit aufgezeichnet.");
        public static string DriveDefaultName(DriveType type) => type switch
        {
            DriveType.Removable => T("USB drive", "USB-Laufwerk"),
            DriveType.Network => T("Network", "Netzwerk"),
            _ => T("Local disk", "Lokaler Datenträger")
        };
        public static string FreeSpace(string size) => T($"{size} free", $"{size} frei");
        public static string RecycleEmptyState => T("Empty", "Leer");
        public static string RecycleItems(long n, string size) => n == 1 ? T($"1 item · {size}", $"1 Element · {size}") : T($"{n} items · {size}", $"{n} Elemente · {size}");
        public static string RecycleDropHint => T("Drop files here to delete", "Zum Löschen hierher ziehen");
        public static string RecycleEmptyAction => T("Empty recycle bin", "Papierkorb leeren");
        public static string GpuTemperature => T("GPU temp.", "GPU-Temp.");
        public static string FpsWaiting => T("waiting for a game…", "wartet auf ein Spiel…");
        public static string FpsMenu => T("Measure FPS (admin helper)…", "FPS messen (Admin-Helfer)…");
        public static string FpsTitle => T("Measure FPS – admin rights needed", "FPS messen – Administratorrechte nötig");
        public static string FpsExplanation => T(
            "To measure the frame rate (FPS) of games, NoFences needs a small helper process with administrator rights. " +
            "Windows only gives the graphics output events (ETW) to programs with admin rights – MSI Afterburner and PresentMon work the same way.\n\n" +
            "• Only this helper runs as administrator, NoFences itself does not.\n" +
            "• It only counts how often frames are shown – no screen content, no keyboard or mouse input.\n" +
            "• The first time, Windows asks for permission (UAC). The helper then creates a task in the Task Scheduler so it can start later without asking.\n" +
            "• Turn it off at any time in the same menu; the task is removed again.\n\n" +
            "Enable FPS measurement?",
            "Um die Bildrate (FPS) von Spielen zu messen, braucht NoFences einen kleinen Hilfsprozess mit Administratorrechten. " +
            "Windows gibt die nötigen Ereignisse der Grafikausgabe (ETW) nur an Programme mit Adminrechten – MSI Afterburner und PresentMon machen es genauso.\n\n" +
            "• Nur dieser Helfer läuft als Administrator, NoFences selbst nicht.\n" +
            "• Er zählt nur, wie oft Bilder ausgegeben werden – keine Bildschirminhalte, keine Tastatur- oder Mauseingaben.\n" +
            "• Beim ersten Mal fragt Windows nach Erlaubnis (UAC). Danach legt der Helfer eine Aufgabe in der Aufgabenplanung an, damit er später ohne Nachfrage starten kann.\n" +
            "• Ausschalten jederzeit im selben Menü; die Aufgabe wird dabei wieder entfernt.\n\n" +
            "FPS-Messung aktivieren?");
        public static string FpsDeclined => T("FPS measurement stays off (no admin rights granted).", "FPS-Messung bleibt aus (keine Adminrechte erteilt).");
        public static string PlaytimeToday => T("today", "heute");
        public static string PlaytimeWeek => T("This week", "Diese Woche");
        public static string PlaytimeMonth => T("This month", "Diesen Monat");
        public static string PlaytimeTotal => T("Total", "Gesamt");
        public static string PlaytimeMissing => T("SC Playtime not found. It records how long you play your games; this widget shows it.",
                                                    "SC Playtime nicht gefunden. Es zeichnet auf, wie lange du deine Spiele spielst, dieses Widget zeigt es an.");
        public static string About => T("About NoFences", "Über NoFences");
        public static string AboutTagline => T("Free desktop fences, folder fences, sticky notes and widgets for Windows.",
                                               "Kostenlose Desktop-Fences, Ordner-Fences, Notizen und Widgets für Windows.");
        public static string AboutSource => T("Source code and downloads on GitHub", "Quellcode und Downloads auf GitHub");
        public static string AboutCredits => T("Based on NoFences by Twometer and contributors — thank you!",
                                               "Basiert auf NoFences von Twometer und Mitwirkenden – danke!");
        public static string AboutLicense => T("Open source under the MIT license.", "Open Source unter der MIT-Lizenz.");
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
        public static string Animations => T("Animations", "Animationen");
        public static string CustomThemes => T("Own styles", "Eigene Styles");
        public static string OpenThemesFolder => T("Open styles folder", "Styles-Ordner öffnen");
        public static string ReloadThemes => T("Reload styles", "Styles neu laden");
        public static string ThemesLoaded(int n) => T($"{n} own style(s) loaded.", $"{n} eigene(r) Style(s) geladen.");
        public static string ThemeErrors(string details) => T($"Some styles could not be loaded:\n{details}", $"Einige Styles konnten nicht geladen werden:\n{details}");
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
        public static string KindWidget => T("Widget", "Widget");
        public static string NewNote => T("New note", "Neue Notiz");
        public static string NoteName => T("Note", "Notiz");
        public static string EditNote => T("Edit note", "Notiz bearbeiten");
        public static string NoteHint => T("Double-click to write.\nLines starting with [ ] become checkboxes.",
                                           "Doppelklick zum Schreiben.\nZeilen mit [ ] am Anfang werden zu Kästchen.");
        public static string KindNote => T("Note (sticky note with text)", "Notiz (Post-it mit Text)");
        public static string FolderMissing(string path) => T($"Folder not found:\n{path}", $"Ordner nicht gefunden:\n{path}");
    }
}
