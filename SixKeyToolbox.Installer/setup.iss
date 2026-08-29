#define MyAppName "SixKeyToolbox"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "yt6983138"
#define MyAppURL "https://github.com/yt6983138/SixKeyToolbox"
#define MyAppExeName "SixKeyToolbox.exe"

[Setup]
AppId=SixKeyToolbox
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=Release
OutputBaseFilename=SixKeyToolbox-Setup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
LicenseFile=..\LICENSE.txt
; SetupIconFile=..\SixKeyToolbox\wwwroot\favicon.png
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\SixKeyToolbox\bin\Release\net10.0\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[Code]
var
  DotNetMissing: Boolean;
  AspNetMissing: Boolean;
  RuntimeCheckPage: TWizardPage;
  DotNetDownloadLabel: TLabel;
  AspNetDownloadLabel: TLabel;

function IsDotNet10Installed(): Boolean;
var
  ResultCode: Integer;
  Output: AnsiString;
  TempFile: String;
begin
  Result := False;
  TempFile := ExpandConstant('{tmp}\dotnet-check.txt');

  if Exec('cmd.exe', '/C dotnet --list-runtimes > "' + TempFile + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    if LoadStringFromFile(TempFile, Output) then
    begin
      if Pos('Microsoft.NETCore.App 10.', Output) > 0 then
        Result := True;
    end;
  end;

  DeleteFile(TempFile);
end;

function IsAspNetCore10Installed(): Boolean;
var
  ResultCode: Integer;
  Output: AnsiString;
  TempFile: String;
begin
  Result := False;
  TempFile := ExpandConstant('{tmp}\dotnet-check.txt');

  if Exec('cmd.exe', '/C dotnet --list-runtimes > "' + TempFile + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    if LoadStringFromFile(TempFile, Output) then
    begin
      if Pos('Microsoft.AspNetCore.App 10.', Output) > 0 then
        Result := True;
    end;
  end;

  DeleteFile(TempFile);
end;

procedure DotNetDownloadLabelClick(Sender: TObject);
var
  ResultCode: Integer;
begin
  ShellExec('open', 'https://aka.ms/dotnet/10.0/dotnet-runtime-win-x64.exe', '', '', SW_SHOW, ewNoWait, ResultCode);
end;

procedure AspNetDownloadLabelClick(Sender: TObject);
var
  ResultCode: Integer;
begin
  ShellExec('open', 'https://aka.ms/dotnet/10.0/aspnetcore-runtime-win-x64.exe', '', '', SW_SHOW, ewNoWait, ResultCode);
end;

function CreateInfoLabel(Page: TWizardPage; Top: Integer; Caption: String): TLabel;
begin
  Result := TLabel.Create(Page);
  Result.Parent := Page.Surface;
  Result.Left := ScaleX(0);
  Result.Top := ScaleY(Top);
  Result.Width := Page.SurfaceWidth;
  Result.Caption := Caption;
  Result.WordWrap := True;
  Result.AutoSize := True;
end;

function CreateLinkLabel(Page: TWizardPage; Top: Integer; Caption: String): TLabel;
begin
  Result := TLabel.Create(Page);
  Result.Parent := Page.Surface;
  Result.Left := ScaleX(16);
  Result.Top := ScaleY(Top);
  Result.Caption := Caption;
  Result.Font.Color := clBlue;
  Result.Font.Style := [fsUnderline];
  Result.Cursor := crHand;
end;

procedure InitializeWizard();
var
  InfoLabel: TLabel;
  YPos: Integer;
begin
  DotNetMissing := not IsDotNet10Installed();
  AspNetMissing := not IsAspNetCore10Installed();

  if DotNetMissing or AspNetMissing then
  begin
    RuntimeCheckPage := CreateCustomPage(
      wpWelcome,
      'Missing Required Components',
      'The following runtime components are required to run ' + ExpandConstant('{#MyAppName}')
    );

    YPos := 0;

    InfoLabel := CreateInfoLabel(RuntimeCheckPage, YPos,
      'The following components need to be installed before you can run this application:');
    YPos := YPos + InfoLabel.Height + ScaleY(16);

    if DotNetMissing then
    begin
      InfoLabel := CreateInfoLabel(RuntimeCheckPage, YPos, '• .NET 10 Runtime (x64)');
      YPos := YPos + InfoLabel.Height + ScaleY(4);

      InfoLabel := CreateInfoLabel(RuntimeCheckPage, YPos, '  Download:');
      YPos := YPos + InfoLabel.Height + ScaleY(4);

      DotNetDownloadLabel := CreateLinkLabel(RuntimeCheckPage, YPos,
        'https://aka.ms/dotnet/10.0/dotnet-runtime-win-x64.exe');
      DotNetDownloadLabel.OnClick := @DotNetDownloadLabelClick;
      YPos := YPos + DotNetDownloadLabel.Height + ScaleY(16);
    end;

    if AspNetMissing then
    begin
      InfoLabel := CreateInfoLabel(RuntimeCheckPage, YPos, '• ASP.NET Core 10 Runtime (x64)');
      YPos := YPos + InfoLabel.Height + ScaleY(4);

      InfoLabel := CreateInfoLabel(RuntimeCheckPage, YPos, '  Download:');
      YPos := YPos + InfoLabel.Height + ScaleY(4);

      AspNetDownloadLabel := CreateLinkLabel(RuntimeCheckPage, YPos,
        'https://aka.ms/dotnet/10.0/aspnetcore-runtime-win-x64.exe');
      AspNetDownloadLabel.OnClick := @AspNetDownloadLabelClick;
      YPos := YPos + AspNetDownloadLabel.Height + ScaleY(16);
    end;

    InfoLabel := CreateInfoLabel(RuntimeCheckPage, YPos,
      'IMPORTANT: You need to install BOTH runtimes separately. ' +
      'The ASP.NET Core Runtime does NOT include the .NET Runtime.');
    YPos := YPos + InfoLabel.Height + ScaleY(16);

    InfoLabel := CreateInfoLabel(RuntimeCheckPage, YPos,
      'After installing the required runtimes, you can continue with this installation. ' +
      'Click Next to continue anyway, or Cancel to exit and install the runtimes first.');
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;

  if (CurPageID = wpSelectDir) and ((not IsDotNet10Installed()) or (not IsAspNetCore10Installed())) then
  begin
    if MsgBox('Warning: Required runtime components are missing. ' +
              'The application will not run without them. ' + #13#10#13#10 +
              'Do you want to continue with the installation anyway?',
              mbConfirmation, MB_YESNO) = IDNO then
    begin
      Result := False;
    end;
  end;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;

  if Assigned(RuntimeCheckPage) and (PageID = RuntimeCheckPage.ID) then
  begin
    if (IsDotNet10Installed()) and (IsAspNetCore10Installed()) then
      Result := True;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if DotNetMissing or AspNetMissing then
    begin
      MsgBox('Installation complete. ' + #13#10#13#10 +
             'REMINDER: You still need to install the missing runtime components before running the application. ' +
             'See the earlier message for download links.',
             mbInformation, MB_OK);
    end;
  end;
end;
