using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.UIA3;
using PlaywrightWindows.Mcp.Core;
using Xunit;

namespace FlaUI.Mcp.Tests;

/// <summary>
/// Validates that screenshot capture works correctly when DPI awareness is enabled.
/// These tests require an interactive desktop session (cannot run headless).
/// </summary>
[Collection("Desktop")]
public class DpiCaptureTests : IDisposable
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int left, top, right, bottom; }

    private Process? _process;

    static DpiCaptureTests()
    {
        // Enable DPI awareness once for the test process, before any UIA/GDI calls.
        DpiUtility.EnablePerMonitorV2();
    }

    public void Dispose()
    {
        if (_process is { HasExited: false })
        {
            _process.Kill();
            _process.WaitForExit(3000);
        }
        _process?.Dispose();
    }

    private (IntPtr hwnd, Process proc) LaunchAndWaitForWindow(string exe, int timeoutMs = 10_000)
    {
        var proc = Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true })!;
        _process = proc;
        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            Thread.Sleep(500);
            proc.Refresh();
            if (proc.MainWindowHandle != IntPtr.Zero)
                return (proc.MainWindowHandle, proc);
        }
        throw new TimeoutException($"{exe} did not produce a MainWindowHandle within {timeoutMs}ms");
    }

    /// <summary>
    /// With PER_MONITOR_AWARE_V2 enabled, UIA BoundingRectangle should return
    /// the same physical-pixel dimensions as GetWindowRect, and Capture.Element
    /// should produce a bitmap matching those dimensions.
    /// This test validates the DPI fix at whatever scale factor the system is using.
    /// </summary>
    [Fact]
    [Trait("Category", "Desktop")]
    public void CaptureElement_WithDpiAwareness_MatchesPhysicalWindowSize()
    {
        var (hwnd, _) = LaunchAndWaitForWindow("mspaint.exe");

        // Physical pixel size from Win32
        GetWindowRect(hwnd, out RECT rect);
        int physicalWidth = rect.right - rect.left;
        int physicalHeight = rect.bottom - rect.top;
        Assert.True(physicalWidth > 0 && physicalHeight > 0,
            $"GetWindowRect returned invalid size: {physicalWidth}x{physicalHeight}");

        // UIA BoundingRectangle (should be physical when caller is DPI-aware)
        using var automation = new UIA3Automation();
        var window = automation.FromHandle(hwnd).AsWindow();
        var bounds = window.BoundingRectangle;

        Assert.Equal(physicalWidth, bounds.Width);
        Assert.Equal(physicalHeight, bounds.Height);

        // Capture.Element should produce a bitmap at physical pixel dimensions
        var capture = Capture.Element(window);
        Assert.Equal(physicalWidth, capture.Bitmap.Width);
        Assert.Equal(physicalHeight, capture.Bitmap.Height);

        // Verify the capture is not blank (sample pixels)
        int nonBlank = 0, total = 0;
        for (int x = 0; x < Math.Min(capture.Bitmap.Width, 200); x += 10)
            for (int y = 0; y < Math.Min(capture.Bitmap.Height, 200); y += 10)
            {
                total++;
                var px = capture.Bitmap.GetPixel(x, y);
                if (px.A > 0 && (px.R > 0 || px.G > 0 || px.B > 0))
                    nonBlank++;
            }

        Assert.True(nonBlank > total / 4,
            $"Capture appears mostly blank: only {nonBlank}/{total} sampled pixels were non-black");

        capture.Dispose();
    }

    /// <summary>
    /// Verifies that the DpiUtility class can be called without throwing.
    /// SetProcessDpiAwarenessContext is idempotent when called with the same context.
    /// </summary>
    [Fact]
    public void DpiUtility_EnablePerMonitorV2_DoesNotThrow()
    {
        // Should not throw even on repeated calls
        var ex = Record.Exception(() => DpiUtility.EnablePerMonitorV2());
        Assert.Null(ex);
    }
}
