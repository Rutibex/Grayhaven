@echo off
cd /d "%~dp0"
dotnet publish src\Greyhaven.ArtProof\Greyhaven.ArtProof.csproj -c Release -r win-x64 --self-contained true -o ArtProof -p:UseSharedCompilation=false -m:1 -nr:false
pause
