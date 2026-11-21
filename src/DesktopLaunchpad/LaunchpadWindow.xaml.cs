using System.Windows;
using System.Windows.Controls;
using DesktopLaunchpad.Cards;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace DesktopLaunchpad;

/// <summary>
/// The full-screen dashboard. It is created once and shown or hidden, so it
/// opens instantly. It covers the monitor the mouse is on.
/// </summary>
public partial class LaunchpadWindow : Window
{
    private static readonly Duration FadeDuration = new(TimeSpan.FromMilliseconds(120));
    private readonly DispatcherTimer _clock;
    private bool _isHiding;

    public LaunchpadWindow()
    {
        InitializeComponent();

        _clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clock.Tick += (_, _) => UpdateClock();

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                HideLaunchpad();
                e.Handled = true;
            }
        };

        // Clicking another window hides the launchpad.
        Deactivated += (_, _) => HideLaunchpad();
    }

    public void ShowLaunchpad()
    {
        if (IsVisible && !_isHiding) { Activate(); return; }
        _isHiding = false;

        MoveToMonitorUnderMouse();
        UpdateClock();
        LoadCards();
        _clock.Start();

        Show();
        WindowState = WindowState.Maximized;
        Activate();
        Focus();

        BeginAnimation(OpacityProperty, new DoubleAnimation(1, FadeDuration));
    }

    public void HideLaunchpad()
    {
        if (!IsVisible || _isHiding) return;
        _isHiding = true;

        var fade = new DoubleAnimation(0, FadeDuration);
        fade.Completed += (_, _) =>
        {
            if (!_isHiding) return; // reopened during the fade
            _clock.Stop();
            Hide();
            _isHiding = false;
        };
        BeginAnimation(OpacityProperty, fade);
    }

    /// <summary>
    /// Places the (un-maximized) window on the monitor under the mouse so that
    /// maximizing fills that monitor.
    /// </summary>
    private void MoveToMonitorUnderMouse()
    {
        WindowState = WindowState.Normal;
        var screen = Forms.Screen.FromPoint(Forms.Cursor.Position).WorkingArea;

        // Screen coordinates are in physical pixels; WPF uses 96-DPI units.
        var dpi = VisualTreeHelper.GetDpi(this);
        Left = (screen.Left + 10) / dpi.DpiScaleX;
        Top = (screen.Top + 10) / dpi.DpiScaleY;
        Width = 400;
        Height = 300;
    }

    /// <summary>Re-reads cards.json so edits show up the next time the launchpad opens.</summary>
    private void LoadCards()
    {
        CardPanel.Children.Clear();
        try
        {
            var cards = CardStore.Load();
            if (cards.Count == 0)
            {
                CardPanel.Children.Add(CardViewFactory.CreateMessage("No cards yet",
                    "Right-click the tray icon and choose Edit cards to add some."));
                return;
            }
            foreach (var card in cards)
                CardPanel.Children.Add(CardViewFactory.Create(card, OnCardClicked));
        }
        catch (CardFileException ex)
        {
            CardPanel.Children.Add(CardViewFactory.CreateMessage("Couldn't load your cards", ex.Message, isError: true));
        }
    }

    private void OnCardClicked(CardDefinition card)
    {
        HideLaunchpad();
        try
        {
            Launcher.Launch(card);
        }
        catch (LaunchException ex)
        {
            MessageBox.Show(ex.Message, "Desktop Launchpad", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void UpdateClock() =>
        ClockText.Text = DateTime.Now.ToString("dddd, MMMM d  ·  h:mm tt");

    private void Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Cards and scroll bars mark their clicks as handled, so anything
        // that reaches here is a click on empty space.
        HideLaunchpad();
    }
}
