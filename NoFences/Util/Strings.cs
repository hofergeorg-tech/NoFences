using System.Globalization;
using NoFences.Model;

namespace NoFences.Util
{
    /// <summary>
    /// UI texts in English, German and Italian. The language follows Windows ("auto") unless chosen in
    /// the settings; anything that isn't German or Italian falls back to English.
    /// </summary>
    public static class Strings
    {
        public static readonly IReadOnlyList<string> Languages = new[] { "auto", "en", "de", "it" };

        /// <summary>"auto", "en", "de" or "it" (from the settings).</summary>
        public static string Language { get; set; } = "auto";

        public static string Effective => Language is "en" or "de" or "it"
            ? Language
            : CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch { "de" => "de", "it" => "it", _ => "en" };

        private static string T(string en, string de, string it) => Effective switch { "de" => de, "it" => it, _ => en };

        private static string T(string en, string de, string it, string fr, string es) =>
            Effective switch { "de" => de, "it" => it, "fr" => fr, "es" => es, _ => en };

        public static string LanguageName(string code) => code switch
        {
            "en" => "English",
            "de" => "Deutsch",
            "it" => "Italiano",
            _ => T("Automatic (Windows language)", "Automatisch (Windows-Sprache)", "Automatica (lingua di Windows)")
        };

        // Documents shown in the app
        public static string HelpDocument => Effective switch { "de" => "HILFE.md", "it" => "AIUTO.md", _ => "HELP.md" };
        public static string ChangelogDocument => Effective switch { "de" => "CHANGELOG.de.md", "it" => "CHANGELOG.it.md", _ => "CHANGELOG.md" };

        #region Style names

        public static string ThemeName(string id) => id switch
        {
            "default" => T("Standard (glass)", "Standard (Glas)", "Standard (vetro)"),
            "windows" => T("Windows accent color", "Windows-Akzentfarbe", "Colore d'accento di Windows", "Couleur d'accentuation Windows", "Color de énfasis de Windows"),
            "starcitizen" => "Star Citizen (HUD)",
            "retroarcade" => "Retro-Arcade",
            "hardware" => T("Hardware (circuit board)", "Hardware (Platine)", "Hardware (circuito)"),
            "nerd" => T("Nerd (terminal)", "Nerd (Terminal)", "Nerd (terminale)"),
            "hobby" => T("Hobby (pinboard)", "Hobby (Pinnwand)", "Hobby (bacheca)"),
            "work" => T("Work (business)", "Arbeit (Business)", "Lavoro (business)"),
            "family" => T("Family", "Familie", "Famiglia"),
            "gaming" => "Gaming (RGB)",
            "finance" => T("Finance (trading desk)", "Finanzen (Börse)", "Finanza (borsa)"),
            "social" => "Social",
            "documents" => T("Documents", "Dokumente", "Documenti"),
            "multimedia" => "Multimedia",
            "music" => T("Music", "Musik", "Musica"),
            "sport" => "Sport",
            "photos" => T("Photos", "Fotos", "Foto"),
            "travel" => T("Travel", "Reisen", "Viaggi"),
            "cooking" => T("Cooking", "Kochen", "Cucina"),
            "nature" => T("Nature", "Natur", "Natura"),
            "postit" => T("Post-it (yellow)", "Post-it (Gelb)", "Post-it (giallo)"),
            "postit-pink" => T("Post-it (pink)", "Post-it (Rosa)", "Post-it (rosa)"),
            "postit-green" => T("Post-it (green)", "Post-it (Grün)", "Post-it (verde)"),
            "postit-blue" => T("Post-it (blue)", "Post-it (Blau)", "Post-it (blu)"),
            "postit-orange" => T("Post-it (orange)", "Post-it (Orange)", "Post-it (arancione)"),
            _ => id
        };

        #endregion

        #region Menus and fences

        public static string Help => T("Help", "Hilfe", "Guida");
        public static string WhatsNew => T("What's new?", "Was ist neu?", "Novità");
        public static string About => T("About NoFences", "Über NoFences", "Informazioni su NoFences");
        public static string BackupLabel => T("Backup:", "Sicherung:", "Backup:", "Sauvegarde :", "Copia:");
        public static string RestoreShort => T("Restore", "Wiederherstellen", "Ripristina", "Restaurer", "Restaurar");
        public static string LanguageMenu =>"Sprache · Language · Lingua";
        public static string AppSettings =>T("Settings…", "Einstellungen…", "Impostazioni…");
        public static string NewFence => T("New fence", "Neuer Fence", "Nuovo recinto");
        public static string NewFolderFence => T("New folder fence…", "Neuer Ordner-Fence…", "Nuovo recinto cartella…");
        public static string FirstFence => T("First fence", "Erster Fence", "Primo recinto");
        public static string ChooseFolder => T("Choose the folder this fence should show", "Ordner wählen, den dieser Fence anzeigen soll", "Scegli la cartella da mostrare in questo recinto");
        public static string Settings => T("Fence settings…", "Fence-Einstellungen…", "Impostazioni recinto…");
        public static string Locked => T("Locked", "Gesperrt", "Bloccato");
        public static string AutoCollapse => T("Collapse when not hovered", "Einklappen wenn Maus weg", "Comprimi quando il mouse è fuori");
        public static string RemoveItem => T("Remove from fence", "Aus Fence entfernen", "Rimuovi dal recinto");
        public static string OpenFolder => T("Open folder in Explorer", "Ordner im Explorer öffnen", "Apri cartella in Esplora file");
        public static string DeleteFence => T("Delete fence", "Fence löschen", "Elimina recinto");
        public static string ReallyDelete(string name) => T($"Really delete the fence \"{name}\"?", $"Fence \"{name}\" wirklich löschen?", $"Eliminare davvero il recinto \"{name}\"?");
        public static string ReallyDeleteFolderNote => T("The folder and its files are not touched.", "Der Ordner und seine Dateien bleiben unangetastet.", "La cartella e i suoi file non vengono toccati.");
        public static string ShowFences => T("Show fences", "Fences anzeigen", "Mostra recinti");
        public static string Autostart => T("Start with Windows", "Mit Windows starten", "Avvia con Windows");
        public static string ShowExtensions => T("Show file extensions", "Dateiendungen anzeigen", "Mostra estensioni dei file");
        public static string ExtFollowExplorer => T("Like Explorer", "Wie im Explorer", "Come in Esplora file");
        public static string ExtAlways => T("Always", "Immer", "Sempre");
        public static string ExtNever => T("Never", "Nie", "Mai");
        public static string Theme => T("Style", "Style", "Stile");
        public static string ThemeGlobal => T("Default style", "Standard-Style", "Stile predefinito");
        public static string ThemeInherit => T("(use default style)", "(Standard-Style verwenden)", "(usa lo stile predefinito)");
        public static string OpenDataFolder => T("Open config folder", "Konfigurationsordner öffnen", "Apri cartella di configurazione");
        public static string Exit => T("Exit", "Beenden", "Esci");
        public static string Rename => T("Rename", "Umbenennen", "Rinomina");
        public static string NewName => T("New name:", "Neuer Name:", "Nuovo nome:");
        public static string RenameFailed(string reason) => T($"Could not rename: {reason}", $"Umbenennen nicht möglich: {reason}", $"Impossibile rinominare: {reason}");
        public static string Search => T("Search", "Suche", "Cerca");
        public static string AlwaysOnTop => T("Always on top", "Immer im Vordergrund", "Sempre in primo piano");
        public static string ProfileAll => T("All fences", "Alle Fences", "Tutti i recinti");
        public static string ProfileMenu(string active) => T($"Profile: {active}", $"Profil: {active}", $"Profilo: {active}");
        public static string ProfileNew => T("New profile…", "Neues Profil…", "Nuovo profilo…");
        public static string ProfileDelete => T("Delete profile", "Profil löschen", "Elimina profilo");
        public static string ProfileNamePrompt => T("Name of the profile (e.g. Work, Gaming):", "Name des Profils (z. B. Arbeit, Gaming):", "Nome del profilo (es. Lavoro, Gaming):");
        public static string ProfileDeleteConfirm(string name) => T(
            $"Delete the profile \"{name}\"? The fences stay; they just no longer belong to it.",
            $"Profil „{name}“ löschen? Die Fences bleiben erhalten, sie gehören nur nicht mehr dazu.",
            $"Eliminare il profilo «{name}»? I recinti restano, semplicemente non ne fanno più parte.");
        public static string ProfileSwitched(string name) => T($"Profile: {name}", $"Profil: {name}", $"Profilo: {name}");
        public static string ProfileHowTo => T("Assign fences: right-click a fence → Show in profile", "Fences zuordnen: Rechtsklick auf ein Fence → In Profil zeigen", "Assegna recinti: clic destro su un recinto → Mostra nel profilo");
        public static string ProfileFenceMenu => T("Show in profile", "In Profil zeigen", "Mostra nel profilo");
        public static string ProfileFenceHint => T("No check = in every profile", "Ohne Haken = in allen Profilen", "Nessuna spunta = in tutti i profili");
        public static string ProfileLabel => T("Active profile", "Aktives Profil", "Profilo attivo");
        public static string SectionProfiles => T("Profiles", "Profile", "Profili");
        public static string OnlyThisDesktop =>T("Only on this virtual desktop", "Nur auf diesem virtuellen Desktop", "Solo su questo desktop virtuale");
        public static string DropHint => T("Drop files or folders here", "Dateien oder Ordner hierher ziehen", "Trascina qui file o cartelle");
        public static string FolderMissing(string path) => T($"Folder not found:\n{path}", $"Ordner nicht gefunden:\n{path}", $"Cartella non trovata:\n{path}");
        public static string FirstStartHint => T("Drag files onto the fence. Right-click a fence for options; the tray icon has the settings and help.",
                                                 "Zieh Dateien auf den Fence. Rechtsklick auf einen Fence zeigt die Optionen, im Tray-Icon gibt es Einstellungen und Hilfe.",
                                                 "Trascina i file nel recinto. Clic destro su un recinto per le opzioni; l'icona nella barra ha impostazioni e guida.");

