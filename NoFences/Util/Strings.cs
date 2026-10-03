using System.Globalization;
using NoFences.Model;

namespace NoFences.Util
{
    /// <summary>
    /// UI texts in English, German, Italian, French and Spanish. The language follows Windows ("auto")
    /// unless chosen in the settings; any other Windows language falls back to English.
    /// </summary>
    public static class Strings
    {
        public static readonly IReadOnlyList<string> Languages = new[] { "auto", "en", "de", "it", "fr", "es" };

        private static readonly string[] Supported = { "en", "de", "it", "fr", "es" };

        /// <summary>"auto" or one of the supported language codes (from the settings).</summary>
        public static string Language { get; set; } = "auto";

        public static string Effective => Supported.Contains(Language)
            ? Language
            : Supported.Contains(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName) ? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName : "en";

        private static string T(string en, string de, string it, string fr, string es) =>
            Effective switch { "de" => de, "it" => it, "fr" => fr, "es" => es, _ => en };

        public static string LanguageName(string code) => code switch
        {
            "en" => "English",
            "de" => "Deutsch",
            "it" => "Italiano",
            "fr" => "Français",
            "es" => "Español",
            _ => T("Automatic (Windows language)", "Automatisch (Windows-Sprache)", "Automatica (lingua di Windows)", "Automatique (langue de Windows)", "Automático (idioma de Windows)")
        };

        // Documents shown in the app
        public static string HelpDocument => Effective switch { "de" => "HILFE.md", "it" => "AIUTO.md", "fr" => "AIDE.md", "es" => "AYUDA.md", _ => "HELP.md" };
        public static string ChangelogDocument => Effective switch
        {
            "de" => "CHANGELOG.de.md", "it" => "CHANGELOG.it.md", "fr" => "CHANGELOG.fr.md", "es" => "CHANGELOG.es.md", _ => "CHANGELOG.md"
        };

        #region Style names

        public static string ThemeName(string id) => id switch
        {
            "default" => T("Standard (glass)", "Standard (Glas)", "Standard (vetro)", "Standard (verre)", "Estándar (cristal)"),
            "windows" => T("Windows accent color", "Windows-Akzentfarbe", "Colore d'accento di Windows", "Couleur d'accentuation Windows", "Color de énfasis de Windows"),
            "starcitizen" => "Star Citizen (HUD)",
            "retroarcade" => "Retro-Arcade",
            "hardware" => T("Hardware (circuit board)", "Hardware (Platine)", "Hardware (circuito)", "Matériel (circuit imprimé)", "Hardware (placa de circuito)"),
            "nerd" => T("Nerd (terminal)", "Nerd (Terminal)", "Nerd (terminale)", "Geek (terminal)", "Friki (terminal)"),
            "hobby" => T("Hobby (pinboard)", "Hobby (Pinnwand)", "Hobby (bacheca)", "Loisirs (tableau en liège)", "Aficiones (tablón de corcho)"),
            "work" => T("Work (business)", "Arbeit (Business)", "Lavoro (business)", "Travail (business)", "Trabajo (negocios)"),
            "family" => T("Family", "Familie", "Famiglia", "Famille", "Familia"),
            "gaming" => "Gaming (RGB)",
            "finance" => T("Finance (trading desk)", "Finanzen (Börse)", "Finanza (borsa)", "Finance (salle de marché)", "Finanzas (bolsa)"),
            "social" => T("Social", "Social", "Social", "Réseaux sociaux", "Redes sociales"),
            "documents" => T("Documents", "Dokumente", "Documenti", "Documents", "Documentos"),
            "multimedia" => T("Multimedia", "Multimedia", "Multimedia", "Multimédia", "Multimedia"),
            "music" => T("Music", "Musik", "Musica", "Musique", "Música"),
            "sport" => T("Sport", "Sport", "Sport", "Sport", "Deporte"),
            "photos" => T("Photos", "Fotos", "Foto", "Photos", "Fotos"),
            "travel" => T("Travel", "Reisen", "Viaggi", "Voyages", "Viajes"),
            "cooking" => T("Cooking", "Kochen", "Cucina", "Cuisine", "Cocina"),
            "nature" => T("Nature", "Natur", "Natura", "Nature", "Naturaleza"),
            "postit" => T("Post-it (yellow)", "Post-it (Gelb)", "Post-it (giallo)", "Post-it (jaune)", "Pósit (amarillo)"),
            "postit-pink" => T("Post-it (pink)", "Post-it (Rosa)", "Post-it (rosa)", "Post-it (rose)", "Pósit (rosa)"),
            "postit-green" => T("Post-it (green)", "Post-it (Grün)", "Post-it (verde)", "Post-it (vert)", "Pósit (verde)"),
            "postit-blue" => T("Post-it (blue)", "Post-it (Blau)", "Post-it (blu)", "Post-it (bleu)", "Pósit (azul)"),
            "postit-orange" => T("Post-it (orange)", "Post-it (Orange)", "Post-it (arancione)", "Post-it (orange)", "Pósit (naranja)"),
            _ => id
        };

        #endregion

        #region Menus and fences

