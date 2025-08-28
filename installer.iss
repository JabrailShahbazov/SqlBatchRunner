; =========================
; ScriptPilot Installer
; =========================

#define AuthorName "Jabrayil Shahbazov"
#define MyAppName "ScriptPilot"
#define MyAppVersion "1.0.0"
#define MyAppPublisher AuthorName
#define MyAppExeName "SqlBatchRunner.Win.exe"
#define MyPublishDir "bin\Release\net9.0-windows\win-x64\publish"
#define MyAppId "{{3B4F2C1A-3C3A-4C9C-9F1B-A1A1A1A1A1A}}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppCopyright=2025 {#AuthorName}
VersionInfoCompany={#AuthorName}
DefaultDirName={pf32}\{#MyAppName}
DefaultGroupName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputBaseFilename={#MyAppName}_Setup_{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
WizardStyle=modern
DisableDirPage=no
DisableProgramGroupPage=no

#ifexist "installer-assets\branding\scriptpilot.ico"
SetupIconFile=installer-assets\branding\scriptpilot.ico
#endif

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: checkedonce

[Dirs]
Name: "{commonappdata}\ScriptPilot"; Flags: uninsneveruninstall

[Files]
; App files
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion
; Initial config by chosen APP language (only if not exists)
Source: "installer-assets\config\appsettings.default.az.json"; DestDir: "{commonappdata}\ScriptPilot"; DestName: "appsettings.json"; Flags: onlyifdoesntexist ignoreversion uninsneveruninstall; Check: IsAz
Source: "installer-assets\config\appsettings.default.en.json"; DestDir: "{commonappdata}\ScriptPilot"; DestName: "appsettings.json"; Flags: onlyifdoesntexist ignoreversion uninsneveruninstall; Check: not IsAz

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Flags: nowait postinstall skipifsilent

[Code]
var
  LangPage: TWizardPage;
  RbAz, RbEn: TRadioButton;
  SignatureLbl: TNewStaticText;

procedure LayoutSignature;
var
  MaxRight: Integer;
begin
  SignatureLbl.AutoSize := True;

  { Alt panel: Next/Cancel düymələri səviyyəsində, solda }
  SignatureLbl.Top :=
    WizardForm.NextButton.Top + (WizardForm.NextButton.Height - SignatureLbl.Height) div 2;
  SignatureLbl.Left := ScaleX(10);

  { Çox uzundursa, düymələrə girməsin }
  MaxRight := WizardForm.NextButton.Left - ScaleX(12);
  if SignatureLbl.Left + SignatureLbl.Width > MaxRight then
  begin
    SignatureLbl.AutoSize := False;
    SignatureLbl.Width := MaxRight - SignatureLbl.Left;
  end;
end;

procedure CreateSignature;
begin
  SignatureLbl := TNewStaticText.Create(WizardForm);
  SignatureLbl.Parent := WizardForm;
  SignatureLbl.Caption := '-- Jabrayil Shahbazov --';
  SignatureLbl.Font.Name := 'Segoe UI';
  SignatureLbl.Font.Style := [fsItalic];
  SignatureLbl.Font.Color := clGray;
  SignatureLbl.ParentFont := False;
  SignatureLbl.Anchors := [akLeft, akBottom];

  LayoutSignature;
end;

procedure InitializeWizard;
begin
  { App dil seçimi (installer UI dili deyil) }
  LangPage := CreateCustomPage(wpSelectDir, 'Application Language', 'Choose the language for ScriptPilot');

  RbAz := TRadioButton.Create(LangPage.Surface);
  RbAz.Parent := LangPage.Surface;
  RbAz.Left := ScaleX(0);
  RbAz.Top := ScaleY(8);
  RbAz.Width := ScaleX(320);
  RbAz.Caption := 'Azerbaijan (default)';
  RbAz.Checked := True;

  RbEn := TRadioButton.Create(LangPage.Surface);
  RbEn.Parent := LangPage.Surface;
  RbEn.Left := ScaleX(0);
  RbEn.Top := RbAz.Top + ScaleY(24);
  RbEn.Width := ScaleX(320);
  RbEn.Caption := 'English';

  CreateSignature;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if Assigned(SignatureLbl) then
    LayoutSignature;
end;

function IsAz: Boolean;
begin
  Result := RbAz.Checked;
end;

