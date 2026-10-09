using System.Drawing;
using NoFences.Themes;
using NoFences.Widgets;

namespace NoFences.Tests
{
    public class LookPackageTests
    {
        [Fact]
        public void HighContrast_IsABasicSolidStyle()
        {
            var theme = ThemeRegistry.Get("contrast");
            Assert.IsType<HighContrastTheme>(theme);
            Assert.Equal(ThemeRegistry.Group.Basic, ThemeRegistry.GroupOf(theme));
            Assert.Equal(255, theme.MinAlpha);
            Assert.False(theme.Glass);
            using var font = theme.CreateLabelFont(1);
            Assert.True(font.Size >= 14 && font.Bold);
        }

        [Fact]
        public void Designer_DefinitionRoundTripsThroughJson()
        {
            var def = new JsonTheme.Definition { Id = "mein-blau", Name = "Mein Blau", Background = JsonTheme.Hex(Color.FromArgb(10, 20, 200)), TitleBackground = JsonTheme.Hex(Color.FromArgb(128, 0, 0, 0)), CornerRadius = 12 };
            var theme = JsonTheme.Parse(JsonTheme.ToJson(def), "fallback");
            Assert.Equal("mein-blau", theme.Id);
            Assert.Equal("#0A14C8", theme.Source.Background);
            Assert.Equal("#00000080", theme.Source.TitleBackground);
            Assert.Equal(12, theme.Source.CornerRadius);
        }

        [Theory]
        [InlineData("Mein Blau", "mein-blau")]
        [InlineData("Grün & Gold!", "gruen-gold")]
        [InlineData("  ", "style")]
        public void Designer_IdsFromNames(string name, string id) => Assert.Equal(id, JsonTheme.IdFor(name));

    }
}