        public static string SortBy => T("Sort by", "Sortieren nach", "Ordina per");
        public static string SortModeName(Model.FenceSortMode mode) => mode switch
        {
            Model.FenceSortMode.Name => T("Name", "Name", "Nome"),
            Model.FenceSortMode.Type => T("Type", "Typ", "Tipo"),
            Model.FenceSortMode.Modified => T("Date modified (newest first)", "Änderungsdatum (neueste zuerst)", "Data di modifica (più recenti prima)"),
            Model.FenceSortMode.Size => T("Size (largest first)", "Größe (größte zuerst)", "Dimensione (più grandi prima)"),
            _ => T("Manual (drag & drop)", "Manuell (Drag & Drop)", "Manuale (trascina e rilascia)")
        };

        public static string AddTab => T("Add tab", "Reiter hinzufügen", "Aggiungi scheda");
        public static string RenameTab => T("Rename tab", "Reiter umbenennen", "Rinomina scheda");
        public static string RemoveTab => T("Remove tab (keeps its links)", "Reiter entfernen (Verknüpfungen bleiben)", "Rimuovi scheda (i collegamenti restano)");
        public static string TabDefaultName(int n) => T($"Tab {n}", $"Reiter {n}", $"Scheda {n}");

        public static string NewWidget => T("New widget", "Neues Widget", "Nuovo widget");
        public static string NewRecent => T("New \"Recent files\" fence", "Neuer Fence „Zuletzt verwendet“", "Nuovo recinto \"File recenti\"");
        public static string RecentName => T("Recent files", "Zuletzt verwendet", "File recenti");
        public static string NewQuickLaunch => T("New quick-launch bar", "Neue Schnellstart-Leiste", "Nuova barra di avvio rapido");
        public static string QuickLaunchName => T("Quick launch", "Schnellstart", "Avvio rapido");
        public static string CompactMode => T("Icons only (compact)", "Nur Icons (kompakt)", "Solo icone (compatto)");
        public static string NewNote => T("New note", "Neue Notiz", "Nuova nota");
        public static string NoteName => T("Note", "Notiz", "Nota");
        public static string EditNote => T("Edit note", "Notiz bearbeiten", "Modifica nota");
        public static string NoteHint => T("Double-click to write.\nLines starting with [ ] become checkboxes.",
                                           "Doppelklick zum Schreiben.\nZeilen mit [ ] am Anfang werden zu Kästchen.",
                                           "Doppio clic per scrivere.\nLe righe che iniziano con [ ] diventano caselle.");

        #endregion

        #region Settings dialogs

        public static string Name => T("Name", "Name", "Nome");
        public static string Folder => T("Folder", "Ordner", "Cartella");
        public static string Browse => T("Browse…", "Durchsuchen…", "Sfoglia…");
        public static string TitleHeight => T("Title height", "Titelhöhe", "Altezza titolo");
        public static string IconSize => T("Icon size", "Icongröße", "Dimensione icone");
        public static string Background => T("Background", "Hintergrund", "Sfondo");
        public static string Opacity => T("Opacity", "Deckkraft", "Opacità");
        public static string Ok => T("OK", "OK", "OK");
        public static string Cancel => T("Cancel", "Abbrechen", "Annulla");
        public static string Close => T("Close", "Schließen", "Chiudi");
        public static string Kind => T("Type", "Typ", "Tipo");
        public static string KindLinks => T("Links (files stay where they are)", "Verknüpfungen (Dateien bleiben wo sie sind)", "Collegamenti (i file restano dove sono)");
        public static string KindFolder => T("Folder (shows a folder's contents)", "Ordner (zeigt den Inhalt eines Ordners)", "Cartella (mostra il contenuto di una cartella)");
        public static string KindNote => T("Note (sticky note with text)", "Notiz (Post-it mit Text)", "Nota (post-it con testo)");
        public static string KindWidget => T("Widget", "Widget", "Widget");
        public static string Preview => T("Preview", "Vorschau", "Anteprima");
        public static string SectionGeneral => T("General", "Allgemein", "Generale");
        public static string SectionAppearance => T("Appearance", "Aussehen", "Aspetto");
        public static string SectionBehavior => T("Behavior", "Verhalten", "Comportamento");
        public static string SectionAutoSort => T("Auto-sort from the desktop", "Vom Desktop einsortieren", "Ordina dal desktop");
        public static string SectionDesktop => T("Desktop", "Desktop", "Desktop");
        public static string SectionUpdates => T("Updates", "Updates", "Aggiornamenti");
        public static string SectionFps => T("FPS measurement", "FPS-Messung", "Misurazione FPS");
        public static string SectionData => T("Data & styles", "Daten & Styles", "Dati e stili");
        public static string SettingsTitle => T("NoFences settings", "NoFences-Einstellungen", "Impostazioni di NoFences");
        public static string LanguageLabel => T("Language", "Sprache", "Lingua");
        public static string VersionLabel(Version v) => T($"Installed version: {v}", $"Installierte Version: {v}", $"Versione installata: {v}");
        public static string FpsShortHint => T("Needs a small helper with administrator rights (Windows asks once). It only counts frames – no screen content, no input.",
                                               "Braucht einen kleinen Helfer mit Administratorrechten (Windows fragt einmal). Er zählt nur Bilder – keine Bildinhalte, keine Eingaben.",
                                               "Richiede un piccolo programma di supporto con diritti di amministratore (Windows lo chiede una volta). Conta solo i fotogrammi, nessun contenuto e nessun input.");
        public static string FpsEnabledLabel => T("Measure FPS of games", "FPS von Spielen messen", "Misura gli FPS dei giochi");

