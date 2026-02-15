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
        ["9"] = VirtualKeyShort.KEY_9
    };

    private readonly ElementRegistry _elementRegistry;

    public SendKeysTool(ElementRegistry elementRegistry)
    {
        _elementRegistry = elementRegistry;
    }

    public override string Name => "windows_send_keys";

    public override string Description =>
        "Send key presses or key chords to an element by ref or the focused element. " +
        "Supports either `chord` (e.g., Ctrl+Right) or `keys` array (e.g., [\"Ctrl\",\"Right\"]).";

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
                description = "Array of key names, e.g. ['Ctrl', 'Right']. Items may also contain '+' separated chords.",
                items = new
                {
                    type = "string"
                }
            }
        }
    };

    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var refId = GetStringArgument(arguments, "ref");
        var chord = GetStringArgument(arguments, "chord");
        var keyList = GetArgument<List<string>>(arguments, "keys");

        if (string.IsNullOrWhiteSpace(chord) && (keyList == null || keyList.Count == 0))
        {
            return Task.FromResult(ErrorResult("Provide either chord or keys."));
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

            var tokens = new List<string>();
            if (!string.IsNullOrWhiteSpace(chord))
            {
                tokens.AddRange(SplitChord(chord));
            }
            if (keyList != null)
            {
                foreach (var item in keyList)
                {
                    tokens.AddRange(SplitChord(item));
                }
            }

            if (tokens.Count == 0)
            {
                return Task.FromResult(ErrorResult("No keys were parsed from chord/keys."));
            }

            var keys = new List<VirtualKeyShort>();
            foreach (var token in tokens)
            {
                if (!TryMapKey(token, out var virtualKey))
                {
                    return Task.FromResult(ErrorResult($"Unsupported key: {token}"));
                }
                keys.Add(virtualKey);
            }

            if (keys.Count == 1)
            {
                Keyboard.Press(keys[0]);
            }
            else
            {
                Keyboard.TypeSimultaneously(keys.ToArray());
            }

            var target = string.IsNullOrWhiteSpace(refId) ? "focused element" : refId;
            var chordText = string.Join("+", tokens);
            return Task.FromResult(TextResult($"Sent keys {chordText} to {target}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ErrorResult($"Failed to send keys: {ex.Message}"));
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