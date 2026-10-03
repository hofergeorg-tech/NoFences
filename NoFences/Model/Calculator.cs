using System.Globalization;

namespace NoFences.Model
{
    /// <summary>
    /// A small calculator for the search box: + − × ÷ (also * / : x), ^, %, parentheses, decimals with point
    /// or comma, and sqrt(…). Only answers when the input really is a calculation (has an operator).
    /// </summary>
    public static class Calculator
    {
        public static bool TryEvaluate(string input, out double value)
        {
            value = 0;
            var text = input.Trim().Replace(" ", "").Replace('×', '*').Replace('÷', '/').Replace('−', '-').Replace(':', '/').Replace(',', '.');
            text = text.Replace("x", "*", StringComparison.OrdinalIgnoreCase);
            if (text.Length < 3 || !text.Any(c => "+-*/^%".Contains(c) || text.Contains("sqrt")) || !text.Any(char.IsDigit))
                return false;
            if (text.Any(c => !(char.IsDigit(c) || "+-*/^%().".Contains(c) || "sqrt".Contains(c))))
                return false;
            try
            {
                var parser = new Parser(text);
                value = parser.Expression();
                return parser.AtEnd && double.IsFinite(value);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>Up to 10 significant digits, in the UI language's number format.</summary>
        public static string Format(double value)
        {
            var culture = new CultureInfo(Util.Strings.Effective);
            var rounded = Math.Round(value, 10);
            return Math.Abs(rounded) >= 1e15 ? rounded.ToString("G10", culture) : rounded.ToString("#,##0.##########", culture);
        }

        /// <summary>Recursive descent: expression = term {(+|-) term}; term = power {(*|/) power}; power = unary [^ power].</summary>
        private sealed class Parser
        {
            private readonly string s;
            private int i;

            public Parser(string text) => s = text;

            public bool AtEnd => i == s.Length;

            private bool Take(char c)
            {
                if (i < s.Length && s[i] == c)
                {
                    i++;
                    return true;
                }
                return false;
            }

            public double Expression()
            {
                var v = Term();
                while (true)
                {
                    if (Take('+'))
                        v += Term();
                    else if (Take('-'))
                        v -= Term();
                    else
                        return v;
                }
            }

            private double Term()
            {
                var v = Power();
                while (true)
                {
                    if (Take('*'))
                        v *= Power();
                    else if (Take('/'))
                        v /= Power();
                    else
                        return v;
                }
            }

            private double Power()
            {
                var v = Unary();
                return Take('^') ? Math.Pow(v, Power()) : v;
            }

            private double Unary()
            {
                if (Take('-'))
                    return -Unary();
                if (Take('+'))
                    return Unary();
                var v = Primary();
                // "20%" = 0.2, so "200*15%" = 30
                return Take('%') ? v / 100 : v;
            }

            private double Primary()
            {
                if (s.AsSpan(i).StartsWith("sqrt("))
                {
                    i += 4;
                    return Math.Sqrt(Primary());
                }
                if (Take('('))
                {
                    var v = Expression();
                    if (!Take(')'))
                        throw new FormatException();
                    return v;
                }
                var start = i;
                while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.'))
                    i++;
                if (start == i)
                    throw new FormatException();
                return double.Parse(s[start..i], NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }
    }
}
