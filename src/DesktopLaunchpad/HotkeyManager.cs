using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace DesktopLaunchpad;

/// <summary>
/// Registers a system-wide hotkey with Windows and raises <see cref="Pressed"/>
/// whenever it is pressed, even when another app has focus.
/// </summary>
public sealed class HotkeyManager : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HotkeyId = 0x4C50; // arbitrary id, unique within this app
    private const uint MOD_NOREPEAT = 0x4000;

    // A message-only window that receives WM_HOTKEY.
    private readonly HwndSource _source;
    private bool _registered;

    public event EventHandler? Pressed;

    public HotkeyManager()
    {
        var parameters = new HwndSourceParameters("DesktopLaunchpadHotkey")
        {
            ParentWindow = new IntPtr(-3), // HWND_MESSAGE
            WindowStyle = 0,
        };
        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    /// <summary>Returns false if another app already owns this key combination.</summary>
    public bool Register(ModifierKeys modifiers, Key key)
    {
        Unregister();
        uint virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);
        _registered = RegisterHotKey(_source.Handle, HotkeyId, (uint)modifiers | MOD_NOREPEAT, virtualKey);
        return _registered;
    }

    public void Unregister()
    {
        if (!_registered) return;
        UnregisterHotKey(_source.Handle, HotkeyId);
        _registered = false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            Pressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
        _source.RemoveHook(WndProc);
        _source.Dispose();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
