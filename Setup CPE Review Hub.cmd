@echo off
setlocal
title CPE Review Hub - MySQL Setup
cd /d "%~dp0"
set "ASPNETCORE_ENVIRONMENT=Development"
dotnet run --no-launch-profile -- --configure-mysql
if errorlevel 1 goto end
dotnet run --no-launch-profile -- --initialize-database
if errorlevel 1 goto end
dotnet run --no-launch-profile -- --seed-course-catalog
if errorlevel 1 goto end
choice /m "Create an administrator account now"
if errorlevel 2 goto end
dotnet run --no-launch-profile -- --create-admin
:end
echo.
echo Setup finished. Review the messages above before starting the app.
pause
