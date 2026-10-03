using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using NoFences.Model;

namespace NoFences.Themes
{
    /// <summary>
    /// A user style defined in a JSON file in the "themes" folder: colors, fonts, border and corners.
    /// Colors are "#RRGGBB" or "#RRGGBBAA".
    /// </summary>
    public sealed class JsonTheme : FenceTheme
    {
        public sealed class Definition
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string Background { get; set; } = "#1E1E2E";
            public int BackgroundAlpha { get; set; } = 180;
            public bool Glass { get; set; } = true;
            public string TitleBackground { get; set; } = "#00000050";
            public string TitleColor { get; set; } = "#FFFFFF";
            public string TitleFont { get; set; } = "Segoe UI Semibold";
            public string TitleAlign { get; set; } = "center";
            public bool TitleUppercase { get; set; }
            public string LabelColor { get; set; } = "#FFFFFF";
            public string LabelShadow { get; set; } = "#000000B0";
            public string LabelFont { get; set; } = "Segoe UI";
            public string Accent { get; set; } = "#89B4FA";
            public string Border { get; set; } = "#00000000";
            public float BorderWidth { get; set; } = 1;
            public int CornerRadius { get; set; } = 8;
        }

        private readonly Definition d;
        private readonly Color background, titleBackground, titleColor, labelColor, labelShadow, accent, border;

        private JsonTheme(Definition definition)
        {
            d = definition;
            background = ParseColor(d.Background);
            titleBackground = ParseColor(d.TitleBackground);
            titleColor = ParseColor(d.TitleColor);
            labelColor = ParseColor(d.LabelColor);
            labelShadow = ParseColor(d.LabelShadow);
            accent = ParseColor(d.Accent);
            border = ParseColor(d.Border);
        }

        public override string Id => d.Id;

        public override string DisplayName => d.Name + " ★";


        public override Color Accent => accent;

        public override bool Glass => d.Glass;

        public override int CornerPreference => d.CornerRadius <= 0 ? 1 : d.CornerRadius <= 4 ? 3 : 2;

        public override int MinAlpha => 0;

        public override Color HintColor => Color.FromArgb(150, labelColor);

        public override (Color Back, Color Fore) EditorColors => (Color.FromArgb(255, background), Color.FromArgb(255, labelColor));

        public override Font CreateTitleFont(int titleHeightPx) => CreateFont(new[] { d.TitleFont }, Math.Max(6, titleHeightPx * 0.44f), FontStyle.Regular, GraphicsUnit.Pixel);

        public override Font CreateLabelFont(float s) => CreateFont(new[] { d.LabelFont }, 12f * s, FontStyle.Regular, GraphicsUnit.Pixel);

        public override string FormatTitle(string title) => d.TitleUppercase ? title.ToUpperInvariant() : title;

        public override void DrawFrame(Graphics g, Rectangle bounds, int titleHeight, FenceInfo info, float s)
        {
            var alpha = Math.Clamp(info.BackgroundAlpha == 100 ? d.BackgroundAlpha : info.BackgroundAlpha, 0, 255);
            using (var bg = new SolidBrush(Color.FromArgb(alpha, background)))
                g.FillRectangle(bg, bounds);
            using (var title = new SolidBrush(titleBackground))
                g.FillRectangle(title, bounds.X, bounds.Y, bounds.Width, titleHeight);
            if (border.A > 0 && d.BorderWidth > 0)
            {
                using var pen = new Pen(border, d.BorderWidth * s);
                var half = d.BorderWidth * s / 2;
                g.DrawRectangle(pen, bounds.X + half, bounds.Y + half, bounds.Width - 2 * half - 1, bounds.Height - 2 * half - 1);
            }
        }

        public override void DrawTitle(Graphics g, Rectangle titleRect, string text, Font font, float s)
        {
            var align = d.TitleAlign.Equals("left", StringComparison.OrdinalIgnoreCase) ? StringAlignment.Near : StringAlignment.Center;
            using var format = TitleFormat(align);
            DrawPlainString(g, text, font, titleColor, RectangleF.Inflate(titleRect, -10 * s, 0), format);
        }

