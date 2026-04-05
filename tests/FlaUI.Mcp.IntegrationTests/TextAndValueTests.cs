using System.Diagnostics;
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

    /// <summary>
    /// Navigate to a tab and poll until an expected element appears.
    /// Returns the ref of the expected element.
    /// </summary>
    private async Task<string> NavigateToTabAndFind(string windowHandle, string tabName, string elementName)
    {
        var tabRef = _fixture.FindRefByName(windowHandle, tabName);
        Assert.NotNull(tabRef);

        var clickTool = new ClickTool(_fixture.Elements);
        await _fixture.CallTool(clickTool, new { @ref = tabRef });

        // Poll for the element to appear after tab switch
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < 5000)
        {
            await Task.Delay(100);
            var found = _fixture.FindRefByName(windowHandle, elementName);
            if (found != null) return found;
        }

        Assert.Fail($"Element \"{elementName}\" not found after navigating to \"{tabName}\" tab.");
        return ""; // unreachable
    }

    [Fact]
    public async Task WinForms_GetText_ReadOnlyField()
    {
        var resultRef = await NavigateToTabAndFind(_fixture.WinFormsHandle, "Forms", "Result");

        var tool = new GetTextTool(_fixture.Elements);
        var text = await _fixture.CallTool(tool, new { @ref = resultRef });
        _output.WriteLine($"Result text: '{text}'");
        Assert.Equal("Computed value here", text);
    }

    [Fact]
    public async Task WinForms_TypeAndGetText()
    {
        var nameRef = await NavigateToTabAndFind(_fixture.WinFormsHandle, "Forms", "Name");

        // Fill the name field
        var fillTool = new FillTool(_fixture.Elements);
        var fillResult = await _fixture.CallTool(fillTool, new { @ref = nameRef, value = "Test User" });
        _output.WriteLine($"Fill result: {fillResult}");

        // Read it back
        var textTool = new GetTextTool(_fixture.Elements);
        var text = await _fixture.CallTool(textTool, new { @ref = nameRef });
        _output.WriteLine($"Read back: '{text}'");
        Assert.Equal("Test User", text);

        // Clear it
        await _fixture.CallTool(fillTool, new { @ref = nameRef, value = "" });
    }

    [Fact]
    public async Task Wpf_GetText_ReadOnlyField()
    {
        var resultRef = await NavigateToTabAndFind(_fixture.WpfHandle, "Forms", "Result");

        var tool = new GetTextTool(_fixture.Elements);
        var text = await _fixture.CallTool(tool, new { @ref = resultRef });
        _output.WriteLine($"Result text: '{text}'");
        Assert.Equal("Computed value here", text);
    }
}
