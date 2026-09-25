@echo off
setlocal
cd /d "%~dp0"

echo ======================================================================
echo   Valheim Item Enhancements - Thunderstore Packager
echo ======================================================================
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0export-dist.ps1" %*

set EXITCODE=%ERRORLEVEL%
if %EXITCODE% NEQ 0 (
    echo.
    echo [ERROR] Packaging failed with exit code %EXITCODE%.
    if /i "%~1" NEQ "-nopause" pause
    exit /b %EXITCODE%
)

echo.
echo [DONE] Package generated successfully.
echo Output folder: %~dp0dist
echo.
if /i "%~1" NEQ "-nopause" pause