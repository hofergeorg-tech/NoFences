using System.Diagnostics;

namespace NoFences.Util
{
    /// <summary>
    /// Starts programs, documents and links on a short-lived STA thread (shell extensions need one):
    /// ShellExecute can take seconds (virus scanner, slow launchers, network shortcuts), and meanwhile
    /// all fences would freeze.
    /// </summary>
    public static class Launcher
    {
        /// <param name="failed">Called on the UI thread if starting failed.</param>
        public static void Start(ProcessStartInfo info, Action<Exception>? failed = null)
        {
            var ui = SynchronizationContext.Current;
            var thread = new Thread(() =>
            {
                try
                {
                    Process.Start(info)?.Dispose();
                }
                catch (Exception e)
                {
                    if (failed == null)
                        return;
                    if (ui != null)
                        ui.Post(_ => failed(e), null);
                    else
                        failed(e);
                }
            }) { IsBackground = true, Name = "Launcher" };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        /// <summary>Like <see cref="Start"/>, showing the error in a message box.</summary>
        public static void StartOrWarn(ProcessStartInfo info) =>
            Start(info, e => MessageBox.Show(e.Message, "NoFences", MessageBoxButtons.OK, MessageBoxIcon.Warning));
    }
}
