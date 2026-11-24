# Desktop Launchpad

A Windows app that opens a dashboard of launch cards when you press a hotkey.
Each card opens a program on your PC or a website in the browser you choose.

Built with C#, .NET 8 and WPF.

## Install it

1. Open the [Releases page](https://github.com/diamonddavesibs/desktop-launchpad/releases) and download
   `DesktopLaunchpad-Setup-<version>.exe` from the newest release.
2. Run it. Windows SmartScreen may say it "protected your PC" because the installer isn't signed:
   click **More info**, then **Run anyway**.
3. Click through the setup. It installs just for you (no administrator prompt) into
   `%LocalAppData%\Programs\Desktop Launchpad`, adds a Start menu shortcut, and can start the
   launchpad when you sign in.

To update, run the newer installer over the top. To remove it, use **Settings > Apps > Installed apps >
Desktop Launchpad > Uninstall**. Your cards in `%AppData%\DesktopLaunchpad` are kept either way.

### Publishing a new release

1. On GitHub, open the **Actions** tab and pick **Release** on the left.
2. Click **Run workflow**, type a version number such as `1.0.1` (bigger than the last one), and click
   **Run workflow**.
3. After a few minutes the new installer appears on the Releases page.

Every pull request also builds an installer you can try first: open the PR's **Checks** tab, click
**Build**, then **Summary**, and download **DesktopLaunchpad-Setup** at the bottom.

To build the installer on your own PC, install [Inno Setup 6](https://jrsoftware.org/isdl.php) and run
`powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1` from the repo folder.
The installer lands in `artifacts\installer`.

## Run it from the code

1. Install [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) with the
   **.NET desktop development** workload.
2. Clone this repo (in Visual Studio: **Clone a repository**, then paste the repo URL).
3. Open `DesktopLaunchpad.sln` and press **F5**.

The app starts in the background. Look for its icon in the system tray, near the clock.

- **Ctrl+Space** opens or closes the launchpad.
- **Esc**, or a click on empty space, closes it.
- Click a card to open it.
- Right-click the tray icon and choose **Exit** to quit.

## Your cards

Open the launchpad and click **Edit** in the top-right corner:

- **+ Add card** opens a form. Pick a program, file or folder with **File…** or **Folder…**,
  or type a web address and choose which browser opens it.
- Choose **Several at once** in the form to make a workspace card: one click opens a whole
  list of programs, folders and websites, for example everything you need to start work.
- Drag a card to move it. Drag the yellow dot in its corner to resize it.
- Click a card (or its pencil) to change it, or the bin to delete it.
- Click **Done** or press **Esc** when finished. Changes are saved as you go.

Right-click the tray icon and tick **Start with Windows** to have the launchpad ready after every restart.

### The cards file

Cards are saved in `%AppData%\DesktopLaunchpad\cards.json` (the previous version is kept as
`cards.json.bak`). You can also edit it by hand: right-click the tray icon and choose
**Edit cards file (advanced)…**, save, then reopen the launchpad.

```json
{
  "cards": [
    { "title": "Notepad", "type": "program", "target": "notepad.exe" },
    { "title": "My project", "type": "program", "target": "C:\\Users\\Me\\Documents\\Project" },
    { "title": "Gmail", "type": "website", "target": "https://mail.google.com", "browser": "chrome", "color": "#2563EB" },
    { "title": "Start work", "type": "workspace", "items": [
        { "type": "website", "target": "https://mail.google.com", "browser": "chrome" },
        { "type": "website", "target": "https://calendar.google.com", "browser": "chrome" },
        { "type": "program", "target": "C:\\Users\\Me\\Documents\\Project" }
    ] }
  ]
}
```

| Field | What it means |
| --- | --- |
| `title` | Text on the card |
| `type` | `program`, `website` or `workspace` |
| `target` | Programs: an .exe name, full path, shortcut, file or folder. Websites: the address |
| `arguments` | Optional, programs only: command-line arguments |
| `browser` | Optional, websites only: `default`, `chrome`, `edge`, `firefox`, `brave`, `opera`, `vivaldi`, or the full path to a browser .exe |
| `color` | Optional card color, such as `#2563EB` |
| `items` | Workspaces only: a list of things to open, each with its own `type` (`program` or `website`), `target`, and optional `arguments` or `browser` |
| `x`, `y`, `width`, `height` | Optional position and size. Leave them out and the card is placed automatically |

In JSON, backslashes in paths are written twice: `C:\\Program Files\\...`.

## Themes

Click **Theme** at the top of the launchpad to change its look. You can pick one of six looks
(Midnight, Light, Ocean, Forest, Sunset, Graphite), a highlight color, a background picture,
how solid the backdrop is, and the card shape. Changes show on the launchpad as you pick them;
Save keeps them and Cancel puts the old look back. Cards you gave their own color keep it, and
their text switches between light and dark so it stays readable.

The choice is saved in `%AppData%\DesktopLaunchpad\settings.json`.

## Project layout

| File | What it does |
| --- | --- |
| `App.xaml.cs` | Starts the tray icon and hotkey, makes sure only one copy runs |
| `HotkeyManager.cs` | Registers the system-wide Ctrl+Space hotkey |
| `TrayIcon.cs` | The notification-area icon and its menu |
| `LaunchpadWindow.xaml` | The full-screen dashboard that shows the cards |
| `Cards/CardStore.cs` | Reads `cards.json` and writes the starter cards on first run |
| `Cards/Launcher.cs` | Opens programs and websites, finds installed browsers |
| `Cards/CardView.cs` | Draws each card and handles click, drag and resize |
| `CardEditorWindow.xaml` | The form for adding and changing a card |
| `ThemeWindow.xaml` | The Theme picker |
| `Themes/Theme.cs` | The built-in looks, and turns the chosen look into colors |
| `Themes/ThemeSettings.cs` | Reads and writes the chosen look in `settings.json` |
| `StartupManager.cs` | The Start with Windows setting |
| `installer/DesktopLaunchpad.iss` | The Inno Setup script that makes the installer |
| `installer/build-installer.ps1` | Publishes the app and builds the installer |
| `.github/workflows/release.yml` | Builds the installer and publishes it as a GitHub release |
