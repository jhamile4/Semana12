@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Scaffold.ps1" %*
exit /b %errorlevel%
