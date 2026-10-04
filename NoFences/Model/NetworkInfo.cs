using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace NoFences.Model
{
    /// <summary>
    /// The PC's addresses for Tools → Network info: local IPv4/IPv6 per adapter, the Wi-Fi name and
    /// signal (Native Wifi API) and, on request, the public address (api.ipify.org).
    /// </summary>
    public static class NetworkInfo
    {
        public sealed record Adapter(string Name, string Kind, IReadOnlyList<string> IPv4, IReadOnlyList<string> IPv6, string? Gateway, string? Mac);

        public sealed record Wifi(string Name, int SignalPercent);

        /// <summary>Connected adapters, the one with a default gateway first.</summary>
        public static List<Adapter> Adapters()
        {
            var result = new List<Adapter>();
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up
                    || nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                    continue;
                var props = nic.GetIPProperties();
                var v4 = props.UnicastAddresses.Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork).Select(a => a.Address.ToString()).ToList();
                var v6 = props.UnicastAddresses
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6 && !a.Address.IsIPv6LinkLocal)
                    .Select(a => a.Address.ToString()).ToList();
                if (v4.Count == 0 && v6.Count == 0)
                    continue;
                var gateway = props.GatewayAddresses.Select(g => g.Address).FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork && !a.Equals(IPAddress.Any));
                var mac = FormatMac(nic.GetPhysicalAddress().GetAddressBytes());
                var kind = nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ? "Wi-Fi" : nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet ? "Ethernet" : nic.NetworkInterfaceType.ToString();
                result.Add(new Adapter(nic.Name, kind, v4, v6, gateway?.ToString(), mac));
            }
            return result.OrderBy(a => a.Gateway == null).ToList();
        }

        public static string? FormatMac(byte[] bytes) =>
            bytes.Length == 6 && bytes.Any(b => b != 0) ? string.Join("-", bytes.Select(b => b.ToString("X2"))) : null;

        /// <summary>The public address as the internet sees it, or null without connection.</summary>
        public static async Task<string?> PublicAddressAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                var text = (await Util.Web.Http.GetStringAsync("https://api.ipify.org", cts.Token)).Trim();
                return IPAddress.TryParse(text, out _) ? text : null;
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                return null;
            }
        }

        #region Wi-Fi (Native Wifi API)

        [DllImport("wlanapi.dll")]
        private static extern int WlanOpenHandle(int clientVersion, IntPtr reserved, out int negotiatedVersion, out IntPtr handle);

        [DllImport("wlanapi.dll")]
        private static extern int WlanCloseHandle(IntPtr handle, IntPtr reserved);

        [DllImport("wlanapi.dll")]
        private static extern int WlanEnumInterfaces(IntPtr handle, IntPtr reserved, out IntPtr list);

        [DllImport("wlanapi.dll")]
        private static extern int WlanQueryInterface(IntPtr handle, ref Guid interfaceGuid, int opCode, IntPtr reserved, out int size, out IntPtr data, out int valueType);

        [DllImport("wlanapi.dll")]
        private static extern void WlanFreeMemory(IntPtr memory);

        private const int CurrentConnection = 7;  // wlan_intf_opcode_current_connection
        private const int StateConnected = 1;     // wlan_interface_state_connected
        private const int InterfaceInfoSize = 16 + 512 + 4;

        /// <summary>The connected Wi-Fi networks (name and signal), empty without Wi-Fi.</summary>
        public static List<Wifi> WifiConnections()
        {
            var result = new List<Wifi>();
            IntPtr handle = IntPtr.Zero, list = IntPtr.Zero;
            try
            {
                if (WlanOpenHandle(2, IntPtr.Zero, out _, out handle) != 0 || WlanEnumInterfaces(handle, IntPtr.Zero, out list) != 0)
                    return result;
                var count = Marshal.ReadInt32(list);
                for (var i = 0; i < count; i++)
                {
                    var item = list + 8 + i * InterfaceInfoSize;
                    var guid = Marshal.PtrToStructure<Guid>(item);
                    if (Marshal.ReadInt32(item + 16 + 512) != StateConnected)
                        continue;
                    if (WlanQueryInterface(handle, ref guid, CurrentConnection, IntPtr.Zero, out _, out var data, out _) != 0)
                        continue;
                    try
                    {
                        result.Add(ReadConnection(data));
                    }
                    finally
                    {
                        WlanFreeMemory(data);
                    }
                }
            }
            catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
            {
                // No Wi-Fi service on this PC
            }
            finally
            {
                if (list != IntPtr.Zero)
                    WlanFreeMemory(list);
                if (handle != IntPtr.Zero)
                    WlanCloseHandle(handle, IntPtr.Zero);
            }
            return result;
        }

        /// <summary>WLAN_CONNECTION_ATTRIBUTES: profile name at 8, SSID length/bytes at 520/524, signal quality at 576.</summary>
        private static Wifi ReadConnection(IntPtr data)
        {
            var profile = Marshal.PtrToStringUni(data + 8) ?? "";
            var ssidLength = Math.Clamp(Marshal.ReadInt32(data + 520), 0, 32);
            var ssidBytes = new byte[ssidLength];
            Marshal.Copy(data + 524, ssidBytes, 0, ssidLength);
            var ssid = Encoding.UTF8.GetString(ssidBytes);
            var signal = Math.Clamp(Marshal.ReadInt32(data + 576), 0, 100);
            // Without location permission Windows hides the network name; the profile is usually named like it
            return new Wifi(ssid.Length > 0 ? ssid : profile, signal);
        }

        #endregion
    }
}
