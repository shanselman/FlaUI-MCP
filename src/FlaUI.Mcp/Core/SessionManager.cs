using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using FlaUIApplication = FlaUI.Core.Application;

namespace PlaywrightWindows.Mcp.Core;

/// <summary>
/// Manages UI Automation sessions and launched applications
/// </summary>
public class SessionManager : IDisposable
{
    private readonly UIA3Automation _automation;
    private readonly Dictionary<string, FlaUIApplication> _applications = new();
    private readonly Dictionary<string, Window> _windows = new();
    private int _windowCounter = 0;

    public SessionManager()
    {
        _automation = new UIA3Automation();
    }

    public UIA3Automation Automation => _automation;

    public (string handle, Window window) LaunchApp(string appPath, string[]? args = null)
    {
        // Use Process.Start for more reliable launching
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = appPath,
            Arguments = args != null ? string.Join(" ", args) : "",
            UseShellExecute = true
        };
        
        var process = System.Diagnostics.Process.Start(psi);
        if (process == null)
        {
            throw new Exception($"Failed to start process: {appPath}");
        }
        
        // Wait for the process to be ready
        try
        {
            process.WaitForInputIdle(5000);
        }
        catch { /* Some processes don't support this */ }
        
        Thread.Sleep(1000); // Extra wait for window to appear
        
        // Find window by process ID from desktop
        var desktop = _automation.GetDesktop();
        Window? window = null;
        
        // Try to find by process ID first
        var element = desktop.FindFirstDescendant(cf => cf.ByProcessId(process.Id));
        if (element != null)
        {
            window = element.AsWindow();
        }
        
        // If not found, the app might have spawned a different process (common for UWP)
        // Search by waiting for a new window
        if (window == null)
        {
            // Get window count before
            var existingTitles = new HashSet<string>(
                _windows.Values.Select(w => w.Title).Where(t => !string.IsNullOrEmpty(t))
            );
            
            // Wait and look for new windows
            for (int i = 0; i < 10 && window == null; i++)
            {
                Thread.Sleep(500);
                var windows = desktop.FindAllChildren(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Window));
                foreach (var w in windows)
                {
                    var win = w.AsWindow();
                    if (win != null && !string.IsNullOrEmpty(win.Title))
                    {
                        // Check if this looks like our app
                        var title = win.Title.ToLowerInvariant();
                        var appName = Path.GetFileNameWithoutExtension(appPath).ToLowerInvariant();
                        if (title.Contains(appName) || !existingTitles.Contains(win.Title))
                        {
                            window = win;
                            break;
                        }
                    }
                }
            }
        }
        
        if (window == null)
        {
            throw new Exception($"Could not find window for {appPath}. Try using windows_list_windows and windows_focus instead.");
        }

        var windowHandle = RegisterWindow(window);
        return (windowHandle, window);
    }

    public (string handle, Window window) AttachToWindow(string title)
    {
        var desktop = _automation.GetDesktop();
        var window = desktop.FindFirstDescendant(cf => cf.ByName(title))?.AsWindow();
        
        if (window == null)
        {
            throw new Exception($"Window not found: {title}");
        }

        var handle = RegisterWindow(window);
        return (handle, window);
    }

    public string RegisterWindow(Window window)
    {
        var handle = $"w{++_windowCounter}";
        _windows[handle] = window;
        return handle;
    }

    public Window? GetWindow(string handle)
    {
        return _windows.TryGetValue(handle, out var window) ? window : null;
    }

    public List<(string handle, string title, string? processName)> ListWindows()
    {
        var desktop = _automation.GetDesktop();
        var windows = desktop.FindAllChildren(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Window));
        
        var result = new List<(string, string, string?)>();
        foreach (var w in windows)
        {
            var window = w.AsWindow();
            if (window != null && !string.IsNullOrEmpty(window.Title))
            {
                var handle = RegisterWindow(window);
                string? processName = null;
                try 
                { 
                    processName = window.Properties.ProcessId.TryGetValue(out var pid) 
                        ? System.Diagnostics.Process.GetProcessById(pid).ProcessName 
                        : null; 
                }
                catch { }
                
                result.Add((handle, window.Title, processName));
            }
        }
        return result;
    }

    public void FocusWindow(string handle)
    {
        var window = GetWindow(handle);
        if (window == null)
        {
            throw new Exception($"Window not found: {handle}");
        }
        window.Focus();
    }

    public void CloseWindow(string handle)
    {
        var window = GetWindow(handle);
        if (window == null)
        {
            throw new Exception($"Window not found: {handle}");
        }
        window.Close();
        _windows.Remove(handle);
    }

    public void Dispose()
    {
        foreach (var app in _applications.Values)
        {
            try { app.Close(); } catch { }
        }
        _applications.Clear();
        _windows.Clear();
        _automation.Dispose();
    }
}
