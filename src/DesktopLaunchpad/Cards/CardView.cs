using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using DesktopLaunchpad.Themes;

namespace DesktopLaunchpad.Cards;

/// <summary>
/// One card on the launchpad. In normal mode a click opens it. In edit mode it
/// can be dragged and resized, and shows edit and delete buttons.
/// </summary>
public sealed class CardView : Border
{
    public const double DefaultWidth = 220;
    public const double DefaultHeight = 132;
    public const double MinCardWidth = 150;
    public const double MinCardHeight = 96;
    private const double GridSize = 10;
    private const double DragThreshold = 4;

    /// <summary>The see-through default card look, used for the "Default" swatch in the card editor.</summary>
    internal static readonly Brush DefaultBackground = Frozen(Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF));

    private readonly Theme _theme = Theme.Current;

    private readonly StackPanel _editButtons;
    private readonly Thumb _resizeGrip;
    private Point _pressPoint;
    private Point _pressOrigin;
    private bool _pressed;
    private bool _dragging;
    private bool _isEditing;

    public CardDefinition Card { get; }

    /// <summary>Clicked in normal mode.</summary>
    public event Action<CardView>? Activated;
    /// <summary>Clicked, or the pencil pressed, in edit mode.</summary>
    public event Action<CardView>? EditRequested;
    public event Action<CardView>? DeleteRequested;
    /// <summary>Moved or resized; the new layout should be saved.</summary>
    public event Action<CardView>? LayoutChanged;

    public CardView(CardDefinition card)
    {
        Card = card;

        Width = card.Width ?? DefaultWidth;
        Height = card.Height ?? DefaultHeight;
        Padding = new Thickness(18, 16, 18, 16);
        CornerRadius = new CornerRadius(_theme.CornerRadius);
        var custom = ParseBrush(card.Color);
        Background = custom ?? _theme.Card;
        var (textBrush, subtleBrush) = _theme.TextOn(custom);
        BorderBrush = _theme.CardBorder;
        BorderThickness = new Thickness(1);
        Cursor = Cursors.Hand;
        ToolTip = card.Type == CardType.Workspace
            ? string.Join("\n", card.Items?.Select(i => i.Target) ?? Enumerable.Empty<string>())
            : card.Target;
        RenderTransformOrigin = new Point(0.5, 0.5);
        RenderTransform = new ScaleTransform(1, 1);

        var content = new StackPanel();
        content.Children.Add(BuildIcon(card, textBrush));
        content.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(card.Title) ? (card.Type == CardType.Workspace ? "Workspace" : card.Target) : card.Title,
            FontSize = 17,
            FontWeight = FontWeights.SemiBold,
            Foreground = textBrush,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 12, 0, 0),
        });
        content.Children.Add(new TextBlock
        {
            Text = Describe(card),
            FontSize = 12,
            Foreground = subtleBrush,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 2, 0, 0),
        });

        _editButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, -6, -8, 0),
            Visibility = Visibility.Collapsed,
        };
        _editButtons.Children.Add(SmallButton("", "Edit card", () => EditRequested?.Invoke(this)));
        _editButtons.Children.Add(SmallButton("", "Delete card", () => DeleteRequested?.Invoke(this)));

        _resizeGrip = new Thumb
        {
            Width = 18,
            Height = 18,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, -12, -10),
            Cursor = Cursors.SizeNWSE,
            Visibility = Visibility.Collapsed,
            ToolTip = "Drag to resize",
            Template = ResizeGripTemplate(_theme.Accent),
        };
        _resizeGrip.DragDelta += (_, e) =>
        {
            Width = Math.Max(MinCardWidth, Width + e.HorizontalChange);
            Height = Math.Max(MinCardHeight, Height + e.VerticalChange);
        };
        _resizeGrip.DragCompleted += (_, _) =>
        {
            Width = Snap(Width);
            Height = Snap(Height);
            LayoutChanged?.Invoke(this);
        };

        var root = new Grid();
        root.Children.Add(content);
        root.Children.Add(_editButtons);
        root.Children.Add(_resizeGrip);
        Child = root;

        MouseEnter += (_, _) => { if (!_isEditing) { BorderBrush = _theme.CardHover; SetScale(1.03); } };
        MouseLeave += (_, _) => { if (!_isEditing) { BorderBrush = _theme.CardBorder; SetScale(1.0); } };
        MouseLeftButtonDown += OnPress;
        MouseMove += OnMove;
        MouseLeftButtonUp += OnRelease;
        LostMouseCapture += (_, _) => { _pressed = false; _dragging = false; };
    }

    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            _isEditing = value;
            _editButtons.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            _resizeGrip.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            BorderBrush = value ? _theme.Accent : _theme.CardBorder;
            BorderThickness = new Thickness(value ? 1.5 : 1);
            Cursor = value ? Cursors.SizeAll : Cursors.Hand;
            SetScale(1.0);
        }
    }

    public double Left => Canvas.GetLeft(this) is double x && !double.IsNaN(x) ? x : 0;
    public double Top => Canvas.GetTop(this) is double y && !double.IsNaN(y) ? y : 0;

    public void MoveTo(double x, double y)
    {
        Canvas.SetLeft(this, Math.Max(0, x));
        Canvas.SetTop(this, Math.Max(0, y));
    }

    /// <summary>Copies the on-screen position and size back into the card definition.</summary>
    public void StoreLayout()
    {
        Card.X = Math.Round(Left);
        Card.Y = Math.Round(Top);
        Card.Width = Math.Round(Width);
        Card.Height = Math.Round(Height);
    }

    private void OnPress(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true; // never let a card click close the launchpad
        if (e.OriginalSource is DependencyObject source && IsInsideButtonOrGrip(source)) return;

        _pressed = true;
        _dragging = false;
        _pressPoint = e.GetPosition(Parent as IInputElement);
        _pressOrigin = new Point(Left, Top);
        CaptureMouse();
    }

    private void OnMove(object sender, MouseEventArgs e)
    {
        if (!_pressed || !_isEditing) return;
        var now = e.GetPosition(Parent as IInputElement);
        var delta = now - _pressPoint;
        if (!_dragging && Math.Abs(delta.X) < DragThreshold && Math.Abs(delta.Y) < DragThreshold) return;

        _dragging = true;
        Panel.SetZIndex(this, 1000);
        MoveTo(_pressOrigin.X + delta.X, _pressOrigin.Y + delta.Y);
    }

    private void OnRelease(object sender, MouseButtonEventArgs e)
    {
        if (!_pressed) return;
        e.Handled = true;
        var wasDragging = _dragging;
        _pressed = false;
        _dragging = false;
        ReleaseMouseCapture();
        Panel.SetZIndex(this, 0);

        if (wasDragging)
        {
            MoveTo(Snap(Left), Snap(Top));
            LayoutChanged?.Invoke(this);
        }
        else if (_isEditing)
        {
            EditRequested?.Invoke(this);
        }
        else if (IsPointInside(e.GetPosition(this)))
        {
            Activated?.Invoke(this);
        }
    }

    private bool IsPointInside(Point p) => p.X >= 0 && p.Y >= 0 && p.X <= ActualWidth && p.Y <= ActualHeight;

    private bool IsInsideButtonOrGrip(DependencyObject source)
    {
        for (var node = source; node is not null && node != this; node = VisualTreeHelper.GetParent(node))
        {
            if (node is ButtonBase || node == _resizeGrip) return true;
        }
        return false;
    }

    /// <summary>A non-clickable card used for "no cards" and error messages.</summary>
    public static FrameworkElement CreateMessage(string heading, string body, bool isError = false)
    {
        var theme = Theme.Current;
        var background = isError ? Frozen(Color.FromArgb(0xCC, 0xB9, 0x1C, 0x1C)) : theme.Card;
        var (text, subtle) = isError ? (Brushes.White, Frozen(Color.FromRgb(0xFE, 0xE2, 0xE2))) : (theme.Text, theme.SubtleText);
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = heading, FontSize = 18, FontWeight = FontWeights.SemiBold, Foreground = text });
        panel.Children.Add(new TextBlock { Text = body, TextWrapping = TextWrapping.Wrap, Foreground = subtle, Margin = new Thickness(0, 6, 0, 0) });

        var border = new Border
        {
            Width = 460,
            Padding = new Thickness(20),
            CornerRadius = new CornerRadius(theme.CornerRadius),
            Background = background,
            BorderBrush = theme.CardBorder,
            BorderThickness = new Thickness(1),
            Child = panel,
        };
        border.MouseLeftButtonDown += (_, e) => e.Handled = true;
        return border;
    }

    private static Button SmallButton(string glyph, string tip, Action onClick)
    {
        var button = new Button
        {
            Content = glyph,
            ToolTip = tip,
            FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
            FontSize = 13,
            Width = 28,
            Height = 28,
            Margin = new Thickness(4, 0, 0, 0),
            Foreground = Brushes.White,
            Background = Frozen(Color.FromArgb(0x99, 0x10, 0x14, 0x18)),
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
        };
        button.Click += (_, e) => { e.Handled = true; onClick(); };
        return button;
    }

    private static ControlTemplate ResizeGripTemplate(Brush fill)
    {
        // A small dot in the theme's accent color in the bottom-right corner.
        var ellipse = new FrameworkElementFactory(typeof(System.Windows.Shapes.Ellipse));
        ellipse.SetValue(System.Windows.Shapes.Shape.FillProperty, fill);
        ellipse.SetValue(System.Windows.Shapes.Shape.StrokeProperty, Brushes.White);
        ellipse.SetValue(System.Windows.Shapes.Shape.StrokeThicknessProperty, 1.5);
        return new ControlTemplate(typeof(Thumb)) { VisualTree = ellipse };
    }

    private static FrameworkElement BuildIcon(CardDefinition card, Brush foreground)
    {
        if (card.Type == CardType.Program && CardIcons.ForProgram(card.Target) is ImageSource image)
            return new Image { Source = image, Width = 36, Height = 36, HorizontalAlignment = HorizontalAlignment.Left };

        // Fallback: a Windows symbol-font glyph (globe for websites, app window for programs).
        return new TextBlock
        {
            Text = card.Type == CardType.Website ? "" : "",
            FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
            FontSize = 32,
            Foreground = foreground,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
    }

    private static string Describe(CardDefinition card)
    {
        if (card.Type == CardType.Workspace)
        {
            var count = card.Items?.Count ?? 0;
            return count == 1 ? "Opens 1 thing" : $"Opens {count} things";
        }
        if (card.Type == CardType.Website)
        {
            var host = Uri.TryCreate(Launcher.NormalizeUrl(card.Target), UriKind.Absolute, out var uri)
                ? uri.Host.Replace("www.", "")
                : card.Target;
            var browser = string.IsNullOrWhiteSpace(card.Browser) || card.Browser.Equals("default", StringComparison.OrdinalIgnoreCase)
                ? "default browser"
                : System.IO.Path.GetFileNameWithoutExtension(card.Browser);
            return $"{host} · {browser}";
        }
        return System.IO.Path.GetFileName(Environment.ExpandEnvironmentVariables(card.Target.TrimEnd('\\', '/')));
    }

    private void SetScale(double scale)
    {
        if (RenderTransform is ScaleTransform t) { t.ScaleX = scale; t.ScaleY = scale; }
    }

    private static double Snap(double value) => Math.Round(value / GridSize) * GridSize;

    public static Brush? ParseBrush(string? color)
    {
        if (string.IsNullOrWhiteSpace(color)) return null;
        try { return Frozen((Color)ColorConverter.ConvertFromString(color)); }
        catch (Exception) { return null; }
    }

    internal static Brush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
