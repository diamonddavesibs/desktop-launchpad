using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DesktopLaunchpad.Cards;

/// <summary>Gets the Windows icon for a program card, cached by path.</summary>
public static class CardIcons
{
    private static readonly Dictionary<string, ImageSource?> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static ImageSource? ForProgram(string target)
    {
        var expanded = Environment.ExpandEnvironmentVariables(target);
        if (Cache.TryGetValue(expanded, out var cached)) return cached;

        ImageSource? image = null;
        try
        {
            var path = Launcher.ResolveExecutable(expanded) ?? (File.Exists(expanded) ? expanded : null);
            if (path is not null)
            {
                using var icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
                if (icon is not null)
                {
                    image = Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    image.Freeze();
                }
            }
        }
        catch
        {
            // No icon is fine; the card shows a fallback glyph.
        }

        Cache[expanded] = image;
        return image;
    }
}
