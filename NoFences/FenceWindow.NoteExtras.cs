using System.Media;
using NoFences.Model;
using NoFences.Util;
using NoFences.Win32;

namespace NoFences
{
    /// <summary>
    /// Notes with a password, pasted images and voice notes. A protected note keeps only the encrypted
    /// text in the config; the plain text lives in memory while unlocked and is forgotten when it locks.
    /// </summary>
    public sealed partial class FenceWindow
    {
        private static readonly TimeSpan AutoLockAfter = TimeSpan.FromMinutes(2);

        private string? unlockedText;
        private string? notePassword;
        private DateTime lastNoteActivity;
        private System.Windows.Forms.Timer? lockTimer;
        private readonly Dictionary<string, Image?> noteImages = new(StringComparer.OrdinalIgnoreCase);
        private SoundPlayer? player;
        private string? playing;

        private bool IsProtectedNote => Info.NoteCipher != null;

        private bool NoteLockedNow => IsProtectedNote && unlockedText == null;

        /// <summary>The note's text: plain, or the unlocked text of a protected note (saved encrypted).</summary>
        private string NoteContent
        {
            get => IsProtectedNote ? unlockedText ?? "" : Info.NoteText;
            set
            {
                if (!IsProtectedNote)
                {
                    Info.NoteText = value;
                    return;
                }
                if (notePassword == null)
                    return; // locked: nothing can change
                unlockedText = value;
                Info.NoteCipher = NoteCrypto.Encrypt(value, notePassword);
            }
        }

        #region Protection

        private void AddNoteProtectionItems(ToolStripItemCollection items)
        {
            if (!IsProtectedNote)
            {
                items.Add(Strings.NoteProtect, null, (_, _) => ProtectNote());
                return;
            }
            if (NoteLockedNow)
            {
                items.Add(Strings.NoteUnlock, null, (_, _) => UnlockNote());
                return;
            }
            items.Add(Strings.NoteLockNow, null, (_, _) => LockNote());
            items.Add(Strings.NoteRemoveProtection, null, (_, _) =>
            {
                Info.NoteText = unlockedText ?? "";
                Info.NoteCipher = null;
                ForgetUnlocked();
                app.RequestSave();
                Invalidate();
            });
        }

