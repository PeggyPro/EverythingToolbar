#include "InnoDependencyInstaller/CodeDependencies.iss"
#include "WixUninstaller.iss"
#include "DotNetInstaller.iss"

#define LauncherBuildDir "..\EverythingToolbar.Launcher\bin\x64\Release\net8.0-windows10.0.17763.0"
#define DeskbandBuildDir "..\EverythingToolbar.Deskband\bin\x64\Release\net8.0-windows10.0.17763.0"

#define MyAppName "EverythingToolbar"
#define MyAppPublisher "Stephan Rumswinkel"
#define MyAppURL "https://www.github.com/srwi/EverythingToolbar"
#define MyAppExeName "EverythingToolbar.Launcher.exe"
#define MyAppVersion GetVersionNumbersString(LauncherBuildDir + "\" + MyAppExeName)

[Setup]
AppId={{b5f0ac2d-98da-4392-9d12-78444db9caa9}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=output
OutputBaseFilename=EverythingToolbar-{#MyAppVersion}
SetupIconFile=..\EverythingToolbar\Images\AppIcon.ico
SolidCompression=yes
WizardStyle=modern
DisableWelcomePage=no
CloseApplications=force

[Files]
; Launcher files
Source: "{#LauncherBuildDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion uninsrestartdelete; Check: IsLauncherSelected
Source: "{#LauncherBuildDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs uninsrestartdelete; Check: IsLauncherSelected
; Deskband files (COM DLL)
Source: "{#DeskbandBuildDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs uninsrestartdelete; Check: IsDeskbandSelected

[Registry]
; Deskband COM registration entries
Root: HKCR; Subkey: "CLSID\{{9D39B79C-E03C-4757-B1B6-ECCE843748F3}"; ValueType: string; ValueName: ""; ValueData: "EverythingToolbar"; Flags: uninsdeletekey; Check: IsDeskbandSelected
Root: HKCR; Subkey: "CLSID\{{9D39B79C-E03C-4757-B1B6-ECCE843748F3}\Implemented Categories"; ValueType: string; ValueName: ""; ValueData: ""; Flags: uninsdeletekey; Check: IsDeskbandSelected
Root: HKCR; Subkey: "CLSID\{{9D39B79C-E03C-4757-B1B6-ECCE843748F3}\Implemented Categories\{{00021492-0000-0000-c000-000000000046}"; ValueType: string; ValueName: ""; ValueData: ""; Flags: uninsdeletekey; Check: IsDeskbandSelected
Root: HKCR; Subkey: "CLSID\{{9D39B79C-E03C-4757-B1B6-ECCE843748F3}\InProcServer32"; ValueType: string; ValueName: ""; ValueData: "{app}\EverythingToolbar.Deskband.comhost.dll"; Flags: uninsdeletekey; Check: IsDeskbandSelected
Root: HKCR; Subkey: "CLSID\{{9D39B79C-E03C-4757-B1B6-ECCE843748F3}\InProcServer32"; ValueType: string; ValueName: "ThreadingModel"; ValueData: "Both"; Flags: uninsdeletekey; Check: IsDeskbandSelected
Root: HKCR; Subkey: "CLSID\{{9D39B79C-E03C-4757-B1B6-ECCE843748F3}\ProgID"; ValueType: string; ValueName: ""; ValueData: "EverythingToolbar.Deskband.Server"; Flags: uninsdeletekey; Check: IsDeskbandSelected
Root: HKCR; Subkey: "EverythingToolbar.Deskband.Server"; ValueType: string; ValueName: ""; ValueData: "EverythingToolbar.Deskband.Server"; Flags: uninsdeletekey; Check: IsDeskbandSelected
Root: HKCR; Subkey: "EverythingToolbar.Deskband.Server\CLSID"; ValueType: string; ValueName: ""; ValueData: "{{9D39B79C-E03C-4757-B1B6-ECCE843748F3}"; Flags: uninsdeletekey; Check: IsDeskbandSelected

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Check: IsLauncherSelected

[UninstallDelete]
; Removing the installation directory is done in case some files were not removed by the uninstaller
Type: filesandordirs; Name: "{app}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent; Check: IsLauncherSelected

[Code]
var
  InstallTypePage: TInputOptionWizardPage;
  AdminNoticeLabel: TNewStaticText;
  SelectedInstallMode: Integer;

function IsLauncherSelected: Boolean;
begin
  Result := SelectedInstallMode = 0;
end;

function IsDeskbandSelected: Boolean;
begin
  Result := SelectedInstallMode = 1;
end;

function IsWindows11OrLater: Boolean;
begin
  Result := GetWindowsVersion >= $0A00016B;
end;

procedure KillAppIfRunning;
var
  ResultCode: Integer;
begin
  Exec('taskkill.exe', '/F /IM "{#MyAppExeName}"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

function InitializeUninstall: Boolean;
begin
  KillAppIfRunning;
  Result := True;
end;

function InitializeSetup: Boolean;
var
  modeArg: String;
begin
  // Always read /mode=launcher or /mode=deskband from CLI, default to launcher
  modeArg := LowerCase(ExpandConstant('{param:mode|launcher}'));
  if modeArg = 'launcher' then
    SelectedInstallMode := 0
  else if modeArg = 'deskband' then
    SelectedInstallMode := 1
  else
    // fallback to default logic if unknown value
    if not IsWindows11OrLater and IsAdminInstallMode then
      SelectedInstallMode := 1
    else
      SelectedInstallMode := 0;

  // Enforce admin for deskband in all modes
  if (IsDeskbandSelected) and not IsAdminInstallMode then
  begin
    MsgBox('The Deskband installation requires administrator privileges. ' +
           'Please rerun the installer as administrator or use /mode=launcher.',
           mbError, MB_OK);
    Result := False;
    exit;
  end;

  AddDotNet80DesktopDependency;
  Result := UninstallWixVersion();
end;

procedure InitializeWizard;
begin
  InstallTypePage := CreateInputOptionPage(wpWelcome,
    'Choose Installation Type', 'Select how you want to install EverythingToolbar',
    'Please specify which installation method you would like to use, then click Next.',
    True, False);
  InstallTypePage.Add(''#13#10'Launcher (Recommended for Windows 11):'#13#10'Pins EverythingToolbar as a regular taskbar icon. ' +
                      'This is the only option compatible with unmodified Windows 11 installations.'#13#10'');
  InstallTypePage.Add(''#13#10'Deskband (Requires Windows 10 or StartAllBack / ExplorerPatcher):'#13#10'' +
                      'Integrates the search bar directly into the taskbar. Only works on Windows 10 or ' +
                      'Windows 11 with third-party tools that restore deskband support.'#13#10'');

  // Set selection from CLI or fallback logic
  InstallTypePage.SelectedValueIndex := SelectedInstallMode;

  AdminNoticeLabel := TNewStaticText.Create(InstallTypePage);
  AdminNoticeLabel.Parent := InstallTypePage.Surface;
  AdminNoticeLabel.AutoSize := False;
  AdminNoticeLabel.Left := 0;
  AdminNoticeLabel.Top := InstallTypePage.Surface.Height - 100;
  AdminNoticeLabel.Width := InstallTypePage.SurfaceWidth;
  AdminNoticeLabel.Height := 80;
  AdminNoticeLabel.WordWrap := True;
  AdminNoticeLabel.Font.Style := [fsBold];
  AdminNoticeLabel.Font.Color := clRed;

  if not IsAdminInstallMode then
  begin
    InstallTypePage.CheckListBox.ItemEnabled[1] := False;
    InstallTypePage.SelectedValueIndex := 0;
    SelectedInstallMode := 0;
    AdminNoticeLabel.Caption := 'Note: The deskband option requires the installer to be run as administrator. ';
    AdminNoticeLabel.Visible := True;
  end
  else
  begin
    AdminNoticeLabel.Visible := False;
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;

  if CurPageID = InstallTypePage.ID then
  begin
    SelectedInstallMode := InstallTypePage.SelectedValueIndex;
  end;
end;
