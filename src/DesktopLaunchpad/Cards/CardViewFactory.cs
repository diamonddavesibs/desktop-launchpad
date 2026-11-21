using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DesktopLaunchpad.Cards;

/// <summary>Builds the on-screen tile for a card.</summary>
public static class CardViewFactory
{
    private const double CardWidth = 220;
    private const double CardHeight = 132;
    private static readonly Brush DefaultBackground = Frozen(Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF));
    private static readonly Brush BorderColor = Frozen(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF));
    private static readonly Brush HoverBorder = Frozen(Color.FromArgb(0xC0, 0xFF, 0xFF, 0xFF));
    private static readonly Brush SubtleText = Frozen(Color.FromRgb(0xC8, 0xD0, 0xD8));

    public static FrameworkElement Create(CardDefinition card, Action<CardDefinition> onClick)
    {
        var icon = BuildIcon(card);

        var title = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(card.Title) ? card.Target : card.Title,
            FontSize = 17,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 12, 0, 0),
        };

        var subtitle = new TextBlock
        {
            Text = Describe(card),
            FontSize = 12,
            Foreground = SubtleText,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 2, 0, 0),
        };

        var content = new StackPanel();
        content.Children.Add(icon);
        content.Children.Add(title);
        content.Children.Add(subtitle);

        var border = new Border
        {
            Width = CardWidth,
            Height = CardHeight,
            Margin = new Thickness(8),
            Padding = new Thickness(18, 16, 18, 16),
            CornerRadius = new CornerRadius(14),
            Background = ParseBrush(card.Color) ?? DefaultBackground,
            BorderBrush = BorderColor,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Child = content,
            ToolTip = card.Target,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new ScaleTransform(1, 1),
        };

        border.MouseEnter += (_, _) => { border.BorderBrush = HoverBorder; SetScale(border, 1.03); };
        border.MouseLeave += (_, _) => { border.BorderBrush = BorderColor; SetScale(border, 1.0); };
        border.MouseLeftButtonDown += (_, e) => e.Handled = true; // don't close the launchpad
        border.MouseLeftButtonUp += (_, e) => { e.Handled = true; onClick(card); };
        return border;
    }

    /// <summary>A non-clickable card used for "no cards" and error messages.</summary>
    public static FrameworkElement CreateMessage(string heading, string body, bool isError = false)
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = heading,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
        });
        panel.Children.Add(new TextBlock
        {
            Text = body,
            TextWrapping = TextWrapping.Wrap,
            Foreground = SubtleText,
            Margin = new Thickness(0, 6, 0, 0),
        });

        var border = new Border
        {
            Width = 460,
            Margin = new Thickness(8),
            Padding = new Thickness(20),
            CornerRadius = new CornerRadius(14),
            Background = isError ? Frozen(Color.FromArgb(0x66, 0xB9, 0x1C, 0x1C)) : DefaultBackground,
            BorderBrush = BorderColor,
            BorderThickness = new Thickness(1),
            Child = panel,
        };
        border.MouseLeftButtonDown += (_, e) => e.Handled = true;
        return border;
    }

    private static FrameworkElement BuildIcon(CardDefinition card)
    {
        if (card.Type == CardType.Program && CardIcons.ForProgram(card.Target) is ImageSource image)
        {
            return new Image
            {
                Source = image,
                Width = 36,
                Height = 36,
                HorizontalAlignment = HorizontalAlignment.Left,
            };
        }

        // Fallback: a Windows symbol-font glyph (globe for websites, app window for programs).
        return new TextBlock
        {
            Text = card.Type == CardType.Website ? "" : "",
            FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
            FontSize = 32,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
    }

    private static string Describe(CardDefinition card)
    {
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

    private static void SetScale(Border border, double scale)
    {
        if (border.RenderTransform is ScaleTransform t)
        {
            t.ScaleX = scale;
            t.ScaleY = scale;
        }
    }

    private static Brush? ParseBrush(string? color)
    {
        if (string.IsNullOrWhiteSpace(color)) return null;
        try
        {
            var parsed = (Color)ColorConverter.ConvertFromString(color);
            return Frozen(parsed);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static Brush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
