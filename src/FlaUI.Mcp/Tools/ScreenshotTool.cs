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
            },
            savePath = new
            {
                type = "string",
                description = "Absolute local .png file path to save the screenshot. UNC and device paths are rejected."
            },
            overwrite = new
            {
                type = "boolean",
                description = "Allow savePath to replace an existing file (default: false)"
            }
        }
    };

    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var handle = GetStringArgument(arguments, "handle");
        var refId = GetStringArgument(arguments, "ref");
        var fullScreen = GetBoolArgument(arguments, "fullScreen", false);
        var background = GetBoolArgument(arguments, "background", false);
        var savePath = GetStringArgument(arguments, "savePath");
        var overwrite = GetBoolArgument(arguments, "overwrite", false);

        if (!TryNormalizeSavePath(savePath, overwrite, out var normalizedSavePath, out var pathError))
        {
            return Task.FromResult(ErrorResult(pathError));
        }

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
                    return Task.FromResult(BuildScreenshotResult(backgroundImage, normalizedSavePath, overwrite));
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

            return Task.FromResult(BuildScreenshotResult(imageData, normalizedSavePath, overwrite));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ErrorResult($"Failed to capture screenshot: {ex.Message}"));
        }
    }

    internal static bool TryNormalizeSavePath(string? savePath, bool overwrite, out string? normalizedPath, out string error)
    {
        normalizedPath = null;
        error = "";

        if (string.IsNullOrWhiteSpace(savePath))
        {
            return true;
        }

        if (!Path.IsPathFullyQualified(savePath))
        {
            error = $"savePath must be an absolute local path: {savePath}";
            return false;
        }

        if (savePath.StartsWith(@"\\") || savePath.StartsWith(@"\\?\") || savePath.StartsWith(@"\\.\"))
        {
            error = "savePath must be a local drive path; UNC and device paths are not allowed";
            return false;
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(savePath);
        }
        catch (Exception ex)
        {
            error = $"savePath is invalid: {ex.Message}";
            return false;
        }

        if (!string.Equals(Path.GetExtension(fullPath), ".png", StringComparison.OrdinalIgnoreCase))
        {
            error = "savePath must end with .png";
            return false;
        }

        if (File.Exists(fullPath) && !overwrite)
        {
            error = $"savePath already exists; pass overwrite=true to replace it: {fullPath}";
            return false;
        }

        normalizedPath = fullPath;
        return true;
    }

    private static McpToolResult BuildScreenshotResult(byte[] imageData, string? savePath, bool overwrite)
    {
        if (string.IsNullOrEmpty(savePath))
        {
            return ImageResult(imageData, "image/png");
        }

        try
        {
            var directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = Path.Combine(directory ?? Directory.GetCurrentDirectory(), $"{Path.GetFileName(savePath)}.{Guid.NewGuid():N}.tmp");
            try
            {
                File.WriteAllBytes(tempPath, imageData);
                File.Move(tempPath, savePath, overwrite);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }
        catch (Exception ex)
        {
            return ErrorResult($"Failed to save screenshot to {savePath}: {ex.Message}");
        }

        return new McpToolResult
        {
            Content = new List<McpContent>
            {
                new() { Type = "text", Text = $"Screenshot saved to {savePath}" },
                new() { Type = "image", Data = Convert.ToBase64String(imageData), MimeType = "image/png" }
            }
        };
    }
}
