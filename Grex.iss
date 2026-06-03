; Inno Setup script for Grex - compiled by build.ps1 (or run directly: iscc Grex.iss).
; Overridable defines (build.ps1 passes these via /D; the fallbacks are for manual runs):
;   AppVersion - product version, e.g. 1.2.0
;   SourceDir  - the published, self-contained win-x64 GUI folder to package
;   OutputDir  - where to write the setup.exe
#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "dist\grex-" + AppVersion + "-win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "dist"
#endif

#define AppName "Grex"
#define AppPublisher "VisorCraft LLC"
#define AppExeName "Grex.exe"
#define AppUrl "https://github.com/visorcraft/Grex"

[Setup]
; AppId uniquely identifies the app for upgrades/uninstall - keep it constant across versions.
AppId={{7287EFD0-6C0F-4D44-9CCF-9E22743A9C45}
AppName={#AppName}
AppVersion={#AppVersion}
VersionInfoVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
LicenseFile=LICENSE
SetupIconFile=Assets\Grex.ico
UninstallDisplayIcon={app}\{#AppExeName}
OutputDir={#OutputDir}
OutputBaseFilename=grex-{#AppVersion}-setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Package everything from the published self-contained GUI folder (app + runtime + Assets).
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent
