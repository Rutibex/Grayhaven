@echo off
cd /d "%~dp0"
py -3 -m pip install -r tools\art\requirements.txt
if errorlevel 1 goto fail
py -3 tools\art\build_art.py
if errorlevel 1 goto fail
echo Art rebuilt. Open Start-Art-Preview.html.
pause
exit /b 0
:fail
echo Art build failed. Previous compiled art is retained.
pause
exit /b 1
