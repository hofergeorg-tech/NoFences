using System.Drawing.Imaging;
using NoFences.Util;
using QRCoder;

namespace NoFences
{
    /// <summary>
    /// Shows any text or link as a QR code – e.g. to open a page on the phone. Starts with the text on
    /// the clipboard; the code can be copied or saved as an image.
    /// </summary>
    public sealed class QrCodeDialog : Form
    {
        private static QrCodeDialog? open;
        private readonly TextBox text = new() { Width = 340, Multiline = true, Height = 54, ScrollBars = ScrollBars.Vertical };
        private readonly PictureBox picture = new() { Size = new Size(340, 340), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White };
        private readonly Label hint = new() { AutoSize = true, ForeColor = SystemColors.GrayText, MaximumSize = new Size(340, 0) };

        public static void ShowSingle()
        {
            if (open is { IsDisposed: false })
            {
                open.Activate();
                return;
            }
            open = new QrCodeDialog();
            open.Show();
        }

        private QrCodeDialog()
        {
            Text = Strings.QrTitle;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(16);
            Font = SystemFonts.MessageBoxFont ?? Font;
            TopMost = true;

            var copy = new Button { Text = Strings.QrCopy, AutoSize = true };
            var save = new Button { Text = Strings.QrSave, AutoSize = true };
            var close = new Button { Text = Strings.Close, AutoSize = true, DialogResult = DialogResult.Cancel };
            CancelButton = close;
            copy.Click += (_, _) =>
            {
                if (picture.Image != null)
                    Clipboard.SetImage(picture.Image);
            };
            save.Click += (_, _) => Save();
            close.Click += (_, _) => Close();

            var layout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            layout.Controls.Add(new Label { Text = Strings.QrPrompt, AutoSize = true });
            layout.Controls.Add(text);
            layout.Controls.Add(picture);
            layout.Controls.Add(hint);
            var buttons = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
            buttons.Controls.AddRange(new Control[] { copy, save, close });
            layout.Controls.Add(buttons);
            Controls.Add(layout);

            try
            {
                if (Clipboard.ContainsText())
                    text.Text = Clipboard.GetText().Trim();
            }
            catch (System.Runtime.InteropServices.ExternalException) { }
            text.TextChanged += (_, _) => Render();
            Shown += (_, _) =>
            {
                text.Focus();
                text.SelectAll();
            };
            Render();
        }

        /// <summary>The QR code as black modules on white with the standard quiet zone; null if empty or too long.</summary>
        public static Bitmap? Create(string content, int pixelsPerModule = 8)
        {
            if (string.IsNullOrEmpty(content))
                return null;
            try
            {
                using var generator = new QRCodeGenerator();
                using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
                var modules = data.ModuleMatrix;
                var size = modules.Count;
                var bitmap = new Bitmap(size * pixelsPerModule, size * pixelsPerModule);
                using var g = Graphics.FromImage(bitmap);
                g.Clear(Color.White);
                for (var y = 0; y < size; y++)
                    for (var x = 0; x < size; x++)
                        if (modules[y][x])
                            g.FillRectangle(Brushes.Black, x * pixelsPerModule, y * pixelsPerModule, pixelsPerModule, pixelsPerModule);
                return bitmap;
            }
            catch (QRCoder.Exceptions.DataTooLongException)
            {
                return null;
            }
        }

        private void Render()
        {
            var old = picture.Image;
            picture.Image = Create(text.Text.Trim());
            old?.Dispose();
            hint.Text = text.Text.Trim().Length == 0 ? Strings.QrEmpty : picture.Image == null ? Strings.QrTooLong : Strings.QrHint;
        }

        private void Save()
        {
            if (picture.Image == null)
                return;
            using var dialog = new SaveFileDialog { Filter = "PNG|*.png", FileName = "qr-code.png" };
            if (dialog.ShowDialog(this) == DialogResult.OK)
                picture.Image.Save(dialog.FileName, ImageFormat.Png);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                picture.Image?.Dispose();
            base.Dispose(disposing);
        }
    }
}
