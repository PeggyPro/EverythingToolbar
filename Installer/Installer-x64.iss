#define LauncherBuildDir "..\EverythingToolbar.Launcher\bin\x64\Release\net8.0-windows10.0.17763.0"
#define DeskbandBuildDir "..\EverythingToolbar.Deskband\bin\x64\Release\net8.0-windows10.0.17763.0"
#define MyAppVersion GetVersionNumbersString(LauncherBuildDir + "\EverythingToolbar.Launcher.exe")
#define ArchitecturesAllowed "x64os"
#define ArchitecturesInstallIn64BitMode "x64os"
#define OutputBaseName "EverythingToolbar-" + MyAppVersion + "-x64"

#include "SharedInstaller.iss"