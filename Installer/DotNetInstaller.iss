[Code]
procedure AddDotNet80DesktopDependency;
begin
  // https://dotnet.microsoft.com/download/dotnet/8.0
  if not Dependency_IsNetCoreInstalled('Microsoft.WindowsDesktop.App', 8, 0, 0) then begin
    Dependency_Add('dotnet80desktop' + Dependency_ArchSuffix + '.exe',
      '/lcid ' + IntToStr(GetUILanguage) + ' /passive /norestart',
      '.NET Desktop Runtime 8.0.13' + Dependency_ArchTitle,
      Dependency_String('https://download.visualstudio.microsoft.com/download/pr/b1827c52-ec83-4b3e-8d24-f321276bcdea/812e8d5871111cdc02cc82209c7d45fd/windowsdesktop-runtime-8.0.13-win-x86.exe',
                        'https://download.visualstudio.microsoft.com/download/pr/fc8c9dea-8180-4dad-bf1b-5f229cf47477/c3f0536639ab40f1470b6bad5e1b95b8/windowsdesktop-runtime-8.0.13-win-x64.exe'),
      '', False, False);
  end;
end;

