namespace NoFences.Model
{
    /// <summary>
    /// "Take a break" after a stretch of active PC use. Being away (no input) for 5 minutes counts as a
    /// break and starts the count again.
    /// </summary>
    public sealed class BreakTracker
    {
        public static readonly TimeSpan AwayIsBreak = TimeSpan.FromMinutes(5);

        private DateTime? activeSince;

        /// <summary>True when it's time to remind; the next stretch starts right away.</summary>
        public bool Update(DateTime now, TimeSpan idle, TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero || idle >= AwayIsBreak)
            {
                activeSince = null;
                return false;
            }
            activeSince ??= now - idle;
            if (now - activeSince.Value < interval)
                return false;
            activeSince = now;
            return true;
        }
    }
}
