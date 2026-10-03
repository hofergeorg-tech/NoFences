using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NoFences.Widgets
{
    /// <summary>
    /// CPU, RAM and GPU readings without admin rights. GPU load/temperature come from NVIDIA's NVML
    /// (ships with the driver) when available, otherwise GPU load from the "GPU Engine" counters.
    /// </summary>
    public sealed class SystemStats : IDisposable
    {
        public double CpuLoad { get; private set; }          // 0..1
        public double RamUsed { get; private set; }          // 0..1
        public ulong RamTotalBytes { get; private set; }
        public ulong RamUsedBytes { get; private set; }
        public double? GpuLoad { get; private set; }         // 0..1, null = unknown
        public int? GpuTemperature { get; private set; }     // °C, null = unknown
        public string? GpuName { get; private set; }

        private long lastIdle, lastKernel, lastUser;
        private readonly Nvml? nvml = Nvml.TryOpen();
        private List<PerformanceCounter>? gpuCounters;
        private DateTime gpuCountersRefreshed;

        public void Sample()
        {
            SampleCpu();
            SampleRam();
            if (nvml != null)
            {
                GpuLoad = nvml.GpuPercent() / 100.0;
                GpuTemperature = nvml.Temperature();
                GpuName = nvml.Name;
            }
            else
            {
                GpuLoad = SampleGpuCounters();
            }
        }

        private void SampleCpu()
        {
            if (!GetSystemTimes(out var idle, out var kernel, out var user))
                return;
            long i = idle.ToLong(), k = kernel.ToLong(), u = user.ToLong();
            var idleDelta = i - lastIdle;
            var totalDelta = (k - lastKernel) + (u - lastUser); // kernel time includes idle time
            if (lastKernel != 0 && totalDelta > 0)
                CpuLoad = Math.Clamp(1.0 - (double)idleDelta / totalDelta, 0, 1);
            (lastIdle, lastKernel, lastUser) = (i, k, u);
        }

        private void SampleRam()
        {
            var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            if (!GlobalMemoryStatusEx(ref status))
                return;
            RamTotalBytes = status.ullTotalPhys;
            RamUsedBytes = status.ullTotalPhys - status.ullAvailPhys;
            RamUsed = RamTotalBytes == 0 ? 0 : (double)RamUsedBytes / RamTotalBytes;
        }

        /// <summary>Sum of the 3D engines' utilization over all processes (Task Manager's GPU column).</summary>
        private double? SampleGpuCounters()
        {
            try
            {
                // The set of counter instances changes as processes start; refresh it now and then.
                if (gpuCounters == null || DateTime.Now - gpuCountersRefreshed > TimeSpan.FromSeconds(10))
                {
                    gpuCounters?.ForEach(c => c.Dispose());
                    var category = new PerformanceCounterCategory("GPU Engine");
                    gpuCounters = category.GetInstanceNames()
                        .Where(n => n.EndsWith("engtype_3D", StringComparison.OrdinalIgnoreCase))
                        .Select(n => new PerformanceCounter("GPU Engine", "Utilization Percentage", n, readOnly: true))
                        .ToList();
                    gpuCounters.ForEach(c => c.NextValue()); // first read is always 0
                    gpuCountersRefreshed = DateTime.Now;
                    return null;
                }
                var sum = 0.0;
                foreach (var c in gpuCounters)
                {
                    try { sum += c.NextValue(); } catch (InvalidOperationException) { } // process exited
                }
                return Math.Clamp(sum / 100.0, 0, 1);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"GPU counters: {e.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            gpuCounters?.ForEach(c => c.Dispose());
            nvml?.Dispose();
        }

        #region Native

        [StructLayout(LayoutKind.Sequential)]
        private struct FILETIME
        {
            public uint Low, High;
            public long ToLong() => ((long)High << 32) | Low;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength, dwMemoryLoad;
            public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile, ullAvailPageFile, ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(out FILETIME idle, out FILETIME kernel, out FILETIME user);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX buffer);

        /// <summary>Minimal NVIDIA Management Library binding (nvml.dll is installed with the driver).</summary>
        private sealed class Nvml : IDisposable
        {
            private readonly IntPtr device;
            public string? Name { get; }

            [StructLayout(LayoutKind.Sequential)]
            private struct NvmlUtilization { public uint gpu, memory; }

            [DllImport("nvml.dll")] private static extern int nvmlInit_v2();
            [DllImport("nvml.dll")] private static extern int nvmlShutdown();
            [DllImport("nvml.dll")] private static extern int nvmlDeviceGetHandleByIndex_v2(uint index, out IntPtr device);
            [DllImport("nvml.dll")] private static extern int nvmlDeviceGetTemperature(IntPtr device, int sensor, out uint temp);
            [DllImport("nvml.dll")] private static extern int nvmlDeviceGetUtilizationRates(IntPtr device, out NvmlUtilization util);
            [DllImport("nvml.dll", CharSet = CharSet.Ansi)] private static extern int nvmlDeviceGetName(IntPtr device, byte[] name, uint length);

            private Nvml(IntPtr device, string? name)
            {
                this.device = device;
                Name = name;
            }

            public static Nvml? TryOpen()
            {
                try
                {
                    if (nvmlInit_v2() != 0 || nvmlDeviceGetHandleByIndex_v2(0, out var dev) != 0)
                        return null;
                    var buffer = new byte[96];
                    var name = nvmlDeviceGetName(dev, buffer, (uint)buffer.Length) == 0
                        ? System.Text.Encoding.ASCII.GetString(buffer).TrimEnd('\0')
                        : null;
                    return new Nvml(dev, name);
                }
                catch (Exception) // DllNotFoundException etc. – no NVIDIA card/driver
                {
                    return null;
                }
            }

            public uint GpuPercent() => nvmlDeviceGetUtilizationRates(device, out var u) == 0 ? u.gpu : 0;

            public int? Temperature() => nvmlDeviceGetTemperature(device, 0, out var t) == 0 ? (int)t : null;

            public void Dispose()
            {
                try { nvmlShutdown(); } catch { }
            }
        }

        #endregion
    }
}
