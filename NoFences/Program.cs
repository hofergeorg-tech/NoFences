using NoFences.Win32;

namespace NoFences
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "--preview")
            {
                ApplicationConfiguration.Initialize();
                PreviewRenderer.Run(args[1]);
                return;
            }
            if (args.Length == 2 && args[0] == "--show")
            {
                // Opens an embedded document (e.g. HILFE.md) on its own; used to check the help texts.
                ApplicationConfiguration.Initialize();
                DocumentViewer.ShowDocument(args[1], args[1]);
                Application.Run(Application.OpenForms[0]!);
                return;
            }

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
