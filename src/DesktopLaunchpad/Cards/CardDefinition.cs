using System.Text.Json.Serialization;

namespace DesktopLaunchpad.Cards;

public enum CardType
{
    /// <summary>Starts a program, shortcut, file or folder on this PC.</summary>
    Program,

    /// <summary>Opens a web address in a browser.</summary>
    Website,

    /// <summary>Opens several programs, folders and websites at once.</summary>
    Workspace,
}

/// <summary>One thing a workspace card opens.</summary>
public sealed class WorkspaceItem
{
    /// <summary>Program or Website.</summary>
    public CardType Type { get; set; } = CardType.Program;
    public string Target { get; set; } = "";
    public string? Arguments { get; set; }
    public string? Browser { get; set; }

    public WorkspaceItem Clone() => (WorkspaceItem)MemberwiseClone();
}

/// <summary>One card on the launchpad, as stored in cards.json.</summary>
public sealed class CardDefinition
{
    /// <summary>Text shown on the card.</summary>
    public string Title { get; set; } = "";

    public CardType Type { get; set; } = CardType.Program;

    /// <summary>
    /// For a program: an .exe name (notepad.exe), a full path, a shortcut (.lnk),
    /// a file or a folder. For a website: the address, such as https://github.com.
    /// </summary>
    public string Target { get; set; } = "";

    /// <summary>Workspace cards only: everything the card opens, in order.</summary>
    public List<WorkspaceItem>? Items { get; set; }

    /// <summary>Optional command-line arguments for a program card.</summary>
    public string? Arguments { get; set; }

    /// <summary>
    /// Website cards only. "default", "chrome", "edge", "firefox", "brave",
    /// "opera", "vivaldi", or the full path to a browser .exe.
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>Optional card background, such as "#2563EB".</summary>
    public string? Color { get; set; }

    /// <summary>Position and size on the launchpad. Left empty, the card is placed automatically.</summary>
    public double? X { get; set; }
    public double? Y { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }

    public CardDefinition Clone()
    {
        var copy = (CardDefinition)MemberwiseClone();
        copy.Items = Items?.Select(i => i.Clone()).ToList();
        return copy;
    }
}

public sealed class CardFile
{
    [JsonPropertyOrder(-1)]
    [JsonPropertyName("$help")]
    public string? Help { get; set; }

    public List<CardDefinition> Cards { get; set; } = new();
}
