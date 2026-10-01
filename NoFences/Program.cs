using NoFences.Win32;

namespace NoFences
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            using var mutex = new Mutex(true, "No_fences", out var createdNew);
            if (!createdNew)
                return;

            // Shell context menus follow the system dark/light setting.
            Native.SetPreferredAppMode(1);

            ApplicationConfiguration.Initialize();
            Application.SetColorMode(SystemColorMode.System);
            Application.Run(new NoFencesApp());
        }
    }
}
