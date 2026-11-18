#define AppName "Recycle Bin Studio"
#define AppExe "RecycleBinStudio.exe"
#define AppUrl "https://github.com/Kaev-1n/recycle-bin-studio"
#define AppIcon "..\src\RecycleBinStudio\Assets\AppIcon.ico"

#if Architecture == "arm64"
  #define AllowedArchitecture "arm64"
#else
  #define AllowedArchitecture "x64os"
#endif

[Setup]
AppId=Kaev-1n.RecycleBinStudio
AppName={#AppName}
AppVersion={#BuildVersion}
AppPublisher=Kaev-1n
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases/latest
DefaultDirName={localappdata}\Programs\Recycle Bin Studio
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
MinVersion=10.0.14393
ArchitecturesAllowed={#AllowedArchitecture}
ArchitecturesInstallIn64BitMode={#AllowedArchitecture}
OutputDir={#BuildOutput}
OutputBaseFilename=RecycleBinStudio-Setup-win-{#Architecture}
SetupIconFile={#AppIcon}
UninstallDisplayIcon={app}\{#AppExe}
WizardStyle=modern
DisableWelcomePage=no
ShowLanguageDialog=auto
SolidCompression=yes
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[CustomMessages]
english.LaunchStudio=Open Recycle Bin Studio
russian.LaunchStudio=Открыть Recycle Bin Studio

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#Payload}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; WorkingDir: "{app}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\RecycleBinStudio"; ValueType: string; ValueName: "InstallLocation"; ValueData: "{app}"; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchStudio}"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent
Filename: "{app}\{#AppExe}"; WorkingDir: "{app}"; Flags: nowait skipifnotsilent; Check: IsAppUpdate

[Code]
function IsAppUpdate: Boolean;
begin
  Result := ExpandConstant('{param:RBSUPDATE|0}') = '1';
end;
