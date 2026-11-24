; Inno Setup script for the Desktop Launchpad installer.
; Build it with installer\build-installer.ps1 (or let GitHub Actions do it).
; It installs for the current user only, so no administrator prompt is needed.

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish"
#endif

#define MyAppName "Desktop Launchpad"
#define MyAppExeName "DesktopLaunchpad.exe"

[Setup]
; Never change AppId: Windows uses it to recognise upgrades and the uninstall entry.
AppId={{B6C5BBA8-90A8-4DA6-A6BA-06CC7230C766}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher=diamonddavesibs
AppPublisherURL=https://github.com/diamonddavesibs/desktop-launchpad
VersionInfoVersion={#MyAppVersion}
; Per-user install into %LocalAppData%\Programs\Desktop Launchpad.
PrivilegesRequired=lowest
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupIconFile=..\src\DesktopLaunchpad\Assets\launchpad.ico
OutputDir=..\artifacts\installer
OutputBaseFilename=DesktopLaunchpad-Setup-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Tasks]
Name: "startup"; Description: "Start Desktop Launchpad when I sign in to Windows"; GroupDescription: "Options:"
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Options:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Same value the app's own "Start with Windows" tray option uses, pointing at the installed copy.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "DesktopLaunchpad"; ValueData: """{app}\{#MyAppExeName}"""; Tasks: startup
; Always remove it on uninstall, even if it was turned on later from the tray menu.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "DesktopLaunchpad"; Flags: uninsdeletevalue dontcreatekey

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Start Desktop Launchpad now"; Flags: nowait postinstall skipifsilent

[Code]
// The app lives in the tray with no window, so close any running copy before
// files are replaced or removed. Cards are saved as you go, so nothing is lost.
procedure StopRunningApp();
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/f /im {#MyAppExeName}', '', SW_HIDE,
    ewWaitUntilTerminated, ResultCode);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  StopRunningApp();
  Result := '';
end;

function InitializeUninstall(): Boolean;
begin
  StopRunningApp();
  Result := True;
end;
