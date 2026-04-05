using PlaywrightWindows.Mcp.Core;
using PlaywrightWindows.Mcp.Tools;
using Xunit.Abstractions;

namespace FlaUI.Mcp.IntegrationTests;

/// <summary>
/// Tests for windows_snapshot tool using the WinForms and WPF test apps.
/// </summary>
[Collection("TestApps")]
public class SnapshotTests
{
    private readonly TestAppFixture _fixture;
    private readonly ITestOutputHelper _output;

    public SnapshotTests(TestAppFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public void WinFormsApp_IsRunning()
    {
        Assert.NotEmpty(_fixture.WinFormsHandle);
        var window = _fixture.GetWinFormsWindow();
        Assert.NotNull(window);
        _output.WriteLine($"WinForms window: {window.Title} (handle: {_fixture.WinFormsHandle})");
    }

    [Fact]
    public void WpfApp_IsRunning()
    {
        Assert.NotEmpty(_fixture.WpfHandle);
        var window = _fixture.GetWpfWindow();
        Assert.NotNull(window);
        _output.WriteLine($"WPF window: {window.Title} (handle: {_fixture.WpfHandle})");
    }

    [Fact]
    public void WinForms_Snapshot_ContainsTabControl()
    {
        var builder = new SnapshotBuilder(_fixture.Elements);
        var snapshot = builder.BuildSnapshot(_fixture.WinFormsHandle, _fixture.GetWinFormsWindow()!);
        _output.WriteLine(snapshot);

        Assert.Contains("Buttons", snapshot);
        Assert.Contains("Forms", snapshot);
        Assert.Contains("Grid", snapshot);
        Assert.Contains("Trees", snapshot);
        Assert.Contains("Dialogs", snapshot);
    }

    [Fact]
    public void Wpf_Snapshot_ContainsTabControl()
    {
        var builder = new SnapshotBuilder(_fixture.Elements);
        var snapshot = builder.BuildSnapshot(_fixture.WpfHandle, _fixture.GetWpfWindow()!);
        _output.WriteLine(snapshot);

        Assert.Contains("Buttons", snapshot);
        Assert.Contains("Forms", snapshot);
        Assert.Contains("Grid", snapshot);
        Assert.Contains("Trees", snapshot);
    }

    [Fact]
    public async Task WinForms_Snapshot_ContainsButtons()
    {
        // Navigate to Buttons tab first — another test may have switched tabs
        var snapshot = _fixture.TakeSnapshot(_fixture.WinFormsHandle);
        var tabRef = TestAppFixture.FindRefInSnapshot(snapshot, "Buttons");
        if (tabRef != null)
        {
            var clickTool = new ClickTool(_fixture.Elements);
            await _fixture.CallTool(clickTool, new { @ref = tabRef });
            await Task.Delay(100);
        }

        var snapshot2 = _fixture.TakeSnapshot(_fixture.WinFormsHandle);
        Assert.Contains("Click Me", snapshot2);
        Assert.Contains("Conditional Button", snapshot2);
    }

    [Fact]
    public async Task WinForms_Snapshot_ContainsGridData()
    {
        // Navigate to Grid tab first — WinForms only shows active tab content
        var snapshot = _fixture.TakeSnapshot(_fixture.WinFormsHandle);
        var gridTabRef = TestAppFixture.FindRefInSnapshot(snapshot, "Grid");

        Assert.NotNull(gridTabRef);

        var clickTool = new ClickTool(_fixture.Elements);
        await _fixture.CallTool(clickTool, new { @ref = gridTabRef });

        // Poll for grid content to appear after tab switch
        var sw = System.Diagnostics.Stopwatch.StartNew();
        string snapshot2 = "";
        while (sw.ElapsedMilliseconds < 5000)
        {
            await Task.Delay(100);
            snapshot2 = _fixture.TakeSnapshot(_fixture.WinFormsHandle);
            if (snapshot2.Contains("Test Data"))
                break;
        }

        _output.WriteLine(snapshot2[..Math.Min(2000, snapshot2.Length)]);
        Assert.Contains("Test Data", snapshot2);
    }
}
