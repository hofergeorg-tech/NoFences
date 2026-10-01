using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

namespace NoFences.Util
{
    public sealed record ReleaseInfo(Version Version, string Tag, string PageUrl, string? ExeUrl, long ExeSize);

    /// <summary>
    /// Checks GitHub for a newer release and replaces the running single-file exe with it.
    /// A running exe can't be overwritten but can be renamed, so: rename to .old, put the new
    /// file in place, start it, exit; the new process deletes the .old file.
    /// </summary>
    public static class UpdateChecker
    {
        public const string Repository = "hofergeorg-tech/NoFences";
        private const string AfterUpdateArg = "--after-update";

        // Must be initialized before Http (static initializers run in source order).
        public static Version CurrentVersion { get; } = Normalize(typeof(UpdateChecker).Assembly.GetName().Version ?? new Version(0, 0, 0));

        private static readonly HttpClient Http = CreateClient();

        /// <summary>Only the published single-file exe can replace itself (Assembly.Location is empty there).</summary>
#pragma warning disable IL3000 // the empty Location is exactly what identifies the single-file build
        public static bool CanSelfUpdate =>
            string.IsNullOrEmpty(typeof(UpdateChecker).Assembly.Location) && Environment.ProcessPath != null;
#pragma warning restore IL3000

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("NoFences", CurrentVersion.ToString()));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            return client;
        }

        private static Version Normalize(Version v) => new(v.Major, v.Minor, Math.Max(0, v.Build));

        /// <summary>The latest release, or null if there is none or it isn't parseable.</summary>
        public static async Task<ReleaseInfo?> GetLatestAsync()
        {
            using var response = await Http.GetAsync($"https://api.github.com/repos/{Repository}/releases/latest");
            if (!response.IsSuccessStatusCode)
                return null;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            var tag = root.GetProperty("tag_name").GetString() ?? "";
            if (!Version.TryParse(tag.TrimStart('v', 'V'), out var version))
                return null;

            string? exeUrl = null;
            long size = 0;
            foreach (var asset in root.GetProperty("assets").EnumerateArray())
            {
                if (asset.GetProperty("name").GetString()?.Equals("NoFences.exe", StringComparison.OrdinalIgnoreCase) == true)
                {
                    exeUrl = asset.GetProperty("browser_download_url").GetString();
                    size = asset.GetProperty("size").GetInt64();
                }
            }
            return new ReleaseInfo(Normalize(version), tag, root.GetProperty("html_url").GetString() ?? "", exeUrl, size);
        }

        public static bool IsNewer(ReleaseInfo release) => release.Version > CurrentVersion;

        /// <summary>Downloads the new exe, swaps it in and starts it. The caller must exit right afterwards.</summary>
        public static async Task InstallAsync(ReleaseInfo release)
        {
            if (!CanSelfUpdate || release.ExeUrl == null)
                throw new InvalidOperationException("Self-update is not possible for this installation.");

            var exe = Environment.ProcessPath!;
            var download = exe + ".new";
            var old = exe + ".old";

            using (var response = await Http.GetAsync(release.ExeUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                await using var file = File.Create(download);
                await response.Content.CopyToAsync(file);
            }
            if (new FileInfo(download).Length != release.ExeSize)
            {
                File.Delete(download);
                throw new IOException("Download incomplete.");
            }

            if (File.Exists(old))
                File.Delete(old);
            File.Move(exe, old);
            try
            {
                File.Move(download, exe);
            }
            catch
            {
                File.Move(old, exe); // put the working version back
                throw;
            }

            Process.Start(new ProcessStartInfo(exe, $"{AfterUpdateArg} {Environment.ProcessId}") { UseShellExecute = false });
        }

        /// <summary>
        /// Called first thing in Main: waits for the previous version to exit and removes its file.
        /// Returns the remaining arguments.
        /// </summary>
        public static string[] FinishUpdate(string[] args)
        {
            if (args.Length >= 2 && args[0] == AfterUpdateArg && int.TryParse(args[1], out var pid))
            {
                try
                {
                    using var previous = Process.GetProcessById(pid);
                    previous.WaitForExit(15000);
                }
                catch (ArgumentException)
                {
                    // already gone
                }
                args = args[2..];
            }

            var leftover = Environment.ProcessPath + ".old";
            for (var i = 0; i < 10 && File.Exists(leftover); i++)
            {
                try { File.Delete(leftover); }
                catch { Thread.Sleep(300); }
            }
            return args;
        }
    }
}
