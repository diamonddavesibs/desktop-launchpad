using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DesktopLaunchpad.Cards;
using DesktopLaunchpad.Themes;
using Forms = System.Windows.Forms;

namespace DesktopLaunchpad;

/// <summary>
/// The full-screen dashboard. It is created once and shown or hidden, so it
/// opens instantly. It covers the monitor the mouse is on.
/// </summary>
public partial class LaunchpadWindow : Window
{
    private const double Gap = 20;
    private const string NormalFooter = "Click a card to open it  ·  Esc or click empty space to close  ·  Edit to move, resize and add cards";
    private const string EditFooter = "Drag cards to move them  ·  Drag the yellow dot to resize  ·  Click a card to change it  ·  Esc or Done when finished";

    private static readonly Duration FadeDuration = new(TimeSpan.FromMilliseconds(120));
    private readonly DispatcherTimer _clock;
    private readonly List<CardDefinition> _cards = new();
    private readonly List<CardView> _views = new();
    private bool _isHiding;
    private bool _isEditing;
    private bool _dialogOpen;

    public LaunchpadWindow()
    {
        InitializeComponent();
        FooterText.Text = NormalFooter;
        ApplyTheme(SettingsStore.Load());

        _clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clock.Tick += (_, _) => UpdateClock();

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape) return;
            if (_isEditing) SetEditing(false);
            else HideLaunchpad();
            e.Handled = true;
        };

        // Clicking another window hides the launchpad, but not our own dialogs.
        Deactivated += (_, _) => { if (!_dialogOpen) HideLaunchpad(); };
    }

    public void ShowLaunchpad()
    {
        if (IsVisible && !_isHiding) { Activate(); return; }
        _isHiding = false;

        UpdateClock();
        _clock.Start();

        CoverMonitorUnderMouse();
        Show();
        CoverMonitorUnderMouse(); // again, in case showing it moved it to a monitor with a different scale
        UpdateLayout(); // so the card area has its real size before placing cards
        LoadCards();
        Activate();
        Focus();

        BeginAnimation(OpacityProperty, new DoubleAnimation(1, FadeDuration));
    }

    public void HideLaunchpad()
    {
        if (!IsVisible || _isHiding) return;
        _isHiding = true;
        if (_isEditing) SetEditing(false);

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
    /// Sizes the window to exactly cover the monitor under the mouse. This is done in
    /// physical pixels with SetWindowPos rather than by maximizing: a window hidden
    /// while maximized can come back at its small "restored" size instead.
    /// </summary>
    private void CoverMonitorUnderMouse()
    {
        WindowState = WindowState.Normal;
        var bounds = Forms.Screen.FromPoint(Forms.Cursor.Position).Bounds;
        var hwnd = new WindowInteropHelper(this).EnsureHandle();
        SetWindowPos(hwnd, IntPtr.Zero, bounds.Left, bounds.Top, bounds.Width, bounds.Height,
            SwpNoZOrder | SwpNoActivate);
    }

    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

    // ----- Cards -----

    /// <summary>Re-reads cards.json so hand edits show up the next time the launchpad opens.</summary>
    private void LoadCards()
    {
        _cards.Clear();
        EditButton.IsEnabled = true;
        try
        {
            _cards.AddRange(CardStore.Load());
        }
        catch (CardFileException ex)
        {
            // Don't allow editing, which would overwrite the file the user is fixing.
            EditButton.IsEnabled = false;
            ShowMessage("Couldn't load your cards", ex.Message + " Fix it with Edit cards file in the tray menu.", isError: true);
            return;
        }
        RebuildViews();
    }

    private void RebuildViews()
    {
        CardCanvas.Children.Clear();
        _views.Clear();

        if (_cards.Count == 0)
        {
            ShowMessage("No cards yet", "Click Edit, then + Add card to make your first one.");
            return;
        }

        foreach (var card in _cards)
        {
            var view = new CardView(card) { IsEditing = _isEditing };
            view.Activated += OnCardActivated;
            view.EditRequested += v => EditCard(v.Card);
            view.DeleteRequested += v => DeleteCard(v.Card);
            view.LayoutChanged += _ => { UpdateCanvasSize(); SaveCards(); };
            _views.Add(view);
        }

        // Cards with a saved position go first, so the rest flow around them.
        var placed = new List<Rect>();
        foreach (var view in _views.Where(v => v.Card.X is not null && v.Card.Y is not null))
        {
            view.MoveTo(view.Card.X!.Value, view.Card.Y!.Value);
            placed.Add(new Rect(view.Left, view.Top, view.Width, view.Height));
        }
        foreach (var view in _views.Where(v => v.Card.X is null || v.Card.Y is null))
        {
            var spot = FindFreeSpot(placed, view.Width, view.Height);
            view.MoveTo(spot.X, spot.Y);
            placed.Add(new Rect(spot, new Size(view.Width, view.Height)));
        }

        foreach (var view in _views) CardCanvas.Children.Add(view);
        UpdateCanvasSize();
    }

    /// <summary>First free slot, scanning left to right then top to bottom.</summary>
    private Point FindFreeSpot(List<Rect> taken, double width, double height)
    {
        var available = Math.Max(CardScroller.ActualWidth - Gap, width);
        var stepX = CardView.DefaultWidth + Gap;
        var stepY = CardView.DefaultHeight + Gap;

        for (var row = 0; row < 500; row++)
        {
            for (var col = 0; ; col++)
            {
                var x = col * stepX;
                if (col > 0 && x + width > available) break;
                var candidate = new Rect(x, row * stepY, width, height);
                var clash = taken.Any(r =>
                    new Rect(r.X - Gap + 1, r.Y - Gap + 1, r.Width + 2 * Gap - 2, r.Height + 2 * Gap - 2).IntersectsWith(candidate));
                if (!clash) return candidate.Location;
            }
        }
        return new Point(0, 0);
    }

    private void UpdateCanvasSize()
    {
        CardCanvas.Width = _views.Count == 0 ? 0 : _views.Max(v => v.Left + v.Width) + Gap;
        CardCanvas.Height = _views.Count == 0 ? 0 : _views.Max(v => v.Top + v.Height) + Gap;
    }

    private void ShowMessage(string heading, string body, bool isError = false)
    {
        CardCanvas.Children.Clear();
        _views.Clear();
        var message = CardView.CreateMessage(heading, body, isError);
        System.Windows.Controls.Canvas.SetLeft(message, 0);
        System.Windows.Controls.Canvas.SetTop(message, 0);
        CardCanvas.Children.Add(message);
        CardCanvas.Width = 480;
        CardCanvas.Height = 200;
    }

    private void SaveCards()
    {
        foreach (var view in _views) view.StoreLayout();
        try
        {
            CardStore.Save(_cards);
        }
        catch (Exception ex)
        {
            RunModal(() => MessageBox.Show(this, $"Couldn't save your cards: {ex.Message}", "Desktop Launchpad",
                MessageBoxButton.OK, MessageBoxImage.Warning));
        }
    }

    private void OnCardActivated(CardView view)
    {
        HideLaunchpad();
        try
        {
            Launcher.Launch(view.Card);
        }
        catch (LaunchException ex)
        {
            MessageBox.Show(ex.Message, "Desktop Launchpad", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // ----- Edit mode -----

    private void SetEditing(bool editing)
    {
        _isEditing = editing;
        EditButton.Content = editing ? "Done" : "Edit";
        AddButton.Visibility = editing ? Visibility.Visible : Visibility.Collapsed;
        FooterText.Text = editing ? EditFooter : NormalFooter;
        foreach (var view in _views) view.IsEditing = editing;
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        SetEditing(!_isEditing);
        if (!_isEditing) SaveCards();
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var editor = new CardEditorWindow(null) { Owner = this };
        if (RunModal(() => editor.ShowDialog()) != true || editor.Result is null) return;

        // Fix the current positions first so existing cards don't move.
        foreach (var view in _views) view.StoreLayout();
        _cards.Add(editor.Result);
        RebuildViews();
        SaveCards();
    }

    private void EditCard(CardDefinition card)
    {
        var editor = new CardEditorWindow(card) { Owner = this };
        if (RunModal(() => editor.ShowDialog()) != true || editor.Result is null) return;

        foreach (var view in _views) view.StoreLayout();
        var index = _cards.IndexOf(card);
        if (index < 0) return;
        _cards[index] = editor.Result;
        RebuildViews();
        SaveCards();
    }

    private void DeleteCard(CardDefinition card)
    {
        var answer = RunModal(() => MessageBox.Show(this, $"Delete the card \"{card.Title}\"?", "Desktop Launchpad",
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No));
        if (answer != MessageBoxResult.Yes) return;

        foreach (var view in _views) view.StoreLayout();
        _cards.Remove(card);
        RebuildViews();
        SaveCards();
    }

    /// <summary>Runs a modal dialog without the launchpad hiding itself when it loses focus.</summary>
    private T RunModal<T>(Func<T> show)
    {
        _dialogOpen = true;
        try { return show(); }
        finally
        {
            _dialogOpen = false;
            Activate();
        }
    }

    // ----- Theme -----

    /// <summary>Switches to a look and redraws the cards in it. Does not save it.</summary>
    public void ApplyTheme(ThemeSettings settings)
    {
        Theme.SetCurrent(settings);
        var theme = Theme.Current;

        Resources["ThemeText"] = theme.Text;
        Resources["ThemeSubtleText"] = theme.SubtleText;
        Resources["ThemeFaintText"] = theme.FaintText;
        Resources["ThemeAccent"] = theme.Accent;
        Resources["ThemeButtonBackground"] = theme.ButtonBackground;
        Resources["ThemeButtonBorder"] = theme.ButtonBorder;
        Backdrop.Background = theme.BackgroundImage ?? Brushes.Transparent;
        BackdropTint.Background = theme.Backdrop;

        if (!IsVisible) return;
        if (!EditButton.IsEnabled)
        {
            LoadCards(); // showing the cards.json error; redraw it in the new look
            return;
        }
        foreach (var view in _views) view.StoreLayout(); // keep cards where they are
        RebuildViews();
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isEditing) SetEditing(false);
        SaveCardsIfLoaded();

        var original = SettingsStore.Load();
        var picker = new ThemeWindow(original, ApplyTheme) { Owner = this };
        if (RunModal(() => picker.ShowDialog()) == true && picker.Result is not null)
        {
            try
            {
                SettingsStore.Save(picker.Result);
            }
            catch (Exception ex)
            {
                RunModal(() => MessageBox.Show(this, $"Couldn't save your theme: {ex.Message}", "Desktop Launchpad",
                    MessageBoxButton.OK, MessageBoxImage.Warning));
            }
            ApplyTheme(picker.Result);
        }
        else
        {
            ApplyTheme(original); // undo the live preview
        }
    }

    /// <summary>Keeps card moves made in edit mode before redrawing the cards in a new look.</summary>
    private void SaveCardsIfLoaded()
    {
        if (_views.Count > 0 && EditButton.IsEnabled) SaveCards();
    }

    // ----- Misc -----

    private void UpdateClock() =>
        ClockText.Text = DateTime.Now.ToString("dddd, MMMM d  ·  h:mm tt");

    private void Backdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Cards, buttons and scroll bars mark their clicks as handled, so
        // anything that reaches here is a click on empty space.
        if (!_isEditing) HideLaunchpad();
    }
}
