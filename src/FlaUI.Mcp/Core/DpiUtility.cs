using System;
using System.Runtime.InteropServices;

namespace PlaywrightWindows.Mcp.Core;

internal static class DpiUtility
{
    // DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2
    private static readonly IntPtr PerMonitorV2 = new IntPtr(-4);

    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

    public static void EnablePerMonitorV2()
    {
        SetProcessDpiAwarenessContext(PerMonitorV2);
    }
}
