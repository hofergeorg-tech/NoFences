using System.Runtime.InteropServices;

namespace NoFences.Win32
{
    public sealed record AudioDevice(string Id, string Name, bool IsDefault);

    /// <summary>
    /// Windows Core Audio: the playback devices, the default one's volume and mute, the microphone's mute,
    /// and switching the default playback device (IPolicyConfig, what the Sound settings use too).
    /// All calls catch COM errors and return a neutral value.
    /// </summary>
    public static class CoreAudio
    {
        private const int Render = 0, Capture = 1;
        private const int Multimedia = 1;
        private const int DeviceStateActive = 1;
        private static Guid noContext = Guid.Empty;

        public static List<AudioDevice> Outputs()
        {
            var list = new List<AudioDevice>();
            try
            {
                var enumerator = Enumerator();
                var defaultId = DefaultId(enumerator, Render);
                Check(enumerator.EnumAudioEndpoints(Render, DeviceStateActive, out var devices));
                Check(devices.GetCount(out var count));
                for (uint i = 0; i < count; i++)
                {
                    Check(devices.Item(i, out var device));
                    Check(device.GetId(out var id));
                    list.Add(new AudioDevice(id, FriendlyName(device), id == defaultId));
                }
            }
            catch (Exception) { }
            return list;
        }

        public static float Volume
        {
            get => WithVolume(Render, v => { v.GetMasterVolumeLevelScalar(out var level); return level; }, 0f);
            set => WithVolume(Render, v => v.SetMasterVolumeLevelScalar(Math.Clamp(value, 0f, 1f), ref noContext), 0);
        }

        public static bool Muted
        {
            get => WithVolume(Render, v => { v.GetMute(out var m); return m; }, false);
            set => WithVolume(Render, v => v.SetMute(value, ref noContext), 0);
        }

        /// <summary>Null when there is no microphone.</summary>
        public static bool? MicMuted
        {
            get => WithVolume<bool?>(Capture, v => { v.GetMute(out var m); return m; }, null);
            set
            {
                if (value is bool mute)
                    WithVolume(Capture, v => v.SetMute(mute, ref noContext), 0);
            }
        }

        public static void SetDefault(string deviceId)
        {
            try
            {
                var policy = (IPolicyConfig)new PolicyConfigClient();
                // Console, multimedia and communications – like choosing it in the sound settings
                for (var role = 0; role < 3; role++)
                    policy.SetDefaultEndpoint(deviceId, role);
            }
            catch (Exception) { }
        }

        #region Plumbing

        private static IMMDeviceEnumerator Enumerator() => (IMMDeviceEnumerator)new MMDeviceEnumerator();

        private static string? DefaultId(IMMDeviceEnumerator enumerator, int flow)
        {
            if (enumerator.GetDefaultAudioEndpoint(flow, Multimedia, out var device) != 0)
                return null;
            device.GetId(out var id);
            return id;
        }

        private static T WithVolume<T>(int flow, Func<IAudioEndpointVolume, T> action, T fallback)
        {
            try
            {
                var enumerator = Enumerator();
                if (enumerator.GetDefaultAudioEndpoint(flow, Multimedia, out var device) != 0)
                    return fallback;
                var iid = typeof(IAudioEndpointVolume).GUID;
                Check(device.Activate(ref iid, 23 /* CLSCTX_ALL */, IntPtr.Zero, out var obj));
                return action((IAudioEndpointVolume)obj);
            }
            catch (Exception)
            {
                return fallback;
            }
        }

        private static string FriendlyName(IMMDevice device)
        {
            try
            {
                Check(device.OpenPropertyStore(0 /* STGM_READ */, out var store));
                var key = new PropertyKey { fmtid = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), pid = 14 };
                Check(store.GetValue(ref key, out var value));
                try
                {
                    return value.vt == 31 /* VT_LPWSTR */ ? Marshal.PtrToStringUni(value.pointer) ?? "?" : "?";
                }
                finally
                {
                    PropVariantClear(ref value);
                }
            }
            catch (Exception)
            {
                return "?";
            }
        }

