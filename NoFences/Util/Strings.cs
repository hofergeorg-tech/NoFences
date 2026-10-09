using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using NoFences.Model;

namespace NoFences.Util
{
    /// <summary>
    /// UI texts. Each language is a JSON file (key → text, placeholders {0}, {1} …) embedded from
    /// Lang\*.json: English, German, Italian, French and Spanish. Files in the data folder's <c>lang</c>
    /// folder replace single texts of a language or add a new language (e.g. nl.json). The language
    /// follows Windows ("auto") unless chosen in the settings; missing texts fall back to English.
    /// </summary>
    public static class Strings
    {
        public const string Fallback = "en";

        /// <summary>The languages that come with NoFences, in menu order.</summary>
        public static readonly IReadOnlyList<string> BuiltIn = new[] { "en", "de", "it", "fr", "es" };

        private static Dictionary<string, Dictionary<string, string>> texts = LoadBuiltIn();

        /// <summary>"auto" and every language code there are texts for.</summary>
        public static IReadOnlyList<string> Languages =>
            new[] { "auto" }.Concat(BuiltIn).Concat(texts.Keys.Except(BuiltIn).Order()).ToList();

        /// <summary>"auto" or one of the language codes (from the settings).</summary>
        public static string Language { get; set; } = "auto";

        public static string Effective
        {
            get
            {
                if (texts.ContainsKey(Language))
                    return Language;
                var ui = CultureInfo.CurrentUICulture;
                return texts.ContainsKey(ui.Name) ? ui.Name : texts.ContainsKey(ui.TwoLetterISOLanguageName) ? ui.TwoLetterISOLanguageName : Fallback;
            }
        }

        /// <summary>Keys asked for that no language has (tests).</summary>
        internal static readonly HashSet<string> MissingKeys = new();

        /// <summary>The text for <paramref name="key"/> in the current language.</summary>
        private static string L(string key) => Lookup(key);

        /// <summary>The text for <paramref name="key"/> with {0}, {1} … filled in.</summary>
        private static string L(string key, params object?[] args)
        {
            var text = Lookup(key);
            try
            {
                return string.Format(CultureInfo.CurrentCulture, text, args);
            }
            catch (FormatException)
            {
                // A broken placeholder in an own translation: show the English text instead
                return string.Format(CultureInfo.CurrentCulture, texts[Fallback].GetValueOrDefault(key, key), args);
            }
        }

        private static string Lookup(string key)
        {
            if (texts.TryGetValue(Effective, out var language) && language.TryGetValue(key, out var text))
                return text;
            if (texts[Fallback].TryGetValue(key, out text))
                return text;
            lock (MissingKeys)
                MissingKeys.Add(key);
            return key;
        }

        /// <summary>Name of a language in that language ("auto" in the current one).</summary>
        public static string LanguageName(string code) =>
            code == "auto" ? L("LanguageAuto")
            : texts.TryGetValue(code, out var language) && language.TryGetValue("_language", out var name) ? name
            : code;

        #region Language files

        private static readonly Regex LanguageFileName = new(@"^[a-z]{2,3}(-[A-Z]{2})?\.json$");

        /// <summary>Folder for own language files (set at startup), or null.</summary>
        public static string? LanguageFolder { get; private set; }

        public static Dictionary<string, string> ParseLanguage(string json)
        {
            var result = new Dictionary<string, string>();
            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            foreach (var property in doc.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                    result[property.Name] = property.Value.GetString()!;
            }
            return result;
        }

        private static Dictionary<string, Dictionary<string, string>> LoadBuiltIn()
        {
            var result = new Dictionary<string, Dictionary<string, string>>();
            foreach (var code in BuiltIn)
            {
                using var stream = typeof(Strings).Assembly.GetManifestResourceStream($"Lang.{code}.json");
                if (stream != null)
                    result[code] = ParseLanguage(new StreamReader(stream).ReadToEnd());
            }
            return result;
        }

        /// <summary>The embedded English texts as JSON (template for a new language).</summary>
        public static string BuiltInJson(string code)
        {
            using var stream = typeof(Strings).Assembly.GetManifestResourceStream($"Lang.{code}.json");
            return stream == null ? "{}" : new StreamReader(stream).ReadToEnd();
        }

        /// <summary>
        /// Adds the language files in <paramref name="folder"/>: de.json replaces single German texts,
        /// nl.json adds Dutch. Returns problems as "file: message".
        /// </summary>
        public static List<string> LoadFolder(string? folder)
        {
            LanguageFolder = folder;
            var errors = new List<string>();
            var loaded = LoadBuiltIn();
            if (folder != null && Directory.Exists(folder))
            {
                foreach (var file in Directory.EnumerateFiles(folder, "*.json").Order())
                {
                    var name = Path.GetFileName(file);
                    if (!LanguageFileName.IsMatch(name))
                        continue; // e.g. the TEMPLATE file
                    var code = Path.GetFileNameWithoutExtension(name);
                    try
                    {
                        _ = new CultureInfo(code);
                        var own = ParseLanguage(File.ReadAllText(file));
                        if (!loaded.TryGetValue(code, out var language))
                            loaded[code] = language = new Dictionary<string, string>();
                        foreach (var (key, value) in own)
                            language[key] = value;
                        if (!language.ContainsKey("_language"))
                            language["_language"] = code;
                    }
                    catch (Exception e) when (e is JsonException or IOException or CultureNotFoundException or UnauthorizedAccessException)
                    {
                        errors.Add($"{name}: {e.Message}");
                    }
                }
            }
            texts = loaded;
            return errors;
        }

        /// <summary>Placeholders like {0} or {1:N0} in a text, as their numbers.</summary>
        public static SortedSet<int> Placeholders(string text) =>
            new(Regex.Matches(text.Replace("{{", "").Replace("}}", ""), @"\{(\d+)[^}]*\}").Select(m => int.Parse(m.Groups[1].Value)));

        /// <summary>All texts of a language (tests and the template).</summary>
        public static IReadOnlyDictionary<string, string> TextsOf(string code) =>
            texts.TryGetValue(code, out var language) ? language : new Dictionary<string, string>();

        #endregion

        // Documents shown in the app
        public static string HelpDocument => Effective switch { "de" => "HILFE.md", "it" => "AIUTO.md", "fr" => "AIDE.md", "es" => "AYUDA.md", _ => "HELP.md" };
        public static string ChangelogDocument => Effective switch
        {
            "de" => "CHANGELOG.de.md", "it" => "CHANGELOG.it.md", "fr" => "CHANGELOG.fr.md", "es" => "CHANGELOG.es.md", _ => "CHANGELOG.md"
        };

        #region Style names

