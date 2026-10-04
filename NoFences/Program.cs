using NoFences.Util;
using NoFences.Win32;

namespace NoFences
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            CleanInheritedEnvironment();
            // Elevated FPS helper modes (no UI, no single-instance lock)
            if (args.Length >= 1 && args[0] == "--fps-helper")
            {
                var parent = args.Length >= 2 && int.TryParse(args[1], out var pid) ? pid : 0;
                FpsHelper.Run(parent, registerTask: args.Contains("--register-task"));
                return;
            }
            if (args.Length == 1 && args[0] == "--fps-helper-uninstall")
            {
                FpsHelper.UnregisterTask();
                return;
            }

            args = UpdateChecker.FinishUpdate(args);
            if (args.Length == 1 && args[0] == "--install-update")
            {
                // Non-interactive update (scripts/testing): install the latest release if newer.
                ApplicationConfiguration.Initialize();
                var latest = UpdateChecker.GetLatestAsync().GetAwaiter().GetResult();
                if (latest != null && UpdateChecker.IsNewer(latest))
                    UpdateChecker.InstallAsync(latest).GetAwaiter().GetResult();
                return;
            }

            if (args.Length == 2 && args[0] == "--preview")
            {
                ApplicationConfiguration.Initialize();
                PreviewRenderer.Run(args[1]);
                return;
            }
            if (args.Length == 3 && args[0] == "--webshot")
            {
                // Checks the web page widget's capture without the full app: --webshot <url> <file.png>
                ApplicationConfiguration.Initialize();
                Widgets.WebPageWidget.Webshot(args[1], args[2]);
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

        /// <summary>
        /// Everything opened from a fence inherits our environment. When NoFences was started from a
        /// VS Code terminal (or another Electron app), ELECTRON_RUN_AS_NODE=1 is set and makes Electron
        /// apps like the RSI Launcher, Discord or Slack start as a bare Node process and exit silently.
        /// </summary>
        private static void CleanInheritedEnvironment()
        {
            foreach (var name in Environment.GetEnvironmentVariables().Keys.Cast<string>().ToList())
            {
                if (name.StartsWith("ELECTRON_", StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("VSCODE_", StringComparison.OrdinalIgnoreCase))
                    Environment.SetEnvironmentVariable(name, null);
            }
        }
    }
}