        public static string AutoSort => T("Patterns", "Muster", "Schemi");
        public static string AutoSortHint => T("New desktop files matching these patterns go into this fence, e.g. *.pdf; *.docx",
                                               "Neue Desktop-Dateien, die passen, landen in diesem Fence, z. B. *.pdf; *.docx",
                                               "I nuovi file sul desktop che corrispondono finiscono in questo recinto, ad es. *.pdf; *.docx");
        public static string AddPreset => T("Add preset…", "Vorlage hinzufügen…", "Aggiungi modello…");
        public static string PresetImages => T("Images", "Bilder", "Immagini");
        public static string PresetDocuments => T("Documents", "Dokumente", "Documenti");
        public static string PresetArchives => T("Archives", "Archive", "Archivi");
        public static string PresetInstallers => T("Installers / programs", "Installer / Programme", "Installer / programmi");
        public static string PresetVideos => T("Videos", "Videos", "Video");
        public static string PresetMusic => T("Music", "Musik", "Musica");
        public static string PresetShortcuts => T("Shortcuts", "Verknüpfungen", "Collegamenti");

        #endregion

        #region Desktop, sorting, peek

        public static string AutoSortEnabled => T("Auto-sort new desktop files", "Neue Desktop-Dateien automatisch einsortieren", "Ordina automaticamente i nuovi file del desktop");
        public static string SortNow => T("Tidy up desktop now", "Desktop jetzt aufräumen", "Riordina il desktop ora");
        public static string SortNowNoRules => T("No fence has auto-sort patterns yet.\nSet them in a fence's settings.",
                                                 "Noch kein Fence hat Einsortier-Regeln.\nDu legst sie in den Fence-Einstellungen fest.",
                                                 "Nessun recinto ha ancora regole di ordinamento.\nImpostale nelle impostazioni di un recinto.");
        public static string SortNowDone(int n) => n == 1
            ? T("1 file sorted into fences.", "1 Datei in Fences einsortiert.", "1 file ordinato nei recinti.")
            : T($"{n} files sorted into fences.", $"{n} Dateien in Fences einsortiert.", $"{n} file ordinati nei recinti.");
        public static string DoubleClickToggle => T("Double-click desktop to hide fences", "Doppelklick auf Desktop blendet Fences aus", "Doppio clic sul desktop nasconde i recinti");
        public static string PeekMenu => T("Bring fences to front", "Fences nach vorne holen", "Porta i recinti in primo piano");
        public static string PeekHotkey => T("Shortcut", "Tastenkürzel", "Scorciatoia");
        public static string HotkeyName(string hotkey) => hotkey switch
        {
            "Off" => T("Off", "Aus", "Disattivata"),
            _ => Effective == "de" ? hotkey.Replace("Ctrl", "Strg").Replace("Space", "Leertaste")
                : Effective == "it" ? hotkey.Replace("Space", "Spazio").Replace("Shift", "Maiusc")
                : hotkey
        };
        public static string HotkeyTaken(string hotkey) => T(
            $"The shortcut {HotkeyName(hotkey)} is already used by another program. Pick another one in the settings.",
            $"Das Tastenkürzel {HotkeyName(hotkey)} wird schon von einem anderen Programm verwendet. Wähle in den Einstellungen ein anderes.",
            $"La scorciatoia {HotkeyName(hotkey)} è già usata da un altro programma. Scegline un'altra nelle impostazioni.");

        #endregion

        #region Updates, styles, backups, export

        public static string CheckForUpdatesAuto => T("Check for updates automatically", "Automatisch nach Updates suchen", "Cerca aggiornamenti automaticamente");
        public static string CheckForUpdatesNow => T("Check for updates now", "Jetzt nach Updates suchen", "Cerca aggiornamenti ora");
        public static string InstallUpdate(Version v) => T($"Install update {v}", $"Update {v} installieren", $"Installa l'aggiornamento {v}");
        public static string UpdateAvailable(Version v) => T($"NoFences {v} is available. Click here to install it.", $"NoFences {v} ist verfügbar. Hier klicken zum Installieren.", $"NoFences {v} è disponibile. Clicca qui per installarlo.");
        public static string UpdateAvailableManual(Version v) => T($"NoFences {v} is available. Click here to open the download page.", $"NoFences {v} ist verfügbar. Hier klicken, um die Download-Seite zu öffnen.", $"NoFences {v} è disponibile. Clicca qui per aprire la pagina di download.");
        public static string UpToDate(Version v) => T($"You have the latest version ({v}).", $"Du hast die neueste Version ({v}).", $"Hai l'ultima versione ({v}).");
        public static string UpdateDownloading => T("Downloading update…", "Update wird heruntergeladen…", "Download dell'aggiornamento…");
        public static string UpdateFailed(string reason) => T($"The update failed: {reason}\nThe download page will open instead.", $"Das Update ist fehlgeschlagen: {reason}\nStattdessen öffnet sich die Download-Seite.", $"L'aggiornamento non è riuscito: {reason}\nSi apre invece la pagina di download.");
        public static string UpdateCheckFailed => T("Could not reach GitHub to check for updates.", "GitHub war für die Update-Prüfung nicht erreichbar.", "Impossibile raggiungere GitHub per cercare aggiornamenti.");
        public static string Animations => T("Animations", "Animationen", "Animazioni");
        public static string CustomThemes => T("Own styles", "Eigene Styles", "Stili personali");
        public static string OpenThemesFolder => T("Open styles folder", "Styles-Ordner öffnen", "Apri cartella degli stili");
        public static string ReloadThemes => T("Reload styles", "Styles neu laden", "Ricarica stili");
        public static string ThemesLoaded(int n) => T($"{n} own style(s) loaded.", $"{n} eigene(r) Style(s) geladen.", $"{n} stile/i personale/i caricato/i.");
        public static string ThemeErrors(string details) => T($"Some styles could not be loaded:\n{details}", $"Einige Styles konnten nicht geladen werden:\n{details}", $"Alcuni stili non sono stati caricati:\n{details}");
        public static string RestoreBackup => T("Restore backup", "Sicherung wiederherstellen", "Ripristina backup");
        public static string NoBackups => T("No backups yet", "Noch keine Sicherungen", "Ancora nessun backup");
        public static string ConfirmRestore(DateTime time) => T(
            $"Restore all fences as they were on {time:g}?\nNoFences restarts; the current state is kept as a backup too.",
            $"Alle Fences auf den Stand vom {time:g} zurücksetzen?\nNoFences startet neu; der aktuelle Stand wird vorher ebenfalls gesichert.",
            $"Ripristinare tutti i recinti com'erano il {time:g}?\nNoFences si riavvia; anche lo stato attuale viene salvato come backup.");
        public static string ExportFences => T("Export fences…", "Fences exportieren…", "Esporta recinti…");
        public static string ImportFences => T("Import fences…", "Fences importieren…", "Importa recinti…");
        public static string ExportFilter => T("NoFences export (*.nofences.json)|*.nofences.json|JSON (*.json)|*.json", "NoFences-Export (*.nofences.json)|*.nofences.json|JSON (*.json)|*.json", "Esportazione NoFences (*.nofences.json)|*.nofences.json|JSON (*.json)|*.json");
        public static string ExportDone(int n) => T($"{n} fences exported.", $"{n} Fences exportiert.", $"{n} recinti esportati.");
        public static string ImportDone(int n) => T($"{n} fences imported.", $"{n} Fences importiert.", $"{n} recinti importati.");
        public static string ImportFailed(string reason) => T($"Import failed: {reason}", $"Import fehlgeschlagen: {reason}", $"Importazione non riuscita: {reason}");

