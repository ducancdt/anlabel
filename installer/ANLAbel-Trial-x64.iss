; Separate 7-day trial installer for ANLAbel

[Setup]
AppId={{5E638998-0D16-4AE0-9E8E-54D84FA26E4A}
AppName=ANLAbel Trial - Label Designer
AppVersion=0.262
AppPublisher=Duc An
AppPublisherURL=https://github.com/ducancdt
AppSupportURL=mailto:ducancdt@gmail.com
DefaultDirName={localappdata}\Programs\ANLAbel Trial
DefaultGroupName=ANLAbel Trial
LicenseFile=..\LICENSE
OutputDir=..\releases\ANLAbel-Trial-7-Day-v0.262
OutputBaseFilename=ANLAbel-Trial-7-Day-v0.262-Setup-x64
SetupIconFile=..\src\ANLAbel.App\anlabel.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\ANLAbel.App.exe
UninstallDisplayName=ANLAbel Trial 7 Day
VersionInfoVersion=0.262.0.0
VersionInfoCompany=Duc An
VersionInfoDescription=ANLAbel Trial - 7 Day
VersionInfoCopyright=Copyright (c) Duc An
; Update-in-place: cung AppId nen chay lai installer tren may da cai se tu nhan dien
; ban cu va ghi de vao dung thu muc cu, khong can go tay truoc. Key kich hoat/trang
; thai trial nam o %LocalAppData%/Registry (ngoai {app}) nen KHONG bi mat khi update.
CloseApplications=yes
CloseApplicationsFilter=ANLAbel.App.exe
RestartApplications=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\publish_out\trial-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\docs\huong-dan-su-dung-ANLAbel.txt"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; DestName: "LICENSE"; Flags: ignoreversion
Source: "..\docs\license-notices.md"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "..\docs\huong-dan-trial-va-kich-hoat.txt"; DestDir: "{app}\docs"; Flags: ignoreversion

[Icons]
Name: "{group}\ANLAbel Trial"; Filename: "{app}\ANLAbel.App.exe"
Name: "{group}\Huong dan su dung"; Filename: "{app}\docs\huong-dan-su-dung-ANLAbel.txt"
Name: "{group}\{cm:UninstallProgram,ANLAbel Trial}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\ANLAbel Trial"; Filename: "{app}\ANLAbel.App.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\ANLAbel.App.exe"; Description: "{cm:LaunchProgram,ANLAbel Trial}"; Flags: nowait postinstall skipifsilent
