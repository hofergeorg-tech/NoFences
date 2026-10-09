using NoFences.Util;

namespace NoFences.Tests
{
    public class UpdateChecksumTests
    {
        private const string Release = """
            {"tag_name":"v9.1.0","html_url":"https://github.com/x/releases/tag/v9.1.0","assets":[
              {"name":"other.zip","browser_download_url":"https://e/other.zip","size":5,"digest":"sha256:00"},
              {"name":"NoFences.exe","browser_download_url":"https://e/NoFences.exe","size":3,
               "digest":"sha256:BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD"}]}
            """;

        [Fact]
        public void Release_CarriesTheExeChecksum()
        {
            var release = UpdateChecker.ParseRelease(Release)!;
            Assert.Equal(new Version(9, 1, 0), release.Version);
            Assert.Equal("https://e/NoFences.exe", release.ExeUrl);
            Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", release.ExeSha256);
            // Older releases have no digest: still installable (size check only)
            Assert.Null(UpdateChecker.ParseRelease(Release.Replace("\"digest\"", "\"other\""))!.ExeSha256);
        }

        [Fact]
        public void Checksum_AcceptsOnlyTheExactFile()
        {
            using var temp = new TempFolder();
            var file = Path.Combine(temp.Path, "NoFences.exe.new");
            File.WriteAllText(file, "abc"); // SHA-256("abc") = ba7816bf…
            Assert.True(UpdateChecker.MatchesChecksum(file, "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
            Assert.True(UpdateChecker.MatchesChecksum(file, null));
            File.WriteAllText(file, "abd");
            Assert.False(UpdateChecker.MatchesChecksum(file, "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
        }
    }
}