        public static string Help => T("Help", "Hilfe", "Guida", "Aide", "Ayuda");
        public static string WhatsNew => T("What's new?", "Was ist neu?", "Novità", "Nouveautés", "Novedades");
        public static string About => T("About NoFences", "Über NoFences", "Informazioni su NoFences", "À propos de NoFences", "Acerca de NoFences");
        public static string BackupLabel => T("Backup:", "Sicherung:", "Backup:", "Sauvegarde :", "Copia:");
        public static string RestoreShort => T("Restore", "Wiederherstellen", "Ripristina", "Restaurer", "Restaurar");
        public static string LanguageMenu => "Language · Sprache · Lingua · Langue · Idioma";
        public static string AppSettings => T("Settings…", "Einstellungen…", "Impostazioni…", "Paramètres…", "Configuración…");
        public static string NewFence => T("New fence", "Neuer Fence", "Nuovo recinto", "Nouvelle barrière", "Nueva valla");
        public static string NewFolderFence => T("New folder fence…", "Neuer Ordner-Fence…", "Nuovo recinto cartella…", "Nouvelle barrière de dossier…", "Nueva valla de carpeta…");
        public static string FirstFence => T("First fence", "Erster Fence", "Primo recinto", "Première barrière", "Primera valla");
        public static string ChooseFolder => T("Choose the folder this fence should show", "Ordner wählen, den dieser Fence anzeigen soll", "Scegli la cartella da mostrare in questo recinto", "Choisissez le dossier à afficher dans cette barrière", "Elige la carpeta que mostrará esta valla");
        public static string Settings => T("Fence settings…", "Fence-Einstellungen…", "Impostazioni recinto…", "Paramètres de la barrière…", "Configuración de la valla…");
        public static string Locked => T("Locked", "Gesperrt", "Bloccato", "Verrouillée", "Bloqueada");
        public static string AutoCollapse => T("Collapse when not hovered", "Einklappen wenn Maus weg", "Comprimi quando il mouse è fuori", "Replier quand la souris s'éloigne", "Contraer cuando el ratón se aleja");
        public static string RemoveItem => T("Remove from fence", "Aus Fence entfernen", "Rimuovi dal recinto", "Retirer de la barrière", "Quitar de la valla");
        public static string OpenFolder => T("Open folder in Explorer", "Ordner im Explorer öffnen", "Apri cartella in Esplora file", "Ouvrir le dossier dans l'Explorateur", "Abrir carpeta en el Explorador");
        public static string DeleteFence => T("Delete fence", "Fence löschen", "Elimina recinto", "Supprimer la barrière", "Eliminar valla");
        public static string ReallyDelete(string name) => T($"Really delete the fence \"{name}\"?", $"Fence \"{name}\" wirklich löschen?", $"Eliminare davvero il recinto \"{name}\"?", $"Supprimer vraiment la barrière « {name} » ?", $"¿Eliminar de verdad la valla «{name}»?");
        public static string ReallyDeleteFolderNote => T("The folder and its files are not touched.", "Der Ordner und seine Dateien bleiben unangetastet.", "La cartella e i suoi file non vengono toccati.", "Le dossier et ses fichiers ne sont pas touchés.", "La carpeta y sus archivos no se tocan.");
        public static string ShowFences => T("Show fences", "Fences anzeigen", "Mostra recinti", "Afficher les barrières", "Mostrar vallas");
        public static string Autostart => T("Start with Windows", "Mit Windows starten", "Avvia con Windows", "Démarrer avec Windows", "Iniciar con Windows");
        public static string ShowExtensions => T("Show file extensions", "Dateiendungen anzeigen", "Mostra estensioni dei file", "Afficher les extensions", "Mostrar extensiones de archivo");
        public static string ExtFollowExplorer => T("Like Explorer", "Wie im Explorer", "Come in Esplora file", "Comme l'Explorateur", "Como el Explorador");
        public static string ExtAlways => T("Always", "Immer", "Sempre", "Toujours", "Siempre");
        public static string ExtNever => T("Never", "Nie", "Mai", "Jamais", "Nunca");
        public static string Theme => T("Style", "Style", "Stile", "Style", "Estilo");
        public static string ThemeGlobal => T("Default style", "Standard-Style", "Stile predefinito", "Style par défaut", "Estilo predeterminado");
        public static string ThemeInherit => T("(use default style)", "(Standard-Style verwenden)", "(usa lo stile predefinito)", "(utiliser le style par défaut)", "(usar el estilo predeterminado)");
        public static string OpenDataFolder => T("Open config folder", "Konfigurationsordner öffnen", "Apri cartella di configurazione", "Ouvrir le dossier de configuration", "Abrir carpeta de configuración");
        public static string Exit => T("Exit", "Beenden", "Esci", "Quitter", "Salir");
        public static string Rename => T("Rename", "Umbenennen", "Rinomina", "Renommer", "Cambiar nombre");
        public static string NewName => T("New name:", "Neuer Name:", "Nuovo nome:", "Nouveau nom :", "Nuevo nombre:");
        public static string RenameFailed(string reason) => T($"Could not rename: {reason}", $"Umbenennen nicht möglich: {reason}", $"Impossibile rinominare: {reason}", $"Impossible de renommer : {reason}", $"No se pudo cambiar el nombre: {reason}");
        public static string Search => T("Search", "Suche", "Cerca", "Rechercher", "Buscar");
        public static string AlwaysOnTop => T("Always on top", "Immer im Vordergrund", "Sempre in primo piano", "Toujours au premier plan", "Siempre visible");
        public static string ProfileAll => T("All fences", "Alle Fences", "Tutti i recinti", "Toutes les barrières", "Todas las vallas");
        public static string ProfileMenu(string active) => T($"Profile: {active}", $"Profil: {active}", $"Profilo: {active}", $"Profil : {active}", $"Perfil: {active}");
        public static string ProfileNew => T("New profile…", "Neues Profil…", "Nuovo profilo…", "Nouveau profil…", "Nuevo perfil…");
        public static string ProfileDelete => T("Delete profile", "Profil löschen", "Elimina profilo", "Supprimer le profil", "Eliminar perfil");
        public static string ProfileNamePrompt => T("Name of the profile (e.g. Work, Gaming):", "Name des Profils (z. B. Arbeit, Gaming):", "Nome del profilo (es. Lavoro, Gaming):", "Nom du profil (par ex. Travail, Jeux) :", "Nombre del perfil (p. ej. Trabajo, Juegos):");
        public static string ProfileDeleteConfirm(string name) => T(
            $"Delete the profile \"{name}\"? The fences stay; they just no longer belong to it.",
            $"Profil „{name}“ löschen? Die Fences bleiben erhalten, sie gehören nur nicht mehr dazu.",
            $"Eliminare il profilo «{name}»? I recinti restano, semplicemente non ne fanno più parte.",
            $"Supprimer le profil « {name} » ? Les barrières restent, elles n'en font simplement plus partie.",
            $"¿Eliminar el perfil «{name}»? Las vallas se quedan; simplemente dejan de pertenecer a él.");
        public static string ProfileSwitched(string name) => T($"Profile: {name}", $"Profil: {name}", $"Profilo: {name}", $"Profil : {name}", $"Perfil: {name}");
        public static string ProfileHowTo => T("Assign fences: right-click a fence → Show in profile", "Fences zuordnen: Rechtsklick auf ein Fence → In Profil zeigen", "Assegna recinti: clic destro su un recinto → Mostra nel profilo", "Attribuer des barrières : clic droit sur une barrière → Afficher dans le profil", "Asignar vallas: clic derecho en una valla → Mostrar en el perfil");
        public static string ProfileFenceMenu => T("Show in profile", "In Profil zeigen", "Mostra nel profilo", "Afficher dans le profil", "Mostrar en el perfil");
        public static string ProfileFenceHint => T("No check = in every profile", "Ohne Haken = in allen Profilen", "Nessuna spunta = in tutti i profili", "Aucune coche = dans tous les profils", "Sin marca = en todos los perfiles");
        public static string RulerTitle => T("Ruler", "Lineal", "Righello", "Règle", "Regla");
        public static string RulerMenu => T("Screen ruler", "Bildschirm-Lineal", "Righello sullo schermo", "Règle à l'écran", "Regla en pantalla");
        public static string RulerTurn => T("Turn (space)", "Drehen (Leertaste)", "Ruota (spazio)", "Tourner (espace)", "Girar (espacio)");
        public static string RulerHelp => T("Drag to move, drag the end to resize, arrows nudge, Esc closes",
                                            "Ziehen verschiebt, Ende ziehen ändert die Länge, Pfeiltasten feinjustieren, Esc schließt",
                                            "Trascina per spostare, trascina l'estremità per allungare, frecce per regolare, Esc chiude",
                                            "Glisser pour déplacer, glisser l'extrémité pour allonger, flèches pour ajuster, Échap ferme",
                                            "Arrastra para mover, arrastra el extremo para alargar, flechas para ajustar, Esc cierra");
        public static string RulerUnitName(RulerWindow.Unit unit) => unit switch
        {
            RulerWindow.Unit.Centimeters => T("Centimetres", "Zentimeter", "Centimetri", "Centimètres", "Centímetros"),
            RulerWindow.Unit.Inches => T("Inches", "Zoll", "Pollici", "Pouces", "Pulgadas"),
            _ => T("Pixels", "Pixel", "Pixel", "Pixels", "Píxeles")
        };
        public static string AssistantMenu =>T("Desktop assistant…", "Desktop-Assistent…", "Assistente desktop…", "Assistant de bureau…", "Asistente de escritorio…");
        public static string AssistantTitle => T("Desktop assistant", "Desktop-Assistent", "Assistente desktop", "Assistant de bureau", "Asistente de escritorio");
        public static string AssistantIntro => T(
            "NoFences found these things on your desktop. Which should get their own fence?",
            "NoFences hat das auf deinem Desktop gefunden. Was soll einen eigenen Fence bekommen?",
            "NoFences ha trovato queste cose sul desktop. Cosa deve avere un recinto proprio?",
            "NoFences a trouvé ceci sur votre bureau. Qu'est-ce qui doit avoir sa propre barrière ?",
            "NoFences encontró esto en tu escritorio. ¿Qué debe tener su propia valla?");
        public static string AssistantNote => T(
            "Nothing is moved: the fences link to the files. To hide the originals, right-click the desktop → View → Show desktop icons.",
            "Es wird nichts verschoben: Die Fences verweisen auf die Dateien. Um die Originale auszublenden: Rechtsklick auf den Desktop → Ansicht → Desktopsymbole anzeigen.",
            "Non viene spostato nulla: i recinti collegano i file. Per nascondere gli originali: clic destro sul desktop → Visualizza → Mostra icone del desktop.",
            "Rien n'est déplacé : les barrières pointent vers les fichiers. Pour masquer les originaux : clic droit sur le bureau → Affichage → Afficher les icônes du bureau.",
            "No se mueve nada: las vallas enlazan a los archivos. Para ocultar los originales: clic derecho en el escritorio → Ver → Mostrar iconos del escritorio.");
        public static string AssistantCreate => T("Create fences", "Fences anlegen", "Crea recinti", "Créer les barrières", "Crear vallas");
        public static string AssistantNothing => T("Your desktop is already tidy – everything is in fences.", "Dein Desktop ist schon aufgeräumt – alles liegt in Fences.", "Il desktop è già in ordine – tutto è nei recinti.", "Votre bureau est déjà rangé – tout est dans des barrières.", "Tu escritorio ya está ordenado: todo está en vallas.");
        public static string AssistantDone(int n) => T($"{n} new fences created.", $"{n} neue Fences angelegt.", $"{n} nuovi recinti creati.", $"{n} nouvelles barrières créées.", $"{n} vallas nuevas creadas.");
        public static string AssistantFirstStart => T(
            "There are icons on your desktop. Shall NoFences sort them into fences (games, programs, documents …)?",
            "Auf deinem Desktop liegen Symbole. Soll NoFences sie in Fences einsortieren (Spiele, Programme, Dokumente …)?",
            "Sul desktop ci sono delle icone. NoFences deve ordinarle in recinti (giochi, programmi, documenti …)?",
            "Il y a des icônes sur votre bureau. NoFences doit-il les ranger dans des barrières (jeux, programmes, documents…) ?",
            "Hay iconos en tu escritorio. ¿Quieres que NoFences los ordene en vallas (juegos, programas, documentos…)?");
        public static string CategoryName(DesktopCategory category) => category switch
        {
            DesktopCategory.Games => T("Games", "Spiele", "Giochi", "Jeux", "Juegos"),
            DesktopCategory.Programs => T("Programs", "Programme", "Programmi", "Programmes", "Programas"),
            DesktopCategory.Documents => T("Documents", "Dokumente", "Documenti", "Documents", "Documentos"),
            DesktopCategory.Images => T("Pictures", "Bilder", "Immagini", "Images", "Imágenes"),
            DesktopCategory.Media => T("Music & videos", "Musik & Videos", "Musica e video", "Musique et vidéos", "Música y vídeos"),
            DesktopCategory.Archives => T("Archives", "Archive", "Archivi", "Archives", "Archivos comprimidos"),
            DesktopCategory.Folders => T("Folders", "Ordner", "Cartelle", "Dossiers", "Carpetas"),
            _ => T("Other", "Sonstiges", "Altro", "Divers", "Otros")
        };
        public static string WallpaperChoose(string profile) => T($"Wallpaper for \"{profile}\"", $"Hintergrundbild für „{profile}“", $"Sfondo per «{profile}»", $"Fond d'écran pour « {profile} »", $"Fondo de pantalla para «{profile}»");
        public static string WallpaperRemove => T("Remove the profile's wallpaper", "Hintergrundbild des Profils entfernen", "Rimuovi lo sfondo del profilo", "Retirer le fond d'écran du profil", "Quitar el fondo del perfil");
        public static string WallpaperFilter => T("Pictures|*.jpg;*.jpeg;*.png;*.bmp", "Bilder|*.jpg;*.jpeg;*.png;*.bmp", "Immagini|*.jpg;*.jpeg;*.png;*.bmp", "Images|*.jpg;*.jpeg;*.png;*.bmp", "Imágenes|*.jpg;*.jpeg;*.png;*.bmp");
        public static string ProfileHotkeysLabel => T("Switch profiles with Ctrl+Alt+F1…F9 (F10: all fences)", "Profile mit Strg+Alt+F1…F9 wechseln (F10: alle Fences)", "Cambia profilo con Ctrl+Alt+F1…F9 (F10: tutti i recinti)", "Changer de profil avec Ctrl+Alt+F1…F9 (F10 : toutes les barrières)", "Cambiar de perfil con Ctrl+Alt+F1…F9 (F10: todas las vallas)");
        public static string ProfileWallpaperHint => T("Wallpaper per profile: tray → Profile ▸ while the profile is active.", "Hintergrundbild pro Profil: Tray → Profil ▸, während das Profil aktiv ist.", "Sfondo per profilo: barra → Profilo ▸ mentre il profilo è attivo.", "Fond d'écran par profil : zone de notification → Profil ▸ pendant que le profil est actif.", "Fondo por perfil: bandeja → Perfil ▸ mientras el perfil está activo.");
        public static string ProfileLabel =>T("Active profile", "Aktives Profil", "Profilo attivo", "Profil actif", "Perfil activo");
        public static string SectionProfiles => T("Profiles", "Profile", "Profili", "Profils", "Perfiles");
        public static string OnlyThisDesktop => T("Only on this virtual desktop", "Nur auf diesem virtuellen Desktop", "Solo su questo desktop virtuale", "Seulement sur ce bureau virtuel", "Solo en este escritorio virtual");
        public static string DropHint => T("Drop files or folders here", "Dateien oder Ordner hierher ziehen", "Trascina qui file o cartelle", "Déposez des fichiers ou dossiers ici", "Arrastra archivos o carpetas aquí");
        public static string FolderMissing(string path) => T($"Folder not found:\n{path}", $"Ordner nicht gefunden:\n{path}", $"Cartella non trovata:\n{path}", $"Dossier introuvable :\n{path}", $"Carpeta no encontrada:\n{path}");
        public static string FirstStartHint => T("Drag files onto the fence. Right-click a fence for options; the tray icon has the settings and help.",
                                                 "Zieh Dateien auf den Fence. Rechtsklick auf einen Fence zeigt die Optionen, im Tray-Icon gibt es Einstellungen und Hilfe.",
                                                 "Trascina i file nel recinto. Clic destro su un recinto per le opzioni; l'icona nella barra ha impostazioni e guida.",
                                                 "Faites glisser des fichiers sur la barrière. Clic droit sur une barrière pour les options ; l'icône de la zone de notification contient les paramètres et l'aide.",
                                                 "Arrastra archivos a la valla. Clic derecho en una valla para ver las opciones; el icono de la bandeja tiene la configuración y la ayuda.");

