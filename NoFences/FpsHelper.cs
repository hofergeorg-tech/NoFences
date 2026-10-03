using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using NoFences.Widgets;
using NoFences.Win32;

namespace NoFences
{
    /// <summary>
    /// <c>NoFences.exe --fps-helper &lt;pid&gt;</c>: the opt-in, elevated FPS measurement.
    /// Windows only gives the graphics "present" events (ETW, as used by PresentMon/Afterburner) to
    /// administrators, so this small separate process runs elevated while NoFences itself does not.
    /// It counts presented frames per process, picks the program in front and writes the frame rate
    /// once a second to fps.json. It reads no content and no input, and stops when NoFences exits
    /// or creates fps.stop.
    /// </summary>
    internal static class FpsHelper
    {
        public const string TaskName = @"NoFences\FPS-Helper";
        private const string SessionName = "NoFences-FPS";

        private static readonly Guid Dxgi = new("CA11C036-0102-4A2D-A6AD-F03CFED5D3C9");
        private static readonly Guid D3D9 = new("783ACA0A-790E-4D7F-8451-AA850511C6B9");
        private static readonly Guid DxgKrnl = new("802EC45A-1E99-4B83-9920-87C98277BA9D");

        public static string StopFile => Path.Combine(Path.GetDirectoryName(FpsReading.FilePath)!, "fps.stop");

        public static bool IsElevated()
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>Entry point for the helper process.</summary>
        public static void Run(int parentPid, bool registerTask)
        {
            if (!IsElevated())
                return;
            if (registerTask)
                RegisterTask();
            Directory.CreateDirectory(Path.GetDirectoryName(FpsReading.FilePath)!);
            TryDelete(StopFile);

            var counts = new Dictionary<int, (int dxgi, int d3d9, int kernel)>();
            var gate = new object();

            using var session = new TraceEventSession(SessionName) { StopOnDispose = true };
            session.EnableProvider(Dxgi, TraceEventLevel.Informational);
            session.EnableProvider(D3D9, TraceEventLevel.Informational);
            // Base | Present keywords: present history for Vulkan/OpenGL and other non-DXGI paths
            session.EnableProvider(DxgKrnl, TraceEventLevel.Informational, 0x1 | 0x8000000);

            session.Source.Dynamic.All += e =>
            {
                if (e.ProcessID <= 0 || e.Opcode != TraceEventOpcode.Start)
                    return;
                var task = e.TaskName ?? "";
                lock (gate)
                {
                    counts.TryGetValue(e.ProcessID, out var c);
                    if (e.ProviderGuid == Dxgi && task == "Present")
                        c.dxgi++;
                    else if (e.ProviderGuid == D3D9 && task == "Present")
                        c.d3d9++;
                    else if (e.ProviderGuid == DxgKrnl && task.StartsWith("PresentHistory", StringComparison.Ordinal))
                        c.kernel++;
                    else
                        return;
                    counts[e.ProcessID] = c;
                }
            };
            var processing = new Thread(() => session.Source.Process()) { IsBackground = true, Name = "ETW" };
            processing.Start();

            var last = 0;
            var lastTick = Stopwatch.StartNew();
            while (!ShouldStop(parentPid))
            {
                Thread.Sleep(1000);
                Dictionary<int, (int dxgi, int d3d9, int kernel)> snapshot;
                lock (gate)
                {
                    snapshot = new Dictionary<int, (int, int, int)>(counts);
                    counts.Clear();
                }
                var seconds = lastTick.Elapsed.TotalSeconds;
                lastTick.Restart();

                // Prefer the API-level count; kernel present history only for apps without DXGI/D3D9 events.
                var fps = snapshot.ToDictionary(kv => kv.Key, kv => (kv.Value.dxgi > 0 ? kv.Value.dxgi : kv.Value.d3d9 > 0 ? kv.Value.d3d9 : kv.Value.kernel) / seconds);
                var target = ChooseTarget(fps, last);
                if (target == 0)
                    continue;
                last = target;
                try
                {
                    new FpsReading(ProcessName(target), fps.GetValueOrDefault(target), DateTime.Now).Write();
                }
                catch (IOException) { }
            }
        }

        /// <summary>The program in front if it draws frames, else the one measured last, else the busiest.</summary>
        private static int ChooseTarget(Dictionary<int, double> fps, int last)
        {
            Native.GetWindowThreadProcessId(Native.GetForegroundWindow(), out var fg);
            if (fps.GetValueOrDefault((int)fg) > 0)
                return (int)fg;
            if (last != 0 && fps.GetValueOrDefault(last) > 0)
                return last;
            var self = Environment.ProcessId;
            var busiest = fps.Where(kv => kv.Key != self && kv.Value >= 1 && !IsSystemProcess(kv.Key)).OrderByDescending(kv => kv.Value).FirstOrDefault();
            return busiest.Key;
        }

        private static bool IsSystemProcess(int pid)
        {
            var name = ProcessName(pid);
            return name is "dwm" or "explorer" or "NoFences" or "csrss" or "System";
        }

        private static string ProcessName(int pid)
        {
            try
            {
                using var p = Process.GetProcessById(pid);
                return p.ProcessName;
            }
            catch (ArgumentException)
            {
                return "?";
            }
        }

        /// <summary>Stop when asked (fps.stop) or when NoFences is gone.</summary>
        private static bool ShouldStop(int parentPid)
        {
            if (File.Exists(StopFile))
                return true;
            if (parentPid > 0)
            {
                try
                {
                    using var parent = Process.GetProcessById(parentPid);
                    return parent.HasExited;
                }
                catch (ArgumentException)
                {
                    return true;
                }
            }
            // Started by the scheduled task: keep running while any NoFences window process exists.
            return Process.GetProcessesByName("NoFences").All(p => p.Id == Environment.ProcessId);
        }

        /// <summary>
        /// A task that NoFences may start later without a UAC prompt (schtasks /Run works without admin
        /// for tasks the user created). It never runs by itself: no trigger is used.
        /// </summary>
        private static void RegisterTask()
        {
            var exe = Environment.ProcessPath!;
            RunSchtasks($"/Create /TN \"{TaskName}\" /TR \"\\\"{exe}\\\" --fps-helper 0\" /SC ONCE /ST 00:00 /SD 01/01/2000 /RL HIGHEST /F");
        }

        public static void UnregisterTask() => RunSchtasks($"/Delete /TN \"{TaskName}\" /F");

        public static int RunSchtasks(string arguments)
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo("schtasks.exe", arguments) { CreateNoWindow = true, UseShellExecute = false });
                p?.WaitForExit(10_000);
                return p?.ExitCode ?? -1;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private static void TryDelete(string path)
        {
            try { File.Delete(path); } catch { }
        }
    }
}
