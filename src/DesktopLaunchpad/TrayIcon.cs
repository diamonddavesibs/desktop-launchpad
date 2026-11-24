using Forms = System.Windows.Forms;

namespace DesktopLaunchpad;

/// <summary>
/// The icon in the Windows notification area, with Open, Edit cards file, Start with Windows and Exit menu items.
/// Double-clicking the icon opens the launchpad.
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _icon;

    public TrayIcon(Action onOpen, Action onEditCards, Action onExit)
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open launchpad (Ctrl+Space)", null, (_, _) => onOpen());
        menu.Items.Add("Edit cards file (advanced)…", null, (_, _) => onEditCards());

        var startWithWindows = new Forms.ToolStripMenuItem("Start with Windows") { CheckOnClick = true };
        menu.Opening += (_, _) => startWithWindows.Checked = StartupManager.IsEnabled;
        startWithWindows.Click += (_, _) =>
        {
            try { StartupManager.SetEnabled(startWithWindows.Checked); }
            catch (Exception ex)
            {
                startWithWindows.Checked = StartupManager.IsEnabled;
                ShowWarning("Couldn't change startup setting", ex.Message);
            }
        };
        menu.Items.Add(startWithWindows);
        menu.Items.Add(new Forms.ToolStripSeparator());
        var version = typeof(TrayIcon).Assembly.GetName().Version;
        menu.Items.Add(new Forms.ToolStripMenuItem($"Version {version?.ToString(3)}") { Enabled = false });
        menu.Items.Add("Exit", null, (_, _) => onExit());

        _icon = new Forms.NotifyIcon
        {
            Icon = LoadAppIcon(),
            Text = "Desktop Launchpad",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icon.DoubleClick += (_, _) => onOpen();
    }

    /// <summary>Loads Assets/launchpad.ico at the size Windows uses for tray icons.</summary>
    private static System.Drawing.Icon LoadAppIcon()
    {
        var uri = new Uri("pack://application:,,,/Assets/launchpad.ico");
        var resource = System.Windows.Application.GetResourceStream(uri);
        if (resource is null) return System.Drawing.SystemIcons.Application;
        using var stream = resource.Stream;
        return new System.Drawing.Icon(stream, Forms.SystemInformation.SmallIconSize);
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