        #endregion

        #region Reminders

        public static string Reminder => T("Reminder…", "Erinnerung…", "Promemoria…");
        public static string ReminderTitle => T("Remind me", "Erinnern", "Ricordami");
        public static string ReminderIn1h => T("In 1 hour", "In 1 Stunde", "Tra 1 ora");
        public static string ReminderTonight => T("Today 6 pm", "Heute 18:00", "Oggi alle 18:00");
        public static string ReminderTomorrow => T("Tomorrow 9 am", "Morgen 9:00", "Domani alle 9:00");
        public static string ReminderRemove => T("Remove", "Entfernen", "Rimuovi");
        public static string ReminderDue(string name) => T($"Reminder: {name}", $"Erinnerung: {name}", $"Promemoria: {name}");

        #endregion

        #region Widgets

        public static string WidgetClock => T("Clock & calendar", "Uhr & Kalender", "Orologio e calendario");
        public static string WidgetSystem => T("System monitor (CPU, RAM, GPU, FPS)", "System-Monitor (CPU, RAM, GPU, FPS)", "Monitor di sistema (CPU, RAM, GPU, FPS)");
        public static string WidgetDrives => T("Drives", "Laufwerke", "Unità");
        public static string WidgetRecycleBin => T("Recycle bin", "Papierkorb", "Cestino");
        public static string WidgetCountdown => T("Countdown", "Countdown", "Conto alla rovescia");
        public static string CountdownSet => T("Set countdown…", "Countdown festlegen…", "Imposta conto alla rovescia…");
        public static string CountdownHint => T("Double-click or right-click → Set countdown", "Doppelklick oder Rechtsklick → Countdown festlegen", "Doppio clic o clic destro → Imposta conto alla rovescia");
        public static string CountdownDays(int n) => n == 1 ? T("1 day", "1 Tag", "1 giorno") : T($"{n} days", $"{n} Tage", $"{n} giorni");
        public static string CountdownReached => T("It's time!", "Es ist so weit!", "Ci siamo!");
        public static string CountdownTitleLabel => T("Title", "Titel", "Titolo");
        public static string CountdownDateLabel => T("Date and time", "Datum und Uhrzeit", "Data e ora");
        public static string WidgetPlaytime => T("Playtime", "Spielzeit", "Tempo di gioco");
        public static string PlaytimeChoose => T("Choose game (exe)…", "Spiel auswählen (EXE)…", "Scegli gioco (exe)…");
        public static string PlaytimeChooseHint => T("Double-click to choose the game's exe. NoFences then records how long it runs.",
                                                     "Doppelklick, um die EXE des Spiels auszuwählen. NoFences zeichnet dann auf, wie lange es läuft.",
                                                     "Doppio clic per scegliere l'eseguibile del gioco. NoFences registra poi per quanto tempo è in esecuzione.");
        public static string PlaytimeExeFilter => T("Programs (*.exe)|*.exe", "Programme (*.exe)|*.exe", "Programmi (*.exe)|*.exe");
        public static string PlaytimeRunning => T("running", "läuft", "in corso");
        public static string PlaytimeToday => T("today", "heute", "oggi");
        public static string PlaytimeWeek => T("This week", "Diese Woche", "Questa settimana");
        public static string PlaytimeMonth => T("This month", "Diesen Monat", "Questo mese");
        public static string PlaytimeTotal => T("Total", "Gesamt", "Totale");
        public static string WidgetWeather => T("Weather", "Wetter", "Meteo");
        public static string WeatherHint => T("Double-click to choose a place.", "Doppelklick, um einen Ort auszuwählen.", "Doppio clic per scegliere una località.");
        public static string WeatherChoose => T("Choose place…", "Ort auswählen…", "Scegli località…");
        public static string WeatherUpdateNow => T("Update now", "Jetzt aktualisieren", "Aggiorna ora");
        public static string WeatherPlaceLabel => T("Town or city:", "Ort oder Stadt:", "Località o città:");
        public static string WeatherSearch => T("Search", "Suchen", "Cerca");
        public static string WeatherLoading => T("Loading…", "Wird geladen…", "Caricamento…");
        public static string WeatherOffline => T("No connection to the weather service.", "Keine Verbindung zum Wetterdienst.", "Nessuna connessione al servizio meteo.");
        public static string WeatherNoPlace => T("No place found.", "Kein Ort gefunden.", "Nessuna località trovata.");
        public static string WeatherCredit => T("Weather data: Open-Meteo.com", "Wetterdaten: Open-Meteo.com", "Dati meteo: Open-Meteo.com");
        public static string WeatherDetails(double feelsLike, double wind) => T(
            $"Feels like {feelsLike:0}° · wind {wind:0} km/h", $"Gefühlt {feelsLike:0}° · Wind {wind:0} km/h", $"Percepita {feelsLike:0}° · vento {wind:0} km/h");
        public static string WeatherKindName(Widgets.WeatherKind kind) => kind switch
        {
            Widgets.WeatherKind.Clear => T("Clear", "Klar", "Sereno"),
            Widgets.WeatherKind.PartlyCloudy => T("Partly cloudy", "Teils bewölkt", "Parzialmente nuvoloso"),
            Widgets.WeatherKind.Cloudy => T("Cloudy", "Bewölkt", "Nuvoloso"),
            Widgets.WeatherKind.Fog => T("Fog", "Nebel", "Nebbia"),
            Widgets.WeatherKind.Drizzle => T("Drizzle", "Nieselregen", "Pioviggine"),
            Widgets.WeatherKind.Rain => T("Rain", "Regen", "Pioggia"),
            Widgets.WeatherKind.Snow => T("Snow", "Schnee", "Neve"),
            _ => T("Thunderstorm", "Gewitter", "Temporale")
        };
        public static string WidgetMedia => T("Now playing (media)", "Medien (läuft gerade)", "In riproduzione (media)");
        public static string MediaNothing => T("Nothing is playing.\nMusic and videos from Spotify, browsers etc. appear here.",
                                              "Gerade läuft nichts.\nMusik und Videos aus Spotify, Browsern usw. erscheinen hier.",
                                              "Nessuna riproduzione.\nMusica e video da Spotify, browser ecc. appaiono qui.");
        public static string WidgetNetwork => T("Network", "Netzwerk", "Rete");
        public static string WidgetClipboard => T("Clipboard history", "Zwischenablage-Verlauf", "Cronologia appunti");
        public static string ClipboardHint => T("Copied texts appear here – click one to copy it again. Kept only until NoFences closes; passwords from password managers are skipped.",
                                               "Kopierte Texte erscheinen hier – anklicken kopiert sie erneut. Nur bis NoFences beendet wird; Passwörter aus Passwort-Managern werden übersprungen.",
                                               "I testi copiati appaiono qui – fai clic per copiarli di nuovo. Conservati solo finché NoFences è aperto; le password dei gestori di password vengono ignorate.");
        public static string ClipboardClear => T("Clear history", "Verlauf leeren", "Cancella cronologia");
        public static string WidgetBattery => T("Battery", "Akku", "Batteria");
        public static string BatteryNone => T("No battery found.", "Kein Akku gefunden.", "Nessuna batteria trovata.");
        public static string BatteryCharging => T("Charging", "Wird geladen", "In carica");
        public static string BatteryPlugged => T("Plugged in", "Am Netz", "Collegato");
        public static string BatteryOnBattery => T("On battery", "Akkubetrieb", "A batteria");
        public static string BatteryLeft(string time) => T($"{time} left", $"noch {time}", $"ancora {time}");
        public static string DriveDefaultName(DriveType type) => type switch
        {
            DriveType.Removable => T("USB drive", "USB-Laufwerk", "Unità USB"),
            DriveType.Network => T("Network", "Netzwerk", "Rete"),
            _ => T("Local disk", "Lokaler Datenträger", "Disco locale")
        };
        public static string FreeSpace(string size) => T($"{size} free", $"{size} frei", $"{size} liberi");
        public static string RecycleEmptyState => T("Empty", "Leer", "Vuoto");
        public static string RecycleItems(long n, string size) => n == 1
            ? T($"1 item · {size}", $"1 Element · {size}", $"1 elemento · {size}")
            : T($"{n} items · {size}", $"{n} Elemente · {size}", $"{n} elementi · {size}");
        public static string RecycleDropHint => T("Drop files here to delete", "Zum Löschen hierher ziehen", "Trascina qui per eliminare");
        public static string RecycleEmptyAction => T("Empty recycle bin", "Papierkorb leeren", "Svuota cestino");
        public static string GpuTemperature => T("GPU temp.", "GPU-Temp.", "Temp. GPU");
        public static string FpsWaiting => T("waiting for a game…", "wartet auf ein Spiel…", "in attesa di un gioco…");
        public static string FpsMenu => T("Measure FPS (admin helper)…", "FPS messen (Admin-Helfer)…", "Misura FPS (supporto amministratore)…");
        public static string FpsTitle => T("Measure FPS – admin rights needed", "FPS messen – Administratorrechte nötig", "Misura FPS – servono diritti di amministratore");
        public static string FpsExplanation => T(
            "To measure the frame rate (FPS) of games, NoFences needs a small helper process with administrator rights. " +
            "Windows only gives the graphics output events (ETW) to programs with admin rights – MSI Afterburner and PresentMon work the same way.\n\n" +
            "• Only this helper runs as administrator, NoFences itself does not.\n" +
            "• It only counts how often frames are shown – no screen content, no keyboard or mouse input.\n" +
            "• The first time, Windows asks for permission (UAC). The helper then creates a task in the Task Scheduler so it can start later without asking.\n" +
            "• Turn it off at any time in the settings; the task is removed again.\n\n" +
            "Enable FPS measurement?",
            "Um die Bildrate (FPS) von Spielen zu messen, braucht NoFences einen kleinen Hilfsprozess mit Administratorrechten. " +
            "Windows gibt die nötigen Ereignisse der Grafikausgabe (ETW) nur an Programme mit Adminrechten – MSI Afterburner und PresentMon machen es genauso.\n\n" +
            "• Nur dieser Helfer läuft als Administrator, NoFences selbst nicht.\n" +
            "• Er zählt nur, wie oft Bilder ausgegeben werden – keine Bildschirminhalte, keine Tastatur- oder Mauseingaben.\n" +
            "• Beim ersten Mal fragt Windows nach Erlaubnis (UAC). Danach legt der Helfer eine Aufgabe in der Aufgabenplanung an, damit er später ohne Nachfrage starten kann.\n" +
            "• Ausschalten jederzeit in den Einstellungen; die Aufgabe wird dabei wieder entfernt.\n\n" +
            "FPS-Messung aktivieren?",
            "Per misurare la frequenza dei fotogrammi (FPS) dei giochi, NoFences ha bisogno di un piccolo processo di supporto con diritti di amministratore. " +
            "Windows fornisce gli eventi dell'output grafico (ETW) solo ai programmi con diritti di amministratore – MSI Afterburner e PresentMon funzionano allo stesso modo.\n\n" +
            "• Solo questo processo di supporto gira come amministratore, NoFences no.\n" +
            "• Conta solo quante volte vengono mostrati i fotogrammi – nessun contenuto dello schermo, nessun input da tastiera o mouse.\n" +
            "• La prima volta Windows chiede il permesso (UAC). Poi il supporto crea un'attività nell'Utilità di pianificazione, così può avviarsi in seguito senza chiedere.\n" +
            "• Puoi disattivarlo in qualsiasi momento nelle impostazioni; l'attività viene rimossa.\n\n" +
            "Attivare la misurazione degli FPS?");
        public static string FpsDeclined => T("FPS measurement stays off (no admin rights granted).", "FPS-Messung bleibt aus (keine Adminrechte erteilt).", "La misurazione degli FPS resta disattivata (diritti di amministratore non concessi).");

