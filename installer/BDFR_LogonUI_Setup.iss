#define MyAppName "BDFR LogonUI"
#define MyAppVersion "0.9-preview"
#define MyAppPublisher "BDFR"
#define MyAppExeName "BDFR.LogonUI.Demo.exe"

[Setup]
AppId={{8C459877-5D36-4FDF-A4B1-9DA9BC4EE370}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\BDFR\LogonUI
DefaultGroupName=BDFR LogonUI
DisableProgramGroupPage=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=BDFR_LogonUI_Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UsePreviousAppDir=yes
CloseApplications=yes
CloseApplicationsFilter=BDFR.LogonUI.Demo.exe,BDFR.LogonUI.Broker.exe
RestartApplications=no
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupLogging=yes
ChangesAssociations=no
CreateAppDir=yes
DirExistsWarning=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\artifacts\BDFR_LogonUI_Demo\BDFR.LogonUI.Demo.exe"; DestDir: "{app}"; Flags: ignoreversion restartreplace
Source: "..\artifacts\BDFR_LogonUI_Demo\BDFR.LogonUI.Broker.exe"; DestDir: "{app}"; Flags: ignoreversion restartreplace
Source: "..\artifacts\BDFR_LogonUI_Demo\organization-message.txt"; DestDir: "{app}"; Flags: ignoreversion onlyifdoesntexist
Source: "..\artifacts\BDFR_LogonUI_Demo\wallpaper\*"; DestDir: "{app}\wallpaper"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\BDFR LogonUI"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\BDFR LogonUI"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch BDFR LogonUI"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\wallpaper"

[Code]
const
  BackupRootName = 'BDFR\LogonUI\InstallerBackup';

procedure BackupExistingFile(const FileName: string);
var
  SourcePath: string;
  BackupRoot: string;
  BackupPath: string;
begin
  SourcePath := ExpandConstant('{app}\') + FileName;
  if not FileExists(SourcePath) then
    exit;

  BackupRoot := ExpandConstant('{commonappdata}\') + BackupRootName;
  ForceDirectories(BackupRoot);

  BackupPath := BackupRoot + '\' + FileName + '.previous';
  FileCopy(SourcePath, BackupPath, False);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  BackupExistingFile('BDFR.LogonUI.Demo.exe');
  BackupExistingFile('BDFR.LogonUI.Broker.exe');
  Result := '';
end;
