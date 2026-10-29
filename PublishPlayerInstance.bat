@echo off
setlocal

rem ---- Edit this if your project file lives somewhere else (relative to this script) ----
set PROJECT=CharManJur\CharManJur.csproj
set OUTPUT=%~dp0PlayerInstance

echo Publishing CharManJur to "%OUTPUT%" ...
dotnet publish "%~dp0%PROJECT%" -f net10.0-windows10.0.19041.0 -c Release -o "%OUTPUT%" ^
  -p:RuntimeIdentifierOverride=win10-x64 ^
  -p:UseMonoRuntime=false ^
  -p:WindowsPackageType=None ^
  -p:WindowsAppSDKSelfContained=true ^
  -p:SelfContained=true

if errorlevel 1 (
    echo.
    echo PUBLISH FAILED. See the messages above.
    pause
    exit /b 1
)

echo.
echo Done. Start the player instance with RunPlayerInstance.bat
pause