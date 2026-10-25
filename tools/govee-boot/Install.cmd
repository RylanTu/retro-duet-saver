@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Manage.ps1" -Action Install
if errorlevel 1 echo Install failed. Check "%TEMP%\RetroDuetLighting-Install.log"
pause
