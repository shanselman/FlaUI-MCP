using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using PlaywrightWindows.Mcp.Core;

namespace PlaywrightWindows.Mcp.Tools;

/// <summary>
/// MCP tool that captures screenshots of windows, elements, or the full screen.
/// Window-level captures use native PrintWindow for background-friendly capture,
/// falling back to standard FlaUI capture when unavailable.
/// </summary>
public class ScreenshotTool : ToolBase
{
    private readonly SessionManager _sessionManager;
    private readonly ElementRegistry _elementRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenshotTool"/> class.
    /// </summary>
    /// <param name="sessionManager">The session manager for window lookups.</param>
    /// <param name="elementRegistry">The element registry for ref-based element lookups.</param>
    public ScreenshotTool(SessionManager sessionManager, ElementRegistry elementRegistry)
    {
        _sessionManager = sessionManager;
        _elementRegistry = elementRegistry;
    }

    /// <inheritdoc />
    public override string Name => "windows_screenshot";

    /// <inheritdoc />
    public override string Description => 
        "Take a screenshot of a window or specific element. Returns the image as base64-encoded PNG. " +
        "Window captures attempt a background-friendly native capture first, then fall back to standard capture.";

    /// <inheritdoc />
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
            }
        }
    };

    /// <inheritdoc />
    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var handle = GetStringArgument(arguments, "handle");
        var refId = GetStringArgument(arguments, "ref");
        var fullScreen = GetBoolArgument(arguments, "fullScreen", false);

        try
        {
            CaptureImage capture;

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

                if (NativeWindowCapture.TryCaptureWindow(window, out var backgroundImage, out _))
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

                var currentWindow = current.AsWindow();
                if (currentWindow != null && NativeWindowCapture.TryCaptureWindow(currentWindow, out var backgroundImage, out _))
                {
                    return Task.FromResult(ImageResult(backgroundImage, "image/png"));
                }

                capture = Capture.Element(current);
            }

            using var stream = new MemoryStream();
            capture.Bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            var imageData = stream.ToArray();

            return Task.FromResult(ImageResult(imageData, "image/png"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ErrorResult($"Failed to capture screenshot: {ex.Message}"));
        }
    }
}