        public static string SortBy => T("Sort by", "Sortieren nach", "Ordina per", "Trier par", "Ordenar por");
        public static string SortModeName(Model.FenceSortMode mode) => mode switch
        {
            Model.FenceSortMode.Name => T("Name", "Name", "Nome", "Nom", "Nombre"),
            Model.FenceSortMode.Type => T("Type", "Typ", "Tipo", "Type", "Tipo"),
            Model.FenceSortMode.Modified => T("Date modified (newest first)", "Änderungsdatum (neueste zuerst)", "Data di modifica (più recenti prima)", "Date de modification (plus récent d'abord)", "Fecha de modificación (más recientes primero)"),
            Model.FenceSortMode.Size => T("Size (largest first)", "Größe (größte zuerst)", "Dimensione (più grandi prima)", "Taille (plus grand d'abord)", "Tamaño (más grandes primero)"),
            _ => T("Manual (drag & drop)", "Manuell (Drag & Drop)", "Manuale (trascina e rilascia)", "Manuel (glisser-déposer)", "Manual (arrastrar y soltar)")
        };

        public static string AddTab => T("Add tab", "Reiter hinzufügen", "Aggiungi scheda", "Ajouter un onglet", "Añadir pestaña");
        public static string RenameTab => T("Rename tab", "Reiter umbenennen", "Rinomina scheda", "Renommer l'onglet", "Cambiar nombre de la pestaña");
        public static string RemoveTab => T("Remove tab (keeps its links)", "Reiter entfernen (Verknüpfungen bleiben)", "Rimuovi scheda (i collegamenti restano)", "Supprimer l'onglet (les raccourcis restent)", "Quitar pestaña (los accesos directos se conservan)");
        public static string TabDefaultName(int n) => T($"Tab {n}", $"Reiter {n}", $"Scheda {n}", $"Onglet {n}", $"Pestaña {n}");

        public static string WidgetGroupName(Widgets.WidgetRegistry.Group group) => group switch
        {
            Widgets.WidgetRegistry.Group.Time => T("Time & planning", "Zeit & Planung", "Tempo e pianificazione", "Temps et planning", "Tiempo y planificación"),
            Widgets.WidgetRegistry.Group.Info => T("Info & news", "Info & News", "Info e notizie", "Infos et actualités", "Información y noticias"),
            Widgets.WidgetRegistry.Group.System => T("System", "System", "Sistema", "Système", "Sistema"),
            _ => T("Games & media", "Spiele & Medien", "Giochi e media", "Jeux et médias", "Juegos y multimedia")
        };
        public static string ThemeGroupName(Themes.ThemeRegistry.Group group) => group switch
        {
            Themes.ThemeRegistry.Group.Basic => T("Basic", "Basis", "Base", "De base", "Básicos"),
            Themes.ThemeRegistry.Group.GamingTech => T("Gaming & tech", "Gaming & Technik", "Gaming e tecnologia", "Jeux et technique", "Juegos y tecnología"),
            Themes.ThemeRegistry.Group.WorkLife => T("Work & everyday", "Arbeit & Alltag", "Lavoro e quotidiano", "Travail et quotidien", "Trabajo y día a día"),
            Themes.ThemeRegistry.Group.Leisure => T("Leisure", "Freizeit", "Tempo libero", "Loisirs", "Ocio"),
            Themes.ThemeRegistry.Group.PostIt => "Post-it",
            _ => T("Own styles", "Eigene Styles", "Stili personali", "Styles personnels", "Estilos propios")
        };
        public static string NewWidget =>T("New widget", "Neues Widget", "Nuovo widget", "Nouveau widget", "Nuevo widget");
        public static string NewRecent => T("New \"Recent files\" fence", "Neuer Fence „Zuletzt verwendet“", "Nuovo recinto \"File recenti\"", "Nouvelle barrière « Fichiers récents »", "Nueva valla «Archivos recientes»");
        public static string RecentName => T("Recent files", "Zuletzt verwendet", "File recenti", "Fichiers récents", "Archivos recientes");
        public static string NewQuickLaunch => T("New quick-launch bar", "Neue Schnellstart-Leiste", "Nuova barra di avvio rapido", "Nouvelle barre de lancement rapide", "Nueva barra de inicio rápido");
        public static string QuickLaunchName => T("Quick launch", "Schnellstart", "Avvio rapido", "Lancement rapide", "Inicio rápido");
        public static string CompactMode => T("Icons only (compact)", "Nur Icons (kompakt)", "Solo icone (compatto)", "Icônes seules (compact)", "Solo iconos (compacto)");
        public static string NewNote => T("New note", "Neue Notiz", "Nuova nota", "Nouvelle note", "Nueva nota");
        public static string NoteName => T("Note", "Notiz", "Nota", "Note", "Nota");
        public static string EditNote => T("Edit note", "Notiz bearbeiten", "Modifica nota", "Modifier la note", "Editar nota");
        public static string NoteHint => T("Double-click to write.\nLines starting with [ ] become checkboxes.",
                                           "Doppelklick zum Schreiben.\nZeilen mit [ ] am Anfang werden zu Kästchen.",
                                           "Doppio clic per scrivere.\nLe righe che iniziano con [ ] diventano caselle.",
                                           "Double-cliquez pour écrire.\nLes lignes commençant par [ ] deviennent des cases à cocher.",
                                           "Haz doble clic para escribir.\nLas líneas que empiezan con [ ] se convierten en casillas.");

        #endregion

        #region Settings dialogs

