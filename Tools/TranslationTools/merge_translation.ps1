<#
.SYNOPSIS
    Merges updated English tor_strings.xml with existing translations.

.DESCRIPTION
    This script helps translators update their translation files when the English
    tor_strings.xml is updated. It preserves all existing translations and adds
    new entries as "TODO [English text]" placeholders.

.PARAMETER LanguageCode
    The two-letter language code (e.g., SP, FR, DE, IT, RU, etc.)

.EXAMPLE
    .\merge_translation.ps1 -LanguageCode SP
    Merges the English file with Spanish translations

.EXAMPLE
    .\merge_translation.ps1 SP
    Same as above (positional parameter)

.NOTES
    Author: TOR Mod Team
    Version: 1.0

    The script will:
    1. Backup your existing translation file
    2. Extract all localization IDs from the English file
    3. Match existing translations from your old file
    4. Create a merged file with the English structure
    5. Mark new entries as "TODO [English text]"
    6. Display statistics about the merge operation
#>

param(
    [Parameter(Mandatory=$true, Position=0, HelpMessage="Language code (e.g., SP, FR, DE)")]
    [ValidateNotNullOrEmpty()]
    [string]$LanguageCode
)

# Script configuration
$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# File paths
$ModuleDataDir = Join-Path $ScriptDir "..\..\ModuleData"
$EnglishFile = Join-Path $ModuleDataDir "tor_strings.xml"
$TranslationDir = Join-Path $ModuleDataDir "Languages\$LanguageCode\TOR_Core"
$TranslationFile = Join-Path $TranslationDir "tor_strings.xml"
$BackupFile = Join-Path $TranslationDir "tor_strings_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss').xml"

# Color output functions
function Write-Success { param($Message) Write-Host $Message -ForegroundColor Green }
function Write-Info { param($Message) Write-Host $Message -ForegroundColor Cyan }
function Write-Warning { param($Message) Write-Host $Message -ForegroundColor Yellow }
function Write-Error { param($Message) Write-Host $Message -ForegroundColor Red }

# Display header
Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "  TOR Translation Merge Tool v1.0" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host ""

# Validate English file exists
if (-not (Test-Path $EnglishFile)) {
    Write-Error "ERROR: English source file not found at: $EnglishFile"
    exit 1
}

Write-Info "Source English file: $EnglishFile"
Write-Info "Target language: $LanguageCode"
Write-Host ""

# Check if translation directory exists
if (-not (Test-Path $TranslationDir)) {
    Write-Warning "Translation directory does not exist. Creating: $TranslationDir"
    New-Item -ItemType Directory -Path $TranslationDir -Force | Out-Null
}

# Load existing translation file if it exists
$ExistingTranslations = @{}
$OldFileExists = $false

if (Test-Path $TranslationFile) {
    Write-Info "Loading existing translation file..."

    # Backup existing file
    Copy-Item -Path $TranslationFile -Destination $BackupFile -Force
    Write-Success "Backup created: $BackupFile"
    Write-Host ""

    $OldFileExists = $true

    try {
        [xml]$OldXml = Get-Content $TranslationFile -Encoding UTF8

        # Extract translations from old file
        # The old file uses <string id="str_tor_xxx" text="Translation"/>
        foreach ($string in $OldXml.SelectNodes("//string")) {
            $id = $string.GetAttribute("id")
            $text = $string.GetAttribute("text")

            if ($id -and $text) {
                $ExistingTranslations[$id] = $text
            }
        }

        Write-Success "Loaded $($ExistingTranslations.Count) existing translations"
    }
    catch {
        Write-Warning "Warning: Could not parse existing translation file. Starting fresh."
        Write-Warning "Error: $_"
    }
}
else {
    Write-Warning "No existing translation file found. Creating new translation file."
}

Write-Host ""
Write-Info "Processing English source file..."
Write-Host ""

