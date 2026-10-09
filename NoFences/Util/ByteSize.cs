namespace NoFences.Util
{
    public static class ByteSize
    {
        /// <summary>"512 B", "3.4 MB", "120 GB" – one decimal below 100.</summary>
        public static string Format(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double value = bytes;
            var unit = 0;
            while (value >= 1024 && unit < units.Length - 1)
            {
                value /= 1024;
                unit++;
            }
            return value >= 100 || unit == 0 ? $"{value:0} {units[unit]}" : $"{value:0.#} {units[unit]}";
        }
    }
}
