@echo off
REM TOR Translation Merge Tool - Windows Batch Launcher
REM This script provides a simple interface for translators to merge updated English strings
REM with their existing translations.

setlocal enabledelayedexpansion

REM Display welcome message
echo.
echo ================================================================================
echo                    TOR Translation Merge Tool
echo ================================================================================
echo.
echo This tool helps you update your translation files when the English text is
echo updated. It will preserve all your existing translations and add new entries
echo that need translation.
echo.
echo ================================================================================
echo.
echo Common Language Codes:
echo   SP - Spanish (Espanol)
echo   FR - French (Francais)
echo   DE - German (Deutsch)
echo   IT - Italian (Italiano)
echo   RU - Russian (Russkiy)
echo   PT - Portuguese (Portugues)
echo   PL - Polish (Polski)
echo   TR - Turkish (Turkce)
echo   CN - Chinese (Simplified)
echo   JP - Japanese (Nihongo)
echo   KR - Korean (Hangugeo)
echo.
echo ================================================================================
echo.

REM Prompt for language code
set /p LANG_CODE="Enter your language code (e.g., SP, FR, DE): "

REM Validate input
if "%LANG_CODE%"=="" (
    echo.
    echo ERROR: No language code entered!
    echo Please run the script again and enter a valid language code.
    echo.
    pause
    exit /b 1
)

REM Trim whitespace and convert to uppercase
for /f "tokens=* delims= " %%a in ("%LANG_CODE%") do set LANG_CODE=%%a
set LANG_CODE=%LANG_CODE: =%

echo.
echo ================================================================================
echo Processing translation for language: %LANG_CODE%
echo ================================================================================
echo.

REM Check if PowerShell is available
where powershell >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo ERROR: PowerShell is not available on this system!
    echo This script requires PowerShell to run.
    echo.
    pause
    exit /b 1
)

REM Get the directory where this batch file is located
set SCRIPT_DIR=%~dp0

REM Check if the PowerShell script exists
if not exist "%SCRIPT_DIR%merge_translation.ps1" (
    echo ERROR: merge_translation.ps1 not found in the current directory!
    echo Expected location: %SCRIPT_DIR%merge_translation.ps1
    echo.
    pause
    exit /b 1
)

REM Run the PowerShell script
REM -ExecutionPolicy Bypass allows the script to run without changing system settings
REM -NoProfile speeds up execution by not loading user profile
REM -File specifies the script to run
powershell.exe -ExecutionPolicy Bypass -NoProfile -File "%SCRIPT_DIR%merge_translation.ps1" -LanguageCode "%LANG_CODE%"

REM Check if PowerShell script succeeded
if %ERRORLEVEL% neq 0 (
    echo.
    echo ================================================================================
    echo ERROR: The merge process encountered an error!
    echo ================================================================================
    echo.
    echo Please check the error messages above for details.
    echo If you need help, please contact the TOR Mod Team.
    echo.
) else (
    echo.
    echo ================================================================================
    echo SUCCESS: Merge completed!
    echo ================================================================================
    echo.
    echo Your translation file has been updated. Next steps:
    echo   1. Open the translation file in your preferred text editor
    echo   2. Search for "TODO" to find entries that need translation
    echo   3. Translate all TODO entries
    echo   4. Update the language name in the file if needed
    echo.
)

echo Press any key to exit...
pause >nul
