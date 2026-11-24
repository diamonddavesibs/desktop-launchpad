using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace DesktopLaunchpad.Cards;

/// <summary>Opens what a card points to.</summary>
public static class Launcher
{
    private static readonly Dictionary<string, string> KnownBrowsers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["chrome"] = "chrome.exe",
        ["edge"] = "msedge.exe",
        ["firefox"] = "firefox.exe",
        ["brave"] = "brave.exe",
        ["opera"] = "opera.exe",
        ["vivaldi"] = "vivaldi.exe",
    };

    /// <summary>Starts the card's target. Throws <see cref="LaunchException"/> with a readable message on failure.</summary>
    public static void Launch(CardDefinition card)
    {
        if (card.Type == CardType.Workspace)
        {
            LaunchWorkspace(card);
            return;
        }

        if (string.IsNullOrWhiteSpace(card.Target))
            throw new LaunchException($"The card \"{card.Title}\" has no target set in cards.json.");

        try
        {
            if (card.Type == CardType.Website) OpenWebsite(card);
            else StartProgram(card);
        }
        catch (LaunchException) { throw; }
        catch (Exception ex)
        {
            throw new LaunchException($"Couldn't open \"{card.Title}\" ({card.Target}): {ex.Message}");
        }
    }

    /// <summary>Opens every item, then reports all failures together.</summary>
    private static void LaunchWorkspace(CardDefinition card)
    {
        var items = card.Items ?? new List<WorkspaceItem>();
        if (items.Count == 0)
            throw new LaunchException($"The workspace \"{card.Title}\" is empty. Click Edit on the launchpad to add things to it.");

        var failures = new List<string>();
        foreach (var item in items)
        {
            try
            {
                Launch(new CardDefinition
                {
                    Title = item.Target,
                    Type = item.Type == CardType.Website ? CardType.Website : CardType.Program,
                    Target = item.Target,
                    Arguments = item.Arguments,
                    Browser = item.Browser,
                });
            }
            catch (LaunchException ex)
            {
                failures.Add(ex.Message);
            }
        }

        if (failures.Count > 0)
            throw new LaunchException($"Some items in \"{card.Title}\" didn't open:\n\n" + string.Join("\n\n", failures));
    }

    /// <summary>
    /// Guesses whether something typed is a website or a program/file/folder.
    /// Paths, existing files and .exe/.lnk names are programs; addresses are websites.
    /// </summary>
    public static CardType GuessType(string target)
    {
        var t = target.Trim();
        if (t.Contains("://", StringComparison.Ordinal) || t.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            return CardType.Website;
        if (Path.IsPathRooted(t) || t.Contains('\\') || t.Contains('%') || File.Exists(t) || Directory.Exists(t))
            return CardType.Program;
        var ext = Path.GetExtension(t).ToLowerInvariant();
        if (ext is ".exe" or ".lnk" or ".bat" or ".cmd" or ".msc" or ".url")
            return CardType.Program;
        return t.Contains('.') && !t.Contains(' ') ? CardType.Website : CardType.Program;
    }

    private static void StartProgram(CardDefinition card)
    {
        var target = Environment.ExpandEnvironmentVariables(card.Target);
        var info = new ProcessStartInfo(target)
        {
            // Lets Windows handle .exe names on the PATH, shortcuts, files and folders.
            UseShellExecute = true,
            Arguments = card.Arguments ?? "",
        };

        var resolved = ResolveExecutable(target);
        if (resolved is not null)
            info.WorkingDirectory = Path.GetDirectoryName(resolved) ?? "";

        Process.Start(info);
    }

    private static void OpenWebsite(CardDefinition card)
    {
        var url = NormalizeUrl(card.Target);
        var browser = card.Browser?.Trim();

        if (string.IsNullOrEmpty(browser) || browser.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            return;
        }

        var exe = KnownBrowsers.TryGetValue(browser, out var exeName)
            ? ResolveExecutable(exeName)
            : ResolveExecutable(Environment.ExpandEnvironmentVariables(browser));

        if (exe is null)
            throw new LaunchException(
                $"Couldn't find the browser \"{browser}\" for the card \"{card.Title}\". " +
                "Check that it's installed, or set browser to \"default\" or the full path to its .exe in cards.json.");

        Process.Start(new ProcessStartInfo(exe) { UseShellExecute = false, ArgumentList = { url } });
    }

    public static string NormalizeUrl(string target)
    {
        var url = target.Trim();
        return url.Contains("://", StringComparison.Ordinal) ? url : "https://" + url;
    }

    /// <summary>
    /// Finds the full path of an executable: an existing full path, a name
    /// registered under App Paths (how Windows finds chrome.exe), or a name on the PATH.
    /// </summary>
    public static string? ResolveExecutable(string nameOrPath)
    {
        if (string.IsNullOrWhiteSpace(nameOrPath)) return null;
        if (Path.IsPathRooted(nameOrPath)) return File.Exists(nameOrPath) ? nameOrPath : null;

        var fileName = Path.HasExtension(nameOrPath) ? nameOrPath : nameOrPath + ".exe";

        foreach (var root in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            using var key = root.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{fileName}");
            if (key?.GetValue(null) is string registered)
            {
                var path = Environment.ExpandEnvironmentVariables(registered.Trim('"'));
                if (File.Exists(path)) return path;
            }
        }

        var searchDirs = new List<string> { Environment.SystemDirectory, Environment.GetFolderPath(Environment.SpecialFolder.Windows) };
        searchDirs.AddRange((Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries));
        foreach (var dir in searchDirs)
        {
            try
            {
                var candidate = Path.Combine(dir.Trim(), fileName);
                if (File.Exists(candidate)) return candidate;
            }
            catch (ArgumentException) { /* malformed PATH entry */ }
        }
        return null;
    }
}

public sealed class LaunchException(string message) : Exception(message);
