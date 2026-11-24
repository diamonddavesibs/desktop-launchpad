using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DesktopLaunchpad.Cards;
using Microsoft.Win32;

namespace DesktopLaunchpad;

/// <summary>
/// Form for adding or changing a card. Works on a copy; <see cref="Result"/>
/// holds the edited card when the user clicks Save.
/// </summary>
public partial class CardEditorWindow : Window
{
    private static readonly (string Label, string? Value)[] Browsers =
    {
        ("Default browser", "default"),
        ("Google Chrome", "chrome"),
        ("Microsoft Edge", "edge"),
        ("Firefox", "firefox"),
        ("Brave", "brave"),
        ("Opera", "opera"),
        ("Vivaldi", "vivaldi"),
    };

    private const string OtherBrowserTag = "__other__";

    private static readonly string?[] Swatches =
    {
        null, "#2563EB", "#7C3AED", "#DB2777", "#DC2626", "#EA580C", "#CA8A04", "#16A34A", "#0D9488", "#1F2937",
    };

    private readonly CardDefinition _card;
    private readonly List<WorkspaceItem> _items;
    private string? _color;
    private bool _ignoreBrowserChange;

    public CardDefinition? Result { get; private set; }

    public CardEditorWindow(CardDefinition? existing)
    {
        InitializeComponent();
        Title = existing is null ? "Add card" : "Edit card";
        _card = existing?.Clone() ?? new CardDefinition { Type = CardType.Program };
        _items = _card.Items ?? new List<WorkspaceItem>();

        TitleBox.Text = _card.Title;
        TargetBox.Text = _card.Type == CardType.Workspace ? "" : _card.Target;
        ArgumentsBox.Text = _card.Arguments ?? "";
        _color = _card.Color;

        FillBrowsers(_card.Browser);
        BuildSwatches();

        RefreshItems();
        if (_card.Type == CardType.Website) WebsiteRadio.IsChecked = true;
        else if (_card.Type == CardType.Workspace) WorkspaceRadio.IsChecked = true;
        else ProgramRadio.IsChecked = true;

        Loaded += (_, _) => { TitleBox.Focus(); TitleBox.SelectAll(); };
    }

    private bool IsWebsite => WebsiteRadio.IsChecked == true;
    private bool IsWorkspace => WorkspaceRadio.IsChecked == true;

    private void Type_Changed(object sender, RoutedEventArgs e)
    {
        if (TargetLabel is null || WorkspacePanel is null) return; // still loading
        var web = IsWebsite;
        var workspace = IsWorkspace;

        TargetLabel.Text = workspace ? "Add a program, folder or website" : web ? "Web address" : "Program, file or folder";
        TargetHint.Text = workspace
            ? "Browse, or type an address or program name and press Enter. Websites open in the browser chosen below."
            : web
                ? "For example https://mail.google.com"
                : "Browse to a program or shortcut, or type a name like notepad.exe.";
        BrowseFileButton.Visibility = web ? Visibility.Collapsed : Visibility.Visible;
        BrowseFolderButton.Visibility = web ? Visibility.Collapsed : Visibility.Visible;
        ArgumentsPanel.Visibility = web || workspace ? Visibility.Collapsed : Visibility.Visible;
        BrowserPanel.Visibility = web || workspace ? Visibility.Visible : Visibility.Collapsed;
        BrowserLabel.Text = workspace ? "Open websites in" : "Open in";
        WorkspacePanel.Visibility = workspace ? Visibility.Visible : Visibility.Collapsed;
    }

    // ----- Workspace list -----

