using System.Drawing.Drawing2D;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using NoFences.Util;

namespace NoFences.Widgets
{
    public enum WeatherKind { Clear, PartlyCloudy, Cloudy, Fog, Drizzle, Rain, Snow, Thunder }

    public sealed record WeatherPlace(string Name, double Latitude, double Longitude, string Detail = "")
    {
        /// <summary>Stored as "lat|lon|name" in the fence's widget option.</summary>
        public string ToOption() => string.Create(CultureInfo.InvariantCulture, $"{Latitude:0.####}|{Longitude:0.####}|{Name}");

        public static WeatherPlace? FromOption(string? option)
        {
            var parts = option?.Split('|', 3);
            if (parts is not { Length: 3 }
                || !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
                || !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
                return null;
            return new WeatherPlace(parts[2], lat, lon);
        }

        public override string ToString() => Detail.Length > 0 ? $"{Name}, {Detail}" : Name;
    }

    public sealed record WeatherDay(DateTime Date, WeatherKind Kind, double Max, double Min);

    public sealed record WeatherReport(double Temperature, double FeelsLike, double Wind, WeatherKind Kind, bool IsDay, IReadOnlyList<WeatherDay> Days);

    /// <summary>Open-Meteo (free, no API key): place search and the current weather with a short forecast.</summary>
    public static class WeatherService
    {
        private static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd($"NoFences/{UpdateChecker.CurrentVersion}");
            return http;
        }

        /// <summary>WMO weather code → one of the drawn icons.</summary>
        public static WeatherKind KindOf(int code) => code switch
        {
            0 => WeatherKind.Clear,
            1 or 2 => WeatherKind.PartlyCloudy,
            3 => WeatherKind.Cloudy,
            45 or 48 => WeatherKind.Fog,
            >= 51 and <= 57 => WeatherKind.Drizzle,
            (>= 61 and <= 67) or (>= 80 and <= 82) => WeatherKind.Rain,
            (>= 71 and <= 77) or 85 or 86 => WeatherKind.Snow,
            >= 95 => WeatherKind.Thunder,
            _ => WeatherKind.Cloudy
        };

        public static async Task<IReadOnlyList<WeatherPlace>> SearchAsync(string name)
        {
            var lang = Strings.Effective;
            var json = await Http.GetStringAsync($"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(name)}&count=8&language={lang}&format=json");
            return ParsePlaces(json);
        }

