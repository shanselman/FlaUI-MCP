using System.Diagnostics;
using PlaywrightWindows.Mcp.Core;
using PlaywrightWindows.Mcp.Tools;
using Xunit.Abstractions;

namespace FlaUI.Mcp.IntegrationTests;

/// <summary>
/// Tests for windows_click tool using the test apps.
/// </summary>
[Collection("TestApps")]
public class ClickTests
{
    private readonly TestAppFixture _fixture;
    private readonly ITestOutputHelper _output;

    public ClickTests(TestAppFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task WinForms_Click_Button_Invoke()
    {
        var buttonRef = _fixture.FindRefByName(_fixture.WinFormsHandle, "Click Me");
        Assert.NotNull(buttonRef);
        _output.WriteLine($"Click Me button ref: {buttonRef}");

        var tool = new ClickTool(_fixture.Elements);
        var result = await _fixture.CallTool(tool, new { @ref = buttonRef });
        _output.WriteLine($"Result: {result}");
        Assert.Contains("Invoked", result);
    }

    [Fact]
    public async Task WinForms_Click_Checkbox_Toggle()
    {
        var cbRef = _fixture.FindRefByName(_fixture.WinFormsHandle, "Enable the button below");
        Assert.NotNull(cbRef);

        var tool = new ClickTool(_fixture.Elements);
        var result = await _fixture.CallTool(tool, new { @ref = cbRef });
        _output.WriteLine($"Result: {result}");
        // WinForms checkboxes support InvokePattern, so ClickTool uses Invoke (not Toggle)
        Assert.True(result.Contains("Invoked") || result.Contains("Toggled"),
            $"Expected Invoked or Toggled, got: {result}");

        // Toggle back to original state
        await _fixture.CallTool(tool, new { @ref = cbRef });
    }

    [Fact]
    public async Task WinForms_EnabledStateChanges_AfterCheckboxClick()
    {
        // First snapshot to find refs
        var builder = new SnapshotBuilder(_fixture.Elements);
        var window = _fixture.GetWinFormsWindow()!;
        var snapshot1 = builder.BuildSnapshot(_fixture.WinFormsHandle, window);
        _output.WriteLine("Initial snapshot (partial):");
        foreach (var line in snapshot1.Split('\n'))
        {
            if (line.Contains("Conditional") || line.Contains("Enable"))
                _output.WriteLine(line);
        }

        // Find the checkbox and button refs
        var cbRef = _fixture.FindRefByName(_fixture.WinFormsHandle, "Enable the button below");
        Assert.NotNull(cbRef);

        // Verify button is initially disabled
        Assert.Contains("disabled", snapshot1.Split('\n').First(l => l.Contains("Conditional Button")));

        // Click the checkbox to enable the button
        var clickTool = new ClickTool(_fixture.Elements);
        await _fixture.CallTool(clickTool, new { @ref = cbRef });

        // Poll for the button to become enabled
        string snapshot2 = "";
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < 5000)
        {
            await Task.Delay(100);
            snapshot2 = builder.BuildSnapshot(_fixture.WinFormsHandle, window);
            var line = snapshot2.Split('\n').FirstOrDefault(l => l.Contains("Conditional Button"));
            if (line != null && !line.Contains("disabled"))
                break;
        }
        _output.WriteLine("\nAfter checkbox click:");
        foreach (var line in snapshot2.Split('\n'))
        {
            if (line.Contains("Conditional") || line.Contains("Enable"))
                _output.WriteLine(line);
        }

        var conditionalLine = snapshot2.Split('\n').First(l => l.Contains("Conditional Button"));
        _output.WriteLine($"\nConditional Button line: {conditionalLine}");

        // Button should now be enabled (no [disabled] tag)
        Assert.DoesNotContain("disabled", conditionalLine);

        // Clean up: uncheck the checkbox
        await _fixture.CallTool(clickTool, new { @ref = cbRef });
    }

    [Fact]
    public async Task Wpf_Click_Button_Invoke()
    {
        var buttonRef = _fixture.FindRefByName(_fixture.WpfHandle, "Click Me");
        Assert.NotNull(buttonRef);
        _output.WriteLine($"Click Me button ref: {buttonRef}");

        var tool = new ClickTool(_fixture.Elements);
        var result = await _fixture.CallTool(tool, new { @ref = buttonRef });
        _output.WriteLine($"Result: {result}");
        Assert.Contains("Invoked", result);
    }
}