    private string SelectedBrowser => (BrowserCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "default";

    private void AddItem(string target)
    {
        target = target.Trim();
        if (target.Length == 0) return;

        var type = Launcher.GuessType(target);
        _items.Add(new WorkspaceItem
        {
            Type = type,
            Target = type == CardType.Website ? Launcher.NormalizeUrl(target) : target,
            Browser = type == CardType.Website ? SelectedBrowser : null,
        });
        TargetBox.Clear();
        ErrorText.Visibility = Visibility.Collapsed;
        RefreshItems();
        ItemsList.SelectedIndex = _items.Count - 1;
    }

    private void RefreshItems()
    {
        var selected = ItemsList.SelectedIndex;
        ItemsList.Items.Clear();
        foreach (var item in _items)
            ItemsList.Items.Add(Describe(item));
        ItemsList.SelectedIndex = Math.Min(selected, _items.Count - 1);
    }

    private static string Describe(WorkspaceItem item)
    {
        if (item.Type == CardType.Website)
        {
            var browser = string.IsNullOrWhiteSpace(item.Browser) || item.Browser == "default"
                ? "default browser"
                : Path.GetFileNameWithoutExtension(item.Browser);
            return $"Website   {item.Target}   ({browser})";
        }
        return $"Program   {item.Target}";
    }

    private void AddItem_Click(object sender, RoutedEventArgs e) => AddItem(TargetBox.Text);

    private void TargetBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (IsWorkspace && e.Key == System.Windows.Input.Key.Enter)
        {
            AddItem(TargetBox.Text);
            e.Handled = true; // don't trigger Save
        }
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e) => MoveSelected(-1);
    private void MoveDown_Click(object sender, RoutedEventArgs e) => MoveSelected(+1);

