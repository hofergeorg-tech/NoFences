using System.Reflection;
using NoFences.Util;

namespace NoFences.Tests
{
    /// <summary>
    /// These tests switch the UI language for a moment. Other tests expect fixed texts, so these run on
    /// their own, after the parallel ones.
    /// </summary>
    [CollectionDefinition(nameof(LanguageSwitching), DisableParallelization = true)]
    public class LanguageSwitching
    {
    }

    [Collection(nameof(LanguageSwitching))]
    public class LanguageTests
    {
        private static IEnumerable<PropertyInfo> TextProperties() =>
            typeof(Strings).GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(string) && p.Name is not ("Language" or "Effective" or "LanguageFolder"));

        /// <summary>A sample value per parameter type, to call every text method once.</summary>
        private static object? Sample(Type type) =>
            type == typeof(string) ? "x"
            : type == typeof(int) ? 2
            : type == typeof(long) ? 2L
            : type == typeof(double) ? 2.5
            : type == typeof(float) ? 2.5f
            : type == typeof(bool) ? true
            : type == typeof(DateTime) ? new DateTime(2026, 10, 4, 14, 30, 0)
            : type == typeof(TimeSpan) ? TimeSpan.FromMinutes(90)
            : type.IsEnum ? Enum.GetValues(type).GetValue(0)
            : type.IsValueType ? Activator.CreateInstance(type)
            : null;

        private static void CallEveryText()
        {
            foreach (var p in TextProperties())
                p.GetValue(null);
            var methods = typeof(Strings).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.ReturnType == typeof(string) && !m.IsSpecialName && !m.ContainsGenericParameters
                            && m.Name is not ("BuiltInJson" or "LanguageName"));
            foreach (var m in methods)
            {
                try
                {
                    m.Invoke(null, m.GetParameters().Select(p => Sample(p.ParameterType)).ToArray());
                }
                catch (TargetInvocationException)
                {
                    // A sample value the method doesn't accept; its texts are checked in the other languages
                }
            }
        }

        [Theory]
        [InlineData("en")]
        [InlineData("de")]
        [InlineData("it")]
        [InlineData("fr")]
        [InlineData("es")]
        public void EveryLanguage_HasAllTextsAndDocuments(string language)
        {
            var before = Strings.Language;
            try
            {
                Strings.Language = language;
                Assert.Equal(language, Strings.Effective);
                foreach (var p in TextProperties())
                    Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(null)), $"{p.Name} is empty in {language}");

                var assembly = typeof(Strings).Assembly;
                Assert.NotNull(assembly.GetManifestResourceStream(Strings.HelpDocument));
                Assert.NotNull(assembly.GetManifestResourceStream(Strings.ChangelogDocument));
            }
            finally
            {
                Strings.Language = before;
            }
        }

        [Fact]
        public void EveryKeyUsedInTheCode_HasAnEnglishText()
        {
            lock (Strings.MissingKeys)
                Strings.MissingKeys.Clear();
            CallEveryText();
            Assert.Empty(Strings.MissingKeys);
        }

        [Theory]
        [InlineData("de")]
        [InlineData("it")]
        [InlineData("fr")]
        [InlineData("es")]
        public void EveryLanguageFile_HasAllKeysWithTheSamePlaceholders(string language)
        {
            var english = Strings.TextsOf("en");
            var texts = Strings.TextsOf(language);
            Assert.Empty(english.Keys.Except(texts.Keys));
            Assert.Empty(texts.Keys.Except(english.Keys));
            foreach (var (key, text) in english)
            {
                Assert.False(string.IsNullOrWhiteSpace(texts[key]), $"{key} is empty in {language}");
                Assert.True(Strings.Placeholders(text).SetEquals(Strings.Placeholders(texts[key])), $"{key}: placeholders differ in {language}: \"{texts[key]}\"");
            }
        }

        [Fact]
        public void OwnLanguageFiles_ReplaceTextsAndAddLanguages()
        {
            var before = Strings.Language;
            using var dir = new TempFolder();
            try
            {
                dir.File("de.json", """{ "Help": "Hilfe!" }""");
                dir.File("nl.json", """{ "_language": "Nederlands", "Help": "Help mij", "Exit": "Afsluiten" }""");
                dir.File("TEMPLATE.en.json", """{ "Help": "ignored" }""");
                dir.File("xx.json", "{ not json");
                var errors = Strings.LoadFolder(dir.Path);

                Assert.Contains(errors, e => e.StartsWith("xx.json"));
                Assert.Contains("nl", Strings.Languages);
                Assert.Equal("Nederlands", Strings.LanguageName("nl"));
                Strings.Language = "nl";
                Assert.Equal("Help mij", Strings.Help);
                Assert.Equal("Afsluiten", Strings.Exit);
                Assert.Equal("Settings…", Strings.AppSettings); // not translated: English
                Strings.Language = "de";
                Assert.Equal("Hilfe!", Strings.Help);
                Assert.Equal("Beenden", Strings.Exit);
            }
            finally
            {
                Strings.LoadFolder(null);
                Strings.Language = before;
            }
            Assert.DoesNotContain("nl", Strings.Languages);
        }

        [Fact]
        public void BrokenPlaceholderInOwnFile_FallsBackToEnglish()
        {
            var before = Strings.Language;
            using var dir = new TempFolder();
            try
            {
                dir.File("de.json", """{ "LanguageFileErrors": "Kaputt {1" }""");
                Strings.LoadFolder(dir.Path);
                Strings.Language = "de";
                Assert.Equal("Some language files could not be read:\nx", Strings.LanguageFileErrors("x"));
            }
            finally
            {
                Strings.LoadFolder(null);
                Strings.Language = before;
            }
        }

        [Fact]
        public void FrenchAndSpanish_AreReallyTranslated()
        {
            var before = Strings.Language;
            try
            {
                Strings.Language = "fr";
                var fr = Strings.AppSettings;
                Strings.Language = "es";
                var es = Strings.AppSettings;
                Strings.Language = "en";
                Assert.NotEqual(Strings.AppSettings, fr);
                Assert.NotEqual(Strings.AppSettings, es);
            }
            finally
            {
                Strings.Language = before;
            }
        }

        [Fact]
        public void EveryLanguage_HasAFlag()
        {
            foreach (var code in Strings.Languages)
                Assert.Equal(18, Flags.For(code).Width);
        }
    }
}
