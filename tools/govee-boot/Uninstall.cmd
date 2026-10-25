@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Manage.ps1" -Action Uninstall
if errorlevel 1 echo Uninstall failed. Check "%TEMP%\RetroDuetLighting-Install.log"
pause
