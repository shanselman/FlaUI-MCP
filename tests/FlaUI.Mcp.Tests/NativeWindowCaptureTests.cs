using System.Drawing;
using PlaywrightWindows.Mcp.Core;
using Xunit;

namespace FlaUI.Mcp.Tests;

public class NativeWindowCaptureTests
{
    [Fact]
    public void IsBlankOrNearlyBlank_ReturnsTrue_ForBlackBitmap()
    {
        using var bitmap = new Bitmap(16, 16);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Black);

        Assert.True(NativeWindowCapture.IsBlankOrNearlyBlank(bitmap));
    }

    [Fact]
    public void IsBlankOrNearlyBlank_ReturnsFalse_ForVisibleContent()
    {
        using var bitmap = new Bitmap(16, 16);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Black);
        bitmap.SetPixel(8, 8, Color.White);

        Assert.False(NativeWindowCapture.IsBlankOrNearlyBlank(bitmap));
    }
}
