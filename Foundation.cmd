@echo off
cd /d "%~dp0"
where py >nul 2>nul
if errorlevel 1 (
 echo Python 3 is required. Install Python 3.10 or newer, then reopen this file.
 pause
 exit /b 1
)
py -3 tools\project.py menu
pause
