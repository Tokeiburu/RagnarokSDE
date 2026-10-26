; -- Server Database Editor --

[Setup]
AppName=Server database editor
AppVersion={#VERSION_NAME}
DefaultDirName={commonpf}\Server database editor
DefaultGroupName=Server database editor
UninstallDisplayIcon={app}\SDE.exe
Compression=lzma2
SolidCompression=yes
OutputDir={#OUTPUT_DIR}
OutputBaseFilename=SDE Installer
WizardImageFile=setupBackground.bmp
DisableProgramGroupPage=yes
ChangesAssociations=yes
DisableDirPage=no
DisableWelcomePage=no

[Files]
Source: "SDE\bin\Release\ActImaging.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "SDE\bin\Release\ColorPicker.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "SDE\bin\Release\Database.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "SDE\bin\Release\Encryption.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "SDE\bin\Release\ErrorManager.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "SDE\bin\Release\Gif.Components.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "SDE\bin\Release\GRF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "SDE\bin\Release\GrfToWpfBridge.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "SDE\bin\Release\ICSharpCode.AvalonEdit.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "SDE\bin\Release\IronPython.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "SDE\bin\Release\Lua.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "SDE\bin\Release\Microsoft.Dynamic.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "SDE\bin\Release\Microsoft.Scripting.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "SDE\bin\Release\Microsoft.Scripting.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "SDE\bin\Release\TokeiLibrary.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "SDE\bin\Release\Utilities.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "SDE\bin\Release\System.IO.Hashing.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "SDE\bin\Release\System.Memory.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "SDE\bin\Release\System.Numerics.Vectors.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "SDE\bin\Release\System.Runtime.CompilerServices.Unsafe.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "SDE\bin\Release\K4os.Compression.LZ4.dll"; DestDir: "{app}"; Flags: ignoreversion

Source: "SDE\bin\Release\SDE.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "SDE\bin\Release\SDE.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "SDE\Resources\sde.ico"; DestDir: "{app}"; Flags: ignoreversion

[UninstallDelete]
Type: files; Name: "{app}\sde.ico"
Type: files; Name: "{app}\crash.log"
Type: files; Name: "{app}\debug.log"
Type: filesandordirs; Name: "{app}\tmp"
Type: files; Name: "{userappdata}\Server database editor\sde.ico"
Type: files; Name: "{userappdata}\Server database editor\crash.log"
Type: files; Name: "{userappdata}\Server database editor\debug.log"
Type: filesandordirs; Name: "{userappdata}\Server database editor\~tmp"

[Icons]
Name: "{group}\Server database editor"; Filename: "{app}\SDE.exe"
Name: "{commondesktop}\Server database editor"; Filename: "{app}\SDE.exe"

[CustomMessages]
DotNetMissing=Act Editor requires .NET Framework 4.8. Do you want to download it? Setup will now exit!

[Code]
function IsDotNet48Installed: Boolean;
var
  Release: Cardinal;
begin
  Result := False;
  if RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) then
  begin
    // 528040 = .NET Framework 4.8
    if Release >= 528040 then
      Result := True;
  end;
end;


function InitializeSetup(): Boolean;
var ErrorCode: Integer;
begin
  if not IsDotNet48Installed then
  begin
    MsgBox('.NET Framework 4.8 is required. The installer will now open the download page.', mbInformation, MB_OK);
    ShellExec('', 'https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    Result := False;  // cancel setup
  end
  else
    Result := True;   // continue setup
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  case CurUninstallStep of
    usPostUninstall:
      begin
        if (FileExists(ExpandConstant('{app}\config.txt')) or FileExists(ExpandConstant('{userappdata}\Act Editor\config.txt'))) then
        begin
        if (MsgBox('Program settings have been found, would you like to remove them?', mbConfirmation, MB_YESNO) = idYes) then
          begin
            DeleteFile(ExpandConstant('{app}\config.txt'));
            DeleteFile(ExpandConstant('{userappdata}\Act Editor\config.txt'));
           end
        end
      end;
  end;
end;