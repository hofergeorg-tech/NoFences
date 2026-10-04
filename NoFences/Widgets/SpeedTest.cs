using System.Diagnostics;
using System.Net.Http;

namespace NoFences.Widgets
{
    /// <summary>
    /// A short speed test against Cloudflare's public speed test servers (speed.cloudflare.com):
    /// downloads and uploads for a few seconds each and measures the throughput.
    /// </summary>
    public static class SpeedTest
    {
        private const string DownUrl = "https://speed.cloudflare.com/__down?bytes=";
        private const string UpUrl = "https://speed.cloudflare.com/__up";
        private static readonly TimeSpan PhaseLength = TimeSpan.FromSeconds(6);

        public sealed record Result(double DownMbit, double UpMbit, DateTime At);

        /// <summary>Megabits per second.</summary>
        public static double Mbit(long bytes, TimeSpan time) => time.TotalSeconds <= 0 ? 0 : bytes * 8 / 1_000_000.0 / time.TotalSeconds;

        public static async Task<Result> RunAsync(IProgress<string>? progress, CancellationToken cancel = default)
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("NoFences");
            progress?.Report("↓");
            var down = await DownloadAsync(http, cancel);
            progress?.Report("↑");
            var up = await UploadAsync(http, cancel);
            return new Result(down, up, DateTime.Now);
        }

        private static async Task<double> DownloadAsync(HttpClient http, CancellationToken cancel)
        {
            // Several requests one after the other until the time is up; the first half second is
            // left out (connection setup, TCP slow start)
            var buffer = new byte[1 << 16];
            var clock = Stopwatch.StartNew();
            long counted = 0;
            TimeSpan? countFrom = null;
            while (clock.Elapsed < PhaseLength)
            {
                using var response = await http.GetAsync(DownUrl + 25_000_000, HttpCompletionOption.ResponseHeadersRead, cancel);
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(cancel);
                int read;
                while ((read = await stream.ReadAsync(buffer, cancel)) > 0)
                {
                    if (clock.Elapsed >= TimeSpan.FromSeconds(0.5))
                    {
                        countFrom ??= clock.Elapsed;
                        counted += read;
                    }
                    if (clock.Elapsed >= PhaseLength)
                        break;
                }
            }
            return Mbit(counted, clock.Elapsed - (countFrom ?? TimeSpan.Zero));
        }

        private static async Task<double> UploadAsync(HttpClient http, CancellationToken cancel)
        {
            var chunk = new byte[2_000_000];
            Random.Shared.NextBytes(chunk); // not compressible
            var clock = Stopwatch.StartNew();
            long sent = 0;
            while (clock.Elapsed < PhaseLength)
            {
                using var content = new ByteArrayContent(chunk);
                using var response = await http.PostAsync(UpUrl, content, cancel);
                response.EnsureSuccessStatusCode();
                sent += chunk.Length;
            }
            return Mbit(sent, clock.Elapsed);
        }
    }
}
