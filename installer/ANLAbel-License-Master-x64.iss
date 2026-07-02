; PRIVATE installer - never distribute this with the customer Trial package

[Setup]
AppId={{A2690B09-4EC7-428A-BB61-1BB018EB4E96}
AppName=ANLAbel License Master
AppVersion=1.0.0
AppPublisher=Duc An
DefaultDirName={localappdata}\Programs\ANLAbel License Master
DefaultGroupName=ANLAbel License Master
OutputDir=..\releases\ANLAbel-License-Master-v1.0
OutputBaseFilename=ANLAbel-License-Master-v1.0-PRIVATE-Setup-x64
SetupIconFile=..\src\ANLAbel.App\anlabel.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\ANLAbel.LicenseGenerator.exe
VersionInfoVersion=1.0.0.0
VersionInfoDescription=ANLAbel License Master - PRIVATE

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\publish_out\license-master-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\docs\huong-dan-trial-va-kich-hoat.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\ANLAbel License Master"; Filename: "{app}\ANLAbel.LicenseGenerator.exe"
Name: "{autodesktop}\ANLAbel License Master"; Filename: "{app}\ANLAbel.LicenseGenerator.exe"

[Run]
Filename: "{app}\ANLAbel.LicenseGenerator.exe"; Description: "Open ANLAbel License Master"; Flags: nowait postinstall skipifsilent
