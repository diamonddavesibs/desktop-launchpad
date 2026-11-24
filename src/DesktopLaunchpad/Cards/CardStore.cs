using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DesktopLaunchpad.Cards;

/// <summary>
/// Reads and writes the card list at %AppData%\DesktopLaunchpad\cards.json.
/// A starter file is created the first time the app runs.
/// </summary>
public static class CardStore
{
    public static string FolderPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopLaunchpad");

    public static string FilePath { get; } = Path.Combine(FolderPath, "cards.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>
    /// Loads the cards. Throws <see cref="CardFileException"/> with a readable
    /// message if the file can't be parsed.
    /// </summary>
    public static IReadOnlyList<CardDefinition> Load()
    {
        EnsureFileExists();
        try
        {
            var json = File.ReadAllText(FilePath);
            var file = JsonSerializer.Deserialize<CardFile>(json, Options);
            return file?.Cards ?? new List<CardDefinition>();
        }
        catch (JsonException ex)
        {
            var where = ex.LineNumber is long line ? $" near line {line + 1}" : "";
            throw new CardFileException($"cards.json has a mistake{where}: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new CardFileException($"Couldn't read cards.json: {ex.Message}", ex);
        }
    }

    public static void EnsureFileExists()
    {
        if (File.Exists(FilePath)) return;
        Directory.CreateDirectory(FolderPath);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(StarterCards(), Options));
    }

    private static CardFile StarterCards() => new()
    {
        Help = "Each card needs a title, a type (program or website) and a target. " +
               "Website cards can set browser to default, chrome, edge, firefox, brave, opera, vivaldi or a path to a browser .exe. " +
               "Optional: arguments (programs), color such as #2563EB. Save this file, then reopen the launchpad.",
        Cards =
        {
            new() { Title = "File Explorer", Type = CardType.Program, Target = "explorer.exe" },
            new() { Title = "Notepad", Type = CardType.Program, Target = "notepad.exe" },
            new() { Title = "Calculator", Type = CardType.Program, Target = "calc.exe" },
            new() { Title = "Google", Type = CardType.Website, Target = "https://www.google.com", Browser = "default" },
            new() { Title = "YouTube", Type = CardType.Website, Target = "https://www.youtube.com", Browser = "default", Color = "#B91C1C" },
            new() { Title = "GitHub", Type = CardType.Website, Target = "https://github.com", Browser = "edge", Color = "#1F2937" },
        },
    };
}

public sealed class CardFileException(string message, Exception inner) : Exception(message, inner);
