using System.Runtime.InteropServices;
using Windows.Devices.Enumeration.Pnp;

namespace NoFences.Widgets
{
    /// <summary>A device's charge: 0..1, or null for "on cable" (wired controller).</summary>
    public sealed record DeviceBattery(string Name, double? Level);

    /// <summary>
    /// Charge of game controllers (XInput, e.g. Xbox controllers) and Bluetooth devices that report it to
    /// Windows (headsets, many mice and keyboards). Read without admin rights.
    /// </summary>
    public static class DeviceBatteries
    {
        // DEVPKEY_Bluetooth_Battery: what Windows shows under "Bluetooth & devices"
        private const string BluetoothBatteryKey = "{104EA319-6EE2-4701-BD47-8DDBF425BBE5} 2";
        private const string NameKey = "System.ItemNameDisplay";

        public static async Task<List<DeviceBattery>> ReadAsync()
        {
            var result = Controllers();
            try
            {
                var devices = await PnpObject.FindAllAsync(PnpObjectType.Device, new[] { BluetoothBatteryKey, NameKey }, "");
                foreach (var device in devices)
                {
                    if (device.Properties.TryGetValue(BluetoothBatteryKey, out var value) && value is byte percent && percent <= 100)
                    {
                        var name = device.Properties.TryGetValue(NameKey, out var n) ? n as string : null;
                        if (!string.IsNullOrWhiteSpace(name) && !result.Any(r => r.Name == name))
                            result.Add(new DeviceBattery(name, percent / 100.0));
                    }
                }
            }
            catch (Exception e)
            {
                Util.Log.Write("Device batteries", Util.Log.Describe(e));
            }
            return result;
        }

        /// <summary>XInput only knows four steps: empty, low, medium, full.</summary>
        public static double? XInputLevel(byte type, byte level) => type switch
        {
            BatteryTypeWired => null,
            _ => level switch { 0 => 0.05, 1 => 0.3, 2 => 0.65, _ => 1.0 }
        };

        private static List<DeviceBattery> Controllers()
        {
            var result = new List<DeviceBattery>();
            for (uint i = 0; i < 4; i++)
            {
                try
                {
                    if (XInputGetBatteryInformation(i, 0, out var info) != 0 || info.BatteryType == BatteryTypeDisconnected)
                        continue;
                    result.Add(new DeviceBattery(Util.Strings.ControllerName((int)i + 1), XInputLevel(info.BatteryType, info.BatteryLevel)));
                }
                catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
                {
                    break;
                }
            }
            return result;
        }

        private const byte BatteryTypeDisconnected = 0, BatteryTypeWired = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct XInputBattery
        {
            public byte BatteryType;
            public byte BatteryLevel;
        }

        [DllImport("xinput1_4.dll")]
        private static extern int XInputGetBatteryInformation(uint userIndex, byte deviceType, out XInputBattery info);
    }
}
