using System.Runtime.InteropServices;
using NoFences.Model;
using NoFences.Util;

namespace NoFences
{
    /// <summary>Clipboard text that clipboard managers and Windows' clipboard history (Win+V) leave alone.</summary>
    internal static class PrivateClipboard
    {
        public static bool SetText(string text)
        {
            try
            {
                var data = new DataObject();
                data.SetData(DataFormats.UnicodeText, text);
                data.SetData("ExcludeClipboardContentFromMonitorProcessing", new MemoryStream(new byte[] { 1, 0, 0, 0 }));
                data.SetData("CanIncludeInClipboardHistory", new MemoryStream(new byte[4]));
                data.SetData("CanUploadToCloudClipboard", new MemoryStream(new byte[4]));
                Clipboard.SetDataObject(data, copy: true);
                return true;
            }
            catch (ExternalException)
            {
                return false;
            }
        }

        /// <summary>Empties the clipboard if it still holds <paramref name="text"/>.</summary>
        public static void ClearIfStill(string text)
        {
            try
            {
                if (Clipboard.ContainsText() && Clipboard.GetText() == text)
                    Clipboard.Clear();
            }
            catch (ExternalException)
            {
            }
        }
    }

    /// <summary>Tools → Password generator: a random password, copied without landing in any clipboard history.</summary>
    internal sealed class PasswordDialog : Form
    {
        private static PasswordDialog? open;
        private readonly TextBox output = new() { Width = 360, ReadOnly = true, Font = new Font("Consolas", 12f) };
        private readonly NumericUpDown length = new() { Minimum = PasswordGenerator.MinLength, Maximum = PasswordGenerator.MaxLength, Value = 20, Width = 60 };
        private readonly CheckBox upper = new() { Text = "A–Z", Checked = true, AutoSize = true };
        private readonly CheckBox lower = new() { Text = "a–z", Checked = true, AutoSize = true };
        private readonly CheckBox digits = new() { Text = "0–9", Checked = true, AutoSize = true };
        private readonly CheckBox symbols = new() { Text = "!#$%", Checked = true, AutoSize = true };
        private readonly CheckBox ambiguous = new() { Text = Strings.PasswordNoAmbiguous, Checked = true, AutoSize = true };
        private readonly Label strength = new() { AutoSize = true, ForeColor = SystemColors.GrayText };
        private readonly System.Windows.Forms.Timer clearTimer = new() { Interval = 60_000 };
        private string? copied;

        public static void ShowSingle()
        {
            if (open is { IsDisposed: false })
            {
                open.Activate();
                return;
            }
            open = new PasswordDialog();
            open.Show();
        }

