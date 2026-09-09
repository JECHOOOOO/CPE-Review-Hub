@echo off
setlocal
title COLMS: CentOs Learning Management System - Local Server
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Start-ReviewHub.ps1"
echo.
echo The local server has stopped. You can close this window.
pause