        public override void DrawItemBackground(Graphics g, Rectangle rect, bool hover, bool selected, float s)
        {
            if (!hover && !selected)
                return;
            FillRounded(g, Color.FromArgb(selected ? 90 : 50, accent), rect, Math.Max(0, d.CornerRadius) * s * 0.6f);
            if (selected)
                DrawRounded(g, Color.FromArgb(200, accent), 1, rect, Math.Max(0, d.CornerRadius) * s * 0.6f);
        }

        public override void DrawLabel(Graphics g, string text, RectangleF rect, Font font, StringFormat format, float s) =>
            DrawShadowedString(g, text, font, labelColor, labelShadow, rect, format, labelShadow.A > 0 ? s : 0);

        public override void DrawScrollbar(Graphics g, Rectangle track, Rectangle thumb, float s) =>
            DrawPillThumb(g, track, thumb, Color.FromArgb(200, accent), s);

        public override void DrawInsertMarker(Graphics g, int x, int top, int height, float s) =>
            DrawBarMarker(g, accent, x, top, height, 2 * s);

        public static Color ParseColor(string value)
        {
            var hex = value.Trim().TrimStart('#');
            if (hex.Length is not (6 or 8) || !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v))
                throw new FormatException($"Invalid color \"{value}\" (use #RRGGBB or #RRGGBBAA).");
            return hex.Length == 6
                ? Color.FromArgb(255, (int)(v >> 16) & 0xFF, (int)(v >> 8) & 0xFF, (int)v & 0xFF)
                : Color.FromArgb((int)v & 0xFF, (int)(v >> 24) & 0xFF, (int)(v >> 16) & 0xFF, (int)(v >> 8) & 0xFF);
        }

        private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

        public static JsonTheme Parse(string json, string fallbackId)
        {
            var def = JsonSerializer.Deserialize<Definition>(json, Options) ?? new Definition();
            if (string.IsNullOrWhiteSpace(def.Id))
                def.Id = fallbackId;
            if (string.IsNullOrWhiteSpace(def.Name))
                def.Name = def.Id;
            return new JsonTheme(def);
        }

        /// <summary>Loads all *.json styles from the folder; returns the styles and the files that failed.</summary>
        public static (List<FenceTheme> Themes, List<string> Errors) LoadFolder(string folder)
        {
            var themes = new List<FenceTheme>();
            var errors = new List<string>();
            if (!Directory.Exists(folder))
                return (themes, errors);
            foreach (var file in Directory.EnumerateFiles(folder, "*.json"))
            {
                try
                {
                    themes.Add(Parse(File.ReadAllText(file), "custom-" + Path.GetFileNameWithoutExtension(file).ToLowerInvariant()));
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Style {file}: {e.Message}");
                    errors.Add($"{Path.GetFileName(file)}: {e.Message}");
                }
            }
            return (themes, errors);
        }

        /// <summary>Written once so users have something to copy and change.</summary>
        public const string ExampleJson = """
            {
              // Copy this file, change the colors and pick the style in NoFences (tray -> Default style, or a fence's menu).
              // Colors: "#RRGGBB" or "#RRGGBBAA" (AA = opacity, 00 = invisible, FF = solid).
              "id": "example-mocha",
              "name": "Beispiel Mocha",
              "background": "#1E1E2E",
              "backgroundAlpha": 190,
              "glass": true,
              "titleBackground": "#11111B90",
              "titleColor": "#CBA6F7",
              "titleFont": "Segoe UI Semibold",
              "titleAlign": "left",
              "titleUppercase": false,
              "labelColor": "#CDD6F4",
              "labelShadow": "#000000A0",
              "labelFont": "Segoe UI",
              "accent": "#89B4FA",
              "border": "#CBA6F780",
              "borderWidth": 1,
              "cornerRadius": 8
            }
            """;
    }
}
