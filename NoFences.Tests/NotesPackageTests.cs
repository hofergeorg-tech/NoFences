using System.Drawing;
using NoFences.Model;
using NoFences.Widgets;

namespace NoFences.Tests
{
    public class NotesPackageTests
    {
        [Fact]
        public void NoteCrypto_RoundTripsAndRejectsWrongPassword()
        {
            var cipher = NoteCrypto.Encrypt("Geheim: PIN 1234 ✓", "pässwort");
            Assert.DoesNotContain("1234", cipher);
            Assert.Equal("Geheim: PIN 1234 ✓", NoteCrypto.Decrypt(cipher, "pässwort"));
            Assert.Null(NoteCrypto.Decrypt(cipher, "falsch"));
            Assert.Null(NoteCrypto.Decrypt("v1:not-base64!", "x"));
            Assert.Null(NoteCrypto.Decrypt("plain text", "x"));
            // Same text twice: different cipher (random salt and nonce)
            Assert.NotEqual(cipher, NoteCrypto.Encrypt("Geheim: PIN 1234 ✓", "pässwort"));
        }

        [Fact]
        public void NoteCrypto_DetectsTampering()
        {
            var cipher = NoteCrypto.Encrypt("text", "pw");
            var bytes = Convert.FromBase64String(cipher[3..]);
            bytes[^1] ^= 1;
            Assert.Null(NoteCrypto.Decrypt("v1:" + Convert.ToBase64String(bytes), "pw"));
        }

        [Fact]
        public void MediaLines_ImagesAndVoiceNotes()
        {
            Assert.Equal(new NoteText.MediaLine("Bild", "note-media/a.png", false), NoteText.Media("![Bild](note-media/a.png)"));
            Assert.Equal(new NoteText.MediaLine("Sprachnotiz 0:12", "note-media/v.wav", true), NoteText.Media("  ![Sprachnotiz 0:12](note-media/v.wav) "));
            Assert.Null(NoteText.Media("text ![Bild](a.png)"));
            Assert.Null(NoteText.Media("- item"));
            Assert.Equal("![a)b](x.png)", NoteText.MediaMarkup("a]b", "x.png"));
            Assert.Equal("1:05", NoteText.Duration(TimeSpan.FromSeconds(65)));
            // Summaries skip pictures
            Assert.Equal("Shopping", NoteText.Summary("![Bild](a.png)\nShopping"));
        }

        [Fact]
        public void WavLength_FromHeader()
        {
            var header = new byte[44];
            "RIFF"u8.ToArray().CopyTo(header, 0);
            "WAVE"u8.ToArray().CopyTo(header, 8);
            BitConverter.GetBytes(32000).CopyTo(header, 28);
            Assert.Equal(TimeSpan.FromSeconds(2), VoiceRecorder.WavLength(header, 44 + 64000));
            Assert.Equal(TimeSpan.Zero, VoiceRecorder.WavLength(new byte[10], 100));
        }

        [Fact]
        public void Clipboard_PinnedStayOnTopAndSurviveClearing()
        {
            var h = new ClipboardHistory(2);
            h.Add("a");
            h.Add("b");
            var a = h.Items.Single(i => i.Text == "a");
            h.Pin(a);
            Assert.Equal(new[] { "a", "b" }, h.Texts);
            // The limit counts unpinned entries only
            h.Add("c");
            h.Add("d");
            Assert.Equal(new[] { "a", "d", "c" }, h.Texts);
            // Copying a pinned text again doesn't list it twice
            h.Add("a");
            Assert.Equal(new[] { "a", "d", "c" }, h.Texts);
            h.Clear();
            Assert.Equal(new[] { "a" }, h.Texts);
            h.Unpin(a);
            Assert.False(a.Pinned);
            h.Clear();
            Assert.Empty(h.Items);
        }

        [Fact]
        public void Clipboard_SameImageListedOnce()
        {
            using var bmp = new Bitmap(40, 30);
            using (var g = Graphics.FromImage(bmp))
                g.Clear(Color.Red);
            var h = new ClipboardHistory(5);
            h.AddImage(ClipItem.FromImage(bmp));
            h.Add("text");
            h.AddImage(ClipItem.FromImage(bmp));
            Assert.Equal(2, h.Items.Count);
            Assert.NotNull(h.Items[0].Png);
            Assert.Equal(40, h.Items[0].Width);
            Assert.True(h.Items[0].Thumb!.Width <= 160);
        }

        [Fact]
        public void ProtectedNote_IsSavedOnlyEncrypted()
        {
            var info = new FenceInfo { Kind = FenceKind.Note, NoteCipher = NoteCrypto.Encrypt("secret", "pw") };
            var json = System.Text.Json.JsonSerializer.Serialize(info, FenceStore.JsonOptions);
            Assert.DoesNotContain("secret", json);
            var copy = System.Text.Json.JsonSerializer.Deserialize<FenceInfo>(json, FenceStore.JsonOptions)!;
            Assert.Equal("secret", NoteCrypto.Decrypt(copy.NoteCipher!, "pw"));
        }
    }
}
