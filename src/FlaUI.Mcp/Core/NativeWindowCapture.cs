using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using FlaUI.Core.AutomationElements;

namespace PlaywrightWindows.Mcp.Core;

internal static class NativeWindowCapture
{
    private const uint PW_RENDERFULLCONTENT = 0x00000002;

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
                if (!PrintWindow(hwnd, hdc, PW_RENDERFULLCONTENT))
                {
                    failureReason = "PrintWindow failed";
                    return false;
                }
            }
            finally
            {
                graphics.ReleaseHdc(hdc);
            }

            if (IsBlankOrNearlyBlank(bitmap))
            {
                failureReason = "PrintWindow produced a blank image";
                return false;
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

    internal static bool IsBlankOrNearlyBlank(Bitmap bitmap)
    {
        const int maxSamples = 1024;
        var stepX = Math.Max(1, bitmap.Width / 32);
        var stepY = Math.Max(1, bitmap.Height / 32);
        var samples = 0;

        for (var y = 0; y < bitmap.Height && samples < maxSamples; y += stepY)
        {
            for (var x = 0; x < bitmap.Width && samples < maxSamples; x += stepX)
            {
                samples++;
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.A > 0 && (pixel.R > 8 || pixel.G > 8 || pixel.B > 8))
                {
                    return false;
                }
            }
        }

        return samples > 0;
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
