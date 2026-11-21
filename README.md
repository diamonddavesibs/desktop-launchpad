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
- Right-click the tray icon and choose **Exit** to quit.

## Project layout

| File | What it does |
| --- | --- |
| `App.xaml.cs` | Starts the tray icon and hotkey, makes sure only one copy runs |
| `HotkeyManager.cs` | Registers the system-wide Ctrl+Space hotkey |
| `TrayIcon.cs` | The notification-area icon and its menu |
| `LaunchpadWindow.xaml` | The full-screen dashboard where cards will live |
