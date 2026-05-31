@echo off
cd /d "%~dp0"
echo Telemovel com Depuracao USB ligada. Depois abre o jogo Walls.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0DebugAndroidLogcat.ps1"
pause