        private void ProtectNote()
        {
            using var first = new InputDialog(Strings.NoteProtect.TrimEnd('…'), Strings.NotePasswordNew, "", password: true);
            if (first.ShowDialog(this) != DialogResult.OK || first.Value.Length == 0)
                return;
            using var again = new InputDialog(Strings.NoteProtect.TrimEnd('…'), Strings.NotePasswordRepeat, "", password: true);
            if (again.ShowDialog(this) != DialogResult.OK)
                return;
            if (again.Value != first.Value)
            {
                MessageBox.Show(this, Strings.NotePasswordMismatch, "NoFences", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            notePassword = first.Value;
            unlockedText = Info.NoteText;
            Info.NoteCipher = NoteCrypto.Encrypt(unlockedText, notePassword);
            Info.NoteText = "";
            app.RequestSave();
            StartLockTimer();
            Invalidate();
        }

        /// <summary>Asks for the password; true if the note is unlocked afterwards.</summary>
        private bool UnlockNote()
        {
            if (!NoteLockedNow)
                return true;
            using var dialog = new InputDialog(Strings.NoteUnlock.TrimEnd('…'), Strings.NotePassword, "", password: true);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return false;
            var text = NoteCrypto.Decrypt(Info.NoteCipher!, dialog.Value);
            if (text == null)
            {
                MessageBox.Show(this, Strings.NotePasswordWrong, "NoFences", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            notePassword = dialog.Value;
            unlockedText = text;
            StartLockTimer();
            Invalidate();
            return true;
        }

        private void LockNote()
        {
            if (Editing)
                EndEditNote();
            ForgetUnlocked();
            Invalidate();
        }

        private void ForgetUnlocked()
        {
            unlockedText = null;
            notePassword = null;
            lockTimer?.Stop();
        }

        private void StartLockTimer()
        {
            lastNoteActivity = DateTime.Now;
            if (lockTimer == null)
            {
                lockTimer = new System.Windows.Forms.Timer { Interval = 15_000 };
                lockTimer.Tick += (_, _) =>
                {
                    if (Bounds.Contains(Cursor.Position) || Editing)
                        lastNoteActivity = DateTime.Now;
                    else if (DateTime.Now - lastNoteActivity > AutoLockAfter)
                        LockNote();
                };
            }
            lockTimer.Start();
        }

        /// <summary>A drawn padlock and "Protected – double-click to unlock".</summary>
        private void DrawLockedNote(Graphics g, Rectangle area)
        {
            var size = Px(26);
            var x = area.X + (area.Width - size) / 2f;
            var y = area.Y + area.Height / 2f - size - Px(6);
            using var pen = new Pen(Color.FromArgb(220, theme.HintColor), Math.Max(2, scale * 2.5f));
            using var brush = new SolidBrush(Color.FromArgb(220, theme.HintColor));
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.DrawArc(pen, x + size * 0.2f, y, size * 0.6f, size * 0.7f, 180, 180);
            g.DrawLine(pen, x + size * 0.2f, y + size * 0.35f, x + size * 0.2f, y + size * 0.5f);
            g.DrawLine(pen, x + size * 0.8f, y + size * 0.35f, x + size * 0.8f, y + size * 0.5f);
            g.FillRectangle(brush, x, y + size * 0.5f, size, size * 0.5f);
            using var hint = new SolidBrush(theme.HintColor);
            using var center = new StringFormat { Alignment = StringAlignment.Center };
            g.DrawString(Strings.NoteLockedHint, labelFont!, hint, new RectangleF(area.X, y + size + Px(8), area.Width, area.Height / 2f), center);
            contentHeight = 0;
        }

        #endregion

        #region Images and voice notes

        /// <summary>Draws an image or voice-note line; returns the height used.</summary>
        private float DrawMediaLine(Graphics g, NoteText.MediaLine media, float x, float y, float width, Rectangle view)
        {
            var path = AppData.Resolve(media.Path);
            if (media.IsAudio)
            {
                var h = Math.Max(noteFont!.Height, Px(26));
                var d = Px(22);
                var circle = new RectangleF(x, y + (h - d) / 2f, d, d);
                using (var fill = new SolidBrush(Color.FromArgb(200, theme.Accent)))
                    g.FillEllipse(fill, circle);
                using (var white = new SolidBrush(Color.White))
                {
                    if (playing == path)
                        g.FillRectangle(white, circle.X + d * 0.32f, circle.Y + d * 0.3f, d * 0.36f, d * 0.4f);
                    else
                        g.FillPolygon(white, new[] { new PointF(circle.X + d * 0.38f, circle.Y + d * 0.27f), new PointF(circle.X + d * 0.38f, circle.Y + d * 0.73f), new PointF(circle.X + d * 0.75f, circle.Y + d * 0.5f) });
                }
                var label = media.Alt.Length > 0 ? media.Alt : Strings.VoiceNote;
                theme.DrawLabel(g, label, new RectangleF(x + d + Px(6), y + (h - noteFont.Height) / 2f, width - d - Px(6), noteFont.Height + 2), noteFont, noteFormat, scale);
                var labelWidth = g.MeasureString(label, noteFont).Width;
                links.Add((new RectangleF(x, y + scrollOffset - titleHeight, d + Px(6) + labelWidth, h), "audio:" + path));
                return h + Px(4);
            }

            var image = NoteImage(path);
            if (image == null)
            {
                var missing = $"[{Path.GetFileName(path)}]";
                theme.DrawLabel(g, missing, new RectangleF(x, y, width, noteFont!.Height + 2), noteFont, noteFormat, scale);
                return noteFont.Height;
            }
            var scaleBy = Math.Min(1f, Math.Min(width / image.Width, width * 0.9f / image.Height));
            var w = image.Width * scaleBy;
            var hgt = image.Height * scaleBy;
            if (y + hgt >= view.Top && y <= view.Bottom)
                g.DrawImage(image, x, y + Px(2), w, hgt);
            links.Add((new RectangleF(x, y + Px(2) + scrollOffset - titleHeight, w, hgt), path));
            return hgt + Px(6);
        }

        private Image? NoteImage(string path)
        {
            if (noteImages.TryGetValue(path, out var cached))
                return cached;
            Image? image = null;
            try
            {
                if (File.Exists(path))
                {
                    // Loaded into memory so the file isn't kept locked
                    using var stream = new MemoryStream(File.ReadAllBytes(path));
                    using var loaded = Image.FromStream(stream);
                    image = new Bitmap(loaded);
                }
            }
            catch (Exception e) when (e is IOException or ArgumentException or UnauthorizedAccessException)
            {
            }
            noteImages[path] = image;
            return image;
        }

        /// <summary>Editor: Ctrl+V with an image on the clipboard stores it and inserts a picture line.</summary>
        private bool PasteImageIntoEditor()
        {
            if (editor == null || !Clipboard.ContainsImage())
                return false;
            using var image = Clipboard.GetImage();
            if (image == null || AppData.NewMediaFile("image", ".png") is not { } file)
                return false;
            image.Save(file.Full, System.Drawing.Imaging.ImageFormat.Png);
            var before = editor.SelectionStart > 0 && editor.Text[editor.SelectionStart - 1] != '\n' ? "\n" : "";
            editor.SelectedText = before + NoteText.MediaMarkup(Strings.NoteImage, file.Stored) + "\n";
            return true;
        }

        private void PlayAudio(string path)
        {
            player?.Stop();
            player?.Dispose();
            player = null;
            if (playing == path)
            {
                playing = null;
                Invalidate();
                return;
            }
            playing = null;
            if (!File.Exists(path))
                return;
            if (!path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                Util.Launcher.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
                return;
            }
            try
            {
                player = new SoundPlayer(path);
                player.Play();
                playing = path;
                // SoundPlayer has no "finished" event: show "stop" for the recording's length
                var length = VoiceRecorder.WavLength(path);
                var done = new System.Windows.Forms.Timer { Interval = (int)Math.Clamp(length.TotalMilliseconds + 300, 500, int.MaxValue) };
                done.Tick += (_, _) =>
                {
                    done.Dispose();
                    if (playing == path)
                    {
                        playing = null;
                        Invalidate();
                    }
                };
                done.Start();
            }
            catch (Exception e) when (e is InvalidOperationException or IOException)
            {
                MessageBox.Show(this, e.Message, "NoFences", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            Invalidate();
        }

        private void RecordVoiceNote()
        {
            if (NoteLockedNow && !UnlockNote())
                return;
            if (AppData.NewMediaFile("voice", ".wav") is not { } file)
                return;
            using var dialog = new VoiceRecordDialog(file.Full);
            if (dialog.ShowDialog(this) != DialogResult.OK || !File.Exists(file.Full))
                return;
            var line = NoteText.MediaMarkup($"{Strings.VoiceNote} {NoteText.Duration(dialog.Length)}", file.Stored);
            NoteContent = string.IsNullOrWhiteSpace(NoteContent) ? line : NoteContent.TrimEnd() + "\n" + line;
            app.RequestSave();
            Invalidate();
        }

        private void DisposeNoteExtras()
        {
            lockTimer?.Dispose();
            player?.Dispose();
            foreach (var image in noteImages.Values)
                image?.Dispose();
            noteImages.Clear();
        }

        #endregion
    }

    /// <summary>Records from the default microphone (Windows' MCI, no extra libraries) into a WAV file.</summary>
    internal static class VoiceRecorder
    {
        [System.Runtime.InteropServices.DllImport("winmm.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int mciSendString(string command, System.Text.StringBuilder? result, int length, IntPtr callback);

        private const string Alias = "nofencesvoice";

        public static bool Start()
        {
            mciSendString($"close {Alias}", null, 0, IntPtr.Zero);
            if (mciSendString($"open new type waveaudio alias {Alias}", null, 0, IntPtr.Zero) != 0)
                return false;
            // 16 kHz mono is plenty for speech and keeps the files small
            mciSendString($"set {Alias} bitspersample 16 channels 1 samplespersec 16000 bytespersec 32000 alignment 2", null, 0, IntPtr.Zero);
            return mciSendString($"record {Alias}", null, 0, IntPtr.Zero) == 0;
        }

        public static bool Stop(string? saveTo)
        {
            mciSendString($"stop {Alias}", null, 0, IntPtr.Zero);
            var ok = saveTo == null || mciSendString($"save {Alias} \"{saveTo}\"", null, 0, IntPtr.Zero) == 0;
            mciSendString($"close {Alias}", null, 0, IntPtr.Zero);
            return ok;
        }

        /// <summary>Length of a PCM WAV file from its header (zero if unreadable).</summary>
        public static TimeSpan WavLength(string path)
        {
            try
            {
                using var reader = new BinaryReader(File.OpenRead(path));
                return WavLength(reader.ReadBytes(64), new FileInfo(path).Length);
            }
            catch (IOException)
            {
                return TimeSpan.Zero;
            }
        }

        public static TimeSpan WavLength(byte[] header, long fileLength)
        {
            if (header.Length < 44 || header[0] != 'R' || header[8] != 'W')
                return TimeSpan.Zero;
            var bytesPerSecond = BitConverter.ToInt32(header, 28);
            return bytesPerSecond <= 0 ? TimeSpan.Zero : TimeSpan.FromSeconds((fileLength - 44) / (double)bytesPerSecond);
        }
    }

    /// <summary>"Recording… 0:07" with Stop and Cancel; at most 10 minutes.</summary>
    internal sealed class VoiceRecordDialog : Form
    {
        private static readonly TimeSpan MaxLength = TimeSpan.FromMinutes(10);
        private readonly string path;
        private readonly Label status = new() { AutoSize = true, Font = new Font(SystemFonts.MessageBoxFont ?? DefaultFont, FontStyle.Bold) };
        private readonly System.Windows.Forms.Timer clock = new() { Interval = 250 };
        private readonly DateTime started = DateTime.Now;
        private bool recording;

        public TimeSpan Length { get; private set; }

        public VoiceRecordDialog(string path)
        {
            this.path = path;
            Text = Strings.VoiceNoteRecord.TrimEnd('…');
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(16);
            Font = SystemFonts.MessageBoxFont ?? Font;

            var stop = new Button { Text = Strings.VoiceNoteStop, AutoSize = true, DialogResult = DialogResult.OK };
            var cancel = new Button { Text = Strings.Cancel, AutoSize = true, DialogResult = DialogResult.Cancel };
            AcceptButton = stop;
            CancelButton = cancel;
            var layout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true };
            layout.Controls.Add(status);
            var buttons = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 12, 0, 0) };
            buttons.Controls.AddRange(new Control[] { stop, cancel });
            layout.Controls.Add(buttons);
            Controls.Add(layout);

            clock.Tick += (_, _) =>
            {
                var elapsed = DateTime.Now - started;
                status.Text = Strings.VoiceNoteRecording(NoteText.Duration(elapsed));
                if (elapsed >= MaxLength)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            };
            Shown += (_, _) =>
            {
                recording = VoiceRecorder.Start();
                if (!recording)
                {
                    MessageBox.Show(this, Strings.VoiceNoteNoMicrophone, "NoFences", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.Cancel;
                    Close();
                    return;
                }
                status.Text = Strings.VoiceNoteRecording("0:00");
                clock.Start();
            };
            status.Text = Strings.VoiceNoteRecording("0:00");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            clock.Stop();
            if (recording)
            {
                recording = false;
                Length = DateTime.Now - started;
                var saved = VoiceRecorder.Stop(DialogResult == DialogResult.OK ? path : null);
                if (!saved)
                    DialogResult = DialogResult.Cancel;
            }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                clock.Dispose();
                status.Font.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