        public static string Name => T("Name", "Name", "Nome", "Nom", "Nombre");
        public static string Folder => T("Folder", "Ordner", "Cartella", "Dossier", "Carpeta");
        public static string Browse => T("Browse…", "Durchsuchen…", "Sfoglia…", "Parcourir…", "Examinar…");
        public static string TitleHeight => T("Title height", "Titelhöhe", "Altezza titolo", "Hauteur du titre", "Altura del título");
        public static string IconSize => T("Icon size", "Icongröße", "Dimensione icone", "Taille des icônes", "Tamaño de iconos");
        public static string Background => T("Background", "Hintergrund", "Sfondo", "Arrière-plan", "Fondo");
        public static string Opacity => T("Opacity", "Deckkraft", "Opacità", "Opacité", "Opacidad");
        public static string Ok => T("OK", "OK", "OK", "OK", "Aceptar");
        public static string Cancel => T("Cancel", "Abbrechen", "Annulla", "Annuler", "Cancelar");
        public static string Close => T("Close", "Schließen", "Chiudi", "Fermer", "Cerrar");
        public static string Kind => T("Type", "Typ", "Tipo", "Type", "Tipo");
        public static string KindLinks => T("Links (files stay where they are)", "Verknüpfungen (Dateien bleiben wo sie sind)", "Collegamenti (i file restano dove sono)", "Raccourcis (les fichiers restent où ils sont)", "Accesos directos (los archivos se quedan donde están)");
        public static string KindFolder => T("Folder (shows a folder's contents)", "Ordner (zeigt den Inhalt eines Ordners)", "Cartella (mostra il contenuto di una cartella)", "Dossier (affiche le contenu d'un dossier)", "Carpeta (muestra el contenido de una carpeta)");
        public static string KindNote => T("Note (sticky note with text)", "Notiz (Post-it mit Text)", "Nota (post-it con testo)", "Note (post-it avec du texte)", "Nota (pósit con texto)");
        public static string KindWidget => T("Widget", "Widget", "Widget", "Widget", "Widget");
        public static string Preview => T("Preview", "Vorschau", "Anteprima", "Aperçu", "Vista previa");
        public static string SectionGeneral => T("General", "Allgemein", "Generale", "Général", "General");
        public static string SectionAppearance => T("Appearance", "Aussehen", "Aspetto", "Apparence", "Apariencia");
        public static string SectionBehavior => T("Behavior", "Verhalten", "Comportamento", "Comportement", "Comportamiento");
        public static string SectionAutoSort => T("Auto-sort from the desktop", "Vom Desktop einsortieren", "Ordina dal desktop", "Ranger depuis le bureau", "Ordenar desde el escritorio");
        public static string SectionDesktop => T("Desktop", "Desktop", "Desktop", "Bureau", "Escritorio");
        public static string SectionUpdates => T("Updates", "Updates", "Aggiornamenti", "Mises à jour", "Actualizaciones");
        public static string SectionFps => T("FPS measurement", "FPS-Messung", "Misurazione FPS", "Mesure des FPS", "Medición de FPS");
        public static string SectionData => T("Data & styles", "Daten & Styles", "Dati e stili", "Données et styles", "Datos y estilos");
        public static string SettingsTitle => T("NoFences settings", "NoFences-Einstellungen", "Impostazioni di NoFences", "Paramètres de NoFences", "Configuración de NoFences");
        public static string LanguageLabel => T("Language", "Sprache", "Lingua", "Langue", "Idioma");
        public static string VersionLabel(Version v) => T($"Installed version: {v}", $"Installierte Version: {v}", $"Versione installata: {v}", $"Version installée : {v}", $"Versión instalada: {v}");
        public static string FpsShortHint => T("Needs a small helper with administrator rights (Windows asks once). It only counts frames – no screen content, no input.",
                                               "Braucht einen kleinen Helfer mit Administratorrechten (Windows fragt einmal). Er zählt nur Bilder – keine Bildinhalte, keine Eingaben.",
                                               "Richiede un piccolo programma di supporto con diritti di amministratore (Windows lo chiede una volta). Conta solo i fotogrammi, nessun contenuto e nessun input.",
                                               "Nécessite un petit assistant avec des droits d'administrateur (Windows le demande une fois). Il compte seulement les images – aucun contenu d'écran, aucune saisie.",
                                               "Necesita un pequeño asistente con permisos de administrador (Windows lo pide una vez). Solo cuenta fotogramas: nada del contenido de la pantalla ni de lo que escribes.");
        public static string FpsEnabledLabel => T("Measure FPS of games", "FPS von Spielen messen", "Misura gli FPS dei giochi", "Mesurer les FPS des jeux", "Medir los FPS de los juegos");

        public static string AutoSort => T("Patterns", "Muster", "Schemi", "Modèles", "Patrones");
        public static string AutoSortHint => T("New desktop files matching these patterns go into this fence, e.g. *.pdf; *.docx",
                                               "Neue Desktop-Dateien, die passen, landen in diesem Fence, z. B. *.pdf; *.docx",
                                               "I nuovi file sul desktop che corrispondono finiscono in questo recinto, ad es. *.pdf; *.docx",
                                               "Les nouveaux fichiers du bureau correspondant à ces modèles vont dans cette barrière, par ex. *.pdf; *.docx",
                                               "Los archivos nuevos del escritorio que coincidan van a esta valla, p. ej. *.pdf; *.docx");
        public static string AddPreset => T("Add preset…", "Vorlage hinzufügen…", "Aggiungi modello…", "Ajouter un modèle…", "Añadir plantilla…");
        public static string PresetImages => T("Images", "Bilder", "Immagini", "Images", "Imágenes");
        public static string PresetDocuments => T("Documents", "Dokumente", "Documenti", "Documents", "Documentos");
        public static string PresetArchives => T("Archives", "Archive", "Archivi", "Archives", "Archivos comprimidos");
        public static string PresetInstallers => T("Installers / programs", "Installer / Programme", "Installer / programmi", "Installateurs / programmes", "Instaladores / programas");
        public static string PresetVideos => T("Videos", "Videos", "Video", "Vidéos", "Vídeos");
        public static string PresetMusic => T("Music", "Musik", "Musica", "Musique", "Música");
        public static string PresetShortcuts => T("Shortcuts", "Verknüpfungen", "Collegamenti", "Raccourcis", "Accesos directos");

        #endregion

        #region Desktop, sorting, peek

        public static string AutoSortEnabled => T("Auto-sort new desktop files", "Neue Desktop-Dateien automatisch einsortieren", "Ordina automaticamente i nuovi file del desktop", "Ranger automatiquement les nouveaux fichiers du bureau", "Ordenar automáticamente los archivos nuevos del escritorio");
        public static string SortNow => T("Tidy up desktop now", "Desktop jetzt aufräumen", "Riordina il desktop ora", "Ranger le bureau maintenant", "Ordenar el escritorio ahora");
        public static string SortNowNoRules => T("No fence has auto-sort patterns yet.\nSet them in a fence's settings.",
                                                 "Noch kein Fence hat Einsortier-Regeln.\nDu legst sie in den Fence-Einstellungen fest.",
                                                 "Nessun recinto ha ancora regole di ordinamento.\nImpostale nelle impostazioni di un recinto.",
                                                 "Aucune barrière n'a encore de règles de rangement.\nDéfinissez-les dans les paramètres d'une barrière.",
                                                 "Ninguna valla tiene aún reglas de ordenación.\nDefínelas en la configuración de una valla.");
        public static string SortNowDone(int n) => n == 1
            ? T("1 file sorted into fences.", "1 Datei in Fences einsortiert.", "1 file ordinato nei recinti.", "1 fichier rangé dans les barrières.", "1 archivo ordenado en las vallas.")
            : T($"{n} files sorted into fences.", $"{n} Dateien in Fences einsortiert.", $"{n} file ordinati nei recinti.", $"{n} fichiers rangés dans les barrières.", $"{n} archivos ordenados en las vallas.");
        public static string DoubleClickToggle => T("Double-click desktop to hide fences", "Doppelklick auf Desktop blendet Fences aus", "Doppio clic sul desktop nasconde i recinti", "Double-clic sur le bureau pour masquer les barrières", "Doble clic en el escritorio para ocultar las vallas");
        public static string PeekMenu => T("Bring fences to front", "Fences nach vorne holen", "Porta i recinti in primo piano", "Afficher les barrières au premier plan", "Traer las vallas al frente");
        public static string PeekHotkey => T("Shortcut", "Tastenkürzel", "Scorciatoia", "Raccourci", "Atajo");
        public static string HotkeyName(string hotkey) => hotkey switch
        {
            "Off" => T("Off", "Aus", "Disattivata", "Désactivé", "Desactivado"),
            _ => Effective switch
            {
                "de" => hotkey.Replace("Ctrl", "Strg").Replace("Space", "Leertaste"),
                "it" => hotkey.Replace("Space", "Spazio").Replace("Shift", "Maiusc"),
                "fr" => hotkey.Replace("Space", "Espace").Replace("Shift", "Maj"),
                "es" => hotkey.Replace("Space", "Espacio").Replace("Shift", "Mayús"),
                _ => hotkey
            }
        };
        public static string HotkeyTaken(string hotkey) => T(
            $"The shortcut {HotkeyName(hotkey)} is already used by another program. Pick another one in the settings.",
            $"Das Tastenkürzel {HotkeyName(hotkey)} wird schon von einem anderen Programm verwendet. Wähle in den Einstellungen ein anderes.",
            $"La scorciatoia {HotkeyName(hotkey)} è già usata da un altro programma. Scegline un'altra nelle impostazioni.",
            $"Le raccourci {HotkeyName(hotkey)} est déjà utilisé par un autre programme. Choisissez-en un autre dans les paramètres.",
            $"El atajo {HotkeyName(hotkey)} ya lo usa otro programa. Elige otro en la configuración.");

        #endregion

        #region Updates, styles, backups, export

