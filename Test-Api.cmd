@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Test-Api.ps1" %*
exit /b %errorlevel%
