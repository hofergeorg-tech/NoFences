using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>Battery charge, whether it's charging and the time left (laptops and tablets).</summary>
    public sealed class BatteryWidget : FenceWidget
    {
        private PowerStatus? status;

        public override string Type => "battery";

        public override int RefreshMs => 5000;

        public override void Refresh() => status = SystemInformation.PowerStatus;

        /// <summary>"2 h 15 min" from seconds; empty when Windows doesn't know (-1).</summary>
        public static string FormatRemaining(int seconds)
        {
            if (seconds < 0)
                return "";
            var t = TimeSpan.FromSeconds(seconds);
            return t.TotalHours >= 1 ? $"{(int)t.TotalHours} h {t.Minutes} min" : $"{t.Minutes} min";
        }

        public override void Draw(WidgetCanvas c)
        {
            status ??= SystemInformation.PowerStatus;
            var line = c.Label.GetHeight(c.G) + c.Px(2);
            if (status.BatteryChargeStatus.HasFlag(BatteryChargeStatus.NoSystemBattery) || status.BatteryLifePercent > 1)
            {
                c.Text(Strings.BatteryNone, new RectangleF(c.Area.X, c.Area.Y, c.Area.Width, line * 2));
                return;
            }

            var percent = status.BatteryLifePercent;
            var charging = status.BatteryChargeStatus.HasFlag(BatteryChargeStatus.Charging);
            var plugged = status.PowerLineStatus == PowerLineStatus.Online;

            using var big = c.Sized(Math.Min(c.Area.Height * 0.34f, c.Px(44)), FontStyle.Bold);
            var bigHeight = big.GetHeight(c.G);
            float y = c.Area.Y;
            c.Text($"{percent * 100:0} %", new RectangleF(c.Area.X, y, c.Area.Width, bigHeight), big);
            y += bigHeight + c.Px(2);

            // Battery outline with fill
            var body = new RectangleF(c.Area.X, y, c.Area.Width - c.Px(6), c.Px(14));
            using (var pen = new Pen(Color.FromArgb(200, c.Theme.HintColor), Math.Max(1, c.S)))
            {
                c.G.DrawRectangle(pen, body.X, body.Y, body.Width, body.Height);
                using var tip = new SolidBrush(Color.FromArgb(200, c.Theme.HintColor));
                c.G.FillRectangle(tip, body.Right, body.Y + body.Height / 4, c.Px(4), body.Height / 2);
            }
            var inner = RectangleF.Inflate(body, -c.Px(2), -c.Px(2));
            // Low battery is the alarming case here, so the bar turns red at the bottom, not the top
            using (var fill = new SolidBrush(percent < 0.15 && !plugged ? Color.FromArgb(230, 255, 90, 70) : Color.FromArgb(230, c.Theme.Accent)))
                c.G.FillRectangle(fill, inner.X, inner.Y, inner.Width * Math.Clamp(percent, 0, 1), inner.Height);
            y += body.Height + c.Px(8);

            var state = charging ? Strings.BatteryCharging : plugged ? Strings.BatteryPlugged : Strings.BatteryOnBattery;
            c.Text(state, new RectangleF(c.Area.X, y, c.Area.Width, line));
            y += line;
            var left = FormatRemaining(status.BatteryLifeRemaining);
            if (!plugged && left.Length > 0)
                c.Text(Strings.BatteryLeft(left), new RectangleF(c.Area.X, y, c.Area.Width, line));
        }
    }
}