        public static string ThemeName(string id) => id switch
        {
            "default" => L("ThemeName.default"),
            "contrast" => L("ThemeName.contrast"),
            "windows" => L("ThemeName.windows"),
            "starcitizen" => "Star Citizen (HUD)",
            "retroarcade" => "Retro-Arcade",
            "hardware" => L("ThemeName.hardware"),
            "nerd" => L("ThemeName.nerd"),
            "hobby" => L("ThemeName.hobby"),
            "work" => L("ThemeName.work"),
            "family" => L("ThemeName.family"),
            "gaming" => "Gaming (RGB)",
            "finance" => L("ThemeName.finance"),
            "social" => L("ThemeName.social"),
            "documents" => L("ThemeName.documents"),
            "multimedia" => L("ThemeName.multimedia"),
            "music" => L("ThemeName.music"),
            "sport" => L("ThemeName.sport"),
            "photos" => L("ThemeName.photos"),
            "travel" => L("ThemeName.travel"),
            "cooking" => L("ThemeName.cooking"),
            "jause" => L("ThemeName.jause"),
            "nature" => L("ThemeName.nature"),
            "postit" => L("ThemeName.postit"),
            "postit-pink" => L("ThemeName.postit-pink"),
            "postit-green" => L("ThemeName.postit-green"),
            "postit-blue" => L("ThemeName.postit-blue"),
            "postit-orange" => L("ThemeName.postit-orange"),
            _ => id
        };

        #endregion

        #region Menus and fences

        public static string Help => L("Help");
        public static string WhatsNew => L("WhatsNew");
        public static string About => L("About");
        public static string BackupLabel => L("BackupLabel");
        public static string RestoreShort => L("RestoreShort");
        public static string LanguageMenu => "Language · Sprache · Lingua · Langue · Idioma";
        public static string AppSettings => L("AppSettings");
        public static string NewFence => L("NewFence");
        public static string NewFolderFence => L("NewFolderFence");
        public static string FirstFence => L("FirstFence");
        public static string ChooseFolder => L("ChooseFolder");
        public static string Settings => L("Settings");
        public static string Locked => L("Locked");
        public static string AutoCollapse => L("AutoCollapse");
        public static string RemoveItem => L("RemoveItem");
        public static string OpenFolder => L("OpenFolder");
        public static string DeleteFence => L("DeleteFence");
        public static string ReallyDelete(string name) => L("ReallyDelete", name);
        public static string ReallyDeleteFolderNote => L("ReallyDeleteFolderNote");
        public static string ShowFences => L("ShowFences");
        public static string Autostart => L("Autostart");
        public static string ShowExtensions => L("ShowExtensions");
        public static string ExtFollowExplorer => L("ExtFollowExplorer");
        public static string ExtAlways => L("ExtAlways");
        public static string ExtNever => L("ExtNever");
        public static string Theme => L("Theme");
        public static string ThemeGlobal => L("ThemeGlobal");
        public static string ThemeInherit => L("ThemeInherit");
        public static string OpenDataFolder => L("OpenDataFolder");
        public static string Exit => L("Exit");
        public static string Rename => L("Rename");
        public static string UndoMenu(string what) => L("UndoMenu", what);
        public static string NothingToUndo => L("NothingToUndo");
        public static string Undone(string what) => L("Undone", what);
        public static string UndoFailed(string error) => L("UndoFailed", error);
        public static string UndoDeleteFence(string name) => L("UndoDeleteFence", name);
        public static string UndoRemoveItems(int count, string fence) => L("UndoRemoveItems", count, fence);
        public static string UndoMoveItems(string fence) => L("UndoMoveItems", fence);
        public static string UndoAddItems(string fence) => L("UndoAddItems", fence);
        public static string UndoRenameItem(string name) => L("UndoRenameItem", name);
        public static string UndoRenameFence(string name) => L("UndoRenameFence", name);
        public static string UndoMoveFence(string name) => L("UndoMoveFence", name);
        public static string UndoResizeFence(string name) => L("UndoResizeFence", name);
        public static string UndoTabs(string name) => L("UndoTabs", name);
        public static string UndoGroup(string name) => L("UndoGroup", name);
        public static string GroupMenu => L("GroupMenu");
        public static string NewGroup => L("NewGroup");
        public static string GroupPrompt => L("GroupPrompt");
        public static string LeaveGroup => L("LeaveGroup");
        public static string FoldGroup => L("FoldGroup");
        public static string UnfoldGroup => L("UnfoldGroup");
        public static string GroupHint => L("GroupHint");
        public static string SidebarGroupName => L("SidebarGroupName");
        public static string DockMenu => L("DockMenu");
        public static string DockHint => L("DockHint");
        public static string DockOff => L("DockOff");
        public static string DockLeft => L("DockLeft");
        public static string DockRight => L("DockRight");
        public static string DockTop => L("DockTop");
        public static string DockBottom => L("DockBottom");
        public static string DockEdgeName(DockEdge edge) => edge switch
        {
            DockEdge.Left => DockLeft,
            DockEdge.Right => DockRight,
            DockEdge.Top => DockTop,
            _ => DockBottom
        };
        public static string DockAutoHide => L("DockAutoHide");
        public static string DockScreen => L("DockScreen");
        public static string BarStyle => L("BarStyle");
        public static string BarStyleNone => L("BarStyleNone");
        public static string NewName => L("NewName");
        public static string RenameFailed(string reason) => L("RenameFailed", reason);
        public static string Search => L("Search");
        public static string AlwaysOnTop => L("AlwaysOnTop");
        public static string ProfileAll => L("ProfileAll");
        public static string ProfileMenu(string active) => L("ProfileMenu", active);
        public static string ProfileNew => L("ProfileNew");
        public static string ProfileDelete => L("ProfileDelete");
        public static string ProfileNamePrompt => L("ProfileNamePrompt");
        public static string ProfileDeleteConfirm(string name) => L("ProfileDeleteConfirm", name);
        public static string ProfileSwitched(string name) => L("ProfileSwitched", name);
        public static string ProfileHowTo => L("ProfileHowTo");
        public static string ProfileFenceMenu => L("ProfileFenceMenu");
        public static string ProfileFenceHint => L("ProfileFenceHint");
        public static string DownloadsMenu => L("DownloadsMenu");
        public static string DownloadsTitle => L("DownloadsTitle");
        public static string CleanupFolders => L("CleanupFolders");
        public static string CleanupAddFolder => L("CleanupAddFolder");
        public static string DownloadsShow => L("DownloadsShow");
        public static string DownloadsOlderThan(int days) => days switch
        {
            7 => L("DownloadsOlderThan.7"),
            30 => L("DownloadsOlderThan.30"),
            90 => L("DownloadsOlderThan.90"),
            _ => L("DownloadsOlderThan.Other")
        };
        public static string DownloadsSize => L("DownloadsSize");
        public static string DownloadsAge => L("DownloadsAge");
        public static string DownloadsRecycle => L("DownloadsRecycle");
        public static string DownloadsNothing => L("DownloadsNothing");
        public static string DownloadsSelected(int n, string size, int all) => L("DownloadsSelected", n, all, size);
        public static string DownloadsConfirm(int n, string size) => L("DownloadsConfirm", n, size);
        public static string WidgetPower =>L("WidgetPower");
        public static string PowerNone => L("PowerNone");
        public static string PowerSettings => L("PowerSettings");
        public static string PowerForProfile(string profile) => L("PowerForProfile", profile);
        public static string PowerKeep => L("PowerKeep");
        public static string QuickNoteLabel =>L("QuickNoteLabel");
        public static string ColorPickerMenu => L("ColorPickerMenu");
        public static string ColorPickerHint => L("ColorPickerHint");
        public static string ColorCopied(string value) => L("ColorCopied", value);
        public static string ToolsMenu =>L("ToolsMenu");
        public static string RulerTitle =>L("RulerTitle");
        public static string RulerMenu => L("RulerMenu");
        public static string RulerTurn => L("RulerTurn");
        public static string RulerHelp => L("RulerHelp");
        public static string RulerUnitName(RulerWindow.Unit unit) => unit switch
        {
            RulerWindow.Unit.Centimeters => L("RulerUnitName.Centimeters"),
            RulerWindow.Unit.Inches => L("RulerUnitName.Inches"),
            _ => L("RulerUnitName.Other")
        };
        public static string AssistantMenu =>L("AssistantMenu");
        public static string AssistantTitle => L("AssistantTitle");
        public static string AssistantIntro => L("AssistantIntro");
        public static string AssistantNote => L("AssistantNote");
        public static string AssistantCreate => L("AssistantCreate");
        public static string AssistantNothing => L("AssistantNothing");
        public static string AssistantDone(int n) => L("AssistantDone", n);
        public static string AssistantFirstStart => L("AssistantFirstStart");
        public static string CategoryName(DesktopCategory category) => category switch
        {
            DesktopCategory.Games => L("CategoryName.Games"),
            DesktopCategory.Programs => L("CategoryName.Programs"),
            DesktopCategory.Documents => L("CategoryName.Documents"),
            DesktopCategory.Images => L("CategoryName.Images"),
            DesktopCategory.Media => L("CategoryName.Media"),
            DesktopCategory.Archives => L("CategoryName.Archives"),
            DesktopCategory.Folders => L("CategoryName.Folders"),
            _ => L("CategoryName.Other")
        };
        public static string WallpaperChoose(string profile) => L("WallpaperChoose", profile);
        public static string WallpaperRemove => L("WallpaperRemove");
        public static string WallpaperFilter => L("WallpaperFilter");
        public static string ProfileHotkeysLabel => L("ProfileHotkeysLabel");
        public static string ProfileWallpaperHint => L("ProfileWallpaperHint");
        public static string ProfileLabel =>L("ProfileLabel");
        public static string SectionProfiles => L("SectionProfiles");
        public static string OnlyThisDesktop => L("OnlyThisDesktop");
        public static string DropHint => L("DropHint");
        public static string FolderMissing(string path) => L("FolderMissing", path);
        public static string FirstStartHint => L("FirstStartHint");

