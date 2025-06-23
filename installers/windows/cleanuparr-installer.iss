#define MyAppName "Cleanuparr"
#define MyAppVersion GetEnv("APP_VERSION")
#define MyAppPublisher "Cleanuparr Team"
#define MyAppURL "https://github.com/Cleanuparr/Cleanuparr"
#define MyAppExeName "Cleanuparr.exe"
#define MyServiceName "Cleanuparr"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
AppId={{E8B2C9D4-6F87-4E42-B5C3-29E121D4BDFF}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=LICENSE
OutputDir=.\installer
OutputBaseFilename=Cleanuparr_Setup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
DisableDirPage=no
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupIconFile=Logo\favicon.ico
WizardStyle=modern
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 0,6.1
Name: "installservice"; Description: "Install as Windows Service (Recommended)"; GroupDescription: "Service Installation"; Flags: checkedonce

[Files]
Source: "dist\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Application icon
Source: "Logo\favicon.ico"; DestDir: "{app}"; Flags: ignoreversion
; Create sample configuration
Source: "config\cleanuparr.json"; DestDir: "{app}\config"; Flags: ignoreversion; AfterInstall: CreateConfigDirs

[Dirs]
Name: "{app}\config"; Permissions: everyone-full

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\favicon.ico"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\favicon.ico"; Tasks: desktopicon

[Run]
; Stop any existing service first
Filename: "{sys}\sc.exe"; Parameters: "stop ""{#MyServiceName}"""; Flags: runhidden; Check: ServiceExists('{#MyServiceName}')
; Wait for service to stop
Filename: "{sys}\timeout.exe"; Parameters: "/t 3"; Flags: runhidden; Check: ServiceExists('{#MyServiceName}')
; Remove existing service
Filename: "{sys}\sc.exe"; Parameters: "delete ""{#MyServiceName}"""; Flags: runhidden; Check: ServiceExists('{#MyServiceName}')
; Install as service
Filename: "{sys}\sc.exe"; Parameters: "create ""{#MyServiceName}"" binPath= ""\""{app}\{#MyAppExeName}\"""" DisplayName= ""{#MyAppName}"" start= auto"; Tasks: installservice; Flags: runhidden
; Configure service description
Filename: "{sys}\sc.exe"; Parameters: "description ""{#MyServiceName}"" ""Cleanuparr download management service"""; Tasks: installservice; Flags: runhidden
; Start the service
Filename: "{sys}\sc.exe"; Parameters: "start ""{#MyServiceName}"""; Tasks: installservice; Flags: runhidden
; Open web interface
Filename: "http://localhost:11011"; Description: "Open Cleanuparr Web Interface"; Flags: postinstall shellexec nowait
; Run directly (if not installed as service)
Filename: "{app}\{#MyAppExeName}"; Description: "Run {#MyAppName} Application"; Flags: nowait postinstall skipifsilent; Check: not IsTaskSelected('installservice')

[UninstallRun]
; Stop the service first
Filename: "{sys}\sc.exe"; Parameters: "stop ""{#MyServiceName}"""; Flags: runhidden; Check: ServiceExists('{#MyServiceName}')
; Wait for service to stop
Filename: "{sys}\timeout.exe"; Parameters: "/t 3"; Flags: runhidden; Check: ServiceExists('{#MyServiceName}')
; Remove the service
Filename: "{sys}\sc.exe"; Parameters: "delete ""{#MyServiceName}"""; Flags: runhidden; Check: ServiceExists('{#MyServiceName}')

[Code]
procedure CreateConfigDirs;
begin
  // Create necessary directories with proper permissions
  ForceDirectories(ExpandConstant('{app}\config'));
end;

function ServiceExists(ServiceName: string): Boolean;
var
  ResultCode: Integer;
begin
  // Check if service exists by trying to query it
  Result := Exec(ExpandConstant('{sys}\sc.exe'), 'query "' + ServiceName + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

// Check for running processes before install
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  // Try to stop the service if it's running
  if ServiceExists('{#MyServiceName}') then
  begin
    Exec(ExpandConstant('{sys}\sc.exe'), 'stop "{#MyServiceName}"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(3000);
  end;
  Result := True;
end;

// Handle cleanup before uninstall
function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  // Stop the service before uninstalling
  if ServiceExists('{#MyServiceName}') then
  begin
    Exec(ExpandConstant('{sys}\sc.exe'), 'stop "{#MyServiceName}"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(3000);
  end;
  Result := True;
end; 