using System.Diagnostics;
using System.Runtime.CompilerServices;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using PlaywrightWindows.Mcp.Core;

namespace FlaUI.Mcp.IntegrationTests;

/// <summary>
/// Shared fixture that launches the WinForms and WPF test apps once per test collection.
/// Provides SessionManager and ElementRegistry for all tests to share.
/// </summary>
public class TestAppFixture : IDisposable
{
    public SessionManager Session { get; }
    public ElementRegistry Elements { get; }

    public string WinFormsHandle { get; private set; } = "";
    public string WpfHandle { get; private set; } = "";

    private Process? _winFormsProcess;
    private Process? _wpfProcess;

    public TestAppFixture()
    {
        Session = new SessionManager();
        Elements = new ElementRegistry();

        _winFormsProcess = LaunchTestApp(FindTestAppPath("WinFormsTestApp"));
        _wpfProcess = LaunchTestApp(FindTestAppPath("WpfTestApp"));

        // Wait for windows to appear and register them
        Thread.Sleep(3000);

        var desktop = Session.Automation.GetDesktop();
        var windows = desktop.FindAllChildren(cf => cf.ByControlType(ControlType.Window));
        foreach (var w in windows)
        {
            var win = w.AsWindow();
            if (win?.Title == "FlaUI-MCP Test App")
            {
                WinFormsHandle = Session.RegisterWindow(win);
            }
            else if (win?.Title == "FlaUI-MCP WPF Test App")
            {
                WpfHandle = Session.RegisterWindow(win);
            }
        }
    }

    private static Process? LaunchTestApp(string? path)
    {
        if (path == null) return null;

        var psi = new ProcessStartInfo(path) { UseShellExecute = true };
        return Process.Start(psi);
    }

    private static string? FindTestAppPath(string appName)
    {
        // Search for the built test app executable
        // Look relative to the integration test assembly location
        var baseDir = AppContext.BaseDirectory;

        // Walk up to find the repo root (contains src/)
        var dir = new DirectoryInfo(baseDir);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
        {
            dir = dir.Parent;
        }

        if (dir == null) return null;

        // Look for the test app in its build output
        var searchPaths = new[]
        {
            // WinForms app (net481)
            Path.Combine(dir.FullName, "tests", "TestApps", appName, "bin", "Debug", "net481", $"{appName}.exe"),
            // WPF app (net8.0-windows)
            Path.Combine(dir.FullName, "tests", "TestApps", appName, "bin", "Debug", "net8.0-windows", $"{appName}.exe"),
            // Release builds
            Path.Combine(dir.FullName, "tests", "TestApps", appName, "bin", "Release", "net481", $"{appName}.exe"),
            Path.Combine(dir.FullName, "tests", "TestApps", appName, "bin", "Release", "net8.0-windows", $"{appName}.exe"),
        };

        return searchPaths.FirstOrDefault(File.Exists);
    }

    public Window? GetWinFormsWindow() => Session.GetWindow(WinFormsHandle);
    public Window? GetWpfWindow() => Session.GetWindow(WpfHandle);

    public void Dispose()
    {
        try { _winFormsProcess?.Kill(); } catch { }
        try { _wpfProcess?.Kill(); } catch { }
        Session.Dispose();
    }
}

/// <summary>
/// Collection definition so all test classes share the same fixture (same app instances).
/// </summary>
[CollectionDefinition("TestApps")]
public class TestAppCollection : ICollectionFixture<TestAppFixture>
{
}
