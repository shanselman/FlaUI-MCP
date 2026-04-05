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

    [Fact]
    public async Task WinForms_GetText_ReadOnlyField()
    {
        // Navigate to Forms tab first by finding and clicking it
        var tabRef = _fixture.FindRefByName(_fixture.WinFormsHandle, "Forms");
        if (tabRef != null)
        {
            var clickTool = new ClickTool(_fixture.Elements);
            await _fixture.CallTool(clickTool, new { @ref = tabRef });
            await Task.Delay(300);
        }

        // Re-snapshot after tab switch
        var resultRef = _fixture.FindRefByName(_fixture.WinFormsHandle, "Result");
        if (resultRef == null)
        {
            _output.WriteLine("SKIP: Could not find Result text box");
            return;
        }

        var tool = new GetTextTool(_fixture.Elements);
        var text = await _fixture.CallTool(tool, new { @ref = resultRef });
        _output.WriteLine($"Result text: '{text}'");
        Assert.Equal("Computed value here", text);
    }

    [Fact]
    public async Task WinForms_TypeAndGetText()
    {
        // Navigate to Forms tab
        var tabRef = _fixture.FindRefByName(_fixture.WinFormsHandle, "Forms");
        if (tabRef != null)
        {
            var clickTool = new ClickTool(_fixture.Elements);
            await _fixture.CallTool(clickTool, new { @ref = tabRef });
            await Task.Delay(300);
        }

        var nameRef = _fixture.FindRefByName(_fixture.WinFormsHandle, "Name");
        if (nameRef == null)
        {
            _output.WriteLine("SKIP: Could not find Name text box");
            return;
        }

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
        // Navigate to Forms tab
        var tabRef = _fixture.FindRefByName(_fixture.WpfHandle, "Forms");
        if (tabRef != null)
        {
            var clickTool = new ClickTool(_fixture.Elements);
            await _fixture.CallTool(clickTool, new { @ref = tabRef });
            await Task.Delay(300);
        }

        var resultRef = _fixture.FindRefByName(_fixture.WpfHandle, "Result");
        if (resultRef == null)
        {
            _output.WriteLine("SKIP: Could not find Result text box");
            return;
        }

        var tool = new GetTextTool(_fixture.Elements);
        var text = await _fixture.CallTool(tool, new { @ref = resultRef });
        _output.WriteLine($"Result text: '{text}'");
        Assert.Equal("Computed value here", text);
    }
}
