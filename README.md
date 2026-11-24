# Desktop Launchpad

A Windows app that opens a dashboard of launch cards when you press a hotkey.
Each card opens a program on your PC or a website in the browser you choose.

Built with C#, .NET 8 and WPF.

## Run it

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
    { "title": "Gmail", "type": "website", "target": "https://mail.google.com", "browser": "chrome", "color": "#2563EB" }
  ]
}
```

| Field | What it means |
| --- | --- |
| `title` | Text on the card |
| `type` | `program` or `website` |
| `target` | Programs: an .exe name, full path, shortcut, file or folder. Websites: the address |
| `arguments` | Optional, programs only: command-line arguments |
| `browser` | Optional, websites only: `default`, `chrome`, `edge`, `firefox`, `brave`, `opera`, `vivaldi`, or the full path to a browser .exe |
| `color` | Optional card color, such as `#2563EB` |
| `x`, `y`, `width`, `height` | Optional position and size. Leave them out and the card is placed automatically |

In JSON, backslashes in paths are written twice: `C:\\Program Files\\...`.

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
| `StartupManager.cs` | The Start with Windows setting |
