using System.Text.Json;

namespace NoFences.Widgets
{
    /// <summary>
    /// What the (optional, elevated) FPS helper measured: frames per second of the program in front.
    /// The helper writes it once a second to a small JSON file that NoFences reads.
    /// </summary>
    public sealed record FpsReading(string Process, double Fps, DateTime Time)
    {
        public static string FilePath => Path.Combine(Util.AppData.Cache, "fps.json");

        private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

        /// <summary>The latest reading if it is fresh (helper running), otherwise null.</summary>
        public static FpsReading? TryRead()
        {
            try
            {
                var reading = JsonSerializer.Deserialize<FpsReading>(File.ReadAllText(FilePath), Options);
                return reading != null && DateTime.Now - reading.Time < TimeSpan.FromSeconds(4) ? reading : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void Write()
        {
            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(this, Options));
            File.Move(tmp, FilePath, overwrite: true);
        }
    }
}
