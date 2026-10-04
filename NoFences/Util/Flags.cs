using System.Drawing.Drawing2D;
using Svg;

namespace NoFences.Util
{
    /// <summary>
    /// Small flags for the language choice (Windows' emoji font has no country flags). The flags are
    /// the SVGs of flag-icons (MIT, Flags\LICENSE.txt), embedded for every country, so own language
    /// files get a flag too. "auto" and languages without a country get a globe.
    /// </summary>
    public static class Flags
    {
        private static readonly Dictionary<(string, int), Bitmap> Cache = new();

        /// <summary>The usual country of a language whose code is not a country code.</summary>
        private static readonly Dictionary<string, string> CountryOfLanguage = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "gb", ["sv"] = "se", ["da"] = "dk", ["nb"] = "no", ["nn"] = "no", ["cs"] = "cz", ["sl"] = "si",
            ["sr"] = "rs", ["bs"] = "ba", ["el"] = "gr", ["uk"] = "ua", ["be"] = "by", ["et"] = "ee", ["ga"] = "ie",
            ["cy"] = "gb-wls", ["eu"] = "es-pv", ["ca"] = "es-ct", ["gl"] = "es-ga", ["sq"] = "al", ["ja"] = "jp",
            ["zh"] = "cn", ["ko"] = "kr", ["vi"] = "vn", ["ms"] = "my", ["hi"] = "in", ["bn"] = "bd", ["ur"] = "pk",
            ["fa"] = "ir", ["ar"] = "sa", ["he"] = "il", ["ka"] = "ge", ["hy"] = "am", ["kk"] = "kz", ["sw"] = "ke",
            ["af"] = "za", ["tl"] = "ph", ["fil"] = "ph", ["lb"] = "lu", ["ta"] = "lk", ["ne"] = "np", ["km"] = "kh",
            ["lo"] = "la", ["my"] = "mm", ["mn"] = "mn", ["am"] = "et", ["fy"] = "nl", ["rm"] = "ch", ["mi"] = "nz",
            ["eo"] = "eu", ["la"] = "va"
        };

        /// <summary>The flag file (country code) for a language: "_flag" in its language file, its region, or its usual country.</summary>
        public static string? CountryFor(string language)
        {
            if (language == "auto")
                return null;
            var own = Strings.TextsOf(language).GetValueOrDefault("_flag");
            var candidates = new[]
            {
                own,
                language.Contains('-') ? language[(language.IndexOf('-') + 1)..] : null,
                CountryOfLanguage.GetValueOrDefault(language),
                language
            };
            return candidates.Where(c => c != null).Select(c => c!.ToLowerInvariant()).FirstOrDefault(Exists);
        }

        public static bool Exists(string country) => typeof(Flags).Assembly.GetManifestResourceInfo($"Flag.{country}.svg") != null;

        /// <summary>A flag <paramref name="height"/> pixels high (3:2); cached, don't dispose.</summary>
        public static Bitmap For(string language, int height = 12)
        {
            // Cached per country: an own language file may change its "_flag"
            var country = CountryFor(language);
            if (Cache.TryGetValue((country ?? "", height), out var cached))
                return cached;
            var width = height * 3 / 2;
            var bitmap = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                if (country != null && Draw(g, country, width, height))
                {
                    using var border = new Pen(Color.FromArgb(90, 0, 0, 0));
                    g.SmoothingMode = SmoothingMode.None;
                    g.DrawRectangle(border, 0, 0, width - 1, height - 1);
                }
                else
                {
                    Globe(g, new RectangleF((width - height) / 2f, 0, height, height));
                }
            }
            Cache[(country ?? "", height)] = bitmap;
            return bitmap;
        }

        /// <summary>Renders the country's SVG flag to fill the bitmap.</summary>
        internal static bool Draw(Graphics g, string country, int width, int height)
        {
            try
            {
                using var stream = typeof(Flags).Assembly.GetManifestResourceStream($"Flag.{country}.svg");
                if (stream == null)
                    return false;
                var svg = SvgDocument.Open<SvgDocument>(stream);
                svg.AspectRatio = new SvgAspectRatio(SvgPreserveAspectRatio.none);
                svg.Width = width;
                svg.Height = height;
                using var image = svg.Draw(width, height);
                g.DrawImage(image, 0, 0, width, height);
                return true;
            }
            catch (Exception e)
            {
                Log.Write("Flags", $"{country}: {Log.Describe(e)}");
                return false;
            }
        }

        private static void Globe(Graphics g, RectangleF r)
        {
            r.Inflate(-0.5f, -0.5f);
            var blue = Color.FromArgb(0, 120, 212);
            using var pen = new Pen(blue, Math.Max(1, r.Height / 12));
            g.DrawEllipse(pen, r);
            g.DrawEllipse(pen, r.X + r.Width * 0.3f, r.Y, r.Width * 0.4f, r.Height);
            g.DrawLine(pen, r.Left, r.Y + r.Height / 2, r.Right, r.Y + r.Height / 2);
            g.DrawLine(pen, r.X + r.Width / 2, r.Top, r.X + r.Width / 2, r.Bottom);
        }
    }
}
