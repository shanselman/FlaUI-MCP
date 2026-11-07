using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using PlaywrightWindows.Mcp.Core;

namespace PlaywrightWindows.Mcp.Tools;

/// <summary>
/// Execute multiple actions in a single call for better performance
/// </summary>
public class BatchTool : ToolBase
{
    private readonly SessionManager _sessionManager;
    private readonly ElementRegistry _elementRegistry;

    public BatchTool(SessionManager sessionManager, ElementRegistry elementRegistry)
    {
        _sessionManager = sessionManager;
        _elementRegistry = elementRegistry;
    }

    public override string Name => "windows_batch";

    public override string Description => 
        "Execute multiple actions in a single call. Much faster than individual calls. " +
        "Supports click, type, fill, and wait actions. Returns results for each action.";

    public override object InputSchema => new
    {
        type = "object",
        properties = new
        {
            actions = new
            {
                type = "array",
                description = "List of actions to execute in order",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        action = new
                        {
                            type = "string",
                            @enum = new[] { "click", "type", "fill", "wait", "snapshot" },
                            description = "Action type"
                        },
                        @ref = new
                        {
                            type = "string",
                            description = "Element ref for click/type/fill actions"
                        },
                        text = new
                        {
                            type = "string",
                            description = "Text for type action"
                        },
                        value = new
                        {
                            type = "string",
                            description = "Value for fill action"
                        },
                        ms = new
                        {
                            type = "integer",
                            description = "Milliseconds for wait action (default: 100)"
                        },
                        handle = new
                        {
                            type = "string",
                            description = "Window handle for snapshot action"
                        },
                        maxDepth = new
                        {
                            type = "integer",
                            description = $"Maximum number of child levels to include in a snapshot (default: {SnapshotBuilder.DefaultMaxDepth}).",
                            minimum = 0,
                            @default = SnapshotBuilder.DefaultMaxDepth
                        }
                    },
                    required = new[] { "action" }
                }
            },
            stopOnError = new
            {
                type = "boolean",
                description = "Stop executing if an action fails (default: true)"
            }
        },
        required = new[] { "actions" }
    };

    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        if (arguments == null || !arguments.Value.TryGetProperty("actions", out var actionsElement))
        {
            return Task.FromResult(ErrorResult("Missing required argument: actions"));
        }

        var stopOnError = true;
        if (arguments.Value.TryGetProperty("stopOnError", out var stopProp))
        {
            stopOnError = stopProp.GetBoolean();
        }

        var results = new List<string>();
        var actions = actionsElement.EnumerateArray().ToList();

        foreach (var (actionObj, index) in actions.Select((a, i) => (a, i)))
        {
            try
            {
                var actionType = actionObj.GetProperty("action").GetString();
                var result = actionType switch
                {
                    "click" => ExecuteClick(actionObj),
                    "type" => ExecuteType(actionObj),
                    "fill" => ExecuteFill(actionObj),
                    "wait" => ExecuteWait(actionObj),
                    "snapshot" => ExecuteSnapshot(actionObj),
                    _ => $"Unknown action: {actionType}"
                };
                results.Add($"{index + 1}. {actionType}: {result}");
            }
            catch (Exception ex)
            {
                results.Add($"{index + 1}. ERROR: {ex.Message}");
                if (stopOnError)
                {
                    results.Add($"Stopped at action {index + 1} due to error");
                    break;
                }
            }
        }

        return Task.FromResult(TextResult(string.Join("\n", results)));
    }

    private string ExecuteClick(JsonElement action)
    {
        var refId = action.TryGetProperty("ref", out var refProp) ? refProp.GetString() : null;
        if (string.IsNullOrEmpty(refId))
        {
            return "Missing ref";
        }

        var element = _elementRegistry.GetElement(refId);
        if (element == null)
        {
            return $"Element not found: {refId}";
        }

        var elementName = element.Properties.Name.ValueOrDefault ?? refId;

        // Try Invoke pattern first
        if (element.Patterns.Invoke.IsSupported)
        {
            element.Patterns.Invoke.Pattern.Invoke();
            return $"Invoked {elementName}";
        }

        // Try Toggle pattern
        if (element.Patterns.Toggle.IsSupported)
        {
            element.Patterns.Toggle.Pattern.Toggle();
            return $"Toggled {elementName}";
        }

        // Fall back to mouse click
        var clickPoint = element.GetClickablePoint();
        Mouse.Click(clickPoint);
        return $"Clicked {elementName}";
    }

    private string ExecuteType(JsonElement action)
    {
        var text = action.TryGetProperty("text", out var textProp) ? textProp.GetString() : null;
        if (string.IsNullOrEmpty(text))
        {
            return "Missing text";
        }

        var refId = action.TryGetProperty("ref", out var refProp) ? refProp.GetString() : null;
        if (!string.IsNullOrEmpty(refId))
        {
            var element = _elementRegistry.GetElement(refId);
            if (element == null)
            {
                return $"Element not found: {refId}";
            }
            element.Focus();
            Thread.Sleep(30);
        }

        Keyboard.Type(text);
        return $"Typed \"{text}\"";
    }

    private string ExecuteFill(JsonElement action)
    {
        var refId = action.TryGetProperty("ref", out var refProp) ? refProp.GetString() : null;
        var value = action.TryGetProperty("value", out var valProp) ? valProp.GetString() : null;

        if (string.IsNullOrEmpty(refId) || value == null)
        {
            return "Missing ref or value";
        }

        var element = _elementRegistry.GetElement(refId);
        if (element == null)
        {
            return $"Element not found: {refId}";
        }

        if (element.Patterns.Value.IsSupported)
        {
            element.Patterns.Value.Pattern.SetValue(value);
            return $"Filled with \"{value}\"";
        }

        // Fallback
        element.Focus();
        Thread.Sleep(30);
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Thread.Sleep(30);
        Keyboard.Type(value);
        return $"Filled with \"{value}\"";
    }

    private string ExecuteWait(JsonElement action)
    {
        var ms = action.TryGetProperty("ms", out var msProp) ? msProp.GetInt32() : 100;
        Thread.Sleep(ms);
        return $"Waited {ms}ms";
    }

    private string ExecuteSnapshot(JsonElement action)
    {
        var handle = action.TryGetProperty("handle", out var handleProp) ? handleProp.GetString() : null;
        
        Window? window = null;
        if (!string.IsNullOrEmpty(handle))
        {
            window = _sessionManager.GetWindow(handle);
            if (window == null)
            {
                return $"Window not found: {handle}";
            }
        }
        else
        {
            // Get focused window
            var focusedElement = _sessionManager.Automation.FocusedElement();
            if (focusedElement != null)
            {
                var current = focusedElement;
                while (current != null)
                {
                    if (current.Properties.ControlType.ValueOrDefault == FlaUI.Core.Definitions.ControlType.Window)
                    {
                        window = current.AsWindow();
                        handle = _sessionManager.RegisterWindow(window);
                        break;
                    }
                    current = current.Parent;
                }
            }
        }

        if (window == null)
        {
            return "No window found";
        }

        var maxDepth = action.TryGetProperty("maxDepth", out var maxDepthProp)
            ? maxDepthProp.GetInt32()
            : SnapshotBuilder.DefaultMaxDepth;
        var snapshot = new SnapshotBuilder(_elementRegistry, maxDepth).BuildSnapshot(handle!, window);
        return $"\n{snapshot}";
    }
}