        // Appearance: style designer
        public static string DesignerMenu => L("DesignerMenu");
        public static string DesignerTitle => L("DesignerTitle");
        public static string DesignerStart => L("DesignerStart");
        public static string DesignerNew => L("DesignerNew");
        public static string DesignerNewName => L("DesignerNewName");
        public static string DesignerGlass => L("DesignerGlass");
        public static string DesignerTitleBar => L("DesignerTitleBar");
        public static string DesignerTitleText => L("DesignerTitleText");
        public static string DesignerUppercase => L("DesignerUppercase");
        public static string DesignerAlignLeft => L("DesignerAlignLeft");
        public static string DesignerAlignCenter => L("DesignerAlignCenter");
        public static string DesignerLabels => L("DesignerLabels");
        public static string DesignerShadow => L("DesignerShadow");
        public static string DesignerAccent => L("DesignerAccent");
        public static string DesignerBorder => L("DesignerBorder");
        public static string DesignerCorners => L("DesignerCorners");
        public static string DesignerSave => L("DesignerSave");
        public static string DesignerSaveApply => L("DesignerSaveApply");
        public static string DesignerFolder => L("DesignerFolder");
        public static string DesignerSaved(string name) => L("DesignerSaved", name);
        public static string Interval(int seconds) => seconds < 60
            ? L("Interval", seconds)
            : L("Interval.Alt", seconds / 60);
        // Weather: rain hint, sun and moon
        public static string RainStarts(int minutes) => L("RainStarts", minutes);
        public static string RainStops(int minutes) => L("RainStops", minutes);
        public static string SunTimes(string rise, string set) => L("SunTimes", rise, set);
        public static string MoonPhaseName(int index) => index switch
        {
            0 => L("MoonPhaseName.0"),
            1 => L("MoonPhaseName.1"),
            2 => L("MoonPhaseName.2"),
            3 => L("MoonPhaseName.3"),
            4 => L("MoonPhaseName.4"),
            5 => L("MoonPhaseName.5"),
            6 => L("MoonPhaseName.6"),
            _ => L("MoonPhaseName.Other")
        };
        public static string MoonTooltip(string phase, int percent) => L("MoonTooltip", phase, percent);
        // System & tools: QR code, magnifier, device batteries, temperature, speed test, autostart, duplicates
        public static string QrMenu => L("QrMenu");
        public static string PasswordMenu => L("PasswordMenu");
        public static string PasswordTitle => L("PasswordTitle");
        public static string PasswordLength => L("PasswordLength");
        public static string PasswordNoAmbiguous => L("PasswordNoAmbiguous");
        public static string PasswordNew => L("PasswordNew");
        public static string PasswordCopy => L("PasswordCopy");
        public static string PasswordHint => L("PasswordHint");
        public static string PasswordStrength(int bits) => L("PasswordStrength", bits);
        public static string PasswordCopied => L("PasswordCopied");
        public static string NetInfoMenu => L("NetInfoMenu");
        public static string NetInfoTitle => L("NetInfoTitle");
        public static string NetInfoRefresh => L("NetInfoRefresh");
        public static string NetInfoClickToCopy => L("NetInfoClickToCopy");
        public static string NetInfoCopied(string value) => L("NetInfoCopied", value);
        public static string NetInfoPublic => L("NetInfoPublic");
        public static string NetInfoLoading => L("NetInfoLoading");
        public static string NetInfoOffline => L("NetInfoOffline");
        public static string NetInfoWifi => L("NetInfoWifi");
        public static string NetInfoSignal(int percent) => L("NetInfoSignal", percent);
        public static string NetInfoGateway => L("NetInfoGateway");
        public static string QrTitle => L("QrTitle");
        public static string QrPrompt => L("QrPrompt");
        public static string QrCopy => L("QrCopy");
        public static string QrSave => L("QrSave");
        public static string QrHint => L("QrHint");
        public static string QrEmpty => L("QrEmpty");
        public static string QrTooLong => L("QrTooLong");
        public static string MagnifierMenu => L("MagnifierMenu");
        public static string ControllerName(int n) => L("ControllerName", n);
        public static string ControllerWired => L("ControllerWired");
        public static string SpeedTest => L("SpeedTest");
        public static string SpeedTestStart => L("SpeedTestStart");
        public static string SpeedTestRunning(string phase) => L("SpeedTestRunning", phase);
        public static string SpeedTestHint => L("SpeedTestHint");
        public static string WidgetAutostart => L("WidgetAutostart");
        public static string AutostartNone => L("AutostartNone");
        public static string AutostartAdminOnly => L("AutostartAdminOnly");
        public static string AutostartOpenSettings => L("AutostartOpenSettings");
        public static string DuplicatesMode => L("DuplicatesMode");
        public static string DuplicatesSearching => L("DuplicatesSearching");
        public static string DuplicatesNone => L("DuplicatesNone");
        public static string DuplicatesOriginal => L("DuplicatesOriginal");
        public static string DuplicatesGroup(string name, int count, string size) => L("DuplicatesGroup", name, count, size);
        // Notes: password, images, voice notes; clipboard pins
        public static string NoteProtect => L("NoteProtect");
        public static string NoteUnlock => L("NoteUnlock");
        public static string NoteLockNow => L("NoteLockNow");
        public static string NoteRemoveProtection => L("NoteRemoveProtection");
        public static string NotePassword => L("NotePassword");
        public static string NotePasswordNew => L("NotePasswordNew");
        public static string NotePasswordRepeat => L("NotePasswordRepeat");
        public static string NotePasswordMismatch => L("NotePasswordMismatch");
        public static string NotePasswordWrong => L("NotePasswordWrong");
        public static string NoteLockedHint => L("NoteLockedHint");
        public static string NoteImage => L("NoteImage");
        public static string VoiceNote => L("VoiceNote");
        public static string VoiceNoteRecord => L("VoiceNoteRecord");
        public static string VoiceNoteStop => L("VoiceNoteStop");
        public static string VoiceNoteRecording(string time) => L("VoiceNoteRecording", time);
        public static string VoiceNoteNoMicrophone => L("VoiceNoteNoMicrophone");
        public static string ClipboardPin => L("ClipboardPin");
        public static string ClipboardUnpin => L("ClipboardUnpin");
        // Profiles: wallpaper by time of day, programs per profile
        public static string SectionTimedWallpaper => L("SectionTimedWallpaper");
        public static string TimedWallpaperFrom => L("TimedWallpaperFrom");
        public static string TimedWallpaperAdd => L("TimedWallpaperAdd");
        public static string FromTime(string time) => L("FromTime", time);
        public static string TimedWallpaperHint => L("TimedWallpaperHint");
        public static string ProfilePrograms(string profile) => L("ProfilePrograms", profile);
        public static string ProfileProgramAdd => L("ProfileProgramAdd");
        public static string ProfileProgramClose => L("ProfileProgramClose");
        public static string ProfileProgramsHint => L("ProfileProgramsHint");
        public static string ProgramFilter => L("ProgramFilter");
        public static string AlarmStop => L("AlarmStop");
        public static string AlarmSnooze(int minutes) => L("AlarmSnooze", minutes);
        public static string AppointmentOffer(string title, string when, int minutes) => L("AppointmentOffer", title, when, minutes);
        public static string AppointmentMenu(int minutes) => L("AppointmentMenu", minutes);
        public static string ItemNoteMenu => L("ItemNoteMenu");
        public static string ItemNoteTitle(string name) => L("ItemNoteTitle", name);
        public static string ItemNotePrompt => L("ItemNotePrompt");
        public static string ShowFolderInFence => L("ShowFolderInFence");
        public static string FenceStatsMenu => L("FenceStatsMenu");
        public static string FenceStatsTitle(string name) => L("FenceStatsTitle", name);
        public static string FenceStatsName => L("FenceStatsName");
        public static string FenceStatsOpened => L("FenceStatsOpened");
        public static string FenceStatsNever => L("FenceStatsNever");
        public static string FenceStatsSummary(int count, int never) => L("FenceStatsSummary", count, never);
        public static string FenceStatsHint => L("FenceStatsHint");
        public static string FenceStatsRemove => L("FenceStatsRemove");
        public static string FenceStatsRecycle => L("FenceStatsRecycle");
        public static string BackgroundPicture => L("BackgroundPicture");
        public static string BackgroundPictureOpacity => L("BackgroundPictureOpacity");
        public static string BackgroundPictureTiled => L("BackgroundPictureTiled");
        public static string PictureFilter => L("PictureFilter");
        public static string OpenWithLabel => L("OpenWithLabel");
        public static string OpenWithHint => L("OpenWithHint");
        public static string OpenWithFailed(string program, string error) => L("OpenWithFailed", program, error);
        // Fading, shortcuts, shelf, bookmarks, recent folders, templates, desktop icons, monitors, hover preview
        public static string FadeFences => L("FadeFences");
        public static string FadeFencesHint => L("FadeFencesHint");
        public static string NoFade => L("NoFade");
        public static string HoverPreviewSetting => L("HoverPreviewSetting");
        public static string FolderEmpty => L("FolderEmpty");
        public static string FolderMore(int n) => L("FolderMore", n);
        public static string FenceHotkey => L("FenceHotkey");
        public static string FenceHotkeyHint => L("FenceHotkeyHint");
        public static string ShelfCleanup => L("ShelfCleanup");
        public static string ShelfDays(int days) => days switch
        {
            0 => L("ShelfDays.0"),
            1 => L("ShelfDays.1"),
            _ => L("ShelfDays.Other", days)
        };
        public static string ShelfName => L("ShelfName");
        public static string MoreFencesMenu => L("MoreFencesMenu");
        public static string NewShelf => L("NewShelf");
        public static string NewRecentFolders => L("NewRecentFolders");
        public static string RecentFoldersName => L("RecentFoldersName");
        public static string NewBookmarks => L("NewBookmarks");
        public static string NoBrowserFound => L("NoBrowserFound");
        public static string TemplatesMenu => L("TemplatesMenu");
        public static string TemplateName(string id) => id switch
        {
            "gaming" => L("TemplateName.gaming"),
            "office" => L("TemplateName.office"),
            _ => L("TemplateName.Other")
        };
        public static string TemplateGamesFence => L("TemplateGamesFence");
        public static string TemplateWorkFence => L("TemplateWorkFence");
        public static string DesktopIconsMenu => L("DesktopIconsMenu");
        public static string DesktopIconsFailed => L("DesktopIconsFailed");
        public static string MoveAllToMonitor => L("MoveAllToMonitor");
        public static string MonitorName(int number, int width, int height, bool primary) =>
            $"{number}: {width} × {height}" + (primary ? L("MonitorName") : "");
        public static string MarkMenu =>L("MarkMenu");
        public static string MarkName(MarkColor mark) => mark switch
        {
            MarkColor.Red => L("MarkName.Red"),
            MarkColor.Orange => L("MarkName.Orange"),
            MarkColor.Yellow => L("MarkName.Yellow"),
            MarkColor.Green => L("MarkName.Green"),
            MarkColor.Blue => L("MarkName.Blue"),
            MarkColor.Purple => L("MarkName.Purple"),
            _ => L("MarkName.Other")
        };
        public static string SortBy =>L("SortBy");
        public static string SortModeName(Model.FenceSortMode mode) => mode switch
        {
            Model.FenceSortMode.Name => L("SortModeName.Name"),
            Model.FenceSortMode.MostUsed => L("SortModeName.MostUsed"),
            Model.FenceSortMode.Type => L("SortModeName.Type"),
            Model.FenceSortMode.Modified => L("SortModeName.Modified"),
            Model.FenceSortMode.Size => L("SortModeName.Size"),
            _ => L("SortModeName.Other")
        };