        public static string CheckForUpdatesAuto => T("Check for updates automatically", "Automatisch nach Updates suchen", "Cerca aggiornamenti automaticamente", "Rechercher les mises à jour automatiquement", "Buscar actualizaciones automáticamente");
        public static string CheckForUpdatesNow => T("Check for updates now", "Jetzt nach Updates suchen", "Cerca aggiornamenti ora", "Rechercher les mises à jour maintenant", "Buscar actualizaciones ahora");
        public static string InstallUpdate(Version v) => T($"Install update {v}", $"Update {v} installieren", $"Installa l'aggiornamento {v}", $"Installer la mise à jour {v}", $"Instalar la actualización {v}");
        public static string UpdateAvailable(Version v) => T($"NoFences {v} is available. Click here to install it.", $"NoFences {v} ist verfügbar. Hier klicken zum Installieren.", $"NoFences {v} è disponibile. Clicca qui per installarlo.", $"NoFences {v} est disponible. Cliquez ici pour l'installer.", $"NoFences {v} está disponible. Haz clic aquí para instalarlo.");
        public static string UpdateAvailableManual(Version v) => T($"NoFences {v} is available. Click here to open the download page.", $"NoFences {v} ist verfügbar. Hier klicken, um die Download-Seite zu öffnen.", $"NoFences {v} è disponibile. Clicca qui per aprire la pagina di download.", $"NoFences {v} est disponible. Cliquez ici pour ouvrir la page de téléchargement.", $"NoFences {v} está disponible. Haz clic aquí para abrir la página de descarga.");
        public static string UpToDate(Version v) => T($"You have the latest version ({v}).", $"Du hast die neueste Version ({v}).", $"Hai l'ultima versione ({v}).", $"Vous avez la dernière version ({v}).", $"Tienes la última versión ({v}).");
        public static string UpdateDownloading => T("Downloading update…", "Update wird heruntergeladen…", "Download dell'aggiornamento…", "Téléchargement de la mise à jour…", "Descargando la actualización…");
        public static string UpdateFailed(string reason) => T($"The update failed: {reason}\nThe download page will open instead.", $"Das Update ist fehlgeschlagen: {reason}\nStattdessen öffnet sich die Download-Seite.", $"L'aggiornamento non è riuscito: {reason}\nSi apre invece la pagina di download.", $"La mise à jour a échoué : {reason}\nLa page de téléchargement va s'ouvrir.", $"La actualización falló: {reason}\nSe abrirá la página de descarga.");
        public static string UpdateCheckFailed => T("Could not reach GitHub to check for updates.", "GitHub war für die Update-Prüfung nicht erreichbar.", "Impossibile raggiungere GitHub per cercare aggiornamenti.", "Impossible de joindre GitHub pour rechercher les mises à jour.", "No se pudo contactar con GitHub para buscar actualizaciones.");
        public static string Animations => T("Animations", "Animationen", "Animazioni", "Animations", "Animaciones");
        public static string CustomThemes => T("Own styles", "Eigene Styles", "Stili personali", "Styles personnels", "Estilos propios");
        public static string OpenThemesFolder => T("Open styles folder", "Styles-Ordner öffnen", "Apri cartella degli stili", "Ouvrir le dossier des styles", "Abrir carpeta de estilos");
        public static string ReloadThemes => T("Reload styles", "Styles neu laden", "Ricarica stili", "Recharger les styles", "Recargar estilos");
        public static string ThemesLoaded(int n) => T($"{n} own style(s) loaded.", $"{n} eigene(r) Style(s) geladen.", $"{n} stile/i personale/i caricato/i.", $"{n} style(s) personnel(s) chargé(s).", $"{n} estilo(s) propio(s) cargado(s).");
        public static string ThemeErrors(string details) => T($"Some styles could not be loaded:\n{details}", $"Einige Styles konnten nicht geladen werden:\n{details}", $"Alcuni stili non sono stati caricati:\n{details}", $"Certains styles n'ont pas pu être chargés :\n{details}", $"Algunos estilos no se pudieron cargar:\n{details}");
        public static string RestoreBackup => T("Restore backup", "Sicherung wiederherstellen", "Ripristina backup", "Restaurer une sauvegarde", "Restaurar copia de seguridad");
        public static string NoBackups => T("No backups yet", "Noch keine Sicherungen", "Ancora nessun backup", "Aucune sauvegarde pour l'instant", "Aún no hay copias de seguridad");
        public static string ConfirmRestore(DateTime time) => T(
            $"Restore all fences as they were on {time:g}?\nNoFences restarts; the current state is kept as a backup too.",
            $"Alle Fences auf den Stand vom {time:g} zurücksetzen?\nNoFences startet neu; der aktuelle Stand wird vorher ebenfalls gesichert.",
            $"Ripristinare tutti i recinti com'erano il {time:g}?\nNoFences si riavvia; anche lo stato attuale viene salvato come backup.",
            $"Restaurer toutes les barrières telles qu'elles étaient le {time:g} ?\nNoFences redémarre ; l'état actuel est aussi sauvegardé.",
            $"¿Restaurar todas las vallas como estaban el {time:g}?\nNoFences se reinicia; el estado actual también se guarda como copia.");
        public static string ExportFences => T("Export fences…", "Fences exportieren…", "Esporta recinti…", "Exporter les barrières…", "Exportar vallas…");
        public static string ImportFences => T("Import fences…", "Fences importieren…", "Importa recinti…", "Importer des barrières…", "Importar vallas…");
        public static string ExportFilter => T("NoFences export (*.nofences.json)|*.nofences.json|JSON (*.json)|*.json", "NoFences-Export (*.nofences.json)|*.nofences.json|JSON (*.json)|*.json", "Esportazione NoFences (*.nofences.json)|*.nofences.json|JSON (*.json)|*.json", "Export NoFences (*.nofences.json)|*.nofences.json|JSON (*.json)|*.json", "Exportación de NoFences (*.nofences.json)|*.nofences.json|JSON (*.json)|*.json");
        public static string ExportDone(int n) => T($"{n} fences exported.", $"{n} Fences exportiert.", $"{n} recinti esportati.", $"{n} barrières exportées.", $"{n} vallas exportadas.");
        public static string ImportDone(int n) => T($"{n} fences imported.", $"{n} Fences importiert.", $"{n} recinti importati.", $"{n} barrières importées.", $"{n} vallas importadas.");
        public static string ImportFailed(string reason) => T($"Import failed: {reason}", $"Import fehlgeschlagen: {reason}", $"Importazione non riuscita: {reason}", $"Échec de l'importation : {reason}", $"Error al importar: {reason}");

        #endregion

        #region Reminders

        public static string Reminder => T("Reminder…", "Erinnerung…", "Promemoria…", "Rappel…", "Recordatorio…");
        public static string ReminderTitle => T("Remind me", "Erinnern", "Ricordami", "Me rappeler", "Recordarme");
        public static string ReminderIn1h => T("In 1 hour", "In 1 Stunde", "Tra 1 ora", "Dans 1 heure", "En 1 hora");
        public static string ReminderTonight => T("Today 6 pm", "Heute 18:00", "Oggi alle 18:00", "Aujourd'hui 18 h", "Hoy a las 18:00");
        public static string ReminderTomorrow => T("Tomorrow 9 am", "Morgen 9:00", "Domani alle 9:00", "Demain 9 h", "Mañana a las 9:00");
        public static string ReminderRemove => T("Remove", "Entfernen", "Rimuovi", "Supprimer", "Quitar");
        public static string ReminderDue(string name) => T($"Reminder: {name}", $"Erinnerung: {name}", $"Promemoria: {name}", $"Rappel : {name}", $"Recordatorio: {name}");

        #endregion

        #region Widgets

