@echo off
setlocal
title Install Whispers of the Spirit

echo Whispers of the Spirit 2.3 Installer
echo ====================================
echo.

if not exist "%~dp0support\Install-Whispers.ps1" (
    echo ERROR: The installer support file is missing.
    echo Please extract the complete ZIP before running this installer.
    echo.
    pause
    exit /b 1
)

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0support\Install-Whispers.ps1"
set "INSTALL_RESULT=%ERRORLEVEL%"

echo.
if not "%INSTALL_RESULT%"=="0" (
    echo Installation did not complete. No existing recordings or user data were deleted.
    echo See the message above for details.
) else (
    echo Installation completed successfully.
)
echo.
pause
exit /b %INSTALL_RESULT%
