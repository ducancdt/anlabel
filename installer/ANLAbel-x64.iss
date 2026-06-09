; ANLAbel Installer Script for Inno Setup
; Created by Duc An | ducancdt@gmail.com

[Setup]
AppId={{A8B2C3D4-E5F6-4789-ABCD-EF0123456789}
AppName=ANLAbel - Label Designer
AppVersion=0.042
AppPublisher=Duc An
AppPublisherURL=https://github.com/ducancdt
AppSupportURL=mailto:ducancdt@gmail.com
AppUpdatesURL=https://github.com/ducancdt
DefaultDirName={autopf}\ANLAbel
DefaultGroupName=ANLAbel
LicenseFile=..\docs\license-notices.md
OutputDir=..\releases
OutputBaseFilename=ANLAbel-v0.042-Setup-x64
SetupIconFile=..\src\ANLAbel.App\anlabel.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\ANLAbel.App.exe
VersionInfoVersion=0.042.0.0
VersionInfoCompany=Duc An
VersionInfoDescription=ANLAbel - Label Designer
VersionInfoCopyright=Copyright (c) Duc An

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\TestOutput\ANLABEL-build-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\docs\huong-dan-su-dung-ANLAbel.txt"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "..\docs\license-notices.md"; DestDir: "{app}\docs"; Flags: ignoreversion

[Icons]
Name: "{group}\ANLAbel"; Filename: "{app}\ANLAbel.App.exe"
Name: "{group}\Huong dan su dung"; Filename: "{app}\docs\huong-dan-su-dung-ANLAbel.txt"
Name: "{group}\{cm:UninstallProgram,ANLAbel}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\ANLAbel"; Filename: "{app}\ANLAbel.App.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\ANLAbel.App.exe"; Description: "{cm:LaunchProgram,ANLAbel}"; Flags: nowait postinstall skipifsilent