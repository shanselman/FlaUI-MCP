using System.Text.Json;
using PlaywrightWindows.Mcp.Core;

namespace PlaywrightWindows.Mcp.Tools;

/// <summary>
/// Get text content of an element
/// </summary>
public class GetTextTool : ToolBase
{
    private readonly ElementRegistry _elementRegistry;

    public GetTextTool(ElementRegistry elementRegistry)
    {
        _elementRegistry = elementRegistry;
    }

    public override string Name => "windows_get_text";

    public override string Description => 
        "Get the text content of an element. Returns the element's Name property, " +
        "or for text inputs, the current value.";

    public override object InputSchema => new
    {
        type = "object",
        properties = new
        {
            @ref = new
            {
                type = "string",
                description = "Element ref from windows_snapshot (e.g., 'w1e5')"
            }
        },
        required = new[] { "ref" }
    };

    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var refId = GetStringArgument(arguments, "ref");
        if (string.IsNullOrEmpty(refId))
        {
            return Task.FromResult(ErrorResult("Missing required argument: ref"));
        }

        var element = _elementRegistry.GetElement(refId);
        if (element == null)
        {
            return Task.FromResult(ErrorResult($"Element not found: {refId}. Run windows_snapshot to refresh element refs."));
        }

        try
        {
            string? text = null;

            // Try Value pattern first (for text inputs)
            if (element.Patterns.Value.IsSupported)
            {
                text = element.Patterns.Value.Pattern.Value.ValueOrDefault;
            }

            if (string.IsNullOrEmpty(text) && element.Patterns.Selection.IsSupported)
            {
                var selected = element.Patterns.Selection.Pattern.Selection.ValueOrDefault;
                if (selected != null && selected.Length > 0)
                {
                    text = selected[0].Properties.Name.ValueOrDefault;
                }
            }

            if (string.IsNullOrEmpty(text) && element.Patterns.LegacyIAccessible.IsSupported)
            {
                text = element.Patterns.LegacyIAccessible.Pattern.Value.ValueOrDefault;
            }

            // Fall back to Name property
            if (string.IsNullOrEmpty(text))
            {
                text = element.Properties.Name.ValueOrDefault;
            }

            // Try Text pattern
            if (string.IsNullOrEmpty(text) && element.Patterns.Text.IsSupported)
            {
                text = element.Patterns.Text.Pattern.DocumentRange.GetText(-1);
            }

            return Task.FromResult(TextResult(text ?? ""));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ErrorResult($"Failed to get text from {refId}: {ex.Message}"));
        }
    }
}