# Load English file
try {
    [xml]$EnglishXml = Get-Content $EnglishFile -Encoding UTF8
}
catch {
    Write-Error "ERROR: Could not parse English source file."
    Write-Error "Error: $_"
    exit 1
}

# Statistics
$TotalEntries = 0
$PreservedTranslations = 0
$NewEntries = 0

# Build output XML using string builder for better formatting
$OutputBuilder = New-Object System.Text.StringBuilder

# Add XML declaration and root elements
[void]$OutputBuilder.AppendLine('<?xml version="1.0" encoding="utf-8"?>')
[void]$OutputBuilder.AppendLine('<base xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" type="string">')
[void]$OutputBuilder.AppendLine('  <tags>')
[void]$OutputBuilder.AppendLine("    <tag language=`"Language Name ($LanguageCode)`" />")
[void]$OutputBuilder.AppendLine('  </tags>')
[void]$OutputBuilder.AppendLine('  <strings>')

# Regular expression to extract localization ID from {=str_tor_xxx} format
$LocalizationIdPattern = '\{=([^}]+)\}'

# Process each string in the English file
foreach ($node in $EnglishXml.SelectNodes("//string")) {
    $TotalEntries++

    $englishId = $node.GetAttribute("id")
    $englishText = $node.GetAttribute("text")

    # Extract the localization ID from the text attribute
    if ($englishText -match $LocalizationIdPattern) {
        $localizationId = $Matches[1]

        # Extract the actual English text (everything after the localization tag)
        $actualEnglishText = $englishText -replace '^\{=[^}]+\}', ''
        $actualEnglishText = $actualEnglishText.Trim()

        # Check if we have an existing translation for this ID
        $translationText = $null

        if ($ExistingTranslations.ContainsKey($localizationId)) {
            $translationText = $ExistingTranslations[$localizationId]
            $PreservedTranslations++
        }
        else {
            # No existing translation - create TODO entry
            $translationText = "TODO [$actualEnglishText]"
            $NewEntries++
        }

        # XML-escape the translation text
        $translationText = $translationText -replace '&', '&amp;'
        $translationText = $translationText -replace '<', '&lt;'
        $translationText = $translationText -replace '>', '&gt;'
        $translationText = $translationText -replace '"', '&quot;'
        $translationText = $translationText -replace "'", '&apos;'

        # Add string element to output
        [void]$OutputBuilder.AppendLine("    <string id=`"$localizationId`" text=`"$translationText`"/>")

    }
    else {
        Write-Warning "Warning: Could not extract localization ID from: $englishText"
    }
}

# Close the XML structure
[void]$OutputBuilder.AppendLine('  </strings>')
[void]$OutputBuilder.AppendLine('</base>')

# Save the output file
try {
    $OutputBuilder.ToString() | Out-File -FilePath $TranslationFile -Encoding UTF8
    Write-Success "Translation file saved: $TranslationFile"
}
catch {
    Write-Error "ERROR: Could not save translation file."
    Write-Error "Error: $_"
    exit 1
}

# Display statistics
Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "  Merge Statistics" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host ""
Write-Info "Total entries in English file: $TotalEntries"
Write-Success "Preserved translations: $PreservedTranslations"
Write-Warning "New entries (marked TODO): $NewEntries"

if ($OldFileExists) {
    $percentComplete = [math]::Round(($PreservedTranslations / $TotalEntries) * 100, 2)
    Write-Host ""
    Write-Info "Translation completion: $percentComplete%"
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host ""

if ($NewEntries -gt 0) {
    Write-Warning "ACTION REQUIRED:"
    Write-Warning "  - Search for 'TODO' in the translation file"
    Write-Warning "  - Translate all entries marked as 'TODO [English text]'"
    Write-Warning "  - Update the language attribute in the <tag> element"
}
else {
    Write-Success "All entries have been translated!"
}

Write-Host ""
Write-Success "Merge completed successfully!"
Write-Host ""
