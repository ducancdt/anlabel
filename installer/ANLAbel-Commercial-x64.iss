; Commercial build without the 7-day trial gate

[Setup]
AppId={{A8B2C3D4-E5F6-4789-ABCD-EF0123456789}
AppName=ANLAbel - Label Designer
AppVersion=0.260
AppPublisher=Duc An
AppPublisherURL=https://github.com/ducancdt
AppSupportURL=mailto:ducancdt@gmail.com
DefaultDirName={localappdata}\Programs\ANLAbel
DefaultGroupName=ANLAbel
LicenseFile=..\LICENSE
OutputDir=..\releases\ANLAbel-Commercial-v0.260
OutputBaseFilename=ANLAbel-Commercial-v0.260-Setup-x64
SetupIconFile=..\src\ANLAbel.App\anlabel.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\ANLAbel.App.exe
VersionInfoVersion=0.260.0.0
VersionInfoCompany=Duc An
VersionInfoDescription=ANLAbel Commercial - Label Designer
; Update-in-place: AppId co dinh nen chay lai installer se tu cap nhat dung thu muc cu.
CloseApplications=yes
CloseApplicationsFilter=ANLAbel.App.exe
RestartApplications=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\publish_out\commercial-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\docs\huong-dan-su-dung-ANLAbel.txt"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; DestName: "LICENSE"; Flags: ignoreversion
Source: "..\docs\license-notices.md"; DestDir: "{app}\docs"; Flags: ignoreversion

[Icons]
Name: "{group}\ANLAbel"; Filename: "{app}\ANLAbel.App.exe"
Name: "{group}\Huong dan su dung"; Filename: "{app}\docs\huong-dan-su-dung-ANLAbel.txt"
Name: "{autodesktop}\ANLAbel"; Filename: "{app}\ANLAbel.App.exe"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Classes\.anlabel"; ValueType: string; ValueName: ""; ValueData: "ANLAbel.Template"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\ANLAbel.Template"; ValueType: string; ValueName: ""; ValueData: "ANLAbel Label Template"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\ANLAbel.Template\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\ANLAbel.App.exe,0"
Root: HKCU; Subkey: "Software\Classes\ANLAbel.Template\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\ANLAbel.App.exe"" ""%1"""

[Run]
Filename: "{app}\ANLAbel.App.exe"; Description: "{cm:LaunchProgram,ANLAbel}"; Flags: nowait postinstall skipifsilent
