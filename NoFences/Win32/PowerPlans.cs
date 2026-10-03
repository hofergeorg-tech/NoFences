using System.Runtime.InteropServices;

namespace NoFences.Win32
{
    public sealed record PowerPlan(Guid Id, string Name, bool Active);

    /// <summary>Windows power plans (Balanced, High performance …): list them and switch the active one.</summary>
    public static class PowerPlans
    {
        private const uint AccessScheme = 16;

        public static List<PowerPlan> All()
        {
            var list = new List<PowerPlan>();
            try
            {
                var active = Active();
                var buffer = new byte[16];
                for (uint i = 0; ; i++)
                {
                    var size = (uint)buffer.Length;
                    if (PowerEnumerate(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, AccessScheme, i, buffer, ref size) != 0)
                        break;
                    var id = new Guid(buffer);
                    list.Add(new PowerPlan(id, FriendlyName(id), id == active));
                }
            }
            catch (Exception) { }
            return list;
        }

        public static Guid? Active()
        {
            try
            {
                if (PowerGetActiveScheme(IntPtr.Zero, out var ptr) != 0)
                    return null;
                try
                {
                    return Marshal.PtrToStructure<Guid>(ptr);
                }
                finally
                {
                    LocalFree(ptr);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool SetActive(Guid id)
        {
            try
            {
                return PowerSetActiveScheme(IntPtr.Zero, ref id) == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string FriendlyName(Guid id)
        {
            uint size = 0;
            PowerReadFriendlyName(IntPtr.Zero, ref id, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref size);
            if (size == 0)
                return id.ToString();
            var buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                return PowerReadFriendlyName(IntPtr.Zero, ref id, IntPtr.Zero, IntPtr.Zero, buffer, ref size) == 0
                    ? Marshal.PtrToStringUni(buffer) ?? id.ToString()
                    : id.ToString();
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        [DllImport("powrprof.dll")]
        private static extern uint PowerEnumerate(IntPtr root, IntPtr subGroup, IntPtr setting, uint access, uint index, byte[] buffer, ref uint size);

        [DllImport("powrprof.dll")]
        private static extern uint PowerReadFriendlyName(IntPtr root, ref Guid scheme, IntPtr subGroup, IntPtr setting, IntPtr buffer, ref uint size);

        [DllImport("powrprof.dll")]
        private static extern uint PowerGetActiveScheme(IntPtr root, out IntPtr scheme);

        [DllImport("powrprof.dll")]
        private static extern uint PowerSetActiveScheme(IntPtr root, ref Guid scheme);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr memory);
    }
}
