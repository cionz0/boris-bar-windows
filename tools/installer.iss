#ifndef MyAppVersion
  #define MyAppVersion "0.1.0"
#endif
#ifndef Rid
  #define Rid "win-x64"
#endif

#define MyAppName "Boris Bar"
#define MyAppPublisher "Vincenzo Roselli"
#define MyAppURL "https://github.com/cionz0/boris-bar-windows"
#define MyAppExeName "BorisBar.exe"

[Setup]
AppId={{B0B15BA4-0C10-4A11-9E22-C10F20B0B15B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={localappdata}\Programs\BorisBar
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\publish
OutputBaseFilename=BorisBar-{#MyAppVersion}-{#Rid}-setup
SetupIconFile=..\assets\fish.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
LicenseFile=..\LICENSE
InfoBeforeFile=..\DISCLAIMER.txt
CloseApplications=yes
#if Pos("arm64", Rid) > 0
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#else
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#endif

[Languages]
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startup"; Description: "Avvia Boris Bar all'accesso di Windows"; Flags: unchecked

[Files]
Source: "..\publish\{#Rid}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "..\tools\import-audio-from-dmg.ps1"; DestDir: "{app}\tools"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "BorisBar"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Avvia Boris Bar"; Flags: nowait postinstall skipifsilent