        public static string AddTab => L("AddTab");
        public static string RenameTab => L("RenameTab");
        public static string RemoveTab => L("RemoveTab");
        public static string TabDefaultName(int n) => L("TabDefaultName", n);

        public static string WidgetGroupName(Widgets.WidgetRegistry.Group group) => group switch
        {
            Widgets.WidgetRegistry.Group.Time => L("WidgetGroupName.Time"),
            Widgets.WidgetRegistry.Group.Info => L("WidgetGroupName.Info"),
            Widgets.WidgetRegistry.Group.System => L("WidgetGroupName.System"),
            _ => L("WidgetGroupName.Other")
        };
        public static string ThemeGroupName(Themes.ThemeRegistry.Group group) => group switch
        {
            Themes.ThemeRegistry.Group.Basic => L("ThemeGroupName.Basic"),
            Themes.ThemeRegistry.Group.GamingTech => L("ThemeGroupName.GamingTech"),
            Themes.ThemeRegistry.Group.WorkLife => L("ThemeGroupName.WorkLife"),
            Themes.ThemeRegistry.Group.Leisure => L("ThemeGroupName.Leisure"),
            Themes.ThemeRegistry.Group.PostIt => "Post-it",
            _ => L("ThemeGroupName.Other")
        };
        public static string NewWidget =>L("NewWidget");
        public static string NewRecent => L("NewRecent");
        public static string RecentName => L("RecentName");
        public static string NewQuickLaunch => L("NewQuickLaunch");
        public static string QuickLaunchName => L("QuickLaunchName");
        public static string CompactMode => L("CompactMode");
        public static string NewNote => L("NewNote");
        public static string NoteName => L("NoteName");
        public static string EditNote => L("EditNote");
        public static string NoteHint => L("NoteHint");
        public static string FormatBoldShort => L("FormatBoldShort");
        public static string FormatItalicShort => L("FormatItalicShort");
        public static string FormatUnderlineShort => L("FormatUnderlineShort");
        public static string FormatBold => L("FormatBold");
        public static string FormatItalic => L("FormatItalic");
        public static string FormatUnderline => L("FormatUnderline");
        public static string FormatStrike => L("FormatStrike");
        public static string FormatHighlight(string color) => L("FormatHighlight", color);
        public static string FormatNoHighlight => L("FormatNoHighlight");
        public static string MarkerYellow => L("MarkerYellow");
        public static string MarkerGreen => L("MarkerGreen");
        public static string MarkerBlue => L("MarkerBlue");
        public static string MarkerPink => L("MarkerPink");
        public static string MarkerOrange => L("MarkerOrange");
        public static string MarkerRed => L("MarkerRed");
        public static string PriorityHigh => L("PriorityHigh");
        public static string PriorityMedium => L("PriorityMedium");
        public static string PriorityLow => L("PriorityLow");
        public static string PriorityNone => L("PriorityNone");
        public static string ChecklistMenu => L("ChecklistMenu");
        public static string NoteDoneNormal => L("NoteDoneNormal");
        public static string NoteDoneBottom => L("NoteDoneBottom");
        public static string NoteDoneHidden => L("NoteDoneHidden");
        public static string ChecklistReset => L("ChecklistReset");
        public static string ChecklistResetNever => L("ChecklistResetNever");
        public static string ChecklistClearNow => L("ChecklistClearNow");
        public static string NoteVersionsMenu => L("NoteVersionsMenu");
        public static string NoteVersionsTitle(string name) => L("NoteVersionsTitle", name);
        public static string NoteVersionsHint => L("NoteVersionsHint");
        public static string NoteVersionRestore => L("NoteVersionRestore");
        public static string NoteVersionLocked => L("NoteVersionLocked");
        public static string NoteExportMenu => L("NoteExportMenu");
        public static string NoteExportMarkdown => L("NoteExportMarkdown");
        public static string NoteExportPdf => L("NoteExportPdf");
        public static string NotePrint => L("NotePrint");
        public static string TemplateSave => L("TemplateSave");
        public static string TemplateNamePrompt => L("TemplateNamePrompt");
        public static string TemplateSaved(string name) => L("TemplateSaved", name);
        public static string TemplateRemove => L("TemplateRemove");
        public static string NoteFromTemplate => L("NoteFromTemplate");
        public static string TemplateShoppingName => L("TemplateShoppingName");
        public static string TemplateShoppingText => L("TemplateShoppingText");
        public static string TemplateWeekName => L("TemplateWeekName");
        public static string TemplateWeekText => L("TemplateWeekText");
        public static string TemplateMeetingName => L("TemplateMeetingName");
        public static string TemplateMeetingText => L("TemplateMeetingText");
        public static string TemplatePackingName => L("TemplatePackingName");
        public static string TemplatePackingText => L("TemplatePackingText");
        public static string TemplateRoutineName => L("TemplateRoutineName");
        public static string TemplateRoutineText => L("TemplateRoutineText");
        public static string NoteDoneName(NoteDoneMode mode) => mode switch
        {
            NoteDoneMode.Bottom => NoteDoneBottom,
            NoteDoneMode.Hidden => NoteDoneHidden,
            _ => NoteDoneNormal
        };
        public static string MarkerColorName(char color) => color switch
        {
            'g' => MarkerGreen,
            'b' => MarkerBlue,
            'p' => MarkerPink,
            'o' => MarkerOrange,
            'r' => MarkerRed,
            _ => MarkerYellow
        };
        public static string PriorityName(int level) => level switch
        {
            3 => PriorityHigh,
            2 => PriorityMedium,
            1 => PriorityLow,
            _ => PriorityNone
        };

