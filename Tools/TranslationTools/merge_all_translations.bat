@echo off
REM Enhanced Translation Merge Tool - Batch Wrapper
REM Processes ALL translation files across all modules
REM Author: TOR Translation Tools

setlocal enabledelayedexpansion

echo ===============================================================================
echo TOR Translation Merge Tool - Enhanced Edition
echo ===============================================================================
echo.
echo This tool will merge ALL translation files across ALL modules:
echo   - TOR_Core
echo   - TOR_Armory
echo   - TOR_Environment
echo.
echo Total files: 39+ files
echo.
echo ===============================================================================
echo.

REM Get language code
set /p LANG_CODE="Enter your language code (SP, FR, DE, etc.): "

if "%LANG_CODE%"=="" (
    echo ERROR: Language code is required!
    pause
    exit /b 1
)

echo.
echo Selected Language: %LANG_CODE%
echo.

REM Ask for dry run
echo Would you like to preview changes first? (Dry Run)
echo   Y = Yes, preview only (recommended for first time)
echo   N = No, apply changes immediately
echo.
set /p DRY_RUN="Preview mode? (Y/N): "

echo.
echo ===============================================================================
echo Running Translation Merge...
echo ===============================================================================
echo.

REM Run PowerShell script
if /i "%DRY_RUN%"=="Y" (
    echo Mode: DRY RUN - No files will be modified
    echo.
    powershell.exe -ExecutionPolicy Bypass -File "%~dp0merge_all_translations.ps1" -LanguageCode %LANG_CODE% -DryRun
) else (
    echo Mode: LIVE - Files will be updated
    echo.
    powershell.exe -ExecutionPolicy Bypass -File "%~dp0merge_all_translations.ps1" -LanguageCode %LANG_CODE%
)

echo.
echo ===============================================================================

if %ERRORLEVEL% EQU 0 (
    echo SUCCESS!
    echo.
    echo Check the merge_report_*.txt file for details.
    if /i NOT "%DRY_RUN%"=="Y" (
        echo Backups have been created in: Backups\%LANG_CODE%\
    )
) else (
    echo.
    echo An error occurred. Check the output above for details.
)

echo.
echo Press any key to exit...
pause >nul
