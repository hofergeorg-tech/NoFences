using System.Diagnostics;
using NoFences.Util;

namespace NoFences
{
    /// <summary>"About NoFences": version, author, credits to the original project, links.</summary>
    public sealed class AboutDialog : Form
    {
        public const string Website = "https://www.georg-hofer.com";
        public const string Repository = "https://github.com/" + UpdateChecker.Repository;
        public const string Original = "https://github.com/Twometer/NoFences";

        /// <summary>PayPal donation link; empty = no donate button anywhere.</summary>
        public const string DonateUrl = "";

        public static bool CanDonate => DonateUrl.Length > 0;

        public static void OpenDonate()
        {
            if (!CanDonate)
                return;
            try { Process.Start(new ProcessStartInfo(DonateUrl) { UseShellExecute = true }); } catch { }
        }

        private static AboutDialog? open;

        public static void ShowSingle()
        {
            if (open is { IsDisposed: false })
            {
                open.Activate();
                return;
            }
            open = new AboutDialog();
            open.Show();
        }

        internal static AboutDialog CreateForPreview() => new() { ShowInTaskbar = false };

        private AboutDialog()
        {
            Text = Strings.About;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(20);
            Font = SystemFonts.MessageBoxFont ?? Font;
            ShowInTaskbar = true;
            try
            {
                using var stream = typeof(AboutDialog).Assembly.GetManifestResourceStream("NoFences.ico");
                if (stream != null)
                    Icon = new Icon(stream);
            }
            catch { }

            var logo = new PictureBox { Size = new Size(64, 64), SizeMode = PictureBoxSizeMode.Zoom, Margin = new Padding(0, 0, 16, 0) };
            try
            {
                using var stream = typeof(AboutDialog).Assembly.GetManifestResourceStream("NoFences.ico");
                if (stream != null)
                    logo.Image = new Icon(stream, 64, 64).ToBitmap();
            }
            catch { }

            var text = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            text.Controls.Add(new Label { Text = $"NoFences {UpdateChecker.CurrentVersion}", AutoSize = true, Font = new Font(Font.FontFamily, Font.Size * 1.6f, FontStyle.Bold) });
            text.Controls.Add(new Label { Text = Strings.AboutTagline, AutoSize = true, MaximumSize = new Size(380, 0), Margin = new Padding(3, 4, 3, 12) });
            text.Controls.Add(new Label { Text = "© 2026 Georg Hofer", AutoSize = true });
            text.Controls.Add(Link("www.georg-hofer.com", Website));
            text.Controls.Add(Link(Strings.AboutSource, Repository));
            text.Controls.Add(new Label { Text = Strings.AboutCredits, AutoSize = true, MaximumSize = new Size(380, 0), Margin = new Padding(3, 12, 3, 0) });
            text.Controls.Add(Link("github.com/Twometer/NoFences", Original));
            text.Controls.Add(new Label { Text = Strings.AboutLicense, AutoSize = true, ForeColor = SystemColors.GrayText, Margin = new Padding(3, 12, 3, 0) });

            var top = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = false, Dock = DockStyle.Fill };
            top.Controls.Add(logo);
            top.Controls.Add(text);

            var ok = new Button { Text = Strings.Ok, DialogResult = DialogResult.OK, AutoSize = true };
            ok.Click += (_, _) => Close();
            var whatsNew = new Button { Text = Strings.WhatsNew, AutoSize = true };
            whatsNew.Click += (_, _) => DocumentViewer.ShowDocument(Strings.ChangelogDocument, Strings.WhatsNew);
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Bottom, Padding = new Padding(0, 16, 0, 0) };
            buttons.Controls.AddRange(new Control[] { ok, whatsNew });
            if (CanDonate)
            {
                var donate = new Button { Text = "♥ " + Strings.Donate, AutoSize = true, ForeColor = Color.FromArgb(0, 112, 186) };
                donate.Click += (_, _) => OpenDonate();
                buttons.Controls.Add(donate);
                text.Controls.Add(new Label { Text = Strings.DonateHint, AutoSize = true, MaximumSize = new Size(380, 0), Margin = new Padding(3, 12, 3, 0) });
            }
            AcceptButton = ok;
            CancelButton = ok;

            Controls.Add(top);
            Controls.Add(buttons);
        }

        private static LinkLabel Link(string text, string url)
        {
            var link = new LinkLabel { Text = text, AutoSize = true };
            link.LinkClicked += (_, _) =>
            {
                try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
                catch { }
            };
            return link;
        }
    }
}