        #endregion

        #region More widgets

        public static string WidgetGames => T("Games (Steam, Epic, GOG, Xbox)", "Spiele (Steam, Epic, GOG, Xbox)", "Giochi (Steam, Epic, GOG, Xbox)", "Jeux (Steam, Epic, GOG, Xbox)", "Juegos (Steam, Epic, GOG, Xbox)");
        public static string GamesSearching => T("Looking for installed games…", "Suche installierte Spiele…", "Cerco i giochi installati…", "Recherche des jeux installés…", "Buscando juegos instalados…");
        public static string GamesNone => T("No games found from Steam, Epic, GOG or the Xbox app.", "Keine Spiele aus Steam, Epic, GOG oder der Xbox-App gefunden.", "Nessun gioco trovato da Steam, Epic, GOG o dall'app Xbox.", "Aucun jeu trouvé dans Steam, Epic, GOG ou l'application Xbox.", "No se encontraron juegos de Steam, Epic, GOG o la app de Xbox.");
        public static string GamesHide(string name) => T($"Hide \"{name}\"", $"„{name}“ ausblenden", $"Nascondi «{name}»", $"Masquer « {name} »", $"Ocultar «{name}»");
        public static string GamesShowHidden(int n) => T($"Show hidden games ({n})", $"Ausgeblendete Spiele zeigen ({n})", $"Mostra giochi nascosti ({n})", $"Afficher les jeux masqués ({n})", $"Mostrar juegos ocultos ({n})");
        public static string GamesSortByName => T("Sort by name", "Nach Name sortieren", "Ordina per nome", "Trier par nom", "Ordenar por nombre");
        public static string GamesRescan => T("Search again", "Erneut suchen", "Cerca di nuovo", "Rechercher à nouveau", "Buscar de nuevo");