        #endregion

        #region Settings dialogs

        public static string Name => L("Name");
        public static string Folder => L("Folder");
        public static string Browse => L("Browse");
        public static string TitleHeight => L("TitleHeight");
        public static string IconSize => L("IconSize");
        public static string Background => L("Background");
        public static string Opacity => L("Opacity");
        public static string Ok => L("Ok");
        public static string Cancel => L("Cancel");
        public static string Close => L("Close");
        public static string Kind => L("Kind");
        public static string KindLinks => L("KindLinks");
        public static string KindFolder => L("KindFolder");
        public static string KindNote => L("KindNote");
        public static string KindWidget => L("KindWidget");
        public static string Preview => L("Preview");
        public static string SectionGeneral => L("SectionGeneral");
        public static string SectionAppearance => L("SectionAppearance");
        public static string SectionBehavior => L("SectionBehavior");
        public static string SectionAutoSort => L("SectionAutoSort");
        public static string SectionDesktop => L("SectionDesktop");
        public static string SectionUpdates => L("SectionUpdates");
        public static string SectionData => L("SectionData");
        public static string SettingsTitle => L("SettingsTitle");
        public static string LanguageLabel => L("LanguageLabel");
        public static string OwnTranslations => L("OwnTranslations");
        public static string LanguageFileErrors(string errors) => L("LanguageFileErrors", errors);
        public static string LanguageFolderReadme(string template) => L("LanguageFolderReadme", template);
        public static string VersionLabel(Version v) => L("VersionLabel", v);

