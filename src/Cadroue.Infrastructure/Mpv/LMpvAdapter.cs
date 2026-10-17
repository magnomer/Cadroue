using System.Runtime.InteropServices;

namespace Cadroue.Infrastructure;

internal static class LMpvAdapter
{
    private const uint LMpvMonitorNearest = 2;
    private const int LMpvMonitorSize = 104;
    private const int LMpvMonitorOffset = 40;
    private const int LMpvDeviceSize = 840;
    private const int LMpvDeviceOffset = 4;
    private const int LMpvAdapterOffset = 68;

    internal static string? LMpvAdapterRead(nint lWindowHandle)
    {
        nint lMonitor = Marshal.AllocHGlobal(LMpvMonitorSize);
        nint lDevice = Marshal.AllocHGlobal(LMpvDeviceSize);
        try
        {
            Marshal.WriteInt32(lMonitor, LMpvMonitorSize);
            if (!GetMonitorInfo(MonitorFromWindow(lWindowHandle, LMpvMonitorNearest), lMonitor))
            {
                return null;
            }

            string? lMonitorName = Marshal.PtrToStringUni(lMonitor + LMpvMonitorOffset);
            for (uint lIndex = 0; ; lIndex++)
            {
                Marshal.WriteInt32(lDevice, LMpvDeviceSize);
                if (!EnumDisplayDevices(null, lIndex, lDevice, 0))
                {
                    return null;
                }

                if (string.Equals(
                    Marshal.PtrToStringUni(lDevice + LMpvDeviceOffset),
                    lMonitorName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return Marshal.PtrToStringUni(lDevice + LMpvAdapterOffset);
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(lMonitor);
            Marshal.FreeHGlobal(lDevice);
        }
    }

    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    private static extern bool GetMonitorInfo(nint hMonitor, nint lpmi);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "EnumDisplayDevicesW")]
    private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, nint lpDisplayDevice, uint dwFlags);
}
