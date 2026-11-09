@echo off
cd /d "%~dp0"
if exist "Runtime\Greyhaven.Launcher.exe" (
 start "" "Runtime\Greyhaven.Launcher.exe" --content-root "%~dp0." --art-proof
 exit /b 0
)
if exist "ArtProof\Greyhaven.ArtProof.exe" (
 "ArtProof\Greyhaven.ArtProof.exe" --assets "%~dp0art\compiled"
 exit /b 0
)
echo Open Start-Art-Preview.html for the offline art proof.
pause
