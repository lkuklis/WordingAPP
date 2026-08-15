; Inno Setup script for the Wording tray application.
;
; Built by .github/workflows/release.yml, which passes the version in:
;   iscc /DAppVersion=2026.8.0 windows\Wording.iss
;
; The published payload is self-contained, so the installer carries the .NET
; runtime and the target machine needs no prerequisites.

#ifndef AppVersion
  #define AppVersion "0.0.0-dev"
#endif

#define AppName "Wording"
#define AppPublisher "Lukasz Kuklis"
#define AppURL "https://github.com/lkuklis/WordingAPP"
#define AppExeName "Wording.WordApp.exe"

[Setup]
; Never change AppId - it is how Windows recognises an upgrade of an existing install.
AppId={{8F3C2A91-6D4E-4B7A-9C15-2E8B7D4F6A03}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases
VersionInfoVersion={#AppVersion}

DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir=..\dist
OutputBaseFilename=Wording-{#AppVersion}-win-x64-setup
SetupIconFile=..\src\Wording.WordApp\Resources\Icon1.ico
UninstallDisplayIcon={app}\{#AppExeName}

; Per-user install by default so no admin prompt is needed; the app writes only
; to %APPDATA% anyway.
PrivilegesRequiredOverridesAllowed=dialog
PrivilegesRequired=lowest

Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startupicon"; Description: "Start {#AppName} when I sign in"; GroupDescription: "Additional options:"

[Files]
; The whole publish folder: the single-file executable plus appsettings.json and
; the starter pack, which are separate content files even in a single-file publish.
Source: "..\dist\publish\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{userstartup}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: startupicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent
