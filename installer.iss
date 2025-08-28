#define MyAppName "ScriptPilot"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "JabrailShahbazov"
#define MyAppExeName "SqlBatchRunner.Win.exe"
#define MyPublishDir "bin\Release\net9.0-windows\win-x64\publish"
#define MyAppId "{{3B4F2C1A-3C3A-4C9C-9F1B-A1A1A1A1A1A}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={pf32}\{#MyAppName}
DefaultGroupName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputBaseFilename={#MyAppName}_Setup_{#MyAppVersion}
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64

[Dirs]
Name: "{commonappdata}\ScriptPilot"; Flags: uninsneveruninstall

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Flags: nowait postinstall skipifsilent
