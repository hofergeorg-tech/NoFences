using System.Net.Http;

namespace NoFences.Util
{
    /// <summary>One HTTP client for the online widgets (calendar, news, prices).</summary>
    public static class Web
    {
        public static HttpClient Http { get; } = Create();

        private static HttpClient Create()
        {
            var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd($"Mozilla/5.0 (Windows NT 10.0; Win64; x64) NoFences/{UpdateChecker.CurrentVersion}");
            return http;
        }

        /// <summary>webcal:// links are plain https.</summary>
        public static string NormalizeUrl(string url)
        {
            url = url.Trim();
            if (url.StartsWith("webcal://", StringComparison.OrdinalIgnoreCase))
                return "https://" + url["webcal://".Length..];
            return url;
        }
    }
}
