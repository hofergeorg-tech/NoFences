using System.Text.RegularExpressions;
using NoFences.Util;

namespace NoFences.Widgets
{
    /// <summary>What the Steam widget shows: which list, wishlist first, minimum discount, maximum price, count, account.</summary>
    internal sealed class SteamDealsDialog : Form
    {
        private readonly ComboBox source = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        private readonly CheckBox wishlistFirst = new() { AutoSize = true };
        private readonly NumericUpDown minDiscount = new() { Minimum = 0, Maximum = 95, Increment = 5, Width = 80 };
        private readonly NumericUpDown maxPrice = new() { Minimum = 0, Maximum = 500, Increment = 5, Width = 80 };
        private readonly NumericUpDown count = new() { Minimum = 3, Maximum = 50, Width = 80 };
        private readonly TextBox steamId = new() { Width = 220 };

        public SteamDealsWidget.Options Result => new()
        {
            Source = (SteamDealsWidget.DealSource)source.SelectedIndex,
            WishlistFirst = wishlistFirst.Checked,
            MinDiscount = (int)minDiscount.Value,
            MaxPrice = (int)maxPrice.Value,
            Count = (int)count.Value,
            SteamId = Regex.IsMatch(steamId.Text.Trim(), @"^7656\d{13}$") ? steamId.Text.Trim() : null
        };

        public SteamDealsDialog(SteamDealsWidget.Options current)
        {
            Text = Strings.WidgetSteamDeals;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(16);
            Font = SystemFonts.MessageBoxFont ?? Font;
            TopMost = true;

            source.Items.AddRange(Enum.GetValues<SteamDealsWidget.DealSource>().Select(s => (object)Strings.SteamSourceName(s)).ToArray());
            source.SelectedIndex = (int)current.Source;
            wishlistFirst.Text = Strings.SteamWishlistFirst;
            wishlistFirst.Checked = current.WishlistFirst;
            minDiscount.Value = Math.Clamp(current.MinDiscount, 0, 95);
            maxPrice.Value = Math.Clamp(current.MaxPrice, 0, 500);
            count.Value = Math.Clamp(current.Count, 3, 50);
            steamId.Text = current.SteamId ?? "";
            steamId.PlaceholderText = Strings.SteamIdAuto;
            source.SelectedIndexChanged += (_, _) => wishlistFirst.Enabled = source.SelectedIndex != (int)SteamDealsWidget.DealSource.Wishlist;
            wishlistFirst.Enabled = source.SelectedIndex != (int)SteamDealsWidget.DealSource.Wishlist;

            var grid = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill };
            SettingsKit.Row(grid, Strings.SteamSourceLabel, source);
            SettingsKit.Row(grid, "", wishlistFirst);
            SettingsKit.Row(grid, Strings.SteamMinDiscount, minDiscount);
            SettingsKit.Row(grid, Strings.SteamMaxPrice, maxPrice);
            SettingsKit.Row(grid, Strings.SteamCount, count);
            SettingsKit.Row(grid, Strings.SteamAccount, steamId);
            var hint = new Label { Text = Strings.SteamIdPrompt, AutoSize = true, MaximumSize = new Size(380, 0), ForeColor = SystemColors.GrayText, Margin = new Padding(0, 6, 0, 0) };
            grid.Controls.Add(hint);
            grid.SetColumnSpan(hint, 2);

            var ok = new Button { Text = Strings.Ok, DialogResult = DialogResult.OK, AutoSize = true };
            var cancel = new Button { Text = Strings.Cancel, DialogResult = DialogResult.Cancel, AutoSize = true };
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Bottom, Padding = new Padding(0, 12, 0, 0) };
            buttons.Controls.AddRange(new Control[] { cancel, ok });
            AcceptButton = ok;
            CancelButton = cancel;
            Controls.Add(grid);
            Controls.Add(buttons);
        }
    }
}