        private PasswordDialog()
        {
            Text = Strings.PasswordTitle;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(16);
            Font = SystemFonts.MessageBoxFont ?? Font;
            TopMost = true;

            var again = new Button { Text = Strings.PasswordNew, AutoSize = true };
            var copy = new Button { Text = Strings.PasswordCopy, AutoSize = true };
            var close = new Button { Text = Strings.Close, AutoSize = true, DialogResult = DialogResult.Cancel };
            CancelButton = close;
            AcceptButton = copy;
            again.Click += (_, _) => Generate();
            copy.Click += (_, _) => Copy();
            close.Click += (_, _) => Close();
            foreach (var box in new[] { upper, lower, digits, symbols, ambiguous })
                box.CheckedChanged += (_, _) => Generate();
            length.ValueChanged += (_, _) => Generate();
            clearTimer.Tick += (_, _) =>
            {
                clearTimer.Stop();
                if (copied != null)
                    PrivateClipboard.ClearIfStill(copied);
                copied = null;
            };

            var layout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            layout.Controls.Add(output);
            layout.Controls.Add(strength);
            var options = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
            options.Controls.Add(new Label { Text = Strings.PasswordLength, AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
            options.Controls.AddRange(new Control[] { length, upper, lower, digits, symbols });
            layout.Controls.Add(options);
            layout.Controls.Add(ambiguous);
            layout.Controls.Add(new Label { Text = Strings.PasswordHint, AutoSize = true, MaximumSize = new Size(360, 0), ForeColor = SystemColors.GrayText, Margin = new Padding(0, 8, 0, 0) });
            var buttons = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
            buttons.Controls.AddRange(new Control[] { again, copy, close });
            layout.Controls.Add(buttons);
            Controls.Add(layout);
            Generate();
        }

        private void Generate()
        {
            output.Text = PasswordGenerator.Generate((int)length.Value, upper.Checked, lower.Checked, digits.Checked, symbols.Checked, ambiguous.Checked);
            strength.Text = Strings.PasswordStrength(PasswordGenerator.Bits((int)length.Value, upper.Checked, lower.Checked, digits.Checked, symbols.Checked));
        }

        private void Copy()
        {
            if (!PrivateClipboard.SetText(output.Text))
                return;
            copied = output.Text;
            clearTimer.Stop();
            clearTimer.Start();
            strength.Text = Strings.PasswordCopied;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Closing early: still clear the clipboard after the minute
            if (copied != null)
            {
                var text = copied;
                var timer = new System.Windows.Forms.Timer { Interval = 60_000 };
                timer.Tick += (_, _) =>
                {
                    timer.Dispose();
                    PrivateClipboard.ClearIfStill(text);
                };
                timer.Start();
            }
            base.OnFormClosed(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                clearTimer.Dispose();
                output.Font.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>Tools → Network info: addresses, Wi-Fi name and signal; a click copies a value.</summary>
    internal sealed class NetworkInfoDialog : Form
    {
        private static NetworkInfoDialog? open;
        private readonly TableLayoutPanel table = new() { ColumnCount = 2, AutoSize = true, Padding = new Padding(0) };
        private readonly Label status = new() { AutoSize = true, ForeColor = SystemColors.GrayText, Margin = new Padding(0, 8, 0, 0) };
        private LinkLabel? publicValue;

        public static void ShowSingle()
        {
            if (open is { IsDisposed: false })
            {
                open.Activate();
                return;
            }
            open = new NetworkInfoDialog();
            open.Show();
        }

        private NetworkInfoDialog()
        {
            Text = Strings.NetInfoTitle;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(16);
            Font = SystemFonts.MessageBoxFont ?? Font;
            TopMost = true;

            var refresh = new Button { Text = Strings.NetInfoRefresh, AutoSize = true };
            var close = new Button { Text = Strings.Close, AutoSize = true, DialogResult = DialogResult.Cancel };
            CancelButton = close;
            refresh.Click += (_, _) => Fill();
            close.Click += (_, _) => Close();

            var layout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            layout.Controls.Add(table);
            layout.Controls.Add(status);
            var buttons = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
            buttons.Controls.AddRange(new Control[] { refresh, close });
            layout.Controls.Add(buttons);
            Controls.Add(layout);
            Fill();
        }

        private void Fill()
        {
            table.SuspendLayout();
            foreach (Control c in table.Controls.Cast<Control>().ToList())
                c.Dispose();
            table.Controls.Clear();
            table.RowCount = 0;
            status.Text = Strings.NetInfoClickToCopy;

            publicValue = Row(Strings.NetInfoPublic, Strings.NetInfoLoading);
            publicValue.Tag = null; // nothing to copy until loaded
            foreach (var wifi in NetworkInfo.WifiConnections())
                Row(Strings.NetInfoWifi, wifi.Name, Strings.NetInfoSignal(wifi.SignalPercent));
            foreach (var adapter in NetworkInfo.Adapters())
            {
                var title = adapter.Name.Equals(adapter.Kind, StringComparison.OrdinalIgnoreCase) ? adapter.Name : $"{adapter.Name} ({adapter.Kind})";
                foreach (var ip in adapter.IPv4)
                    Row(title, ip);
                foreach (var ip in adapter.IPv6.Take(2))
                    Row("  IPv6", ip);
                if (adapter.Gateway != null)
                    Row("  " + Strings.NetInfoGateway, adapter.Gateway);
                if (adapter.Mac != null)
                    Row("  MAC", adapter.Mac);
            }
            table.ResumeLayout();
            LoadPublicAddress();
        }

        private async void LoadPublicAddress()
        {
            var label = publicValue;
            var address = await NetworkInfo.PublicAddressAsync();
            if (label == null || label.IsDisposed)
                return;
            label.Text = address ?? Strings.NetInfoOffline;
            label.Tag = address;
        }

        /// <summary>A name and a value that copies itself when clicked.</summary>
        private LinkLabel Row(string name, string value, string? extra = null)
        {
            var row = table.RowCount++;
            table.Controls.Add(new Label { Text = name, AutoSize = true, Margin = new Padding(0, 4, 16, 4) }, 0, row);
            var link = new LinkLabel { Text = extra == null ? value : $"{value}  ·  {extra}", Tag = value, AutoSize = true, Margin = new Padding(0, 4, 0, 4) };
            link.LinkClicked += (_, _) =>
            {
                if (link.Tag is not string text)
                    return;
                try
                {
                    Clipboard.SetText(text);
                    status.Text = Strings.NetInfoCopied(text);
                }
                catch (ExternalException)
                {
                }
            };
            table.Controls.Add(link, 1, row);
            return link;
        }
    }
}
