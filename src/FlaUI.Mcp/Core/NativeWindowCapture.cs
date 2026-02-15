using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using FlaUI.Core.AutomationElements;

namespace PlaywrightWindows.Mcp.Core;

/// <summary>
/// Captures window content using the native Win32 PrintWindow API,
/// which can render windows even when they are occluded by other windows.
/// </summary>
internal static class NativeWindowCapture
{
    private const uint PW_RENDERFULLCONTENT = 0x00000002;

    /// <summary>
    /// Attempts to capture a window's content using the native PrintWindow API.
    /// This works even when the window is behind other windows.
    /// </summary>
    /// <param name="window">The window to capture.</param>
    /// <param name="imageData">The captured image as a PNG byte array, or empty on failure.</param>
    /// <param name="failureReason">A description of why capture failed, or null on success.</param>
    /// <returns>True if the window was captured successfully; false otherwise.</returns>
    public static bool TryCaptureWindow(Window window, out byte[] imageData, out string? failureReason)
    {
        imageData = Array.Empty<byte>();
        failureReason = null;

        if (!window.Properties.NativeWindowHandle.TryGetValue(out var nativeWindowHandle) || nativeWindowHandle == 0)
        {
            failureReason = "No native window handle available";
            return false;
        }

        var hwnd = new IntPtr(nativeWindowHandle);

        if (!GetWindowRect(hwnd, out var rect))
        {
            failureReason = "Could not read window bounds";
            return false;
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;

        if (width <= 0 || height <= 0)
        {
            failureReason = "Window has invalid bounds";
            return false;
        }

        try
        {
            using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);
            var hdc = graphics.GetHdc();

            try
            {
                var rendered = PrintWindow(hwnd, hdc, PW_RENDERFULLCONTENT);
                if (!rendered)
                {
                    failureReason = "PrintWindow failed";
                    return false;
                }
            }
            finally
            {
                graphics.ReleaseHdc(hdc);
            }

            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            imageData = stream.ToArray();
            return true;
        }
        catch (Exception ex)
        {
            failureReason = ex.Message;
            return false;
        }
    }

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
