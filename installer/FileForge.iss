; FileForge Windows Installer Script (Inno Setup 6)
; Compiles via CI (choco install innosetup) or locally with ISCC.exe

#ifndef MyAppVersion
  #define MyAppVersion "1.1.0"
#endif

#define MyAppName "FileForge"
#define MyAppPublisher "Dhayananth J (0-Name-Alex)"
#define MyAppURL "https://github.com/0-Name-Alex/FileForge"
#define MyAppExeName "FileForge.App.exe"

[Setup]
AppId={{8F93A4C2-6B7D-4E2A-9D1E-42B6E9A3F110}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
PrivilegesRequired=lowest
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir=..\dist
OutputBaseFilename=FileForge-Setup-v{#MyAppVersion}-win-x64
SetupIconFile=..\assets\app_icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "contextmenu"; Description: "Add 'Convert with FileForge' to Windows Explorer right-click menu"; GroupDescription: "Windows Integration:"
Name: "sendto"; Description: "Add FileForge to the 'Send To' menu"; GroupDescription: "Windows Integration:"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\assets\app_icon.ico"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\assets\app_icon.ico"; Tasks: desktopicon
Name: "{usersendto}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\assets\app_icon.ico"; Tasks: sendto

[Registry]
; Right-click any file -> Convert with FileForge
Root: HKCU; Subkey: "Software\Classes\*\shell\FileForge"; ValueType: string; ValueName: ""; ValueData: "Convert with FileForge"; Tasks: contextmenu; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\*\shell\FileForge"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#MyAppExeName}"",0"; Tasks: contextmenu; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\*\shell\FileForge\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: contextmenu; Flags: uninsdeletekey

; Right-click any folder -> Convert with FileForge
Root: HKCU; Subkey: "Software\Classes\Directory\shell\FileForge"; ValueType: string; ValueName: ""; ValueData: "Convert with FileForge"; Tasks: contextmenu; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Directory\shell\FileForge"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#MyAppExeName}"",0"; Tasks: contextmenu; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\Directory\shell\FileForge\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: contextmenu; Flags: uninsdeletekey

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