        public static string AutoSort => L("AutoSort");
        public static string AutoSortHint => L("AutoSortHint");
        public static string AddPreset => L("AddPreset");
        public static string PresetImages => L("PresetImages");
        public static string PresetDocuments => L("PresetDocuments");
        public static string PresetArchives => L("PresetArchives");
        public static string PresetInstallers => L("PresetInstallers");
        public static string PresetVideos => L("PresetVideos");
        public static string PresetMusic => L("PresetMusic");
        public static string PresetShortcuts => L("PresetShortcuts");

        #endregion

        #region Desktop, sorting, peek

        public static string AutoSortEnabled => L("AutoSortEnabled");
        public static string SortNow => L("SortNow");
        public static string SortNowNoRules => L("SortNowNoRules");
        public static string SortNowDone(int n) => n == 1
            ? L("SortNowDone")
            : L("SortNowDone.Alt", n);
        public static string DoubleClickToggle => L("DoubleClickToggle");
        public static string PeekMenu => L("PeekMenu");
        public static string PeekHotkey => L("PeekHotkey");
        public static string HotkeyName(string hotkey) => hotkey switch
        {
            "Off" => L("HotkeyName.Off"),
            _ => hotkey.Replace("Ctrl", L("Key.Ctrl")).Replace("Shift", L("Key.Shift")).Replace("Space", L("Key.Space"))
        };
        public static string HotkeyTaken(string hotkey) => L("HotkeyTaken", HotkeyName(hotkey));

        #endregion

        #region Updates, styles, backups, export

        public static string CheckForUpdatesAuto => L("CheckForUpdatesAuto");
        public static string CheckForUpdatesNow => L("CheckForUpdatesNow");
        public static string InstallUpdate(Version v) => L("InstallUpdate", v);
        public static string UpdateAvailable(Version v) => L("UpdateAvailable", v);
        public static string UpdateAvailableManual(Version v) => L("UpdateAvailableManual", v);
        public static string UpToDate(Version v) => L("UpToDate", v);
        public static string UpdateDownloading => L("UpdateDownloading");
        public static string UpdateFailed(string reason) => L("UpdateFailed", reason);
        public static string UpdateCheckFailed => L("UpdateCheckFailed");
        public static string Animations => L("Animations");
        public static string CustomThemes => L("CustomThemes");
        public static string OpenThemesFolder => L("OpenThemesFolder");
        public static string ReloadThemes => L("ReloadThemes");
        public static string ThemesLoaded(int n) => L("ThemesLoaded", n);
        public static string ThemeErrors(string details) => L("ThemeErrors", details);
        public static string RestoreBackup => L("RestoreBackup");
        public static string NoBackups => L("NoBackups");
        public static string ConfirmRestore(DateTime time) => L("ConfirmRestore", time);
        public static string ExportFences => L("ExportFences");
        public static string ImportFences => L("ImportFences");
        public static string ExportFilter => L("ExportFilter");
        public static string ExportDone(int n) => L("ExportDone", n);
        public static string ImportDone(int n) => L("ImportDone", n);
        public static string ImportFailed(string reason) => L("ImportFailed", reason);
        public static string ImportNetworkHeading(int count) => L("ImportNetworkHeading", count);
        public static string ImportNetworkText => L("ImportNetworkText");
        public static string ImportNetworkMore(int count) => L("ImportNetworkMore", count);
        public static string ImportWithoutNetwork => L("ImportWithoutNetwork");
        public static string ImportAnyway => L("ImportAnyway");

        #endregion

        #region Reminders

        public static string Reminder => L("Reminder");
        public static string ReminderTitle => L("ReminderTitle");
        public static string ReminderIn1h => L("ReminderIn1h");
        public static string ReminderTonight => L("ReminderTonight");
        public static string ReminderTomorrow => L("ReminderTomorrow");
        public static string ReminderRemove => L("ReminderRemove");
        public static string ReminderDue(string name) => L("ReminderDue", name);

        #endregion

        #region Widgets

        public static string WidgetClock => L("WidgetClock");
        public static string WidgetCountdown => L("WidgetCountdown");
        public static string CountdownSet => L("CountdownSet");
        public static string CountdownHint => L("CountdownHint");
        public static string CountdownDays(int n) => n == 1
            ? L("CountdownDays")
            : L("CountdownDays.Alt", n);
        public static string CountdownReached => L("CountdownReached");
        public static string CountdownTitleLabel => L("CountdownTitleLabel");
        public static string CountdownDateLabel => L("CountdownDateLabel");
        public static string PlaytimeExeFilter => L("PlaytimeExeFilter");
        public static string PlaytimeToday => L("PlaytimeToday");
        public static string WidgetWeather => L("WidgetWeather");
        public static string WeatherHint => L("WeatherHint");
        public static string WeatherChoose => L("WeatherChoose");
        public static string WeatherUpdateNow => L("WeatherUpdateNow");
        public static string WeatherPlaceLabel => L("WeatherPlaceLabel");
        public static string WeatherSearch => L("WeatherSearch");
        public static string WeatherLoading => L("WeatherLoading");
        public static string WeatherOffline => L("WeatherOffline");
        public static string WeatherNoPlace => L("WeatherNoPlace");
        public static string WeatherCredit => L("WeatherCredit");
        public static string WeatherDetails(double feelsLike, double wind) => L("WeatherDetails", feelsLike, wind);
        public static string WeatherKindName(Widgets.WeatherKind kind) => kind switch
        {
            Widgets.WeatherKind.Clear => L("WeatherKindName.Clear"),
            Widgets.WeatherKind.PartlyCloudy => L("WeatherKindName.PartlyCloudy"),
            Widgets.WeatherKind.Cloudy => L("WeatherKindName.Cloudy"),
            Widgets.WeatherKind.Fog => L("WeatherKindName.Fog"),
            Widgets.WeatherKind.Drizzle => L("WeatherKindName.Drizzle"),
            Widgets.WeatherKind.Rain => L("WeatherKindName.Rain"),
            Widgets.WeatherKind.Snow => L("WeatherKindName.Snow"),
            _ => L("WeatherKindName.Other")
        };
        public static string WidgetMedia => L("WidgetMedia");
        public static string MediaNothing => L("MediaNothing");
        public static string WidgetNetwork => L("WidgetNetwork");
        public static string WidgetClipboard => L("WidgetClipboard");
        public static string ClipboardHint => L("ClipboardHint");
        public static string ClipboardClear => L("ClipboardClear");
        public static string WidgetBattery => L("WidgetBattery");
        public static string BatteryNone => L("BatteryNone");
        public static string BatteryCharging => L("BatteryCharging");
        public static string BatteryPlugged => L("BatteryPlugged");
        public static string BatteryOnBattery => L("BatteryOnBattery");
        public static string BatteryLeft(string time) => L("BatteryLeft", time);
        public static string RecycleItems(long n, string size) => n == 1
            ? L("RecycleItems", size)
            : L("RecycleItems.Alt", n, size);
        public static string FpsRemoved => L("FpsRemoved");
        public static string WidgetsRetired(string names) => L("WidgetsRetired", names);