        private static void Check(int hr)
        {
            if (hr != 0)
                Marshal.ThrowExceptionForHR(hr);
        }

        [DllImport("ole32.dll")]
        private static extern int PropVariantClear(ref PropVariant value);

        [StructLayout(LayoutKind.Sequential)]
        private struct PropertyKey { public Guid fmtid; public int pid; }

        [StructLayout(LayoutKind.Explicit, Size = 24)]
        private struct PropVariant
        {
            [FieldOffset(0)] public ushort vt;
            [FieldOffset(8)] public IntPtr pointer;
        }

        [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumerator { }

        [ComImport, Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
        private class PolicyConfigClient { }

        [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            [PreserveSig] int EnumAudioEndpoints(int dataFlow, int stateMask, out IMMDeviceCollection devices);
            [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
        }

        [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceCollection
        {
            [PreserveSig] int GetCount(out uint count);
            [PreserveSig] int Item(uint index, out IMMDevice device);
        }

        [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            [PreserveSig] int Activate(ref Guid iid, int clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object instance);
            [PreserveSig] int OpenPropertyStore(int access, out IPropertyStore store);
            [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
            [PreserveSig] int GetState(out int state);
        }

        [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore
        {
            [PreserveSig] int GetCount(out int count);
            [PreserveSig] int GetAt(int index, out PropertyKey key);
            [PreserveSig] int GetValue(ref PropertyKey key, out PropVariant value);
        }

        [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioEndpointVolume
        {
            [PreserveSig] int RegisterControlChangeNotify(IntPtr notify);
            [PreserveSig] int UnregisterControlChangeNotify(IntPtr notify);
            [PreserveSig] int GetChannelCount(out int count);
            [PreserveSig] int SetMasterVolumeLevel(float levelDb, ref Guid context);
            [PreserveSig] int SetMasterVolumeLevelScalar(float level, ref Guid context);
            [PreserveSig] int GetMasterVolumeLevel(out float levelDb);
            [PreserveSig] int GetMasterVolumeLevelScalar(out float level);
            [PreserveSig] int SetChannelVolumeLevel(uint channel, float levelDb, ref Guid context);
            [PreserveSig] int SetChannelVolumeLevelScalar(uint channel, float level, ref Guid context);
            [PreserveSig] int GetChannelVolumeLevel(uint channel, out float levelDb);
            [PreserveSig] int GetChannelVolumeLevelScalar(uint channel, out float level);
            [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, ref Guid context);
            [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
        }

        /// <summary>Undocumented but stable since Windows 7; only SetDefaultEndpoint is used.</summary>
        [ComImport, Guid("F8679F50-850A-41CF-9C72-430F290290C8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPolicyConfig
        {
            [PreserveSig] int GetMixFormat(string deviceId, IntPtr format);
            [PreserveSig] int GetDeviceFormat(string deviceId, bool defaultFormat, IntPtr format);
            [PreserveSig] int ResetDeviceFormat(string deviceId);
            [PreserveSig] int SetDeviceFormat(string deviceId, IntPtr endpointFormat, IntPtr mixFormat);
            [PreserveSig] int GetProcessingPeriod(string deviceId, bool defaultPeriod, IntPtr defaultPeriodOut, IntPtr minimumPeriod);
            [PreserveSig] int SetProcessingPeriod(string deviceId, IntPtr period);
            [PreserveSig] int GetShareMode(string deviceId, IntPtr mode);
            [PreserveSig] int SetShareMode(string deviceId, IntPtr mode);
            [PreserveSig] int GetPropertyValue(string deviceId, bool fxStore, IntPtr key, IntPtr value);
            [PreserveSig] int SetPropertyValue(string deviceId, bool fxStore, IntPtr key, IntPtr value);
            [PreserveSig] int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string deviceId, int role);
            [PreserveSig] int SetEndpointVisibility(string deviceId, bool visible);
        }

        #endregion
    }
}