        public static string WidgetAgenda => T("Appointments (calendar)", "Termine (Kalender)", "Appuntamenti (calendario)", "Rendez-vous (agenda)", "Citas (calendario)");
        public static string AgendaHint => T("Double-click and paste the link of your calendar (.ics) – from Google, Outlook or iCloud.", "Doppelklick und den Link deines Kalenders (.ics) einfügen – aus Google, Outlook oder iCloud.", "Doppio clic e incolla il link del tuo calendario (.ics) – da Google, Outlook o iCloud.", "Double-cliquez et collez le lien de votre agenda (.ics) – Google, Outlook ou iCloud.", "Haz doble clic y pega el enlace de tu calendario (.ics): Google, Outlook o iCloud.");
        public static string AgendaPrompt => T(
            "Calendar links (.ics), one per line.\nGoogle: Calendar settings → \"Secret address in iCal format\". Outlook: Settings → Calendar → Shared calendars → Publish → ICS. iCloud: share the calendar publicly.",
            "Kalender-Links (.ics), einer pro Zeile.\nGoogle: Kalendereinstellungen → „Privatadresse im iCal-Format“. Outlook: Einstellungen → Kalender → Freigegebene Kalender → Veröffentlichen → ICS. iCloud: Kalender öffentlich freigeben.",
            "Link dei calendari (.ics), uno per riga.\nGoogle: impostazioni del calendario → \"Indirizzo segreto in formato iCal\". Outlook: Impostazioni → Calendario → Calendari condivisi → Pubblica → ICS. iCloud: condividi il calendario pubblicamente.",
            "Liens d'agenda (.ics), un par ligne.\nGoogle : paramètres de l'agenda → « Adresse secrète au format iCal ». Outlook : Paramètres → Calendrier → Calendriers partagés → Publier → ICS. iCloud : partager l'agenda publiquement.",
            "Enlaces de calendario (.ics), uno por línea.\nGoogle: configuración del calendario → «Dirección secreta en formato iCal». Outlook: Configuración → Calendario → Calendarios compartidos → Publicar → ICS. iCloud: comparte el calendario públicamente.");
        public static string AgendaSet => T("Calendar links…", "Kalender-Links…", "Link dei calendari…", "Liens d'agenda…", "Enlaces de calendario…");
        public static string AgendaFailed => T("The calendar could not be loaded. Check the link.", "Der Kalender konnte nicht geladen werden. Prüfe den Link.", "Impossibile caricare il calendario. Controlla il link.", "Impossible de charger l'agenda. Vérifiez le lien.", "No se pudo cargar el calendario. Comprueba el enlace.");
        public static string AgendaEmpty => T("No appointments in the next two weeks.", "Keine Termine in den nächsten zwei Wochen.", "Nessun appuntamento nelle prossime due settimane.", "Aucun rendez-vous dans les deux prochaines semaines.", "No hay citas en las próximas dos semanas.");
        public static string AgendaTomorrow => T("Tomorrow", "Morgen", "Domani", "Demain", "Mañana");
        public static string AgendaAllDay => T("all day", "ganztägig", "tutto il giorno", "journée", "todo el día");

        public static string WidgetPhotos => T("Photo frame", "Fotorahmen", "Cornice foto", "Cadre photo", "Marco de fotos");
        public static string PhotosHint => T("Double-click to choose a folder with pictures.", "Doppelklick, um einen Ordner mit Bildern auszuwählen.", "Doppio clic per scegliere una cartella di immagini.", "Double-cliquez pour choisir un dossier d'images.", "Haz doble clic para elegir una carpeta de imágenes.");
        public static string PhotosNone => T("No pictures in this folder.", "Keine Bilder in diesem Ordner.", "Nessuna immagine in questa cartella.", "Aucune image dans ce dossier.", "No hay imágenes en esta carpeta.");
        public static string PhotosChoose => T("Choose picture folder…", "Bilderordner wählen…", "Scegli cartella immagini…", "Choisir le dossier d'images…", "Elegir carpeta de imágenes…");
        public static string PhotosInterval => T("Change picture every", "Bild wechseln alle", "Cambia immagine ogni", "Changer d'image toutes les", "Cambiar imagen cada");
        public static string PhotosShowFile => T("Show in Explorer", "Im Explorer zeigen", "Mostra in Esplora file", "Afficher dans l'Explorateur", "Mostrar en el Explorador");

        public static string WidgetFocus => T("Focus timer (Pomodoro)", "Fokus-Timer (Pomodoro)", "Timer di concentrazione (Pomodoro)", "Minuteur de concentration (Pomodoro)", "Temporizador de concentración (Pomodoro)");
        public static string FocusStart => T("Start", "Start", "Avvia", "Démarrer", "Iniciar");
        public static string FocusPause => T("Pause", "Pause", "Pausa", "Pause", "Pausa");
        public static string FocusReset => T("Reset", "Zurücksetzen", "Azzera", "Réinitialiser", "Reiniciar");
        public static string FocusRound(int n) => T($"Focus · round {n}", $"Fokus · Runde {n}", $"Concentrazione · giro {n}", $"Concentration · tour {n}", $"Concentración · ronda {n}");
        public static string FocusShortBreak => T("Short break", "Kurze Pause", "Pausa breve", "Courte pause", "Descanso corto");
        public static string FocusLongBreak => T("Long break", "Lange Pause", "Pausa lunga", "Longue pause", "Descanso largo");
        public static string FocusBreak(int minutes) => T($"Time for a {minutes}-minute break!", $"Zeit für {minutes} Minuten Pause!", $"È ora di una pausa di {minutes} minuti!", $"C'est l'heure d'une pause de {minutes} minutes !", $"¡Hora de un descanso de {minutes} minutos!");
        public static string FocusBackToWork => T("Break's over – next focus round.", "Pause vorbei – nächste Fokus-Runde.", "Pausa finita – prossimo giro di concentrazione.", "Fin de la pause – prochain tour de concentration.", "Se acabó el descanso: siguiente ronda de concentración.");
        public static string FocusTiming => T("Timing", "Zeiten", "Tempi", "Durées", "Tiempos");
        public static string FocusPreset(int focus, int shortBreak, int longBreak) => T(
            $"{focus} min focus · {shortBreak}/{longBreak} min break", $"{focus} Min. Fokus · {shortBreak}/{longBreak} Min. Pause", $"{focus} min concentrazione · {shortBreak}/{longBreak} min pausa",
            $"{focus} min de concentration · {shortBreak}/{longBreak} min de pause", $"{focus} min de concentración · {shortBreak}/{longBreak} min de descanso");
        public static string FocusSkip => T("Skip to next phase", "Zur nächsten Phase springen", "Passa alla fase successiva", "Passer à la phase suivante", "Saltar a la siguiente fase");

        public static string WidgetNews => T("News (RSS)", "News (RSS)", "Notizie (RSS)", "Actualités (RSS)", "Noticias (RSS)");
        public static string NewsHint => T("Double-click to choose news feeds.", "Doppelklick, um News-Feeds auszuwählen.", "Doppio clic per scegliere i feed di notizie.", "Double-cliquez pour choisir des flux d'actualités.", "Haz doble clic para elegir fuentes de noticias.");
        public static string NewsPrompt => T("Feed links (RSS or Atom), one per line – or add one of these:", "Feed-Links (RSS oder Atom), einer pro Zeile – oder einen davon hinzufügen:", "Link dei feed (RSS o Atom), uno per riga – oppure aggiungine uno di questi:", "Liens de flux (RSS ou Atom), un par ligne – ou ajoutez l'un de ceux-ci :", "Enlaces de fuentes (RSS o Atom), uno por línea, o añade uno de estos:");
        public static string NewsSet => T("News feeds…", "News-Feeds…", "Feed di notizie…", "Flux d'actualités…", "Fuentes de noticias…");

        public static string WidgetTicker => T("Prices (stocks, crypto)", "Kurse (Aktien, Krypto)", "Quotazioni (azioni, cripto)", "Cours (actions, crypto)", "Cotizaciones (acciones, cripto)");
        public static string TickerSet => T("Symbols…", "Symbole…", "Simboli…", "Symboles…", "Símbolos…");
        public static string TickerPrompt => T(
            "Symbols as on Yahoo Finance, one per line (up to 12): AAPL, MSFT, ^GDAXI (DAX), ^ATX, BTC-EUR, ETH-EUR, EURUSD=X …",
            "Symbole wie bei Yahoo Finance, eines pro Zeile (bis zu 12): AAPL, MSFT, ^GDAXI (DAX), ^ATX, BTC-EUR, ETH-EUR, EURUSD=X …",
            "Simboli come su Yahoo Finance, uno per riga (fino a 12): AAPL, MSFT, FTSEMIB.MI, ^GDAXI, BTC-EUR, ETH-EUR, EURUSD=X …",
            "Symboles comme sur Yahoo Finance, un par ligne (jusqu'à 12) : AAPL, MSFT, ^FCHI (CAC 40), ^GDAXI, BTC-EUR, ETH-EUR, EURUSD=X …",
            "Símbolos como en Yahoo Finance, uno por línea (hasta 12): AAPL, MSFT, ^IBEX, ^GDAXI, BTC-EUR, ETH-EUR, EURUSD=X …");

