; 桌面翻译器 - Inno Setup 安装脚本
; 请确保已安装 Inno Setup: https://jrsoftware.org/isinfo.php

; 版本号可以通过命令行参数传入: /DMyAppVersion=1.0.0
; 如果没有传入，则使用默认值
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

#define MyAppName "桌面翻译器"
#define MyAppNameEN "DesktopTranslator"
#define MyAppPublisher "Desktop Translator"
#define MyAppExeName "DesktopTranslator.exe"
#define MyAppURL "https://github.com/yourusername/desktop-translator"
#define MyAppDescription "AI 驱动的桌面实时翻译工具"

[Setup]
AppId={{B2C3D4E5-F6A7-8901-BCDE-F12345678901}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppNameEN}
DefaultGroupName={#MyAppName}
OutputDir=installer
OutputBaseFilename=DesktopTranslator_Setup_v{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=admin
WizardStyle=modern
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
VersionInfoDescription={#MyAppDescription}
VersionInfoVersion={#MyAppVersion}

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加图标:"; Flags: checked

[Files]
Source: "DesktopTranslator\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\卸载 {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动 {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\DesktopTranslator"
