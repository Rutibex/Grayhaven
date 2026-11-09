@echo off
cd /d "%~dp0"
py -3 tools\project.py build all
set BUILD_RESULT=%ERRORLEVEL%
pause
exit /b %BUILD_RESULT%
