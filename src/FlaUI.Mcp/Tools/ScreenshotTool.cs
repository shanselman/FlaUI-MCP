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
            savePath = new
            {
                type = "string",
                description = "Absolute file path to save the screenshot PNG. If omitted, image is returned inline only."
            }
        }
    };

    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var handle = GetStringArgument(arguments, "handle");
        var refId = GetStringArgument(arguments, "ref");
        var fullScreen = GetBoolArgument(arguments, "fullScreen", false);
        var savePath = GetStringArgument(arguments, "savePath");

        // Validate savePath is absolute if provided
        if (!string.IsNullOrEmpty(savePath) && !Path.IsPathFullyQualified(savePath))
        {
            return Task.FromResult(ErrorResult($"savePath must be an absolute path: {savePath}"));
        }

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

            using var stream = new MemoryStream();
            capture.Bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            var imageData = stream.ToArray();

            // Save to file if savePath is provided
            if (!string.IsNullOrEmpty(savePath))
            {
                try
                {
                    var directory = Path.GetDirectoryName(savePath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                }
                catch (Exception ex)
                {
                    return Task.FromResult(ErrorResult($"Failed to create directory for screenshot: {ex.Message}"));
                }

                try
                {
                    File.WriteAllBytes(savePath, imageData);
                }
                catch (Exception ex)
                {
                    return Task.FromResult(ErrorResult($"Failed to save screenshot to {savePath}: {ex.Message}"));
                }

                var absolutePath = Path.GetFullPath(savePath);
                return Task.FromResult(new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new() { Type = "text", Text = $"Screenshot saved to {absolutePath}" },
                        new() { Type = "image", Data = Convert.ToBase64String(imageData), MimeType = "image/png" }
                    }
                });
            }

            return Task.FromResult(ImageResult(imageData, "image/png"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ErrorResult($"Failed to capture screenshot: {ex.Message}"));
        }
    }
}
