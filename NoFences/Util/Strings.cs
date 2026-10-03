using System.Globalization;

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
        public static string AppSettings => T("Settings…", "Einstellungen…", "Impostazioni…");
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
        public static string OnlyThisDesktop => T("Only on this virtual desktop", "Nur auf diesem virtuellen Desktop", "Solo su questo desktop virtuale");
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
        public static string PlaytimeGame => T("Game", "Spiel", "Gioco");
        public static string PlaytimeLastPlayed => T("Most recently played", "Zuletzt gespieltes Spiel", "Giocato più di recente");
        public static string PlaytimeNoSessions => T("No playtime recorded yet.", "Noch keine Spielzeit aufgezeichnet.", "Nessun tempo di gioco registrato.");
        public static string PlaytimeToday => T("today", "heute", "oggi");
        public static string PlaytimeWeek => T("This week", "Diese Woche", "Questa settimana");
        public static string PlaytimeMonth => T("This month", "Diesen Monat", "Questo mese");
        public static string PlaytimeTotal => T("Total", "Gesamt", "Totale");
        public static string PlaytimeMissing => T("No playtime data found. This widget shows the playtime of your games as recorded by the free tool SC Playtime (works for any game).",
                                                  "Keine Spielzeit-Daten gefunden. Das Widget zeigt die Spielzeit deiner Spiele, die das kostenlose Tool SC Playtime aufzeichnet (für beliebige Spiele).",
                                                  "Nessun dato sul tempo di gioco. Questo widget mostra il tempo di gioco registrato dallo strumento gratuito SC Playtime (per qualsiasi gioco).");
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

        #region About

        public static string AboutTagline => T("Free desktop fences, folder fences, sticky notes and widgets for Windows.",
                                               "Kostenlose Desktop-Fences, Ordner-Fences, Notizen und Widgets für Windows.",
                                               "Recinti per il desktop, recinti cartella, note e widget gratuiti per Windows.");
        public static string AboutSource => T("Source code and downloads on GitHub", "Quellcode und Downloads auf GitHub", "Codice sorgente e download su GitHub");
        public static string AboutCredits => T("Based on NoFences by Twometer and contributors — thank you!",
                                               "Basiert auf NoFences von Twometer und Mitwirkenden – danke!",
                                               "Basato su NoFences di Twometer e collaboratori – grazie!");
        public static string AboutLicense => T("Open source under the MIT license.", "Open Source unter der MIT-Lizenz.", "Open source con licenza MIT.");

        #endregion
    }
}
