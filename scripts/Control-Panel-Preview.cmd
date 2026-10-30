@echo off
set "app=%~dp0RetroDuetSaver.exe"
if not exist "%app%" set "app=%~dp0..\dist\RetroDuetSaver-v3.6.8\RetroDuetSaver.exe"
start "" "%app%" --mac-control