    private void MoveSelected(int offset)
    {
        var index = ItemsList.SelectedIndex;
        var target = index + offset;
        if (index < 0 || target < 0 || target >= _items.Count) return;
        (_items[index], _items[target]) = (_items[target], _items[index]);
        RefreshItems();
        ItemsList.SelectedIndex = target;
    }

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        var index = ItemsList.SelectedIndex;
        if (index < 0) return;
        _items.RemoveAt(index);
        RefreshItems();
    }

    private void BrowseFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose a program, shortcut or file",
            Filter = "Programs and shortcuts (*.exe;*.lnk;*.bat;*.cmd)|*.exe;*.lnk;*.bat;*.cmd|All files (*.*)|*.*",
            DereferenceLinks = false,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu) + @"\Programs",
        };
        if (dialog.ShowDialog(this) != true) return;

        if (IsWorkspace) { AddItem(dialog.FileName); return; }
        TargetBox.Text = dialog.FileName;
        if (string.IsNullOrWhiteSpace(TitleBox.Text))
            TitleBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
    }

    private void BrowseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Choose a folder" };
        if (dialog.ShowDialog(this) != true) return;

        if (IsWorkspace) { AddItem(dialog.FolderName); return; }
        TargetBox.Text = dialog.FolderName;
        if (string.IsNullOrWhiteSpace(TitleBox.Text))
            TitleBox.Text = Path.GetFileName(dialog.FolderName.TrimEnd('\\'));
    }

    private void FillBrowsers(string? current)
    {
        _ignoreBrowserChange = true;
        foreach (var (label, value) in Browsers)
        {
            var item = new ComboBoxItem { Content = label, Tag = value };
            BrowserCombo.Items.Add(item);
            if (string.Equals(value, current ?? "default", StringComparison.OrdinalIgnoreCase))
                BrowserCombo.SelectedItem = item;
        }

        if (BrowserCombo.SelectedItem is null && !string.IsNullOrWhiteSpace(current))
        {
            var custom = new ComboBoxItem { Content = Path.GetFileNameWithoutExtension(current), Tag = current, ToolTip = current };
            BrowserCombo.Items.Add(custom);
            BrowserCombo.SelectedItem = custom;
        }

        BrowserCombo.Items.Add(new ComboBoxItem { Content = "Other browser (.exe)…", Tag = OtherBrowserTag });
        BrowserCombo.SelectedIndex = Math.Max(0, BrowserCombo.SelectedIndex);
        _lastBrowserValue = (BrowserCombo.SelectedItem as ComboBoxItem)?.Tag as string;
        _ignoreBrowserChange = false;
    }

    private string? _lastBrowserValue;

    private void BrowserCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_ignoreBrowserChange || BrowserCombo.SelectedItem is not ComboBoxItem item) return;
        if (item.Tag as string != OtherBrowserTag)
        {
            _lastBrowserValue = item.Tag as string;
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Choose a browser",
            Filter = "Programs (*.exe)|*.exe",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        };

        _ignoreBrowserChange = true;
        if (dialog.ShowDialog(this) == true)
        {
            var custom = new ComboBoxItem
            {
                Content = Path.GetFileNameWithoutExtension(dialog.FileName),
                Tag = dialog.FileName,
                ToolTip = dialog.FileName,
            };
            BrowserCombo.Items.Insert(BrowserCombo.Items.Count - 1, custom);
            BrowserCombo.SelectedItem = custom;
            _lastBrowserValue = dialog.FileName;
        }
        else
        {
            // Put the previous choice back.
            BrowserCombo.SelectedItem = BrowserCombo.Items.OfType<ComboBoxItem>()
                .FirstOrDefault(i => string.Equals(i.Tag as string, _lastBrowserValue ?? "default", StringComparison.OrdinalIgnoreCase))
                ?? BrowserCombo.Items[0];
        }
        _ignoreBrowserChange = false;
    }

    private void BuildSwatches()
    {
        SwatchPanel.Children.Clear();
        foreach (var hex in Swatches)
        {
            var selected = string.Equals(hex, _color, StringComparison.OrdinalIgnoreCase);
            var swatch = new Button
            {
                Width = 30,
                Height = 30,
                MinWidth = 0,
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 8, 8),
                ToolTip = hex ?? "Default (follows the theme)",
                Background = CardView.ParseBrush(hex) ?? CardView.DefaultBackground,
                BorderBrush = selected ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x3A, 0x44, 0x4F)),
                BorderThickness = new Thickness(selected ? 3 : 1),
                Content = hex is null ? "∅" : null,
                Foreground = Brushes.White,
            };
            swatch.Click += (_, _) => { _color = hex; BuildSwatches(); };
            SwatchPanel.Children.Add(swatch);
        }

        // Keep a custom color from cards.json selectable.
        if (_color is not null && !Swatches.Contains(_color, StringComparer.OrdinalIgnoreCase) && CardView.ParseBrush(_color) is Brush brush)
        {
            SwatchPanel.Children.Add(new Border
            {
                Width = 30, Height = 30, Margin = new Thickness(0, 0, 8, 8),
                Background = brush, BorderBrush = Brushes.White, BorderThickness = new Thickness(3),
                ToolTip = _color,
            });
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (IsWorkspace)
        {
            SaveWorkspace();
            return;
        }

        var target = TargetBox.Text.Trim();
        if (target.Length == 0)
        {
            ShowError(IsWebsite ? "Enter a web address." : "Choose a program, file or folder.");
            TargetBox.Focus();
            return;
        }

        _card.Type = IsWebsite ? CardType.Website : CardType.Program;
        _card.Target = IsWebsite ? Launcher.NormalizeUrl(target) : target;
        _card.Title = TitleBox.Text.Trim().Length > 0 ? TitleBox.Text.Trim() : DefaultTitle(_card);
        _card.Arguments = IsWebsite || string.IsNullOrWhiteSpace(ArgumentsBox.Text) ? null : ArgumentsBox.Text.Trim();
        _card.Browser = IsWebsite ? (BrowserCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "default" : null;
        _card.Color = _color;
        _card.Items = null;

        Result = _card;
        DialogResult = true;
    }

    private void SaveWorkspace()
    {
        // Something typed but not added yet is almost certainly meant to be in the list.
        if (TargetBox.Text.Trim().Length > 0) AddItem(TargetBox.Text);

        if (_items.Count == 0)
        {
            ShowError("Add at least one program, folder or website to the workspace.");
            TargetBox.Focus();
            return;
        }

        _card.Type = CardType.Workspace;
        _card.Title = TitleBox.Text.Trim().Length > 0 ? TitleBox.Text.Trim() : "Workspace";
        _card.Target = "";
        _card.Arguments = null;
        _card.Browser = null;
        _card.Items = _items;
        _card.Color = _color;

        Result = _card;
        DialogResult = true;
    }

    private static string DefaultTitle(CardDefinition card)
    {
        if (card.Type == CardType.Website && Uri.TryCreate(card.Target, UriKind.Absolute, out var uri))
            return uri.Host.Replace("www.", "");
        return Path.GetFileNameWithoutExtension(card.Target.TrimEnd('\\', '/'));
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
