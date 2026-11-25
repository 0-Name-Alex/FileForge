using System.Windows;
using System.Windows.Input;
using FileForge.App.ViewModels;
using FileForge.Platform.Windows.Hotkeys;
using FileForge.Platform.Windows.Memory;
using FileForge.Platform.Windows.Tray;

namespace FileForge.App;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private QuickLauncherWindow? _quickLauncher;
    private WindowsHotkeyService? _hotkeyService;
    private SystemTrayService? _trayService;
    private MainViewModel? _mainVm;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mainVm = new MainViewModel();

        // Support launching with file/folder arguments (e.g., Windows Explorer context menu or Send To)
        var initialPaths = e.Args.Where(a => !string.IsNullOrWhiteSpace(a)).ToArray();
        if (initialPaths.Length > 0)
        {
            _mainVm.AddPaths(initialPaths);
        }

        _mainWindow = new MainWindow { DataContext = _mainVm };
        _mainWindow.Closing += (_, args) =>
        {
            // Minimize to tray instead of exiting
            args.Cancel = true;
            _mainWindow.Hide();
            MemoryOptimizer.TrimWorkingSet();
        };
        _mainWindow.Show();

        // System tray
        _trayService = new SystemTrayService();
        _trayService.ShowRequested += (_, _) => ShowMainWindow();
        _trayService.QuickLauncherRequested += (_, _) => ToggleQuickLauncher();
        _trayService.ExitRequested += (_, _) => ExitApp();

        // Global hotkey: Ctrl+Alt+F
        _hotkeyService = new WindowsHotkeyService();
        _hotkeyService.Register("QuickLaunch", ModifierKeys.Control | ModifierKeys.Alt, Key.F);
        _hotkeyService.HotkeyPressed += (_, name) =>
        {
            if (name == "QuickLaunch")
            {
                Dispatcher.Invoke(ToggleQuickLauncher);
            }
        };
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null) return;
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void ToggleQuickLauncher()
    {
        if (_quickLauncher is { IsVisible: true })
        {
            _quickLauncher.Close();
            _quickLauncher = null;
            return;
        }

        _quickLauncher = new QuickLauncherWindow(_mainVm!);
        _quickLauncher.Closed += (_, _) => _quickLauncher = null;
        _quickLauncher.Show();
        _quickLauncher.Activate();
    }

    private void ExitApp()
    {
        _hotkeyService?.Dispose();
        _trayService?.Dispose();
        _quickLauncher?.Close();
        Shutdown();
    }
}
