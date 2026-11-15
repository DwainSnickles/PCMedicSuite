; PC Medic Suite installer — Inno Setup 6
; Per-machine install into Program Files. No Start Menu or Desktop shortcuts,
; no "launch when finished" prompt — a clean install/uninstall entry only.

#define AppName "PC Medic Suite"
#define AppExe "PCMedicSuite.exe"
#define AppVersion "1.4.0"
#define AppPublisher "DwainSnickles"
#define AppId "{{8E4B7C2A-3D5F-4E6A-9B1C-2F7D8E5A6C3B}"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
VersionInfoVersion={#AppVersion}.0
VersionInfoDescription={#AppName} installer
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}.0
DefaultDirName={commonpf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=.
OutputBaseFilename=PCMedicSuite-Setup-{#AppVersion}-x64
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\..\pcmedic.ico
UninstallDisplayIcon={app}\{#AppExe}
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\Portable\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