        public static string WidgetClock => T("Clock & calendar", "Uhr & Kalender", "Orologio e calendario", "Horloge et calendrier", "Reloj y calendario");
        public static string WidgetSystem => T("System monitor (CPU, RAM, GPU, FPS)", "System-Monitor (CPU, RAM, GPU, FPS)", "Monitor di sistema (CPU, RAM, GPU, FPS)", "Moniteur système (CPU, RAM, GPU, FPS)", "Monitor del sistema (CPU, RAM, GPU, FPS)");
        public static string WidgetDrives => T("Drives", "Laufwerke", "Unità", "Lecteurs", "Unidades");
        public static string WidgetRecycleBin => T("Recycle bin", "Papierkorb", "Cestino", "Corbeille", "Papelera");
        public static string WidgetCountdown => T("Countdown", "Countdown", "Conto alla rovescia", "Compte à rebours", "Cuenta atrás");
        public static string CountdownSet => T("Set countdown…", "Countdown festlegen…", "Imposta conto alla rovescia…", "Définir le compte à rebours…", "Configurar cuenta atrás…");
        public static string CountdownHint => T("Double-click or right-click → Set countdown", "Doppelklick oder Rechtsklick → Countdown festlegen", "Doppio clic o clic destro → Imposta conto alla rovescia", "Double-clic ou clic droit → Définir le compte à rebours", "Doble clic o clic derecho → Configurar cuenta atrás");
        public static string CountdownDays(int n) => n == 1
            ? T("1 day", "1 Tag", "1 giorno", "1 jour", "1 día")
            : T($"{n} days", $"{n} Tage", $"{n} giorni", $"{n} jours", $"{n} días");
        public static string CountdownReached => T("It's time!", "Es ist so weit!", "Ci siamo!", "C'est l'heure !", "¡Ya es la hora!");
        public static string CountdownTitleLabel => T("Title", "Titel", "Titolo", "Titre", "Título");
        public static string CountdownDateLabel => T("Date and time", "Datum und Uhrzeit", "Data e ora", "Date et heure", "Fecha y hora");
        public static string WidgetPlaytime => T("Playtime", "Spielzeit", "Tempo di gioco", "Temps de jeu", "Tiempo de juego");
        public static string PlaytimeChoose => T("Choose game (exe)…", "Spiel auswählen (EXE)…", "Scegli gioco (exe)…", "Choisir le jeu (exe)…", "Elegir juego (exe)…");
        public static string PlaytimeChooseHint => T("Double-click to choose the game's exe. NoFences then records how long it runs.",
                                                     "Doppelklick, um die EXE des Spiels auszuwählen. NoFences zeichnet dann auf, wie lange es läuft.",
                                                     "Doppio clic per scegliere l'eseguibile del gioco. NoFences registra poi per quanto tempo è in esecuzione.",
                                                     "Double-cliquez pour choisir l'exe du jeu. NoFences enregistre ensuite combien de temps il tourne.",
                                                     "Haz doble clic para elegir el exe del juego. NoFences registra luego cuánto tiempo se ejecuta.");
        public static string PlaytimeExeFilter => T("Programs (*.exe)|*.exe", "Programme (*.exe)|*.exe", "Programmi (*.exe)|*.exe", "Programmes (*.exe)|*.exe", "Programas (*.exe)|*.exe");
        public static string PlaytimeRunning => T("running", "läuft", "in corso", "en cours", "en curso");
        public static string PlaytimeToday => T("today", "heute", "oggi", "aujourd'hui", "hoy");
        public static string PlaytimeWeek => T("This week", "Diese Woche", "Questa settimana", "Cette semaine", "Esta semana");
        public static string PlaytimeMonth => T("This month", "Diesen Monat", "Questo mese", "Ce mois-ci", "Este mes");
        public static string PlaytimeTotal => T("Total", "Gesamt", "Totale", "Total", "Total");
        public static string WidgetWeather => T("Weather", "Wetter", "Meteo", "Météo", "Tiempo");
        public static string WeatherHint => T("Double-click to choose a place.", "Doppelklick, um einen Ort auszuwählen.", "Doppio clic per scegliere una località.", "Double-cliquez pour choisir un lieu.", "Haz doble clic para elegir un lugar.");
        public static string WeatherChoose => T("Choose place…", "Ort auswählen…", "Scegli località…", "Choisir le lieu…", "Elegir lugar…");
        public static string WeatherUpdateNow => T("Update now", "Jetzt aktualisieren", "Aggiorna ora", "Actualiser", "Actualizar ahora");
        public static string WeatherPlaceLabel => T("Town or city:", "Ort oder Stadt:", "Località o città:", "Ville ou village :", "Ciudad o pueblo:");
        public static string WeatherSearch => T("Search", "Suchen", "Cerca", "Rechercher", "Buscar");
        public static string WeatherLoading => T("Loading…", "Wird geladen…", "Caricamento…", "Chargement…", "Cargando…");
        public static string WeatherOffline => T("No connection to the weather service.", "Keine Verbindung zum Wetterdienst.", "Nessuna connessione al servizio meteo.", "Pas de connexion au service.", "Sin conexión con el servicio.");
        public static string WeatherNoPlace => T("No place found.", "Kein Ort gefunden.", "Nessuna località trovata.", "Aucun lieu trouvé.", "No se encontró ningún lugar.");
        public static string WeatherCredit => T("Weather data: Open-Meteo.com", "Wetterdaten: Open-Meteo.com", "Dati meteo: Open-Meteo.com", "Données météo : Open-Meteo.com", "Datos del tiempo: Open-Meteo.com");
        public static string WeatherDetails(double feelsLike, double wind) => T(
            $"Feels like {feelsLike:0}° · wind {wind:0} km/h", $"Gefühlt {feelsLike:0}° · Wind {wind:0} km/h", $"Percepita {feelsLike:0}° · vento {wind:0} km/h",
            $"Ressenti {feelsLike:0}° · vent {wind:0} km/h", $"Sensación {feelsLike:0}° · viento {wind:0} km/h");
        public static string WeatherKindName(Widgets.WeatherKind kind) => kind switch
        {
            Widgets.WeatherKind.Clear => T("Clear", "Klar", "Sereno", "Dégagé", "Despejado"),
            Widgets.WeatherKind.PartlyCloudy => T("Partly cloudy", "Teils bewölkt", "Parzialmente nuvoloso", "Partiellement nuageux", "Parcialmente nublado"),
            Widgets.WeatherKind.Cloudy => T("Cloudy", "Bewölkt", "Nuvoloso", "Nuageux", "Nublado"),
            Widgets.WeatherKind.Fog => T("Fog", "Nebel", "Nebbia", "Brouillard", "Niebla"),
            Widgets.WeatherKind.Drizzle => T("Drizzle", "Nieselregen", "Pioviggine", "Bruine", "Llovizna"),
            Widgets.WeatherKind.Rain => T("Rain", "Regen", "Pioggia", "Pluie", "Lluvia"),
            Widgets.WeatherKind.Snow => T("Snow", "Schnee", "Neve", "Neige", "Nieve"),
            _ => T("Thunderstorm", "Gewitter", "Temporale", "Orage", "Tormenta")
        };
        public static string WidgetMedia => T("Now playing (media)", "Medien (läuft gerade)", "In riproduzione (media)", "En cours de lecture (médias)", "Reproduciendo (multimedia)");
        public static string MediaNothing => T("Nothing is playing.\nMusic and videos from Spotify, browsers etc. appear here.",
                                              "Gerade läuft nichts.\nMusik und Videos aus Spotify, Browsern usw. erscheinen hier.",
                                              "Nessuna riproduzione.\nMusica e video da Spotify, browser ecc. appaiono qui.",
                                              "Aucune lecture en cours.\nLa musique et les vidéos de Spotify, des navigateurs, etc. apparaissent ici.",
                                              "No se está reproduciendo nada.\nLa música y los vídeos de Spotify, navegadores, etc. aparecen aquí.");
        public static string WidgetNetwork => T("Network", "Netzwerk", "Rete", "Réseau", "Red");
        public static string WidgetClipboard => T("Clipboard history", "Zwischenablage-Verlauf", "Cronologia appunti", "Historique du presse-papiers", "Historial del portapapeles");
        public static string ClipboardHint => T("Copied texts appear here – click one to copy it again. Kept only until NoFences closes; passwords from password managers are skipped.",
                                               "Kopierte Texte erscheinen hier – anklicken kopiert sie erneut. Nur bis NoFences beendet wird; Passwörter aus Passwort-Managern werden übersprungen.",
                                               "I testi copiati appaiono qui – fai clic per copiarli di nuovo. Conservati solo finché NoFences è aperto; le password dei gestori di password vengono ignorate.",
                                               "Les textes copiés apparaissent ici – cliquez pour les copier à nouveau. Conservés jusqu'à la fermeture de NoFences ; les mots de passe des gestionnaires de mots de passe sont ignorés.",
                                               "Los textos copiados aparecen aquí: haz clic para copiarlos de nuevo. Solo se guardan hasta cerrar NoFences; las contraseñas de los gestores de contraseñas se omiten.");
        public static string ClipboardClear => T("Clear history", "Verlauf leeren", "Cancella cronologia", "Effacer l'historique", "Borrar historial");
        public static string WidgetBattery => T("Battery", "Akku", "Batteria", "Batterie", "Batería");
        public static string BatteryNone => T("No battery found.", "Kein Akku gefunden.", "Nessuna batteria trovata.", "Aucune batterie trouvée.", "No se encontró ninguna batería.");
        public static string BatteryCharging => T("Charging", "Wird geladen", "In carica", "En charge", "Cargando");
        public static string BatteryPlugged => T("Plugged in", "Am Netz", "Collegato", "Sur secteur", "Conectado");
        public static string BatteryOnBattery => T("On battery", "Akkubetrieb", "A batteria", "Sur batterie", "Con batería");
        public static string BatteryLeft(string time) => T($"{time} left", $"noch {time}", $"ancora {time}", $"encore {time}", $"quedan {time}");
        public static string DriveDefaultName(DriveType type) => type switch
        {
            DriveType.Removable => T("USB drive", "USB-Laufwerk", "Unità USB", "Clé USB", "Unidad USB"),
            DriveType.Network => T("Network", "Netzwerk", "Rete", "Réseau", "Red"),
            _ => T("Local disk", "Lokaler Datenträger", "Disco locale", "Disque local", "Disco local")
        };
        public static string FreeSpace(string size) => T($"{size} free", $"{size} frei", $"{size} liberi", $"{size} libres", $"{size} libres");
        public static string RecycleEmptyState => T("Empty", "Leer", "Vuoto", "Vide", "Vacía");
        public static string RecycleItems(long n, string size) => n == 1
            ? T($"1 item · {size}", $"1 Element · {size}", $"1 elemento · {size}", $"1 élément · {size}", $"1 elemento · {size}")
            : T($"{n} items · {size}", $"{n} Elemente · {size}", $"{n} elementi · {size}", $"{n} éléments · {size}", $"{n} elementos · {size}");
        public static string RecycleDropHint => T("Drop files here to delete", "Zum Löschen hierher ziehen", "Trascina qui per eliminare", "Déposez ici pour supprimer", "Arrastra aquí para eliminar");
        public static string RecycleEmptyAction => T("Empty recycle bin", "Papierkorb leeren", "Svuota cestino", "Vider la corbeille", "Vaciar la papelera");
        public static string GpuTemperature => T("GPU temp.", "GPU-Temp.", "Temp. GPU", "Temp. GPU", "Temp. GPU");
        public static string FpsWaiting => T("waiting for a game…", "wartet auf ein Spiel…", "in attesa di un gioco…", "en attente d'un jeu…", "esperando un juego…");
        public static string FpsMenu => T("Measure FPS (admin helper)…", "FPS messen (Admin-Helfer)…", "Misura FPS (supporto amministratore)…", "Mesurer les FPS (assistant admin)…", "Medir FPS (asistente de administrador)…");
        public static string FpsTitle => T("Measure FPS – admin rights needed", "FPS messen – Administratorrechte nötig", "Misura FPS – servono diritti di amministratore", "Mesurer les FPS – droits d'administrateur requis", "Medir FPS – se necesitan permisos de administrador");
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
            "Attivare la misurazione degli FPS?",
            "Pour mesurer la fréquence d'images (FPS) des jeux, NoFences a besoin d'un petit processus assistant avec des droits d'administrateur. " +
            "Windows ne fournit les événements de sortie graphique (ETW) qu'aux programmes disposant de ces droits – MSI Afterburner et PresentMon fonctionnent de la même façon.\n\n" +
            "• Seul cet assistant s'exécute en administrateur, pas NoFences.\n" +
            "• Il compte seulement combien d'images sont affichées – aucun contenu d'écran, aucune saisie clavier ou souris.\n" +
            "• La première fois, Windows demande l'autorisation (UAC). L'assistant crée ensuite une tâche dans le Planificateur de tâches pour démarrer plus tard sans demander.\n" +
            "• Désactivable à tout moment dans les paramètres ; la tâche est alors supprimée.\n\n" +
            "Activer la mesure des FPS ?",
            "Para medir la tasa de fotogramas (FPS) de los juegos, NoFences necesita un pequeño proceso asistente con permisos de administrador. " +
            "Windows solo entrega los eventos de salida gráfica (ETW) a programas con esos permisos; MSI Afterburner y PresentMon funcionan igual.\n\n" +
            "• Solo este asistente se ejecuta como administrador, NoFences no.\n" +
            "• Solo cuenta cuántos fotogramas se muestran: nada del contenido de la pantalla ni del teclado o el ratón.\n" +
            "• La primera vez, Windows pide permiso (UAC). Después el asistente crea una tarea en el Programador de tareas para poder iniciarse sin preguntar.\n" +
            "• Puedes desactivarlo cuando quieras en la configuración; la tarea se elimina.\n\n" +
            "¿Activar la medición de FPS?");
        public static string FpsDeclined => T("FPS measurement stays off (no admin rights granted).", "FPS-Messung bleibt aus (keine Adminrechte erteilt).", "La misurazione degli FPS resta disattivata (diritti di amministratore non concessi).", "La mesure des FPS reste désactivée (droits d'administrateur non accordés).", "La medición de FPS sigue desactivada (no se concedieron permisos de administrador).");

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
        public static string FocusProfileMenu => T("Focus mode: switch to profile", "Fokus-Modus: zu Profil wechseln", "Modalità concentrazione: passa al profilo", "Mode concentration : passer au profil", "Modo concentración: cambiar al perfil");
        public static string FocusProfileNone => T("Don't switch", "Nicht wechseln", "Non cambiare", "Ne pas changer", "No cambiar");
        public static string FocusProfileHint => T("Create a profile first (tray → Profile)", "Zuerst ein Profil anlegen (Tray → Profil)", "Crea prima un profilo (barra → Profilo)", "Créez d'abord un profil (zone de notification → Profil)", "Crea primero un perfil (bandeja → Perfil)");
        public static string FocusSkip =>T("Skip to next phase", "Zur nächsten Phase springen", "Passa alla fase successiva", "Passer à la phase suivante", "Saltar a la siguiente fase");

        public static string WidgetNews => T("News (RSS)", "News (RSS)", "Notizie (RSS)", "Actualités (RSS)", "Noticias (RSS)");
        public static string NewsHint => T("Double-click to choose news feeds.", "Doppelklick, um News-Feeds auszuwählen.", "Doppio clic per scegliere i feed di notizie.", "Double-cliquez pour choisir des flux d'actualités.", "Haz doble clic para elegir fuentes de noticias.");
        public static string NewsPrompt => T("Feed links (RSS or Atom), one per line – or add one of these:", "Feed-Links (RSS oder Atom), einer pro Zeile – oder einen davon hinzufügen:", "Link dei feed (RSS o Atom), uno per riga – oppure aggiungine uno di questi:", "Liens de flux (RSS ou Atom), un par ligne – ou ajoutez l'un de ceux-ci :", "Enlaces de fuentes (RSS o Atom), uno por línea, o añade uno de estos:");
        public static string NewsFailed => T("The news feeds could not be loaded. NoFences tries again every 30 seconds.",
                                             "Die News-Feeds konnten nicht geladen werden. NoFences versucht es alle 30 Sekunden erneut.",
                                             "Impossibile caricare i feed di notizie. NoFences riprova ogni 30 secondi.",
                                             "Impossible de charger les flux d'actualités. NoFences réessaie toutes les 30 secondes.",
                                             "No se pudieron cargar las fuentes de noticias. NoFences lo vuelve a intentar cada 30 segundos.");
        public static string NewsNotAFeed(string host) => T(
            $"{host}: this is a web page, not a news feed (RSS/Atom).", $"{host}: Das ist eine Webseite, kein News-Feed (RSS/Atom).",
            $"{host}: è una pagina web, non un feed di notizie (RSS/Atom).", $"{host} : c'est une page web, pas un flux d'actualités (RSS/Atom).",
            $"{host}: es una página web, no una fuente de noticias (RSS/Atom).");
        public static string NewsCheckLinks => T(
            "No news feed found at these addresses. Right-click → News feeds… to pick a feed (or one of the ready-made ones).",
            "Unter diesen Adressen gibt es keinen News-Feed. Rechtsklick → News-Feeds… und einen Feed eintragen (oder einen der fertigen wählen).",
            "A questi indirizzi non c'è un feed di notizie. Clic destro → Feed di notizie… per inserirne uno (o sceglierne uno pronto).",
            "Aucun flux d'actualités à ces adresses. Clic droit → Flux d'actualités… pour en saisir un (ou en choisir un tout prêt).",
            "En estas direcciones no hay ninguna fuente de noticias. Clic derecho → Fuentes de noticias… para poner una (o elegir una ya preparada).");
        public static string NewsBroken(string host) => T($"{host}: the feed is damaged.", $"{host}: Der Feed ist fehlerhaft.", $"{host}: il feed è danneggiato.", $"{host} : le flux est endommagé.", $"{host}: la fuente está dañada.");
        public static string NewsUnreachable(string host) => T($"{host} can't be reached.", $"{host} ist nicht erreichbar.", $"{host} non è raggiungibile.", $"{host} est injoignable.", $"{host} no está disponible.");
        public static string NewsSet => T("News feeds…", "News-Feeds…", "Feed di notizie…", "Flux d'actualités…", "Fuentes de noticias…");

        public static string WidgetStatus => T("Service status (RSI, Discord …)", "Dienst-Status (RSI, Discord …)", "Stato dei servizi (RSI, Discord …)", "État des services (RSI, Discord…)", "Estado de servicios (RSI, Discord…)");
        public static string StatusSet => T("Services…", "Dienste…", "Servizi…", "Services…", "Servicios…");
        public static string StatusPrompt => T(
            "Status pages, one per line – add one of these or paste the address of another status page:",
            "Statusseiten, eine pro Zeile – eine davon hinzufügen oder die Adresse einer anderen Statusseite einfügen:",
            "Pagine di stato, una per riga – aggiungine una di queste o incolla l'indirizzo di un'altra pagina di stato:",
            "Pages d'état, une par ligne – ajoutez-en une ou collez l'adresse d'une autre page d'état :",
            "Páginas de estado, una por línea: añade una de estas o pega la dirección de otra página de estado:");
        public static string StatusUnknown => T("unknown", "unbekannt", "sconosciuto", "inconnu", "desconocido");
        public static string StatusLevelName(Widgets.ServiceLevel level) => level switch
        {
            Widgets.ServiceLevel.Ok => T("all good", "alles ok", "tutto ok", "tout va bien", "todo bien"),
            Widgets.ServiceLevel.Notice => T("maintenance", "Wartung", "manutenzione", "maintenance", "mantenimiento"),
            Widgets.ServiceLevel.Degraded => T("problems", "Störungen", "problemi", "perturbations", "problemas"),
            Widgets.ServiceLevel.Down => T("outage", "Ausfall", "interruzione", "panne", "caída"),
            _ => StatusUnknown
        };
        public static string WidgetAudio =>T("Sound (volume, devices)", "Sound (Lautstärke, Geräte)", "Audio (volume, dispositivi)", "Son (volume, périphériques)", "Sonido (volumen, dispositivos)");
        public static string AudioNone => T("No playback device found.", "Kein Wiedergabegerät gefunden.", "Nessun dispositivo di riproduzione trovato.", "Aucun périphérique de lecture trouvé.", "No se encontró ningún dispositivo de reproducción.");
        public static string AudioMuted => T("Muted", "Stumm", "Muto", "Muet", "Silenciado");
        public static string AudioMuteTip => T("Mute / unmute", "Stumm / Ton an", "Muto / audio attivo", "Couper / rétablir le son", "Silenciar / activar sonido");
        public static string AudioMicTip => T("Microphone on / off", "Mikrofon an / aus", "Microfono on / off", "Micro activé / désactivé", "Micrófono sí / no");
        public static string AudioSettings => T("Sound settings…", "Sound-Einstellungen…", "Impostazioni audio…", "Paramètres de son…", "Configuración de sonido…");
        public static string WidgetScreenTime =>T("Screen time", "Bildschirmzeit", "Tempo di utilizzo", "Temps d'écran", "Tiempo de pantalla");
        public static string ScreenTimeWeek => T("7 days", "7 Tage", "7 giorni", "7 jours", "7 días");
        public static string ScreenTimeHint => T(
            "From now on NoFences notes which program is in front while you use the PC (not while you're away). Stays on this PC.",
            "Ab jetzt merkt sich NoFences, welches Programm im Vordergrund ist, während du den PC benutzt (nicht, wenn du weg bist). Bleibt auf diesem PC.",
            "Da ora NoFences annota quale programma è in primo piano mentre usi il PC (non quando sei via). Resta su questo PC.",
            "Désormais, NoFences note quel programme est au premier plan pendant que vous utilisez le PC (pas en votre absence). Reste sur ce PC.",
            "A partir de ahora NoFences anota qué programa está en primer plano mientras usas el PC (no cuando no estás). Se queda en este PC.");
        public static string WidgetTicker =>T("Prices (stocks, crypto)", "Kurse (Aktien, Krypto)", "Quotazioni (azioni, cripto)", "Cours (actions, crypto)", "Cotizaciones (acciones, cripto)");
        public static string TickerFailed => T("Prices could not be loaded. NoFences tries again every 30 seconds.",
                                              "Die Kurse konnten nicht geladen werden. NoFences versucht es alle 30 Sekunden erneut.",
                                              "Impossibile caricare le quotazioni. NoFences riprova ogni 30 secondi.",
                                              "Impossible de charger les cours. NoFences réessaie toutes les 30 secondes.",
                                              "No se pudieron cargar las cotizaciones. NoFences lo vuelve a intentar cada 30 segundos.");
        public static string TickerSet =>T("Symbols…", "Symbole…", "Simboli…", "Symboles…", "Símbolos…");
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
        public static string SearchWindowsSettings => T("Windows settings", "Windows-Einstellungen", "Impostazioni di Windows", "Paramètres Windows", "Configuración de Windows");
        public static string SearchApp => T("App", "App", "App", "Application", "Aplicación");
        public static string SearchCopyResult => T("Enter copies the result", "Enter kopiert das Ergebnis", "Invio copia il risultato", "Entrée copie le résultat", "Intro copia el resultado");

