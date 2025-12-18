@echo off
echo ===============================================================================
echo TOR Translation Merge Tool
echo ===============================================================================
echo.
echo This tool will merge ALL translation files across ALL modules
echo.

set /p LANG_CODE="Enter language code (SP, FR, DE, etc.): "

if "%LANG_CODE%"=="" (
    echo ERROR: Language code is required!
    pause
    exit /b 1
)

echo.
set /p DRY_RUN="Preview mode? (Y/N): "
echo.

if /i "%DRY_RUN%"=="Y" (
    powershell.exe -ExecutionPolicy Bypass -File "%~dp0merge_all_translations.ps1" -LanguageCode "%LANG_CODE%" -DryRun
) else (
    powershell.exe -ExecutionPolicy Bypass -File "%~dp0merge_all_translations.ps1" -LanguageCode "%LANG_CODE%"
)
