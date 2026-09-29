; Script generated for LabStock Medical Laboratory Inventory System
#define MyAppName "LabStock"
#define MyAppVersion "1.0"
#define MyAppPublisher "LabStock Medical Systems"
#define MyAppExeName "LabStock.exe"
#define MySourceDir "d:\Новая папка\LabStock\bin\Release\net8.0-windows\win-x64\self-contained"

[Setup]
AppId={{E1B32A90-4F42-4D3C-91B6-7B91223405B1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=d:\Новая папка\Installer_Output
OutputBaseFilename=LabStock_Setup_v1.0
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