        #endregion

        #region Sync

        public static string SectionSync => T("Use on several PCs", "Auf mehreren PCs nutzen", "Usa su più PC", "Utiliser sur plusieurs PC", "Usar en varios PC");
        public static string SyncHint => T(
            "Keep fences, notes, playtime and styles in a shared folder such as OneDrive – every PC that points to it shows the same fences. Positions are kept per monitor setup.",
            "Fences, Notizen, Spielzeit und Styles in einem gemeinsamen Ordner wie OneDrive ablegen – jeder PC, der darauf zeigt, hat dieselben Fences. Positionen gelten pro Monitor-Anordnung.",
            "Conserva recinti, note, tempo di gioco e stili in una cartella condivisa come OneDrive: ogni PC che la usa mostra gli stessi recinti. Le posizioni valgono per ogni disposizione dei monitor.",
            "Gardez barrières, notes, temps de jeu et styles dans un dossier partagé comme OneDrive : chaque PC qui l'utilise affiche les mêmes barrières. Les positions sont gardées par configuration d'écrans.",
            "Guarda vallas, notas, tiempo de juego y estilos en una carpeta compartida como OneDrive: cada PC que la use muestra las mismas vallas. Las posiciones se guardan por configuración de monitores.");
        public static string SyncChoose => T("Choose shared folder…", "Gemeinsamen Ordner wählen…", "Scegli cartella condivisa…", "Choisir le dossier partagé…", "Elegir carpeta compartida…");
        public static string SyncChooseTitle => T("Shared folder for NoFences (e.g. in OneDrive)", "Gemeinsamer Ordner für NoFences (z. B. in OneDrive)", "Cartella condivisa per NoFences (ad es. in OneDrive)", "Dossier partagé pour NoFences (par ex. dans OneDrive)", "Carpeta compartida para NoFences (p. ej. en OneDrive)");
        public static string SyncExistingQuestion => T(
            "This folder already contains NoFences settings (probably from your other PC).\n\nYes: use them on this PC too.\nNo: replace them with this PC's fences.",
            "In diesem Ordner liegen schon NoFences-Einstellungen (vermutlich von deinem anderen PC).\n\nJa: diese auch auf diesem PC verwenden.\nNein: mit den Fences dieses PCs überschreiben.",
            "Questa cartella contiene già impostazioni di NoFences (probabilmente dall'altro PC).\n\nSì: usale anche su questo PC.\nNo: sostituiscile con i recinti di questo PC.",
            "Ce dossier contient déjà des réglages NoFences (sans doute de votre autre PC).\n\nOui : les utiliser aussi sur ce PC.\nNon : les remplacer par les barrières de ce PC.",
            "Esta carpeta ya contiene ajustes de NoFences (probablemente de tu otro PC).\n\nSí: usarlos también en este PC.\nNo: reemplazarlos por las vallas de este PC.");
        public static string SyncActive(string folder) => T($"Shared folder: {folder}", $"Gemeinsamer Ordner: {folder}", $"Cartella condivisa: {folder}", $"Dossier partagé : {folder}", $"Carpeta compartida: {folder}");
        public static string SyncStop => T("Stop sharing (keep a copy on this PC)", "Nicht mehr teilen (Kopie auf diesem PC behalten)", "Smetti di condividere (tieni una copia su questo PC)", "Arrêter le partage (garder une copie sur ce PC)", "Dejar de compartir (guardar una copia en este PC)");
        public static string SyncStopQuestion => T(
            "Copy the shared fences to this PC and stop using the shared folder? NoFences restarts.",
            "Die gemeinsamen Fences auf diesen PC kopieren und den gemeinsamen Ordner nicht mehr verwenden? NoFences startet neu.",
            "Copiare i recinti condivisi su questo PC e smettere di usare la cartella condivisa? NoFences si riavvia.",
            "Copier les barrières partagées sur ce PC et ne plus utiliser le dossier partagé ? NoFences redémarre.",
            "¿Copiar las vallas compartidas a este PC y dejar de usar la carpeta compartida? NoFences se reinicia.");
        public static string SyncPortable => T("Not available in portable mode (the data lives next to NoFences.exe).", "Im portablen Modus nicht verfügbar (die Daten liegen neben NoFences.exe).", "Non disponibile in modalità portatile (i dati sono accanto a NoFences.exe).", "Indisponible en mode portable (les données sont à côté de NoFences.exe).", "No disponible en modo portátil (los datos están junto a NoFences.exe).");
        public static string SyncReloaded => T("Fences updated from another PC.", "Fences von einem anderen PC aktualisiert.", "Recinti aggiornati da un altro PC.", "Barrières mises à jour depuis un autre PC.", "Vallas actualizadas desde otro PC.");

        #endregion

        #region Automation and search