        public static IReadOnlyList<WeatherPlace> ParsePlaces(string json)
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("results", out var results))
                return Array.Empty<WeatherPlace>();
            var list = new List<WeatherPlace>();
            foreach (var r in results.EnumerateArray())
            {
                var name = Str(r, "name");
                // "Wien, Österreich" rather than "Wien, Wien, Österreich"
                var detail = string.Join(", ", new[] { Str(r, "admin1"), Str(r, "country") }.Where(s => s.Length > 0 && s != name).Distinct());
                list.Add(new WeatherPlace(name.Replace("|", "/"), r.GetProperty("latitude").GetDouble(), r.GetProperty("longitude").GetDouble(), detail));
            }
            return list;
        }

        private static string Str(JsonElement e, string name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

        public static async Task<WeatherReport> FetchAsync(WeatherPlace place)
        {
            var url = string.Create(CultureInfo.InvariantCulture, $"https://api.open-meteo.com/v1/forecast?latitude={place.Latitude}&longitude={place.Longitude}")
                      + "&current=temperature_2m,apparent_temperature,weather_code,wind_speed_10m,is_day"
                      + "&daily=weather_code,temperature_2m_max,temperature_2m_min&timezone=auto&forecast_days=4";
            return ParseReport(await Http.GetStringAsync(url));
        }

        public static WeatherReport ParseReport(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var current = doc.RootElement.GetProperty("current");
            var daily = doc.RootElement.GetProperty("daily");
            var dates = daily.GetProperty("time").EnumerateArray().ToList();
            var codes = daily.GetProperty("weather_code").EnumerateArray().ToList();
            var max = daily.GetProperty("temperature_2m_max").EnumerateArray().ToList();
            var min = daily.GetProperty("temperature_2m_min").EnumerateArray().ToList();
            var days = new List<WeatherDay>();
            for (var i = 0; i < dates.Count && i < codes.Count && i < max.Count && i < min.Count; i++)
            {
                if (codes[i].ValueKind != JsonValueKind.Number || max[i].ValueKind != JsonValueKind.Number || min[i].ValueKind != JsonValueKind.Number)
                    continue;
                days.Add(new WeatherDay(DateTime.ParseExact(dates[i].GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    KindOf(codes[i].GetInt32()), max[i].GetDouble(), min[i].GetDouble()));
            }
            return new WeatherReport(
                current.GetProperty("temperature_2m").GetDouble(),
                current.GetProperty("apparent_temperature").GetDouble(),
                current.GetProperty("wind_speed_10m").GetDouble(),
                KindOf(current.GetProperty("weather_code").GetInt32()),
                !current.TryGetProperty("is_day", out var day) || day.GetInt32() == 1,
                days);
        }
    }

    /// <summary>Simple weather icons drawn with shapes, so they take the style's colors.</summary>
    public static class WeatherIcon
    {
        public static void Draw(Graphics g, RectangleF r, WeatherKind kind, bool day, Color fg, Color accent)
        {
            var oldMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var w = r.Width;
            var stroke = Math.Max(1.2f, w / 18);
            var sunColor = Color.FromArgb(255, 255, 196, 60);

            switch (kind)
            {
                case WeatherKind.Clear:
                    if (day)
                        Sun(g, new RectangleF(r.X + w * 0.15f, r.Y + w * 0.15f, w * 0.7f, w * 0.7f), sunColor, stroke);
                    else
                        Moon(g, new RectangleF(r.X + w * 0.2f, r.Y + w * 0.2f, w * 0.6f, w * 0.6f), fg);
                    break;
                case WeatherKind.PartlyCloudy:
                    if (day)
                        Sun(g, new RectangleF(r.X + w * 0.38f, r.Y + w * 0.08f, w * 0.52f, w * 0.52f), sunColor, stroke);
                    else
                        Moon(g, new RectangleF(r.X + w * 0.45f, r.Y + w * 0.12f, w * 0.42f, w * 0.42f), fg);
                    Cloud(g, new RectangleF(r.X, r.Y + w * 0.32f, w * 0.82f, w * 0.5f), fg);
                    break;
                case WeatherKind.Fog:
                    Cloud(g, new RectangleF(r.X + w * 0.08f, r.Y + w * 0.1f, w * 0.84f, w * 0.5f), Color.FromArgb(150, fg));
                    using (var pen = new Pen(fg, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        for (var i = 0; i < 3; i++)
                            g.DrawLine(pen, r.X + w * (0.12f + i * 0.06f), r.Y + w * (0.68f + i * 0.1f), r.X + w * (0.88f - i * 0.06f), r.Y + w * (0.68f + i * 0.1f));
                    }
                    break;
                default:
                    Cloud(g, new RectangleF(r.X + w * 0.05f, r.Y + w * 0.1f, w * 0.9f, w * 0.55f), fg);
                    Below(g, r, kind, fg, accent, stroke);
                    break;
            }
            g.SmoothingMode = oldMode;
        }

        private static void Below(Graphics g, RectangleF r, WeatherKind kind, Color fg, Color accent, float stroke)
        {
            var w = r.Width;
            var top = r.Y + w * 0.7f;
            switch (kind)
            {
                case WeatherKind.Drizzle:
                case WeatherKind.Rain:
                    using (var pen = new Pen(accent, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    {
                        var drops = kind == WeatherKind.Rain ? 3 : 2;
                        var length = kind == WeatherKind.Rain ? w * 0.2f : w * 0.1f;
                        for (var i = 0; i < drops; i++)
                        {
                            var x = r.X + w * (0.3f + i * 0.4f / Math.Max(1, drops - 1));
                            if (drops == 2)
                                x = r.X + w * (0.38f + i * 0.24f);
                            g.DrawLine(pen, x, top, x - length * 0.35f, top + length);
                        }
                    }
                    break;
                case WeatherKind.Snow:
                    using (var brush = new SolidBrush(fg))
                    {
                        var d = w * 0.09f;
                        foreach (var (fx, fy) in new[] { (0.28f, 0.0f), (0.5f, 0.1f), (0.72f, 0.0f), (0.39f, 0.18f), (0.61f, 0.18f) })
                            g.FillEllipse(brush, r.X + w * fx - d / 2, top + w * fy, d, d);
                    }
                    break;
                case WeatherKind.Thunder:
                    using (var brush = new SolidBrush(Color.FromArgb(255, 255, 196, 60)))
                    {
                        g.FillPolygon(brush, new[]
                        {
                            new PointF(r.X + w * 0.55f, top - w * 0.05f), new PointF(r.X + w * 0.38f, top + w * 0.16f),
                            new PointF(r.X + w * 0.5f, top + w * 0.16f), new PointF(r.X + w * 0.42f, top + w * 0.3f),
                            new PointF(r.X + w * 0.64f, top + w * 0.08f), new PointF(r.X + w * 0.52f, top + w * 0.08f)
                        });
                    }
                    break;
            }
        }

        private static void Sun(Graphics g, RectangleF r, Color color, float stroke)
        {
            var cx = r.X + r.Width / 2;
            var cy = r.Y + r.Height / 2;
            var core = r.Width * 0.27f;
            using var brush = new SolidBrush(color);
            g.FillEllipse(brush, cx - core, cy - core, core * 2, core * 2);
            using var pen = new Pen(color, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            for (var i = 0; i < 8; i++)
            {
                var a = i * Math.PI / 4;
                float Inner(double f) => (float)f * r.Width * 0.37f;
                float Outer(double f) => (float)f * r.Width * 0.5f;
                g.DrawLine(pen, cx + Inner(Math.Cos(a)), cy + Inner(Math.Sin(a)), cx + Outer(Math.Cos(a)), cy + Outer(Math.Sin(a)));
            }
        }

        private static void Moon(Graphics g, RectangleF r, Color color)
        {
            using var moon = new GraphicsPath();
            moon.AddEllipse(r);
            using var cut = new GraphicsPath();
            cut.AddEllipse(r.X + r.Width * 0.35f, r.Y - r.Height * 0.15f, r.Width, r.Height);
            using var region = new Region(moon);
            region.Exclude(cut);
            using var brush = new SolidBrush(color);
            g.FillRegion(brush, region);
        }

        private static void Cloud(Graphics g, RectangleF r, Color color)
        {
            // Winding, or the overlapping ellipses would cancel each other out
            using var path = new GraphicsPath(FillMode.Winding);
            path.AddEllipse(r.X, r.Y + r.Height * 0.35f, r.Width * 0.45f, r.Height * 0.65f);
            path.AddEllipse(r.X + r.Width * 0.22f, r.Y, r.Width * 0.5f, r.Height * 0.9f);
            path.AddEllipse(r.X + r.Width * 0.5f, r.Y + r.Height * 0.25f, r.Width * 0.5f, r.Height * 0.75f);
            path.AddRectangle(new RectangleF(r.X + r.Width * 0.2f, r.Y + r.Height * 0.6f, r.Width * 0.6f, r.Height * 0.4f));
            using var region = new Region(path);
            using var brush = new SolidBrush(color);
            g.FillRegion(brush, region);
        }
    }
}