        #endregion

        #region More widgets

        public static string WidgetTimer => L("WidgetTimer");
        public static string TimerHint => L("TimerHint");
        public static string TimerDone => L("TimerDone");
        public static string TimerNew => L("TimerNew");
        public static string TimerPrompt => L("TimerPrompt");
        public static string TimerStopSound => L("TimerStopSound");
        public static string TimerRinging(string what) => L("TimerRinging", what);
        public static string AlarmsHeading => L("AlarmsHeading");
        public static string AlarmNew => L("AlarmNew");
        public static string AlarmDelete => L("AlarmDelete");
        public static string AlarmOnce => L("AlarmOnce");
        public static string AlarmTimeLabel => L("AlarmTimeLabel");
        public static string WidgetHabits => L("WidgetHabits");
        public static string HabitsHint => L("HabitsHint");
        public static string HabitAdd => L("HabitAdd");
        public static string HabitPrompt => L("HabitPrompt");
        public static string HabitDelete(string name) => L("HabitDelete", name);
        public static string HabitStreak(int days) => L("HabitStreak", days);
        public static string WidgetProgress => L("WidgetProgress");
        public static string ProgressDay => L("ProgressDay");
        public static string ProgressWeek(int week) => L("ProgressWeek", week);
        public static string SectionBreaks => L("SectionBreaks");
        public static string BreakEvery => L("BreakEvery");
        public static string BreakTextLabel => L("BreakTextLabel");
        public static string BreakDefaultText => L("BreakDefaultText");
        public static string BreakHint => L("BreakHint");
        public static string BreakReminder(int minutes, string text) => L("BreakReminder", minutes, text);
        public static string WidgetGameNews =>L("WidgetGameNews");
        public static string GameNewsNone => L("GameNewsNone");

        public static string WidgetAgenda => L("WidgetAgenda");
        public static string AgendaHint => L("AgendaHint");
        public static string AgendaPrompt => L("AgendaPrompt");
        public static string AgendaSet => L("AgendaSet");
        public static string AgendaFailed => L("AgendaFailed");
        public static string AgendaEmpty => L("AgendaEmpty");
        public static string AgendaTomorrow => L("AgendaTomorrow");
        public static string AgendaAllDay => L("AgendaAllDay");

        public static string WidgetPhotos => L("WidgetPhotos");
        public static string PhotosHint => L("PhotosHint");
        public static string PhotosNone => L("PhotosNone");
        public static string PhotosChoose => L("PhotosChoose");
        public static string PhotosInterval => L("PhotosInterval");
        public static string PhotosShowFile => L("PhotosShowFile");

        public static string WidgetFocus => L("WidgetFocus");
        public static string FocusStart => L("FocusStart");
        public static string FocusPause => L("FocusPause");
        public static string FocusReset => L("FocusReset");
        public static string FocusRound(int n) => L("FocusRound", n);
        public static string FocusShortBreak => L("FocusShortBreak");
        public static string FocusLongBreak => L("FocusLongBreak");
        public static string FocusBreak(int minutes) => L("FocusBreak", minutes);
        public static string FocusBackToWork => L("FocusBackToWork");
        public static string FocusTiming => L("FocusTiming");
        public static string FocusPreset(int focus, int shortBreak, int longBreak) => L("FocusPreset", focus, shortBreak, longBreak);
        public static string FocusProfileMenu => L("FocusProfileMenu");
        public static string FocusProfileNone => L("FocusProfileNone");
        public static string FocusProfileHint => L("FocusProfileHint");
        public static string FocusSkip =>L("FocusSkip");

        public static string WidgetNews => L("WidgetNews");
        public static string NewsHint => L("NewsHint");
        public static string NewsPrompt => L("NewsPrompt");
        public static string NewsFailed => L("NewsFailed");
        public static string NewsNotAFeed(string host) => L("NewsNotAFeed", host);
        public static string NewsCheckLinks => L("NewsCheckLinks");
        public static string NewsBroken(string host) => L("NewsBroken", host);
        public static string NewsUnreachable(string host) => L("NewsUnreachable", host);
        public static string NewsSet => L("NewsSet");

        public static string WidgetWorldClock => L("WidgetWorldClock");
        public static string WorldClockHint => L("WorldClockHint");
        public static string WorldClockSet => L("WorldClockSet");
        public static string WorldClockAdd => L("WorldClockAdd");
        public static string WorldClockZone => L("WorldClockZone");
        public static string WorldClockLabel => L("WorldClockLabel");
        public static string WorldClockYesterday => L("WorldClockYesterday");
        public static string WidgetTodo =>L("WidgetTodo");
        public static string TodoHint => L("TodoHint");
        public static string TodoAdd => L("TodoAdd");
        public static string TodoEdit(string text) => L("TodoEdit", Short(text));
        public static string TodoDelete => L("TodoDelete");
        public static string TodoClearDone => L("TodoClearDone");
        public static string TodoTextLabel => L("TodoTextLabel");
        public static string TodoDueLabel => L("TodoDueLabel");
        public static string TodoDue(string fence, string items) => L("TodoDue", fence, items);
        private static string Short(string text) => text.Length <= 24 ? text : text[..23] + "…";
        public static string RepeatLabel => L("RepeatLabel");
        public static string RepeatName(Repeat repeat) => repeat switch
        {
            Repeat.Daily => L("RepeatName.Daily"),
            Repeat.Weekdays => L("RepeatName.Weekdays"),
            Repeat.Weekly => L("RepeatName.Weekly"),
            Repeat.Monthly => L("RepeatName.Monthly"),
            _ => L("RepeatName.Other")
        };
        public static string WidgetStatus =>L("WidgetStatus");
        public static string StatusSet => L("StatusSet");
        public static string StatusPrompt => L("StatusPrompt");
        public static string StatusUnknown => L("StatusUnknown");
        public static string StatusLevelName(Widgets.ServiceLevel level) => level switch
        {
            Widgets.ServiceLevel.Ok => L("StatusLevelName.Ok"),
            Widgets.ServiceLevel.Notice => L("StatusLevelName.Notice"),
            Widgets.ServiceLevel.Degraded => L("StatusLevelName.Degraded"),
            Widgets.ServiceLevel.Down => L("StatusLevelName.Down"),
            _ => StatusUnknown
        };
        public static string WidgetAudio =>L("WidgetAudio");
        public static string AudioNone => L("AudioNone");
        public static string AudioMuted => L("AudioMuted");
        public static string AudioMuteTip => L("AudioMuteTip");
        public static string AudioMicTip => L("AudioMicTip");
        public static string AudioSettings => L("AudioSettings");
        public static string WidgetScreenTime =>L("WidgetScreenTime");
        public static string ScreenTimeWeek => L("ScreenTimeWeek");
        public static string ScreenTimeHint => L("ScreenTimeHint");
        public static string WidgetTicker =>L("WidgetTicker");
        public static string TickerFailed => L("TickerFailed");
        public static string TickerSet =>L("TickerSet");
        public static string TickerPrompt => L("TickerPrompt");