        public static string ProfileSwitchedAuto(string name) => T($"Profile: {name} (automatic)", $"Profil: {name} (automatisch)", $"Profilo: {name} (automatico)", $"Profil : {name} (automatique)", $"Perfil: {name} (automático)");
        public static string SectionAutomation => T("Automation", "Automatik", "Automazione", "Automatisation", "Automatización");
        public static string SectionProfileRules => T("Switch profiles automatically", "Profile automatisch wechseln", "Cambia profilo automaticamente", "Changer de profil automatiquement", "Cambiar de perfil automáticamente");
        public static string RulesHint => T(
            "E.g. \"Gaming\" while a game runs, \"Work\" on weekdays from 8 to 17. A running program wins over a time rule; when no rule applies, the previous profile comes back.",
            "Z. B. „Gaming“, solange ein Spiel läuft, „Arbeit“ werktags von 8 bis 17 Uhr. Ein laufendes Programm hat Vorrang vor einer Zeitregel; gilt keine Regel mehr, kommt das vorherige Profil zurück.",
            "Ad es. \"Gaming\" mentre è in esecuzione un gioco, \"Lavoro\" nei giorni feriali dalle 8 alle 17. Un programma in esecuzione ha la precedenza su una regola oraria; quando nessuna regola vale più, torna il profilo precedente.",
            "Par ex. « Jeux » pendant qu'un jeu tourne, « Travail » en semaine de 8 h à 17 h. Un programme en cours l'emporte sur une règle horaire ; quand plus aucune règle ne s'applique, le profil précédent revient.",
            "P. ej. «Juegos» mientras se ejecuta un juego, «Trabajo» entre semana de 8 a 17. Un programa en ejecución tiene prioridad sobre una regla horaria; cuando ya no se aplica ninguna regla, vuelve el perfil anterior.");
        public static string RuleAdd => T("Add rule…", "Regel hinzufügen…", "Aggiungi regola…", "Ajouter une règle…", "Añadir regla…");
        public static string RuleEdit => T("Edit…", "Bearbeiten…", "Modifica…", "Modifier…", "Editar…");
        public static string RuleRemove => T("Remove", "Entfernen", "Rimuovi", "Supprimer", "Quitar");
        public static string RuleTitle => T("Profile rule", "Profilregel", "Regola del profilo", "Règle de profil", "Regla de perfil");
        public static string RuleProfile => T("Switch to profile", "Zu Profil wechseln", "Passa al profilo", "Passer au profil", "Cambiar al perfil");
        public static string RuleByProgram => T("While this program runs:", "Solange dieses Programm läuft:", "Mentre è in esecuzione questo programma:", "Tant que ce programme tourne :", "Mientras se ejecuta este programa:");
        public static string RuleByTime => T("On these days and times:", "An diesen Tagen und Uhrzeiten:", "In questi giorni e orari:", "Ces jours et heures :", "En estos días y horas:");
        public static string RuleFrom => T("from", "von", "dalle", "de", "de");
        public static string RuleTo => T("to", "bis", "alle", "à", "a");
        public static string RuleWhileRunning(string profile, string program) => T(
            $"{profile} – while {program} runs", $"{profile} – solange {program} läuft", $"{profile} – mentre è in esecuzione {program}",
            $"{profile} – tant que {program} tourne", $"{profile} – mientras se ejecuta {program}");
        public static string RuleAtTimes(string profile, string days, string from, string to) => $"{profile} – {days}  {from}–{to}";
        public static string SectionFullscreen => T("Full screen", "Vollbild", "Schermo intero", "Plein écran", "Pantalla completa");
        public static string HideOnFullscreen => T("Hide fences while a program runs full screen", "Fences ausblenden, solange ein Programm im Vollbild läuft", "Nascondi i recinti mentre un programma è a schermo intero", "Masquer les barrières pendant qu'un programme est en plein écran", "Ocultar las vallas mientras un programa está en pantalla completa");
        public static string HideOnFullscreenHint => T(
            "Games, videos and presentations: only fences on that monitor are hidden, and they come back right after.",
            "Spiele, Videos und Präsentationen: Nur die Fences auf diesem Monitor werden ausgeblendet, danach sind sie sofort wieder da.",
            "Giochi, video e presentazioni: vengono nascosti solo i recinti su quel monitor e tornano subito dopo.",
            "Jeux, vidéos et présentations : seules les barrières de cet écran sont masquées, et elles reviennent juste après.",
            "Juegos, vídeos y presentaciones: solo se ocultan las vallas de ese monitor y vuelven justo después.");
        public static string SectionAutoTheme => T("Light and dark style", "Heller und dunkler Style", "Stile chiaro e scuro", "Style clair et sombre", "Estilo claro y oscuro");
        public static string AutoThemeLabel => T("Switch automatically", "Automatisch wechseln", "Cambia automaticamente", "Changer automatiquement", "Cambiar automáticamente");
        public static string AutoThemeModeName(AutoThemeMode mode) => mode switch
        {
            AutoThemeMode.Windows => T("Like Windows (light/dark mode)", "Wie Windows (heller/dunkler Modus)", "Come Windows (modalità chiara/scura)", "Comme Windows (mode clair/sombre)", "Como Windows (modo claro/oscuro)"),
            AutoThemeMode.Time => T("By time of day", "Nach Uhrzeit", "In base all'ora", "Selon l'heure", "Según la hora"),
            _ => T("Off", "Aus", "Disattivato", "Désactivé", "Desactivado")
        };
        public static string LightThemeLabel => T("Light style", "Heller Style", "Stile chiaro", "Style clair", "Estilo claro");
        public static string DarkThemeLabel => T("Dark style", "Dunkler Style", "Stile scuro", "Style sombre", "Estilo oscuro");
        public static string DarkTimesLabel => T("Dark from / to", "Dunkel von / bis", "Scuro dalle / alle", "Sombre de / à", "Oscuro de / a");
        public static string AutoThemeHint => T(
            "Applies to fences without their own style.", "Gilt für Fences ohne eigenen Style.", "Vale per i recinti senza uno stile proprio.",
            "S'applique aux barrières sans style propre.", "Se aplica a las vallas sin estilo propio.");
        public static string ThemeGlobalAutoHint => T(
            "Light/dark switching is on (Automation), so the light and dark styles set there are used.",
            "Der Hell/Dunkel-Wechsel ist an (Automatik), daher gelten die dort gewählten Styles.",
            "Il cambio chiaro/scuro è attivo (Automazione), quindi valgono gli stili scelti lì.",
            "Le changement clair/sombre est activé (Automatisation) : ce sont les styles choisis là-bas qui s'appliquent.",
            "El cambio claro/oscuro está activado (Automatización), así que se usan los estilos elegidos allí.");
        public static string SectionSearch => T("Search", "Suche", "Ricerca", "Recherche", "Búsqueda");
        public static string SearchMenu => T("Search fences…", "Fences durchsuchen…", "Cerca nei recinti…", "Rechercher dans les barrières…", "Buscar en las vallas…");
        public static string SearchHotkeyLabel => T("Shortcut:", "Tastenkürzel:", "Scorciatoia:", "Raccourci :", "Atajo:");
        public static string SearchHint => T(
            "Finds everything in your fences – links, folder contents, tabs and notes – and opens it with Enter.",
            "Findet alles in deinen Fences – Verknüpfungen, Ordnerinhalte, Reiter und Notizen – und öffnet es mit Enter.",
            "Trova tutto nei tuoi recinti – collegamenti, contenuto delle cartelle, schede e note – e lo apre con Invio.",
            "Trouve tout dans vos barrières – raccourcis, contenu des dossiers, onglets et notes – et l'ouvre avec Entrée.",
            "Encuentra todo en tus vallas – accesos directos, contenido de carpetas, pestañas y notas – y lo abre con Intro.");
        public static string SearchPlaceholder => T("Search in all fences…", "In allen Fences suchen…", "Cerca in tutti i recinti…", "Rechercher dans toutes les barrières…", "Buscar en todas las vallas…");
        public static string SearchFooter(int n) => T($"{n} items in your fences", $"{n} Einträge in deinen Fences", $"{n} elementi nei tuoi recinti", $"{n} éléments dans vos barrières", $"{n} elementos en tus vallas");
        public static string SearchNothing => T("Nothing found", "Nichts gefunden", "Nessun risultato", "Aucun résultat", "No se encontró nada");
        public static string SearchKeys => T("Enter opens · ↑↓ choose · Esc closes", "Enter öffnet · ↑↓ auswählen · Esc schließt", "Invio apre · ↑↓ scegli · Esc chiude", "Entrée ouvre · ↑↓ choisir · Échap ferme", "Intro abre · ↑↓ elegir · Esc cierra");
        public static string SearchInFence(string fence) => T($"in {fence}", $"in {fence}", $"in {fence}", $"dans {fence}", $"en {fence}");
        public static string SearchInNote(string fence) => T($"Note: {fence}", $"Notiz: {fence}", $"Nota: {fence}", $"Note : {fence}", $"Nota: {fence}");

        #endregion

        #region About

        public static string AboutTagline => T("Free desktop fences, folder fences, sticky notes and widgets for Windows.",
                                               "Kostenlose Desktop-Fences, Ordner-Fences, Notizen und Widgets für Windows.",
                                               "Recinti per il desktop, recinti cartella, note e widget gratuiti per Windows.");
        public static string AboutSource => T("Source code and downloads on GitHub", "Quellcode und Downloads auf GitHub", "Codice sorgente e download su GitHub");
        public static string AboutCredits => T("Based on NoFences by Twometer and contributors — thank you!",
                                               "Basiert auf NoFences von Twometer und Mitwirkenden – danke!",
                                               "Basato su NoFences di Twometer e collaboratori – grazie!");
        public static string Donate => T("Donate (PayPal)", "Spenden (PayPal)", "Dona (PayPal)");
        public static string DonateHint => T("NoFences is free. If you like it, a small donation helps keep it going – thank you!",
                                             "NoFences ist kostenlos. Wenn es dir gefällt, hilft eine kleine Spende beim Weitermachen – danke!",
                                             "NoFences è gratuito. Se ti piace, una piccola donazione aiuta a portarlo avanti – grazie!");
        public static string AboutLicense => T("Open source under the MIT license.", "Open Source unter der MIT-Lizenz.", "Open source con licenza MIT.");

        #endregion
    }
}
