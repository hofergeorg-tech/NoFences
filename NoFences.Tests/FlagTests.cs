using NoFences.Util;

namespace NoFences.Tests
{
    [Collection(nameof(LanguageSwitching))]
    public class FlagTests
    {
        [Theory]
        [InlineData("en", "gb")]
        [InlineData("de", "de")]
        [InlineData("nl", "nl")]
        [InlineData("sv", "se")]
        [InlineData("pt-BR", "br")]
        [InlineData("ja", "jp")]
        [InlineData("ca", "es-ct")]
        [InlineData("auto", null)]
        [InlineData("qq", null)]
        public void CountryFor_Language(string language, string? country) =>
            Assert.Equal(country, Flags.CountryFor(language));

        [Fact]
        public void EveryEmbeddedFlag_Renders()
        {
            var countries = typeof(Flags).Assembly.GetManifestResourceNames()
                .Where(n => n.StartsWith("Flag.") && n.EndsWith(".svg"))
                .Select(n => n["Flag.".Length..^".svg".Length])
                .ToList();
            Assert.True(countries.Count > 250);
            var broken = new List<string>();
            foreach (var country in countries)
            {
                using var bitmap = new Bitmap(18, 12);
                using (var g = Graphics.FromImage(bitmap))
                {
                    if (!Flags.Draw(g, country, 18, 12))
                    {
                        broken.Add(country);
                        continue;
                    }
                }
                // Something was drawn in the middle (not left transparent)
                if (bitmap.GetPixel(9, 6).A == 0 && bitmap.GetPixel(4, 3).A == 0)
                    broken.Add(country + " (empty)");
            }
            Assert.Empty(broken);
        }

        [Fact]
        public void OwnLanguageFile_CanChooseItsFlag()
        {
            using var dir = new TempFolder();
            try
            {
                dir.File("de-AT.json", """{ "_language": "Österreichisch" }""");
                dir.File("eo.json", """{ "_language": "Esperanto", "_flag": "un" }""");
                Strings.LoadFolder(dir.Path);
                Assert.Equal("at", Flags.CountryFor("de-AT"));
                Assert.Equal("un", Flags.CountryFor("eo"));
            }
            finally
            {
                Strings.LoadFolder(null);
            }
        }
    }
}