        /// <summary>Searchable Windows settings pages: name (with a few extra words to find it by) and address.</summary>
        public static IEnumerable<(string Name, string Uri)> SettingsPageNames() => new[]
        {
            (T("Display (resolution, scaling)", "Bildschirm (Auflösung, Skalierung)", "Schermo (risoluzione, ridimensionamento)", "Écran (résolution, mise à l'échelle)", "Pantalla (resolución, escala)"), "ms-settings:display"),
            (T("Sound (volume, devices)", "Sound (Lautstärke, Geräte)", "Audio (volume, dispositivi)", "Son (volume, périphériques)", "Sonido (volumen, dispositivos)"), "ms-settings:sound"),
            (T("Bluetooth & devices", "Bluetooth & Geräte", "Bluetooth e dispositivi", "Bluetooth et appareils", "Bluetooth y dispositivos"), "ms-settings:bluetooth"),
            (T("Wi-Fi", "WLAN", "Wi-Fi", "Wi-Fi", "Wi-Fi"), "ms-settings:network-wifi"),
            (T("Network & internet", "Netzwerk & Internet", "Rete e Internet", "Réseau et Internet", "Red e Internet"), "ms-settings:network"),
            (T("Windows Update", "Windows Update", "Windows Update", "Windows Update", "Windows Update"), "ms-settings:windowsupdate"),
            (T("Installed apps (uninstall)", "Installierte Apps (deinstallieren)", "App installate (disinstalla)", "Applications installées (désinstaller)", "Aplicaciones instaladas (desinstalar)"), "ms-settings:appsfeatures"),
            (T("Startup apps (autostart)", "Autostart-Apps", "App di avvio", "Applications de démarrage", "Aplicaciones de inicio"), "ms-settings:startupapps"),
            (T("Default apps", "Standard-Apps", "App predefinite", "Applications par défaut", "Aplicaciones predeterminadas"), "ms-settings:defaultapps"),
            (T("Background (wallpaper)", "Hintergrund (Hintergrundbild)", "Sfondo", "Arrière-plan (fond d'écran)", "Fondo (fondo de pantalla)"), "ms-settings:personalization-background"),
            (T("Colors (dark mode, accent color)", "Farben (dunkler Modus, Akzentfarbe)", "Colori (modalità scura, colore d'accento)", "Couleurs (mode sombre, couleur d'accentuation)", "Colores (modo oscuro, color de énfasis)"), "ms-settings:colors"),
            (T("Taskbar", "Taskleiste", "Barra delle applicazioni", "Barre des tâches", "Barra de tareas"), "ms-settings:taskbar"),
            (T("Notifications", "Benachrichtigungen", "Notifiche", "Notifications", "Notificaciones"), "ms-settings:notifications"),
            (T("Power & battery (sleep)", "Energie & Akku (Energiesparen)", "Alimentazione e batteria (sospensione)", "Alimentation et batterie (veille)", "Energía y batería (suspensión)"), "ms-settings:powersleep"),
            (T("Storage (free up space)", "Speicher (Speicherplatz freigeben)", "Archiviazione (libera spazio)", "Stockage (libérer de l'espace)", "Almacenamiento (liberar espacio)"), "ms-settings:storagesense"),
            (T("Mouse", "Maus", "Mouse", "Souris", "Ratón"), "ms-settings:mousetouchpad"),
            (T("Keyboard & language", "Tastatur & Sprache", "Tastiera e lingua", "Clavier et langue", "Teclado e idioma"), "ms-settings:regionlanguage"),
            (T("Date & time", "Datum & Uhrzeit", "Data e ora", "Date et heure", "Fecha y hora"), "ms-settings:dateandtime"),
            (T("Gaming (Game Mode)", "Spielen (Spielmodus)", "Giochi (modalità gioco)", "Jeux (mode Jeu)", "Juegos (modo de juego)"), "ms-settings:gaming-gamemode"),
            (T("Graphics (GPU per app)", "Grafik (GPU pro App)", "Grafica (GPU per app)", "Graphiques (GPU par application)", "Gráficos (GPU por aplicación)"), "ms-settings:display-advancedgraphics"),
            (T("Privacy & security", "Datenschutz & Sicherheit", "Privacy e sicurezza", "Confidentialité et sécurité", "Privacidad y seguridad"), "ms-settings:privacy"),
            (T("Windows Security (virus protection)", "Windows-Sicherheit (Virenschutz)", "Sicurezza di Windows (antivirus)", "Sécurité Windows (antivirus)", "Seguridad de Windows (antivirus)"), "windowsdefender:"),
            (T("Printers & scanners", "Drucker & Scanner", "Stampanti e scanner", "Imprimantes et scanners", "Impresoras y escáneres"), "ms-settings:printers"),
            (T("Accounts", "Konten", "Account", "Comptes", "Cuentas"), "ms-settings:yourinfo"),
            (T("About this PC (system info)", "Info (Systeminformationen)", "Informazioni sul PC", "À propos de ce PC", "Acerca de este PC"), "ms-settings:about"),
        };

        public static string SearchInFence(string fence) => T($"in {fence}", $"in {fence}", $"in {fence}", $"dans {fence}", $"en {fence}");
        public static string SearchInNote(string fence) => T($"Note: {fence}", $"Notiz: {fence}", $"Nota: {fence}", $"Note : {fence}", $"Nota: {fence}");

        #endregion

        #region About

        public static string AboutTagline => T("Free desktop fences, folder fences, sticky notes and widgets for Windows.",
                                               "Kostenlose Desktop-Fences, Ordner-Fences, Notizen und Widgets für Windows.",
                                               "Recinti per il desktop, recinti cartella, note e widget gratuiti per Windows.",
                                               "Barrières de bureau, barrières de dossier, notes et widgets gratuits pour Windows.",
                                               "Vallas de escritorio, vallas de carpeta, notas y widgets gratuitos para Windows.");
        public static string AboutSource => T("Source code and downloads on GitHub", "Quellcode und Downloads auf GitHub", "Codice sorgente e download su GitHub", "Code source et téléchargements sur GitHub", "Código fuente y descargas en GitHub");
        public static string AboutCredits => T("Based on NoFences by Twometer and contributors — thank you!",
                                               "Basiert auf NoFences von Twometer und Mitwirkenden – danke!",
                                               "Basato su NoFences di Twometer e collaboratori – grazie!",
                                               "Basé sur NoFences de Twometer et ses contributeurs – merci !",
                                               "Basado en NoFences de Twometer y colaboradores: ¡gracias!");
        public static string Donate => T("Donate (PayPal)", "Spenden (PayPal)", "Dona (PayPal)", "Faire un don (PayPal)", "Donar (PayPal)");
        public static string DonateHint => T("NoFences is free. If you like it, a small donation helps keep it going – thank you!",
                                             "NoFences ist kostenlos. Wenn es dir gefällt, hilft eine kleine Spende beim Weitermachen – danke!",
                                             "NoFences è gratuito. Se ti piace, una piccola donazione aiuta a portarlo avanti – grazie!",
                                             "NoFences est gratuit. S'il vous plaît, un petit don aide à le faire vivre – merci !",
                                             "NoFences es gratuito. Si te gusta, una pequeña donación ayuda a mantenerlo: ¡gracias!");
        public static string AboutLicense => T("Open source under the MIT license.", "Open Source unter der MIT-Lizenz.", "Open source con licenza MIT.", "Open source sous licence MIT.", "Código abierto con licencia MIT.");

        #endregion
    }
}
