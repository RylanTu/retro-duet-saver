@echo off
set "app=%~dp0RetroDuetSaver.exe"
if not exist "%app%" set "app=%~dp0..\dist\RetroDuetSaver-v3.6.8\RetroDuetSaver.exe"
if not exist "%app%" (
  echo Build the project first or extract the release ZIP.
  pause
  exit /b 1
)
start "" "%app%" /s
