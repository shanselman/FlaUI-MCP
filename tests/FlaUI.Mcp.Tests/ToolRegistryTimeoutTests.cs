using System.Diagnostics;
using System.Text.Json;
using PlaywrightWindows.Mcp;
using Xunit;

namespace FlaUI.Mcp.Tests;

public class ToolRegistryTimeoutTests
{
    [Fact]
    public async Task ExecuteToolAsync_TimesOutSynchronousToolBody()
    {
        var registry = new ToolRegistry(TimeSpan.FromMilliseconds(50));
        registry.RegisterTool(new BlockingTool(TimeSpan.FromMilliseconds(500)));

        var sw = Stopwatch.StartNew();
        var result = await registry.ExecuteToolAsync("blocking", arguments: null);
        sw.Stop();

        Assert.True(result.IsError);
        Assert.Contains("timed out", result.Content[0].Text);
        Assert.True(sw.Elapsed < TimeSpan.FromMilliseconds(400), $"Timeout returned too slowly: {sw.Elapsed}");
    }

    [Fact]
    public async Task ExecuteToolAsync_ReturnsSuccessfulToolResult()
    {
        var registry = new ToolRegistry(TimeSpan.FromSeconds(1));
        registry.RegisterTool(new SuccessfulTool());

        var result = await registry.ExecuteToolAsync("successful", arguments: null);

        Assert.NotEqual(true, result.IsError);
        Assert.Equal("ok", result.Content[0].Text);
    }

    private sealed class BlockingTool : ITool
    {
        private readonly TimeSpan _delay;

        public BlockingTool(TimeSpan delay)
        {
            _delay = delay;
        }

        public string Name => "blocking";

        public McpTool GetDefinition() => new()
        {
            Name = Name,
            Description = "Blocks synchronously",
            InputSchema = new { type = "object" }
        };

        public Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
        {
            Thread.Sleep(_delay);
            return Task.FromResult(new McpToolResult
            {
                Content = new List<McpContent> { new() { Type = "text", Text = "late" } }
            });
        }
    }

    private sealed class SuccessfulTool : ITool
    {
        public string Name => "successful";

        public McpTool GetDefinition() => new()
        {
            Name = Name,
            Description = "Succeeds",
            InputSchema = new { type = "object" }
        };

        public Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
        {
            return Task.FromResult(new McpToolResult
            {
                Content = new List<McpContent> { new() { Type = "text", Text = "ok" } }
            });
        }
    }
}
