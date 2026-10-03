using System.Globalization;
using System.Text.Json;
using NoFences.Util;

namespace NoFences.Widgets
{
    public sealed record Quote(string Symbol, string Name, double Price, double Change, string Currency, IReadOnlyList<double> Day);

    /// <summary>
    /// Prices of stocks, indices and crypto (Yahoo Finance symbols: AAPL, ^GDAXI, BTC-EUR …) with the
    /// change since the previous close and a small chart of the day. Updated every five minutes.
    /// </summary>
    public sealed class TickerWidget : FenceWidget
    {
        public const string DefaultSymbols = "BTC-EUR\n^GDAXI\n^ATX\nAAPL";
        private static readonly TimeSpan UpdateEvery = TimeSpan.FromMinutes(5);

        private readonly Func<string?> getOption;
        private readonly Action<string?> setOption;
        private List<Quote> quotes = new();
        private string? loadedFor;
        private DateTime nextFetch;
        private bool fetching, failed;

        public TickerWidget(Func<string?> getOption, Action<string?> setOption)
        {
            this.getOption = getOption;
            this.setOption = setOption;
        }

        public override string Type => "ticker";

        public override int RefreshMs => 20_000;

        public static IReadOnlyList<string> Symbols(string? option) =>
            (option ?? DefaultSymbols).Split(new[] { '\n', ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToUpperInvariant()).Distinct().Take(12).ToList();

        internal void SetPreview(IEnumerable<Quote> demo)
        {
            quotes = demo.ToList();
            loadedFor = getOption();
            nextFetch = DateTime.MaxValue;
        }

        public override void Refresh()
        {
            if (PreviewMode)
                return;
            var option = getOption();
            if (option != loadedFor)
                nextFetch = DateTime.MinValue;
            if (fetching || DateTime.UtcNow < nextFetch)
                return;
            _ = FetchAsync(option);
        }

        private async Task FetchAsync(string? option)
        {
            fetching = true;
            try
            {
                var symbols = Symbols(option);
                var tasks = symbols.Select(async s =>
                {
                    try
                    {
                        var json = await Web.Http.GetStringAsync($"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(s)}?range=1d&interval=15m");
                        return ParseChart(json, s);
                    }
                    catch (Exception e)
                    {
                        Log.Write("Prices", $"{s}: {Log.Describe(e)}");
                        return null;
                    }
                }).ToList();
                var results = (await Task.WhenAll(tasks)).OfType<Quote>().ToList();
                // A failed update keeps the prices that are already there
                if (results.Count > 0 || option != loadedFor)
                    quotes = results;
                failed = results.Count == 0 && symbols.Count > 0;
                loadedFor = option;
                nextFetch = DateTime.UtcNow + (failed ? TimeSpan.FromSeconds(30) : UpdateEvery);
                RequestRedraw();
            }
            finally
            {
                fetching = false;
            }
        }

        public static Quote? ParseChart(string json, string symbol)
        {
            using var doc = JsonDocument.Parse(json);
            var result = doc.RootElement.GetProperty("chart").GetProperty("result");
            if (result.ValueKind != JsonValueKind.Array || result.GetArrayLength() == 0)
                return null;
            var r = result[0];
            var meta = r.GetProperty("meta");
            if (!meta.TryGetProperty("regularMarketPrice", out var priceEl))
                return null;
            var price = priceEl.GetDouble();
            var previous = meta.TryGetProperty("chartPreviousClose", out var pc) && pc.ValueKind == JsonValueKind.Number ? pc.GetDouble()
                : meta.TryGetProperty("previousClose", out var pc2) && pc2.ValueKind == JsonValueKind.Number ? pc2.GetDouble() : price;
            string Str(string name) => meta.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
            // shortName is sometimes cut and padded ("DAX                           P")
            var name = Str("longName");
            if (name.Length == 0)
                name = Str("shortName");
            name = string.Join(' ', name.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            var day = new List<double>();
            if (r.TryGetProperty("indicators", out var ind) && ind.TryGetProperty("quote", out var q) && q.GetArrayLength() > 0
                && q[0].TryGetProperty("close", out var close) && close.ValueKind == JsonValueKind.Array)
                day.AddRange(close.EnumerateArray().Where(v => v.ValueKind == JsonValueKind.Number).Select(v => v.GetDouble()));
            var change = previous == 0 ? 0 : (price - previous) / previous;
            return new Quote(symbol, name.Length > 0 ? name : symbol, price, change, Str("currency"), day);
        }

        public static string FormatPrice(double price, string currency)
        {
            var culture = new CultureInfo(Strings.Effective);
            var digits = price >= 1000 ? "N0" : price >= 1 ? "N2" : "N4";
            var symbol = currency switch { "EUR" => "€", "USD" => "$", "GBP" => "£", "CHF" => "CHF", "JPY" => "¥", _ => currency };
            return $"{price.ToString(digits, culture)} {symbol}".Trim();
        }

        public override void Draw(WidgetCanvas c)
        {
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            if (quotes.Count == 0)
            {
                if (failed)
                    c.TextWrapped(Strings.TickerFailed, c.Area.X, c.Area.Y, c.Area.Width, c.Label, 3);
                else
                    c.Text(Strings.WeatherLoading, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line));
                return;
            }

            float y = c.Area.Y;
            using var small = c.Sized(c.Label.GetHeight(c.G) * 0.8f);
            var smallLine = small.GetHeight(c.G);
            var chartWidth = c.Area.Width * 0.28f;
            foreach (var q in quotes)
            {
                var rowHeight = line + smallLine + c.Px(8);
                if (y + rowHeight > c.Area.Bottom + c.Px(4))
                    break;
                var textWidth = c.Area.Width - chartWidth - c.Px(8);
                c.Text(q.Name, new RectangleF(c.Area.X, y, textWidth * 0.55f, line));
                c.Text(FormatPrice(q.Price, q.Currency), new RectangleF(c.Area.X + textWidth * 0.4f, y, textWidth * 0.6f, line), align: StringAlignment.Far);
                c.Text(q.Symbol, new RectangleF(c.Area.X, y + line, textWidth * 0.6f, smallLine), small);

                var up = q.Change >= 0;
                var color = up ? Color.FromArgb(255, 70, 200, 110) : Color.FromArgb(255, 240, 80, 70);
                using (var brush = new SolidBrush(color))
                using (var format = new StringFormat { Alignment = StringAlignment.Far })
                    c.G.DrawString($"{(up ? "▲" : "▼")} {Math.Abs(q.Change) * 100:0.00} %", small, brush, new RectangleF(c.Area.X, y + line, textWidth, smallLine), format);

                DrawSpark(c, new RectangleF(c.Area.Right - chartWidth, y + c.Px(3), chartWidth, line + smallLine - c.Px(4)), q.Day, color);
                y += rowHeight;
                using var pen = new Pen(Color.FromArgb(40, c.Theme.HintColor));
                c.G.DrawLine(pen, c.Area.X, y - c.Px(4), c.Area.Right, y - c.Px(4));
            }
        }

        private static void DrawSpark(WidgetCanvas c, RectangleF rect, IReadOnlyList<double> values, Color color)
        {
            if (values.Count < 2)
                return;
            var min = values.Min();
            var max = values.Max();
            var range = Math.Max(max - min, Math.Abs(max) * 1e-6 + 1e-9);
            var points = values.Select((v, i) => new PointF(rect.X + rect.Width * i / (values.Count - 1), rect.Bottom - (float)((v - min) / range) * rect.Height)).ToArray();
            var oldMode = c.G.SmoothingMode;
            c.G.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pen = new Pen(color, Math.Max(1, 1.4f * c.S));
            c.G.DrawLines(pen, points);
            c.G.SmoothingMode = oldMode;
        }

        public override void DoubleClick(Point p) => Edit(null);

        public override void AddMenuItems(ToolStripItemCollection menu, IWin32Window owner)
        {
            menu.Add(Strings.TickerSet, null, (_, _) => Edit(owner));
            menu.Add(Strings.WeatherUpdateNow, null, (_, _) =>
            {
                nextFetch = DateTime.MinValue;
                Refresh();
            });
        }

        private void Edit(IWin32Window? owner)
        {
            using var dialog = new TextListDialog(Strings.WidgetTicker, Strings.TickerPrompt, string.Join("\n", Symbols(getOption())));
            if (dialog.ShowDialog(owner) != DialogResult.OK)
                return;
            setOption(string.Join("\n", Symbols(dialog.Value)));
            Refresh();
        }
    }
}
