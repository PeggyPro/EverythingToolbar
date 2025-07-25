[Code]
function GetUninstallString(ProductCode: string): string;
var
  UninstallKey: string;
  UninstallString: string;
begin
  Result := '';
  UninstallKey := 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\' + ProductCode;

  if RegQueryStringValue(HKLM, UninstallKey, 'UninstallString', UninstallString) then
    Result := UninstallString
  else if RegQueryStringValue(HKCU, UninstallKey, 'UninstallString', UninstallString) then
    Result := UninstallString;
end;

function UninstallWixVersion(): Boolean;
var
  UninstallString: string;
  ResultCode: Integer;
  ProductCode: string;
begin
  Result := True;

  ProductCode := '{BAF2A7BE-A3BD-4263-9F73-4CDA8D311642}';

  UninstallString := GetUninstallString(ProductCode);

  if UninstallString <> '' then
  begin
    if MsgBox('A previous version of EverythingToolbar was detected. Due to changes in the installation process, ' +
              'it needs to be removed before continuing. Do you want to uninstall the previous version now? ' +
              'Your settings will be unaffected.',
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      if not Exec('msiexec.exe', '/x' + ProductCode + ' /passive /norestart', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode) then
      begin
        MsgBox('Failed to uninstall the previous version. Please uninstall it manually before continuing.', mbError, MB_OK);
        Result := False;
      end;
      MsgBox('Uninstallation completed. Installation will continue.', mbInformation, MB_OK);
    end
    else
    begin
      Result := False;
    end;
  end;
end;
