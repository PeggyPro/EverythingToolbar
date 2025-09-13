#define LauncherBuildDir "..\EverythingToolbar.Launcher\bin\arm64\Release\net8.0-windows10.0.17763.0"
#define DeskbandBuildDir "..\EverythingToolbar.Deskband\bin\arm64\Release\net8.0-windows10.0.17763.0"
#define MyAppVersion GetVersionNumbersString(LauncherBuildDir + "\EverythingToolbar.Launcher.exe")
#define ArchitecturesAllowed "arm64"
#define ArchitecturesInstallIn64BitMode "arm64"
#define OutputBaseName "EverythingToolbar-" + MyAppVersion + "-arm64"

#include "SharedInstaller.iss"