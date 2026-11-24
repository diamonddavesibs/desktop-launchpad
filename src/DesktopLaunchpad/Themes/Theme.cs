using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DesktopLaunchpad.Themes;

/// <summary>A built-in look: backdrop, text and card colors.</summary>
public sealed record ThemePreset(
    string Id,
    string Name,
    Color BackdropTop,
    Color BackdropBottom,
    Color Text,
    Color SubtleText,
    Color Card,
    Color CardBorder,
    Color CardHover,
    Color Accent)
{
    public static readonly IReadOnlyList<ThemePreset> All = new[]
    {
        new ThemePreset("midnight", "Midnight",
            Rgb(0x10, 0x14, 0x18), Rgb(0x10, 0x14, 0x18),
            Colors.White, Rgb(0xB0, 0xBA, 0xC5),
            Argb(0x26, 0xFF, 0xFF, 0xFF), Argb(0x40, 0xFF, 0xFF, 0xFF), Argb(0xC0, 0xFF, 0xFF, 0xFF),
            Rgb(0xFF, 0xC8, 0x3C)),

        new ThemePreset("light", "Light",
            Rgb(0xF3, 0xF4, 0xF6), Rgb(0xE5, 0xE7, 0xEB),
            Rgb(0x11, 0x18, 0x27), Rgb(0x4B, 0x55, 0x63),
            Argb(0xE6, 0xFF, 0xFF, 0xFF), Argb(0x26, 0x00, 0x00, 0x00), Argb(0x99, 0x25, 0x63, 0xEB),
            Rgb(0x25, 0x63, 0xEB)),

        new ThemePreset("ocean", "Ocean",
            Rgb(0x0B, 0x25, 0x45), Rgb(0x13, 0x4E, 0x6F),
            Colors.White, Rgb(0xB6, 0xD4, 0xEA),
            Argb(0x33, 0x7D, 0xC4, 0xFF), Argb(0x4D, 0x7D, 0xC4, 0xFF), Argb(0xCC, 0x38, 0xBD, 0xF8),
            Rgb(0x38, 0xBD, 0xF8)),

        new ThemePreset("forest", "Forest",
            Rgb(0x0B, 0x1F, 0x16), Rgb(0x1A, 0x3A, 0x2A),
            Colors.White, Rgb(0xBC, 0xD9, 0xC8),
            Argb(0x2E, 0x86, 0xEF, 0xAC), Argb(0x47, 0x86, 0xEF, 0xAC), Argb(0xCC, 0x4A, 0xDE, 0x80),
            Rgb(0x4A, 0xDE, 0x80)),

        new ThemePreset("sunset", "Sunset",
            Rgb(0x3B, 0x07, 0x64), Rgb(0x9A, 0x34, 0x12),
            Colors.White, Rgb(0xF5, 0xD0, 0xC5),
            Argb(0x2E, 0xFF, 0xE4, 0xD6), Argb(0x4D, 0xFF, 0xE4, 0xD6), Argb(0xCC, 0xFB, 0x92, 0x3C),
            Rgb(0xFB, 0x92, 0x3C)),

        new ThemePreset("graphite", "Graphite",
            Rgb(0x00, 0x00, 0x00), Rgb(0x0A, 0x0A, 0x0A),
            Rgb(0xF5, 0xF5, 0xF5), Rgb(0xA3, 0xA3, 0xA3),
            Rgb(0x1F, 0x1F, 0x1F), Rgb(0x33, 0x33, 0x33), Rgb(0xA3, 0xA3, 0xA3),
            Rgb(0xE5, 0xE5, 0xE5)),
    };

    public static ThemePreset Default => All[0];

    public static ThemePreset Find(string? id) =>
        All.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase)) ?? Default;

    private static Color Rgb(byte r, byte g, byte b) => Color.FromRgb(r, g, b);
    private static Color Argb(byte a, byte r, byte g, byte b) => Color.FromArgb(a, r, g, b);
}

/// <summary>
/// The look actually in use: a preset with the user's choices (accent, background
/// picture, backdrop strength, card shape) applied, as ready-to-use brushes.
/// </summary>
public sealed class Theme
{
    public static Theme Current { get; private set; } = From(new ThemeSettings());

