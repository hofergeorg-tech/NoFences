using NoFences.Util;

namespace NoFences.Model
{
    /// <summary>Common Windows settings pages for the search (opened with their ms-settings: address).</summary>
    public static class SettingsPages
    {
        public static IEnumerable<SearchItem> All() =>
            Strings.SettingsPageNames().Select(p => new SearchItem(p.Name, p.Uri, Strings.SearchWindowsSettings, null, SearchKind.Setting));
    }
}
