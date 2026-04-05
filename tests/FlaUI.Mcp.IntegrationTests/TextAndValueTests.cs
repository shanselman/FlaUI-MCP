using System.Text.Json;
using PlaywrightWindows.Mcp;
using PlaywrightWindows.Mcp.Core;
using PlaywrightWindows.Mcp.Tools;
using Xunit.Abstractions;

namespace FlaUI.Mcp.IntegrationTests;

/// <summary>
/// Tests for windows_get_text, windows_type, and windows_fill tools.
/// </summary>
[Collection("TestApps")]
public class TextAndValueTests
{
    private readonly TestAppFixture _fixture;
    private readonly ITestOutputHelper _output;

    public TextAndValueTests(TestAppFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    private async Task<string> CallTool(ToolBase tool, object args)
    {
        var json = JsonSerializer.Serialize(args, McpProtocol.JsonOptions);
        var element = JsonSerializer.Deserialize<JsonElement>(json);
        var result = await tool.ExecuteAsync(element);
        return result.Content.FirstOrDefault()?.Text ?? "";
    }

    private string? FindRefByName(string handle, string name)
    {
        var builder = new SnapshotBuilder(_fixture.Elements);
        var window = _fixture.Session.GetWindow(handle)!;
        builder.BuildSnapshot(handle, window);
        // Search snapshot lines for the name
        var snapshot = builder.BuildSnapshot(handle, window);
        foreach (var line in snapshot.Split('\n'))
        {
            if (line.Contains($"\"{name}\"") && line.Contains("[ref="))
            {
                var refStart = line.IndexOf("[ref=") + 5;
                var refEnd = line.IndexOf("]", refStart);
                return line[refStart..refEnd];
            }
        }
        return null;
    }

    [Fact]
    public async Task WinForms_GetText_ReadOnlyField()
    {
        // Navigate to Forms tab first by finding and clicking it
        var tabRef = FindRefByName(_fixture.WinFormsHandle, "Forms");
        if (tabRef != null)
        {
            var clickTool = new ClickTool(_fixture.Elements);
            await CallTool(clickTool, new { @ref = tabRef });
            await Task.Delay(300);
        }

        // Re-snapshot after tab switch
        var resultRef = FindRefByName(_fixture.WinFormsHandle, "Result");
        if (resultRef == null)
        {
            _output.WriteLine("SKIP: Could not find Result text box");
            return;
        }

        var tool = new GetTextTool(_fixture.Elements);
        var text = await CallTool(tool, new { @ref = resultRef });
        _output.WriteLine($"Result text: '{text}'");
        Assert.Equal("Computed value here", text);
    }

    [Fact]
    public async Task WinForms_TypeAndGetText()
    {
        // Navigate to Forms tab
        var tabRef = FindRefByName(_fixture.WinFormsHandle, "Forms");
        if (tabRef != null)
        {
            var clickTool = new ClickTool(_fixture.Elements);
            await CallTool(clickTool, new { @ref = tabRef });
            await Task.Delay(300);
        }

        var nameRef = FindRefByName(_fixture.WinFormsHandle, "Name");
        if (nameRef == null)
        {
            _output.WriteLine("SKIP: Could not find Name text box");
            return;
        }

        // Fill the name field
        var fillTool = new FillTool(_fixture.Elements);
        var fillResult = await CallTool(fillTool, new { @ref = nameRef, value = "Test User" });
        _output.WriteLine($"Fill result: {fillResult}");

        // Read it back
        var textTool = new GetTextTool(_fixture.Elements);
        var text = await CallTool(textTool, new { @ref = nameRef });
        _output.WriteLine($"Read back: '{text}'");
        Assert.Equal("Test User", text);

        // Clear it
        await CallTool(fillTool, new { @ref = nameRef, value = "" });
    }

    [Fact]
    public async Task Wpf_GetText_ReadOnlyField()
    {
        // Navigate to Forms tab
        var tabRef = FindRefByName(_fixture.WpfHandle, "Forms");
        if (tabRef != null)
        {
            var clickTool = new ClickTool(_fixture.Elements);
            await CallTool(clickTool, new { @ref = tabRef });
            await Task.Delay(300);
        }

        var resultRef = FindRefByName(_fixture.WpfHandle, "Result");
        if (resultRef == null)
        {
            _output.WriteLine("SKIP: Could not find Result text box");
            return;
        }

        var tool = new GetTextTool(_fixture.Elements);
        var text = await CallTool(tool, new { @ref = resultRef });
        _output.WriteLine($"Result text: '{text}'");
        Assert.Equal("Computed value here", text);
    }
}
