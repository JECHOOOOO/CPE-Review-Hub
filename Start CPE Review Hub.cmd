@echo off
setlocal
title CPE Review Hub - Local Server
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Start-ReviewHub.ps1"
echo.
echo The local server has stopped. You can close this window.
pause
