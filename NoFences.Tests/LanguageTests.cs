using System.Reflection;
using NoFences.Util;

namespace NoFences.Tests
{
    public class LanguageTests
    {
        private static IEnumerable<PropertyInfo> TextProperties() =>
            typeof(Strings).GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(string) && p.Name is not ("Language" or "Effective"));

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
