using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DesktopLaunchpad.Themes;
using Microsoft.Win32;

namespace DesktopLaunchpad;

/// <summary>
/// Lets the user pick the launchpad's look. Every change is previewed on the
/// launchpad right away; <see cref="Result"/> holds the choice when Save is clicked.
/// </summary>
public partial class ThemeWindow : Window
{
    private static readonly string?[] Accents =
    {
        null, "#FFC83C", "#2563EB", "#38BDF8", "#7C3AED", "#DB2777", "#DC2626", "#EA580C", "#16A34A", "#E5E7EB",
    };

    private static readonly Brush Outline = Frozen(Color.FromRgb(0x3A, 0x44, 0x4F));

    private readonly Action<ThemeSettings> _preview;
    private ThemeSettings _settings;
    private bool _loading;

    public ThemeSettings? Result { get; private set; }

    public ThemeWindow(ThemeSettings current, Action<ThemeSettings> preview)
    {
        InitializeComponent();
        _preview = preview;
        _settings = current.Clone();
        Refresh();
    }

    /// <summary>Redraws every control from the current choices.</summary>
    private void Refresh()
    {
        _loading = true;
        BuildPresets();
        BuildAccents();
        BuildShapes();

        var hasImage = !string.IsNullOrWhiteSpace(_settings.BackgroundImage);
        ImageText.Text = hasImage ? Path.GetFileName(_settings.BackgroundImage) : "None";
        ImageText.ToolTip = hasImage ? _settings.BackgroundImage : null;
        ImageText.Foreground = hasImage ? Brushes.White : Frozen(Color.FromRgb(0x80, 0x90, 0x9C));
        RemoveImageButton.IsEnabled = hasImage;

        StrengthSlider.Value = Math.Clamp(_settings.BackdropStrength ?? 80, ThemeSettings.MinStrength, 100);
        StrengthText.Text = $"{StrengthSlider.Value:0}%";
        _loading = false;
    }

    private void Changed()
    {
        Refresh();
        _preview(_settings.Clone());
    }

    private void BuildPresets()
    {
        PresetPanel.Children.Clear();
        foreach (var preset in ThemePreset.All)
        {
            var selected = string.Equals(preset.Id, _settings.Theme, StringComparison.OrdinalIgnoreCase);
            var miniCard = new Border
            {
                Width = 44,
                Height = 24,
                CornerRadius = new CornerRadius(5),
                Background = Frozen(preset.Card),
                BorderBrush = Frozen(preset.CardBorder),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Left,
            };
            var label = new TextBlock
            {
                Text = preset.Name,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Frozen(preset.Text),
                Margin = new Thickness(0, 6, 0, 0),
            };
            var face = new StackPanel { Margin = new Thickness(10, 8, 10, 8) };
            face.Children.Add(miniCard);
            face.Children.Add(label);

            var tile = new Button
            {
                Width = 152,
                Height = 72,
                MinWidth = 0,
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 10, 10),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                ToolTip = preset.Name,
                Background = new LinearGradientBrush(preset.BackdropTop, preset.BackdropBottom, new Point(0, 0), new Point(1, 1)),
                BorderBrush = selected ? Brushes.White : Outline,
                BorderThickness = new Thickness(selected ? 3 : 1),
                Content = face,
            };
            tile.Click += (_, _) => { _settings.Theme = preset.Id; Changed(); };
            PresetPanel.Children.Add(tile);
        }
    }

    private void BuildAccents()
    {
        AccentPanel.Children.Clear();
        var themeAccent = ThemePreset.Find(_settings.Theme).Accent;
        foreach (var hex in Accents)
        {
            var selected = string.Equals(hex, _settings.Accent, StringComparison.OrdinalIgnoreCase);
            var color = Theme.TryParse(hex) ?? themeAccent;
            var swatch = new Button
            {
                Width = 30,
                Height = 30,
                MinWidth = 0,
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 8, 8),
                ToolTip = hex ?? "The look's own color",
                Background = Frozen(color),
                BorderBrush = selected ? Brushes.White : Outline,
                BorderThickness = new Thickness(selected ? 3 : 1),
                Content = hex is null ? "A" : null,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.Black,
            };
            swatch.Click += (_, _) => { _settings.Accent = hex; Changed(); };
            AccentPanel.Children.Add(swatch);
        }

        // Keep a custom color typed into settings.json selectable.
        if (_settings.Accent is not null && !Accents.Contains(_settings.Accent, StringComparer.OrdinalIgnoreCase)
            && Theme.TryParse(_settings.Accent) is Color custom)
        {
            AccentPanel.Children.Add(new Border
            {
                Width = 30, Height = 30, Margin = new Thickness(0, 0, 8, 8),
                Background = Frozen(custom), BorderBrush = Brushes.White, BorderThickness = new Thickness(3),
                ToolTip = _settings.Accent,
            });
        }
    }

    private void BuildShapes()
    {
        ShapePanel.Children.Clear();
        var current = ThemeSettings.Shapes.Any(s => string.Equals(s.Id, _settings.CardShape, StringComparison.OrdinalIgnoreCase))
            ? _settings.CardShape
            : ThemeSettings.Shapes[0].Id;
        foreach (var shape in ThemeSettings.Shapes)
        {
            var radio = new RadioButton
            {
                Content = shape.Label,
                GroupName = "Shape",
                IsChecked = string.Equals(shape.Id, current, StringComparison.OrdinalIgnoreCase),
            };
            var id = shape.Id;
            radio.Checked += (_, _) =>
            {
                if (_loading) return;
                _settings.CardShape = id;
                Changed();
            };
            ShapePanel.Children.Add(radio);
        }
    }

    private void ChooseImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose a background picture",
            Filter = "Pictures|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tif;*.tiff|All files|*.*",
        };
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        if (Directory.Exists(pictures)) dialog.InitialDirectory = pictures;
        if (dialog.ShowDialog(this) != true) return;

        _settings.BackgroundImage = dialog.FileName;
        // A picture is easier to enjoy with a lighter tint over it.
        if ((_settings.BackdropStrength ?? 80) > 60) _settings.BackdropStrength = 50;
        Changed();
    }

    private void RemoveImage_Click(object sender, RoutedEventArgs e)
    {
        _settings.BackgroundImage = null;
        Changed();
    }

    private void StrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading || StrengthText is null) return;
        _settings.BackdropStrength = (int)Math.Round(e.NewValue);
        StrengthText.Text = $"{e.NewValue:0}%";
        _preview(_settings.Clone());
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _settings = new ThemeSettings();
        Changed();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        Result = _settings.Clone();
        DialogResult = true;
    }

    private static Brush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
