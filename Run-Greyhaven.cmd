@echo off
cd /d "%~dp0"
if not exist "Runtime\Greyhaven.Launcher.exe" goto missing
start "" "Runtime\Greyhaven.Launcher.exe" --content-root "%~dp0."
exit /b 0
:missing
echo Extract the complete Greyhaven release ZIP first. The Runtime folder is required.
pause
exit /b 1
