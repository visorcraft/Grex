; Inno Setup script for Grex - compiled by build.ps1 (or run directly: iscc Grex.iss).
; Overridable defines (build.ps1 passes these via /D; the fallbacks are for manual runs):
;   AppVersion - product version, e.g. 1.2.0
;   SourceDir  - the published, self-contained win-x64 GUI folder to package
;   CliDir     - the published, self-contained win-x64 CLI folder to package
;   OutputDir  - where to write the setup.exe
#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "dist\grex-" + AppVersion + "-win-x64"
#endif
#ifndef CliDir
  #define CliDir "dist\grex-cli-" + AppVersion + "-win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "dist"
#endif

#define AppName "Grex"
#define AppPublisher "VisorCraft LLC"
#define AppExeName "Grex.exe"
#define AppUrl "https://github.com/visorcraft/Grex"
; VersionInfoVersion must be purely numeric (X.X.X[.X]); strip any pre-release suffix (e.g. "-rc1").
#define NumericVersion Copy(AppVersion, 1, Pos("-", AppVersion + "-") - 1)

[Setup]
; AppId uniquely identifies the app for upgrades/uninstall - keep it constant across versions.
AppId={{7287EFD0-6C0F-4D44-9CCF-9E22743A9C45}
AppName={#AppName}
AppVersion={#AppVersion}
VersionInfoVersion={#NumericVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases
; Per-user install: no UAC prompt; installs under the user's LocalAppData ({autopf} -> {localappdata}\Programs).
PrivilegesRequired=lowest
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
ChangesEnvironment=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "addtopath"; Description: "Add the grex-cli command-line tool to my PATH"; GroupDescription: "Command-line tool:"; Flags: unchecked

[Files]
; The GUI (self-contained app + runtime + Assets).
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion
; The CLI, kept in its own subfolder so its bundled runtime can't collide with the GUI's.
Source: "{#CliDir}\*"; DestDir: "{app}\cli"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Registry]
; Optionally add the CLI folder to the per-user PATH (HKCU - no admin needed). Check avoids duplicates.
Root: HKCU; Subkey: "Environment"; ValueType: expandsz; ValueName: "Path"; ValueData: "{olddata};{app}\cli"; \
    Flags: preservestringtype; Tasks: addtopath; Check: NeedsAddPath('{app}\cli')

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[Code]
{ Returns True only if the given folder is not already on the per-user PATH. }
function NeedsAddPath(Param: string): Boolean;
var
  OrigPath: string;
begin
  if not RegQueryStringValue(HKEY_CURRENT_USER, 'Environment', 'Path', OrigPath) then
  begin
    Result := True;
    exit;
  end;
  Result := Pos(';' + Uppercase(ExpandConstant(Param)) + ';', ';' + Uppercase(OrigPath) + ';') = 0;
end;

{ On uninstall, remove the CLI folder from the per-user PATH (leaves the rest of PATH untouched). }
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Path, CliDir: string;
begin
  if CurUninstallStep <> usPostUninstall then
    exit;
  if not RegQueryStringValue(HKEY_CURRENT_USER, 'Environment', 'Path', Path) then
    exit;
  CliDir := ExpandConstant('{app}\cli');
  StringChangeEx(Path, ';' + CliDir, '', True);
  StringChangeEx(Path, CliDir + ';', '', True);
  StringChangeEx(Path, CliDir, '', True);
  RegWriteExpandStringValue(HKEY_CURRENT_USER, 'Environment', 'Path', Path);
end;
