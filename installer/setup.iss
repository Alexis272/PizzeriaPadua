; PizzeriaPadua - Inno Setup Installer
; Build with: ISCC.exe setup.iss

#define MyAppName "PizzeriaPadua"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "PizzeriaPadua"
#define MyAppURL "http://localhost:5000/PizzeriaPadua/"
#define MyAppExeName "PizzeriaPadua.API.exe"

[Setup]
AppId={{C9A5B4E3-6D8F-4B2C-8E7A-3F9D4B1C2E5A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=.
OutputBaseFilename=PizzeriaPadua_Installer
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
CloseApplications=no
DisableReadyPage=no
DisableFinishedPage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"; Flags: checkedonce
Name: "startup"; Description: "Start PizzeriaPadua automatically when &Windows starts"; GroupDescription: "Startup options:"; Flags: checkedonce

[Files]
Source: "..\PizzeriaPadua.API\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\PizzeriaPadua.API\publish\*.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "PizzeriaPadua"; ValueData: "{app}\{#MyAppExeName}"; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Start PizzeriaPadua"; Flags: nowait postinstall skipifsilent shellexec
Filename: "{#MyAppURL}"; Description: "Open PizzeriaPadua in your browser"; Flags: nowait postinstall skipifsilent shellexec

[UninstallRun]
Filename: "taskkill"; Parameters: "/f /im PizzeriaPadua.API.exe"; Flags: runhidden

[Code]
function InitializeSetup: Boolean;
begin
  Result := True;
end;
