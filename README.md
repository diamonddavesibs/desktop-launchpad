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

Cards live in `%AppData%\DesktopLaunchpad\cards.json`. Right-click the tray icon and
choose **Edit cards…** to open it in Notepad. Save, then reopen the launchpad to see changes.

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
| `Cards/CardViewFactory.cs` | Draws each card |
