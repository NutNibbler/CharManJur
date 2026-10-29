@echo off
setlocal

rem ---- The name picks the data folder. Different names = different players on this PC. ----
set NAME=%~1
if "%NAME%"=="" set NAME=Player

set CHARMANJUR_DATA=%LOCALAPPDATA%\CharManJur_%NAME%
if not exist "%CHARMANJUR_DATA%" mkdir "%CHARMANJUR_DATA%"

set EXE=%~dp0PlayerInstance\CharManJur.exe
if not exist "%EXE%" (
    echo "%EXE%" not found. Run PublishPlayerInstance.bat first.
    pause
    exit /b 1
)

echo Starting instance "%NAME%" with data in %CHARMANJUR_DATA%
start "" "%EXE%"