        #endregion

        #region Sync

        public static string SectionSync => L("SectionSync");
        public static string SyncHint => L("SyncHint");
        public static string SyncChoose => L("SyncChoose");
        public static string SyncChooseTitle => L("SyncChooseTitle");
        public static string SyncExistingQuestion => L("SyncExistingQuestion");
        public static string SyncActive(string folder) => L("SyncActive", folder);
        public static string SyncStop => L("SyncStop");
        public static string SyncStopQuestion => L("SyncStopQuestion");
        public static string SyncReloaded => L("SyncReloaded");

        #endregion

        #region Automation and search

        public static string ProfileSwitchedAuto(string name) => L("ProfileSwitchedAuto", name);
        public static string SectionAutomation => L("SectionAutomation");
        public static string SectionProfileRules => L("SectionProfileRules");
        public static string RulesHint => L("RulesHint");
        public static string RuleAdd => L("RuleAdd");
        public static string RuleEdit => L("RuleEdit");
        public static string RuleRemove => L("RuleRemove");
        public static string RuleTitle => L("RuleTitle");
        public static string RuleProfile => L("RuleProfile");
        public static string RuleByProgram => L("RuleByProgram");
        public static string RuleByTime => L("RuleByTime");
        public static string RuleFrom => L("RuleFrom");
        public static string RuleTo => L("RuleTo");
        public static string RuleWhileRunning(string profile, string program) => L("RuleWhileRunning", profile, program);
        public static string RuleAtTimes(string profile, string days, string from, string to) => $"{profile} – {days}  {from}–{to}";
        public static string SectionFullscreen => L("SectionFullscreen");
        public static string HideOnFullscreen => L("HideOnFullscreen");
        public static string HideOnFullscreenHint => L("HideOnFullscreenHint");
        public static string SectionAutoTheme => L("SectionAutoTheme");
        public static string AutoThemeLabel => L("AutoThemeLabel");
        public static string AutoThemeModeName(AutoThemeMode mode) => mode switch
        {
            AutoThemeMode.Windows => L("AutoThemeModeName.Windows"),
            AutoThemeMode.Time => L("AutoThemeModeName.Time"),
            _ => L("AutoThemeModeName.Other")
        };
        public static string LightThemeLabel => L("LightThemeLabel");
        public static string DarkThemeLabel => L("DarkThemeLabel");
        public static string DarkTimesLabel => L("DarkTimesLabel");
        public static string AutoThemeHint => L("AutoThemeHint");
        public static string ThemeGlobalAutoHint => L("ThemeGlobalAutoHint");
        public static string SectionSearch => L("SectionSearch");
        public static string SearchMenu => L("SearchMenu");
        public static string SearchHotkeyLabel => L("SearchHotkeyLabel");
        public static string SearchHint => L("SearchHint");
        public static string SearchPlaceholder => L("SearchPlaceholder");
        public static string SearchFooter(int n) => L("SearchFooter", n);
        public static string SearchNothing => L("SearchNothing");
        public static string SearchKeys => L("SearchKeys");
        public static string SearchWindowsSettings => L("SearchWindowsSettings");
        public static string SearchApp => L("SearchApp");
        public static string SearchCopyResult => L("SearchCopyResult");

        /// <summary>Searchable Windows settings pages: name (with a few extra words to find it by) and address.</summary>
        public static IEnumerable<(string Name, string Uri)> SettingsPageNames() => new[]
        {
            (L("SettingsPageNames"), "ms-settings:display"),
            (L("SettingsPageNames.2"), "ms-settings:sound"),
            (L("SettingsPageNames.3"), "ms-settings:bluetooth"),
            (L("SettingsPageNames.4"), "ms-settings:network-wifi"),
            (L("SettingsPageNames.5"), "ms-settings:network"),
            (L("SettingsPageNames.6"), "ms-settings:windowsupdate"),
            (L("SettingsPageNames.7"), "ms-settings:appsfeatures"),
            (L("SettingsPageNames.8"), "ms-settings:startupapps"),
            (L("SettingsPageNames.9"), "ms-settings:defaultapps"),
            (L("SettingsPageNames.10"), "ms-settings:personalization-background"),
            (L("SettingsPageNames.11"), "ms-settings:colors"),
            (L("SettingsPageNames.12"), "ms-settings:taskbar"),
            (L("SettingsPageNames.13"), "ms-settings:notifications"),
            (L("SettingsPageNames.14"), "ms-settings:powersleep"),
            (L("SettingsPageNames.15"), "ms-settings:storagesense"),
            (L("SettingsPageNames.16"), "ms-settings:mousetouchpad"),
            (L("SettingsPageNames.17"), "ms-settings:regionlanguage"),
            (L("SettingsPageNames.18"), "ms-settings:dateandtime"),
            (L("SettingsPageNames.19"), "ms-settings:gaming-gamemode"),
            (L("SettingsPageNames.20"), "ms-settings:display-advancedgraphics"),
            (L("SettingsPageNames.21"), "ms-settings:privacy"),
            (L("SettingsPageNames.22"), "windowsdefender:"),
            (L("SettingsPageNames.23"), "ms-settings:printers"),
            (L("SettingsPageNames.24"), "ms-settings:yourinfo"),
            (L("SettingsPageNames.25"), "ms-settings:about"),
        };

        public static string SearchInFence(string fence) => L("SearchInFence", fence);
        public static string SearchInNote(string fence) => L("SearchInNote", fence);

        #endregion

        #region About

        public static string AboutTagline => L("AboutTagline");
        public static string AboutSource => L("AboutSource");
        public static string AboutCredits => L("AboutCredits");
        public static string Donate => L("Donate");
        public static string DonateHint => L("DonateHint");
        public static string AboutLicense => L("AboutLicense");

        #endregion
    }
}
