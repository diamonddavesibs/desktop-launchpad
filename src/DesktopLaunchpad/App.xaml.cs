using System.Windows;
using System.Windows.Input;

namespace DesktopLaunchpad;

/// <summary>
/// Starts the app in the background: a tray icon plus a global hotkey that
/// toggles the launchpad window. The app keeps running until Exit is chosen
/// from the tray menu.
/// </summary>
public partial class App : Application
{
    private const string SingleInstanceName = "DesktopLaunchpad.SingleInstance";

    private Mutex? _singleInstance;
    private HotkeyManager? _hotkey;
    private TrayIcon? _tray;
    private LaunchpadWindow? _launchpad;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new Mutex(true, SingleInstanceName, out bool isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show("Desktop Launchpad is already running. Look for its icon in the system tray.",
                "Desktop Launchpad", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        _launchpad = new LaunchpadWindow();
        _tray = new TrayIcon(onOpen: ShowLaunchpad, onExit: ExitApp);

        _hotkey = new HotkeyManager();
        _hotkey.Pressed += (_, _) => ToggleLaunchpad();
        if (_hotkey.Register(ModifierKeys.Control, Key.Space))
        {
            _tray.ShowInfo("Desktop Launchpad is running", "Press Ctrl+Space to open your launchpad.");
        }
        else
        {
            _tray.ShowWarning("Hotkey unavailable",
                "Ctrl+Space is already used by another app. Open the launchpad from the tray icon instead.");
        }
    }

    private void ToggleLaunchpad()
    {
        if (_launchpad is null) return;
        if (_launchpad.IsVisible) _launchpad.HideLaunchpad();
        else _launchpad.ShowLaunchpad();
    }

    private void ShowLaunchpad() => _launchpad?.ShowLaunchpad();

    private void ExitApp()
    {
        _hotkey?.Dispose();
        _tray?.Dispose();
        _launchpad?.Close();
        _singleInstance?.ReleaseMutex();
        _singleInstance?.Dispose();
        Shutdown();
    }
}
