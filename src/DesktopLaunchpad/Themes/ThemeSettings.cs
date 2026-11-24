using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using DesktopLaunchpad.Cards;

namespace DesktopLaunchpad.Themes;

/// <summary>The look the user picked, stored in %AppData%\DesktopLaunchpad\settings.json.</summary>
public sealed class ThemeSettings
{
    public const int MinStrength = 30;

    /// <summary>A preset id: midnight, light, ocean, forest, sunset or graphite.</summary>
    public string Theme { get; set; } = ThemePreset.Default.Id;

    /// <summary>Optional highlight color, such as "#22C55E". Empty uses the theme's own.</summary>
    public string? Accent { get; set; }

    /// <summary>Optional picture shown behind the cards.</summary>
    public string? BackgroundImage { get; set; }

    /// <summary>How solid the backdrop is, 30 to 100. Lower lets more of the desktop or picture show through.</summary>
    public int? BackdropStrength { get; set; }

    /// <summary>rounded, square or round.</summary>
    public string CardShape { get; set; } = "rounded";

    public static readonly (string Id, string Label, double Radius)[] Shapes =
    {
        ("rounded", "Rounded", 14),
        ("square", "Square", 3),
        ("round", "Extra round", 26),
    };

    public static double CornerFor(string? shape)
    {
        foreach (var s in Shapes)
            if (string.Equals(s.Id, shape, StringComparison.OrdinalIgnoreCase)) return s.Radius;
        return Shapes[0].Radius;
    }

    public ThemeSettings Clone() => (ThemeSettings)MemberwiseClone();
}

public static class SettingsStore
{
    public static string FilePath { get; } = Path.Combine(CardStore.FolderPath, "settings.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Loads the saved look. A missing or broken file just gives the default look.</summary>
    public static ThemeSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new ThemeSettings();
            return JsonSerializer.Deserialize<ThemeSettings>(File.ReadAllText(FilePath), Options) ?? new ThemeSettings();
        }
        catch (Exception)
        {
            return new ThemeSettings();
        }
    }

    public static void Save(ThemeSettings settings)
    {
        Directory.CreateDirectory(CardStore.FolderPath);
        var temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, Options));
        File.Move(temp, FilePath, overwrite: true);
    }
}
