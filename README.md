# FlaUI-MCP

An MCP (Model Context Protocol) server that enables AI agents to automate Windows desktop applications using accessibility APIs - the same way Playwright automates browsers.

[![Build](https://github.com/shanselman/FlaUI-MCP/actions/workflows/build.yml/badge.svg)](https://github.com/shanselman/FlaUI-MCP/actions/workflows/build.yml)
[![GitHub release](https://img.shields.io/github/v/release/shanselman/FlaUI-MCP)](https://github.com/shanselman/FlaUI-MCP/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Why This Exists

When Playwright's MCP server automates browsers, it provides:
- `browser_snapshot` → Structured accessibility tree with element refs
- `browser_click ref="..."` → Click by ref, not coordinates

**FlaUI-MCP brings the same pattern to Windows desktop apps:**
- `windows_snapshot` → Accessibility tree with refs like `w1e5`
- `windows_click ref="w1e5"` → Click element by ref

No screenshot parsing. No coordinate guessing. Just semantic element references.

## Quick Demo

```
Agent: Calculate 3 × 3

1. windows_launch { "app": "calc.exe" }
   → Window handle: w1

2. windows_snapshot { "handle": "w1" }
   → - window "Calculator" [ref=w1]
       - button "Three" [ref=w1e43]
       - button "Multiply by" [ref=w1e35]
       - button "Equals" [ref=w1e38]
       - text "Display is 0" [ref=w1e15]

3. windows_batch { "actions": [
     {"action": "click", "ref": "w1e43"},
     {"action": "click", "ref": "w1e35"},
     {"action": "click", "ref": "w1e43"},
     {"action": "click", "ref": "w1e38"},
     {"action": "snapshot", "handle": "w1"}
   ]}
   → 1. click: Invoked Three
     2. click: Invoked Multiply by
     3. click: Invoked Three
     4. click: Invoked Equals
     5. snapshot: ... "Display is 9" ...
```

## Installation

### Prerequisites
- Windows 10/11
- .NET 8.0 Runtime

### Download Release

Download the latest release from [Releases](https://github.com/shanselman/FlaUI-MCP/releases) and extract to a folder.

### Configure MCP Client

Add to your MCP configuration (e.g., `~/.copilot/mcp-config.json`):

```json
{
  "mcpServers": {
    "windows": {
      "type": "local",
      "command": "C:\\path\\to\\FlaUI-MCP.exe",
      "tools": ["*"]
    }
  }
}
```

Or using `dotnet run`:

```json
{
  "mcpServers": {
    "windows": {
      "type": "local",
      "command": "dotnet",
      "args": ["run", "--project", "C:\\path\\to\\src\\FlaUI.Mcp"]
    }
  }
}
```

## Available Tools

| Tool | Description |
|------|-------------|
| `windows_launch` | Launch a Windows application |
| `windows_snapshot` | Get accessibility tree with element refs |
| `windows_click` | Click an element by ref |
| `windows_type` | Type text into an element |
| `windows_send_keys` | Send key presses or key chords (for example `Ctrl+A`) |
| `windows_fill` | Clear and fill a text field |
| `windows_get_text` | Get text content of an element |
| `windows_screenshot` | Capture window/element as PNG |
| `windows_list_windows` | List all open windows |
| `windows_focus` | Bring a window to foreground |
| `windows_close` | Close a window |
| `windows_batch` | Execute multiple actions in one call |

## How It Works

### The Accessibility Snapshot

When you call `windows_snapshot`, you get a structured text tree:

```
- window "Calculator" [ref=w1e1]
  - group "Number pad" [ref=w1e39]
    - button "Seven" [ref=w1e47]
    - button "Eight" [ref=w1e48]
    - button "Nine" [ref=w1e49]
  - text "Display is 0" [ref=w1e15]
```

This comes from **Windows UI Automation** - the same API screen readers use. Each element has:
- **Role** (button, text, group, textbox)
- **Name** ("Seven", "Display is 0")
- **Ref** (w1e47) - a handle for interaction
- **State** ([disabled], [readonly], [checked])

### Why Not Screenshots?

| Approach | Pros | Cons |
|----------|------|------|
| **Accessibility Tree** | Semantic, precise, fast, works at any resolution | Requires UI Automation support |
| **Screenshot + Vision** | Works with any app | Slow, expensive, imprecise, resolution-dependent |

FlaUI-MCP uses accessibility because it's what screen readers use - it's designed for programmatic UI interaction.

## Building from Source

```powershell
# Clone
git clone https://github.com/shanselman/FlaUI-MCP.git
cd FlaUI-MCP

# Build
dotnet build src/FlaUI.Mcp

# Run
dotnet run --project src/FlaUI.Mcp
```

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│  AI Agent (GitHub Copilot, Claude, etc.)                        │
│  - Calls MCP tools: windows_snapshot, windows_click, etc.       │
└─────────────────────────────────────────────────────────────────┘
                              │ MCP Protocol (JSON-RPC over stdio)
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  FlaUI-MCP Server (.NET 8)                                      │
│  - Implements MCP tool handlers                                 │
│  - Builds agent-friendly accessibility snapshots                │
│  - Maps element refs ↔ AutomationElements                       │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  FlaUI Library (github.com/FlaUI/FlaUI)                         │
│  - UIA3Automation for modern apps (WPF, UWP, Win32)            │
│  - Control patterns: Invoke, Value, Toggle, Selection           │
│  - Tree walking and element discovery                           │
└─────────────────────────────────────────────────────────────────┘
```

## Supported Applications

Works with any Windows application that supports UI Automation:
- ✅ Win32 apps (Notepad, Explorer, etc.)
- ✅ WPF applications
- ✅ WinForms applications  
- ✅ UWP/Store apps (Calculator, Settings, etc.)
- ⚠️ Electron apps (partial - depends on accessibility implementation)
- ❌ Games (typically no UI Automation support)

## Contributing

Contributions welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

MIT License - see [LICENSE](LICENSE) for details.

## Acknowledgments

- [FlaUI](https://github.com/FlaUI/FlaUI) - The excellent .NET UI Automation library this project is built on
- [Playwright](https://playwright.dev/) - Inspiration for the snapshot/ref interaction model
- [Model Context Protocol](https://modelcontextprotocol.io/) - The protocol that makes this possible
