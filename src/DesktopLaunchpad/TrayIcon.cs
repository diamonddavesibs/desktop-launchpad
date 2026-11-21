using Forms = System.Windows.Forms;

namespace DesktopLaunchpad;

/// <summary>
/// The icon in the Windows notification area, with Open and Exit menu items.
/// Double-clicking the icon opens the launchpad.
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _icon;

    public TrayIcon(Action onOpen, Action onExit)
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open launchpad (Ctrl+Space)", null, (_, _) => onOpen());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => onExit());

        _icon = new Forms.NotifyIcon
        {
            // Placeholder icon until the app gets its own.
            Icon = System.Drawing.SystemIcons.Application,
            Text = "Desktop Launchpad",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icon.DoubleClick += (_, _) => onOpen();
    }

    public void ShowInfo(string title, string text) =>
        _icon.ShowBalloonTip(3000, title, text, Forms.ToolTipIcon.Info);

    public void ShowWarning(string title, string text) =>
        _icon.ShowBalloonTip(5000, title, text, Forms.ToolTipIcon.Warning);

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
