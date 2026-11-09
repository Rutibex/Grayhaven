@echo off
cd /d "%~dp0"
py -3 tools\project.py original-build
if errorlevel 1 goto fail
py -3 tools\project.py content-check
if errorlevel 1 goto fail
py -3 tools\project.py runtime-publish
if errorlevel 1 goto fail
echo Build complete. Open Run-Greyhaven.cmd or Map-Editor.cmd.
pause
exit /b 0
:fail
echo Build failed. Review artifacts\logs for details.
pause
exit /b 1
