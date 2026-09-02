@echo off
cd /d "%~dp0"
set "ASPNETCORE_ENVIRONMENT=Development"
dotnet run --no-launch-profile -- --configure-email
echo.
echo Setup finished. Review the message above before starting the app.
pause
