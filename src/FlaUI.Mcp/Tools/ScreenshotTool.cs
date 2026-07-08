using System.Text.Json;
using FlaUI.Core.Capturing;
using PlaywrightWindows.Mcp.Core;

namespace PlaywrightWindows.Mcp.Tools;

/// <summary>
/// Take a screenshot
/// </summary>
public class ScreenshotTool : ToolBase
{
    private readonly SessionManager _sessionManager;
    private readonly ElementRegistry _elementRegistry;

    public ScreenshotTool(SessionManager sessionManager, ElementRegistry elementRegistry)
    {
        _sessionManager = sessionManager;
        _elementRegistry = elementRegistry;
    }

    public override string Name => "windows_screenshot";

    public override string Description => 
        "Take a screenshot of a window or specific element. Returns the image as base64-encoded PNG.";

    public override object InputSchema => new
    {
        type = "object",
        properties = new
        {
            handle = new
            {
                type = "string",
                description = "Window handle. If omitted, captures the foreground window."
            },
            @ref = new
            {
                type = "string",
                description = "Element ref to capture. If omitted, captures the whole window."
            },
            fullScreen = new
            {
                type = "boolean",
                description = "Capture the entire screen (default: false)"
            },
            background = new
            {
                type = "boolean",
                description = "Use native background window capture for a window handle, falling back to normal capture if unavailable (default: false)"
            }
        }
    };

    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var handle = GetStringArgument(arguments, "handle");
        var refId = GetStringArgument(arguments, "ref");
        var fullScreen = GetBoolArgument(arguments, "fullScreen", false);
        var background = GetBoolArgument(arguments, "background", false);

        try
        {
            CaptureImage capture;

            if (background && (fullScreen || !string.IsNullOrEmpty(refId) || string.IsNullOrEmpty(handle)))
            {
                return Task.FromResult(ErrorResult("background capture requires a window handle and cannot be combined with ref or fullScreen"));
            }

            if (fullScreen)
            {
                capture = Capture.Screen();
            }
            else if (!string.IsNullOrEmpty(refId))
            {
                var element = _elementRegistry.GetElement(refId);
                if (element == null)
                {
                    return Task.FromResult(ErrorResult($"Element not found: {refId}"));
                }
                capture = Capture.Element(element);
            }
            else if (!string.IsNullOrEmpty(handle))
            {
                var window = _sessionManager.GetWindow(handle);
                if (window == null)
                {
                    return Task.FromResult(ErrorResult($"Window not found: {handle}"));
                }

                if (background && NativeWindowCapture.TryCaptureWindow(window, out var backgroundImage, out _))
                {
                    return Task.FromResult(ImageResult(backgroundImage, "image/png"));
                }

                capture = Capture.Element(window);
            }
            else
            {
                // Capture foreground window
                var focusedElement = _sessionManager.Automation.FocusedElement();
                if (focusedElement == null)
                {
                    return Task.FromResult(ErrorResult("No focused window found"));
                }

                // Walk up to find the window
                var current = focusedElement;
                while (current != null && current.Properties.ControlType.ValueOrDefault != FlaUI.Core.Definitions.ControlType.Window)
                {
                    current = current.Parent;
                }

                if (current == null)
                {
                    return Task.FromResult(ErrorResult("Could not find window for focused element"));
                }

                capture = Capture.Element(current);
            }

            byte[] imageData;
            using (capture)
            {
                using var stream = new MemoryStream();
                capture.Bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                imageData = stream.ToArray();
            }

            return Task.FromResult(ImageResult(imageData, "image/png"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ErrorResult($"Failed to capture screenshot: {ex.Message}"));
        }
    }
}