    public static void SetCurrent(ThemeSettings settings) => Current = From(settings);

    public required ThemePreset Preset { get; init; }
    public required Brush Backdrop { get; init; }
    public Brush? BackgroundImage { get; init; }
    public required Brush Text { get; init; }
    public required Brush SubtleText { get; init; }
    public required Brush FaintText { get; init; }
    public required Brush Card { get; init; }
    public required Brush CardBorder { get; init; }
    public required Brush CardHover { get; init; }
    public required Brush Accent { get; init; }
    public required Brush ButtonBackground { get; init; }
    public required Brush ButtonBorder { get; init; }
    public required double CornerRadius { get; init; }

    /// <summary>True when the text is dark (a light theme).</summary>
    public bool IsLight => Luminance(Preset.Text) < 0.5;

    public static Theme From(ThemeSettings settings)
    {
        var preset = ThemePreset.Find(settings.Theme);
        var strength = Math.Clamp(settings.BackdropStrength ?? 80, ThemeSettings.MinStrength, 100) / 100.0;
        var alpha = (byte)Math.Round(255 * strength);
        var accent = TryParse(settings.Accent) ?? preset.Accent;

        var top = Color.FromArgb(alpha, preset.BackdropTop.R, preset.BackdropTop.G, preset.BackdropTop.B);
        var bottom = Color.FromArgb(alpha, preset.BackdropBottom.R, preset.BackdropBottom.G, preset.BackdropBottom.B);
        Brush backdrop = top == bottom
            ? new SolidColorBrush(top)
            : new LinearGradientBrush(top, bottom, new Point(0, 0), new Point(1, 1));
        backdrop.Freeze();

        var light = Luminance(preset.Text) < 0.5;
        return new Theme
        {
            Preset = preset,
            Backdrop = backdrop,
            BackgroundImage = LoadImage(settings.BackgroundImage),
            Text = Frozen(preset.Text),
            SubtleText = Frozen(preset.SubtleText),
            FaintText = Frozen(Color.FromArgb(0xB0, preset.SubtleText.R, preset.SubtleText.G, preset.SubtleText.B)),
            Card = Frozen(preset.Card),
            CardBorder = Frozen(preset.CardBorder),
            CardHover = settings.Accent is null ? Frozen(preset.CardHover) : Frozen(accent),
            Accent = Frozen(accent),
            ButtonBackground = Frozen(light ? Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF)),
            ButtonBorder = Frozen(light ? Color.FromArgb(0x33, 0x00, 0x00, 0x00) : Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF)),
            CornerRadius = ThemeSettings.CornerFor(settings.CardShape),
        };
    }

    /// <summary>Text that stays readable on a card with its own color.</summary>
    public (Brush Text, Brush Subtle) TextOn(Brush? cardColor)
    {
        if (cardColor is not SolidColorBrush solid) return (Text, SubtleText);
        // A see-through custom color mostly shows the backdrop, so judge it by that.
        if (solid.Color.A < 0x80) return (Text, SubtleText);
        return Luminance(solid.Color) > 0.6
            ? (Frozen(Color.FromRgb(0x11, 0x18, 0x27)), Frozen(Color.FromRgb(0x37, 0x41, 0x51)))
            : (Brushes.White, Frozen(Color.FromRgb(0xE2, 0xE8, 0xF0)));
    }

    private static double Luminance(Color c) => (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255.0;

    private static Brush? LoadImage(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        try
        {
            var full = Environment.ExpandEnvironmentVariables(path);
            if (!File.Exists(full)) return null;
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad; // don't keep the file locked
            bitmap.UriSource = new Uri(full, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            var brush = new ImageBrush(bitmap) { Stretch = Stretch.UniformToFill };
            brush.Freeze();
            return brush;
        }
        catch (Exception)
        {
            return null; // a broken picture just means no picture
        }
    }

    public static Color? TryParse(string? color)
    {
        if (string.IsNullOrWhiteSpace(color)) return null;
        try { return (Color)ColorConverter.ConvertFromString(color); }
        catch (Exception) { return null; }
    }

    private static Brush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
