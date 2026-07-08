using System.Text.Json;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using PlaywrightWindows.Mcp.Core;

namespace PlaywrightWindows.Mcp.Tools;

/// <summary>
/// Send key presses and key chords to an element or the currently focused control.
/// </summary>
public class SendKeysTool : ToolBase
{
    private static readonly VirtualKeyShort[] ModifierKeys =
    {
        VirtualKeyShort.CONTROL,
        VirtualKeyShort.LCONTROL,
        VirtualKeyShort.RCONTROL,
        VirtualKeyShort.SHIFT,
        VirtualKeyShort.LSHIFT,
        VirtualKeyShort.RSHIFT,
        VirtualKeyShort.ALT,
        VirtualKeyShort.LMENU,
        VirtualKeyShort.RMENU,
        VirtualKeyShort.LWIN,
        VirtualKeyShort.RWIN
    };

    private static readonly Dictionary<string, VirtualKeyShort> KeyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ctrl"] = VirtualKeyShort.CONTROL,
        ["control"] = VirtualKeyShort.CONTROL,
        ["shift"] = VirtualKeyShort.SHIFT,
        ["alt"] = VirtualKeyShort.ALT,
        ["menu"] = VirtualKeyShort.ALT,
        ["enter"] = VirtualKeyShort.ENTER,
        ["return"] = VirtualKeyShort.ENTER,
        ["tab"] = VirtualKeyShort.TAB,
        ["space"] = VirtualKeyShort.SPACE,
        ["esc"] = VirtualKeyShort.ESCAPE,
        ["escape"] = VirtualKeyShort.ESCAPE,
        ["left"] = VirtualKeyShort.LEFT,
        ["right"] = VirtualKeyShort.RIGHT,
        ["up"] = VirtualKeyShort.UP,
        ["down"] = VirtualKeyShort.DOWN,
        ["home"] = VirtualKeyShort.HOME,
        ["end"] = VirtualKeyShort.END,
        ["pageup"] = VirtualKeyShort.PRIOR,
        ["pagedown"] = VirtualKeyShort.NEXT,
        ["media_next"] = VirtualKeyShort.MEDIA_NEXT_TRACK,
        ["media_prev"] = VirtualKeyShort.MEDIA_PREV_TRACK,
        ["media_play_pause"] = VirtualKeyShort.MEDIA_PLAY_PAUSE,
        ["media_stop"] = VirtualKeyShort.MEDIA_STOP,
        ["f1"] = VirtualKeyShort.F1,
        ["f2"] = VirtualKeyShort.F2,
        ["f3"] = VirtualKeyShort.F3,
        ["f4"] = VirtualKeyShort.F4,
        ["f5"] = VirtualKeyShort.F5,
        ["f6"] = VirtualKeyShort.F6,
        ["f7"] = VirtualKeyShort.F7,
        ["f8"] = VirtualKeyShort.F8,
        ["f9"] = VirtualKeyShort.F9,
        ["f10"] = VirtualKeyShort.F10,
        ["f11"] = VirtualKeyShort.F11,
        ["f12"] = VirtualKeyShort.F12,
        ["a"] = VirtualKeyShort.KEY_A,
        ["b"] = VirtualKeyShort.KEY_B,
        ["c"] = VirtualKeyShort.KEY_C,
        ["d"] = VirtualKeyShort.KEY_D,
        ["e"] = VirtualKeyShort.KEY_E,
        ["f"] = VirtualKeyShort.KEY_F,
        ["g"] = VirtualKeyShort.KEY_G,
        ["h"] = VirtualKeyShort.KEY_H,
        ["i"] = VirtualKeyShort.KEY_I,
        ["j"] = VirtualKeyShort.KEY_J,
        ["k"] = VirtualKeyShort.KEY_K,
        ["l"] = VirtualKeyShort.KEY_L,
        ["m"] = VirtualKeyShort.KEY_M,
        ["n"] = VirtualKeyShort.KEY_N,
        ["o"] = VirtualKeyShort.KEY_O,
        ["p"] = VirtualKeyShort.KEY_P,
        ["q"] = VirtualKeyShort.KEY_Q,
        ["r"] = VirtualKeyShort.KEY_R,
        ["s"] = VirtualKeyShort.KEY_S,
        ["t"] = VirtualKeyShort.KEY_T,
        ["u"] = VirtualKeyShort.KEY_U,
        ["v"] = VirtualKeyShort.KEY_V,
        ["w"] = VirtualKeyShort.KEY_W,
        ["x"] = VirtualKeyShort.KEY_X,
        ["y"] = VirtualKeyShort.KEY_Y,
        ["z"] = VirtualKeyShort.KEY_Z,
        ["0"] = VirtualKeyShort.KEY_0,
        ["1"] = VirtualKeyShort.KEY_1,
        ["2"] = VirtualKeyShort.KEY_2,
        ["3"] = VirtualKeyShort.KEY_3,
        ["4"] = VirtualKeyShort.KEY_4,
        ["5"] = VirtualKeyShort.KEY_5,
        ["6"] = VirtualKeyShort.KEY_6,
        ["7"] = VirtualKeyShort.KEY_7,
        ["8"] = VirtualKeyShort.KEY_8,
        ["9"] = VirtualKeyShort.KEY_9,
        ["backspace"] = VirtualKeyShort.BACK,
        ["bksp"] = VirtualKeyShort.BACK,
        ["delete"] = VirtualKeyShort.DELETE,
        ["del"] = VirtualKeyShort.DELETE,
        ["insert"] = VirtualKeyShort.INSERT,
        ["ins"] = VirtualKeyShort.INSERT,
        ["win"] = VirtualKeyShort.LWIN,
        ["windows"] = VirtualKeyShort.LWIN,
        ["meta"] = VirtualKeyShort.LWIN,
        ["printscreen"] = VirtualKeyShort.SNAPSHOT,
        ["prtsc"] = VirtualKeyShort.SNAPSHOT,
        ["prtscr"] = VirtualKeyShort.SNAPSHOT,
        ["pause"] = VirtualKeyShort.PAUSE,
        ["break"] = VirtualKeyShort.PAUSE
    };

    private readonly ElementRegistry _elementRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="SendKeysTool"/> class.
    /// </summary>
    /// <param name="elementRegistry">Registry used to resolve element references for focus targeting.</param>
    public SendKeysTool(ElementRegistry elementRegistry)
    {
        _elementRegistry = elementRegistry;
    }

    /// <summary>
    /// Gets the MCP tool name.
    /// </summary>
    public override string Name => "windows_send_keys";

    /// <summary>
    /// Gets the MCP tool description.
    /// </summary>
    public override string Description =>
        "Send key presses or key chords to an element by ref or the focused element. " +
        "Supports either `chord` (single chord, e.g., Ctrl+Right) or `keys` array (sequence, e.g., [\"Ctrl+C\",\"Ctrl+V\"]).";

    /// <summary>
    /// Gets the JSON schema for tool inputs.
    /// </summary>
    public override object InputSchema => new
    {
        type = "object",
        properties = new
        {
            @ref = new
            {
                type = "string",
                description = "Element ref from windows_snapshot (e.g., 'w1e5'). If omitted, sends to focused element."
            },
            chord = new
            {
                type = "string",
                description = "Single key chord string, e.g. 'Ctrl+Right' or 'Alt+F4'."
            },
            keys = new
            {
                type = "array",
                description = "Sequence of key presses/chords, e.g. ['Ctrl+C', 'Ctrl+V', 'Enter'].",
                items = new
                {
                    type = "string"
                }
            }
        }
    };

    /// <summary>
    /// Executes the key sending action.
    /// </summary>
    /// <param name="arguments">Tool arguments containing optional target ref and either chord or keys.</param>
    /// <returns>An MCP result with operation status or error details.</returns>
    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var refId = GetStringArgument(arguments, "ref");
        var chord = GetStringArgument(arguments, "chord");
        var keyList = GetArgument<List<string>>(arguments, "keys");
        var hasChord = !string.IsNullOrWhiteSpace(chord);
        var hasKeys = keyList != null && keyList.Count > 0;

        if (!hasChord && !hasKeys)
        {
            return Task.FromResult(ErrorResult("Provide either chord or keys."));
        }

        if (hasChord && hasKeys)
        {
            return Task.FromResult(ErrorResult("Provide either chord or keys, not both."));
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(refId))
            {
                var element = _elementRegistry.GetElement(refId);
                if (element == null)
                {
                    return Task.FromResult(ErrorResult($"Element not found: {refId}. Run windows_snapshot to refresh element refs."));
                }

                element.Focus();
                Thread.Sleep(50);
            }

            if (hasChord)
            {
                var chordTokens = SplitChord(chord).ToList();
                if (chordTokens.Count == 0)
                {
                    return Task.FromResult(ErrorResult("No keys were parsed from chord."));
                }

                var chordKeys = TryResolveKeys(chordTokens, out var chordError);
                if (chordError != null)
                {
                    return Task.FromResult(ErrorResult(chordError));
                }

                PressKeys(chordKeys);

                var targetForChord = string.IsNullOrWhiteSpace(refId) ? "focused element" : refId;
                var chordText = string.Join("+", chordTokens);
                return Task.FromResult(TextResult($"Sent keys {chordText} to {targetForChord}"));
            }

            var actions = new List<string>();
            foreach (var item in keyList!)
            {
                var stepTokens = SplitChord(item).ToList();
                if (stepTokens.Count == 0)
                {
                    return Task.FromResult(ErrorResult("No keys were parsed from keys sequence."));
                }

                var stepKeys = TryResolveKeys(stepTokens, out var stepError);
                if (stepError != null)
                {
                    return Task.FromResult(ErrorResult(stepError));
                }

                PressKeys(stepKeys);
                actions.Add(string.Join("+", stepTokens));
                Thread.Sleep(30);
            }

            var targetName = string.IsNullOrWhiteSpace(refId) ? "focused element" : refId;
            return Task.FromResult(TextResult($"Sent key sequence [{string.Join(", ", actions)}] to {targetName}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ErrorResult($"Failed to send keys: {ex.Message}"));
        }
    }

    private static List<VirtualKeyShort> TryResolveKeys(List<string> tokens, out string? error)
    {
        var keys = new List<VirtualKeyShort>();
        foreach (var token in tokens)
        {
            if (!TryMapKey(token, out var virtualKey))
            {
                error = $"Unsupported key: {token}";
                return keys;
            }

            keys.Add(virtualKey);
        }

        error = null;
        return keys;
    }

    private static void PressKeys(List<VirtualKeyShort> keys)
    {
        var pressedModifiers = keys.Where(k => ModifierKeys.Contains(k)).ToList();
        try
        {
            if (keys.Count == 1)
            {
                Keyboard.Press(keys[0]);
                Thread.Sleep(10);
                Keyboard.Release(keys[0]);
                return;
            }

            Keyboard.TypeSimultaneously(keys.ToArray());
        }
        finally
        {
            foreach (var mod in pressedModifiers)
            {
                Keyboard.Release(mod);
            }
        }
    }

    private static bool TryMapKey(string token, out VirtualKeyShort key)
    {
        var normalized = token.Trim();
        return KeyMap.TryGetValue(normalized, out key);
    }

    private static IEnumerable<string> SplitChord(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Enumerable.Empty<string>();
        }

        return input
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => !string.IsNullOrWhiteSpace(part));
    }
}