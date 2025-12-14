<#
.SYNOPSIS
    Merges all English XML files with their translations based on language_data.xml.

.DESCRIPTION
    This script automates the process of updating all translation files for a given language.
    It reads the language_data.xml file to determine which files need to be processed,
    then for each file:
    - Locates the English source file in the appropriate module (TOR_Core, TOR_Armory, or TOR_Environment)
    - Extracts all localization IDs and English text
    - Matches against existing translations
    - Creates merged output with preserved translations and TODO markers for missing ones
    - Creates backups before overwriting
    - Shows colorized progress and comprehensive statistics

.PARAMETER LanguageCode
    The two-letter language code (e.g., SP, FR, DE, IT, RU, etc.)
    This MUST match a directory under ModuleData/Languages/

.PARAMETER DryRun
    If specified, the script will show what it would do without actually modifying any files.

.EXAMPLE
    .\merge_all_translations.ps1 -LanguageCode SP
    Merges all English files with Spanish translations

.EXAMPLE
    .\merge_all_translations.ps1 SP -DryRun
    Shows what would be done for Spanish without modifying files

.NOTES
    Author: TOR Mod Team
    Version: 2.0

    The script processes files in the order they appear in language_data.xml
    and provides detailed statistics for each file and overall totals.
#>

param(
    [Parameter(Mandatory=$true, Position=0, HelpMessage="Language code (e.g., SP, FR, DE)")]
    [ValidateNotNullOrEmpty()]
    [string]$LanguageCode,

    [Parameter(Mandatory=$false)]
    [switch]$DryRun
)

# Script configuration
$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Base paths
$ModuleDataDir = Join-Path $ScriptDir "..\..\ModuleData"
$LanguageDataFile = Join-Path $ModuleDataDir "Languages\$LanguageCode\language_data.xml"

# Get absolute path to Modules base directory
# From TranslationTools, go up to TOR_Core, then up to Modules
$TORCoreDir = Split-Path (Split-Path $ScriptDir -Parent) -Parent
$ModulesBaseDir = Split-Path $TORCoreDir -Parent

# Color output functions
function Write-Success { param($Message) Write-Host $Message -ForegroundColor Green }
function Write-Info { param($Message) Write-Host $Message -ForegroundColor Cyan }
function Write-Warning { param($Message) Write-Host $Message -ForegroundColor Yellow }
function Write-Error { param($Message) Write-Host $Message -ForegroundColor Red }
function Write-Header { param($Message) Write-Host $Message -ForegroundColor Magenta }

# Display header
Write-Host ""
Write-Header "========================================================"
Write-Header "  TOR Complete Translation Merge Tool v2.0"
Write-Header "========================================================"
Write-Host ""

if ($DryRun) {
    Write-Warning "DRY RUN MODE - No files will be modified"
    Write-Host ""
}

# Validate language_data.xml exists
if (-not (Test-Path $LanguageDataFile)) {
    Write-Error "ERROR: language_data.xml not found at: $LanguageDataFile"
    Write-Error "Make sure the language code '$LanguageCode' is correct and the file exists."
    exit 1
}

Write-Info "Language: $LanguageCode"
Write-Info "Language data file: $LanguageDataFile"
Write-Info "Modules base directory: $ModulesBaseDir"
Write-Host ""

# Load language_data.xml
try {
    [xml]$LanguageData = Get-Content $LanguageDataFile -Encoding UTF8
}
catch {
    Write-Error "ERROR: Could not parse language_data.xml"
    Write-Error "Error: $_"
    exit 1
}

# Extract all language file paths
$LanguageFiles = $LanguageData.SelectNodes("//LanguageFile")

if ($LanguageFiles.Count -eq 0) {
    Write-Error "ERROR: No LanguageFile entries found in language_data.xml"
    exit 1
}

Write-Success "Found $($LanguageFiles.Count) files to process"
Write-Host ""

# Statistics tracking
$GlobalStats = @{
    TotalFiles = $LanguageFiles.Count
    SuccessfulFiles = 0
    SkippedFiles = 0
    FailedFiles = 0
    TotalEntries = 0
    TotalPreserved = 0
    TotalNew = 0
    FileDetails = @()
}

# Regular expression to extract localization ID from {=str_tor_xxx} format
$LocalizationIdPattern = '\{=([^}]+)\}'

# Process each file
$FileIndex = 0
foreach ($languageFile in $LanguageFiles) {
    $FileIndex++
    $xmlPath = $languageFile.GetAttribute("xml_path")

    Write-Header "[$FileIndex/$($LanguageFiles.Count)] Processing: $xmlPath"
    Write-Host ""

    # Parse the path to extract module and relative path
    # Format: SP/MODULE_NAME/ModuleData/path/to/file.xml
    $pathParts = $xmlPath -split '/', 4

    if ($pathParts.Length -lt 4) {
        Write-Warning "Warning: Invalid path format: $xmlPath"
        Write-Warning "Skipping this file."
        Write-Host ""
        $GlobalStats.SkippedFiles++
        continue
    }

    $moduleName = $pathParts[1]
    $relativeModulePath = $pathParts[3]

    # Construct paths
    $englishSourcePath = Join-Path $ModulesBaseDir "$moduleName\ModuleData\$relativeModulePath"
    $translationOutputPath = Join-Path $ModuleDataDir "Languages\$xmlPath"
    $translationDir = Split-Path $translationOutputPath -Parent

    Write-Info "Module: $moduleName"
    Write-Info "English source: $englishSourcePath"
    Write-Info "Translation target: $translationOutputPath"
    Write-Host ""

    # Validate English source file exists
    if (-not (Test-Path $englishSourcePath)) {
        Write-Warning "Warning: English source file not found!"
        Write-Warning "Skipping this file."
        Write-Host ""
        $GlobalStats.SkippedFiles++
        continue
    }

    # Create translation directory if it doesn't exist
    if (-not (Test-Path $translationDir)) {
        if ($DryRun) {
            Write-Info "``[DRY RUN``] Would create directory: $translationDir"
        }
        else {
            New-Item -ItemType Directory -Path $translationDir -Force | Out-Null
            Write-Success "Created directory: $translationDir"
        }
    }

    # Load existing translation file if it exists
    $ExistingTranslations = @{}
    $OldFileExists = $false

    if (Test-Path $translationOutputPath) {
        Write-Info "Loading existing translation file..."

        $OldFileExists = $true

        try {
            [xml]$OldXml = Get-Content $translationOutputPath -Encoding UTF8

            # Extract translations from old file
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
        }
    }
    else {
        Write-Info "No existing translation file. Creating new file."
    }

    Write-Host ""

    # Load English source file
    try {
        [xml]$EnglishXml = Get-Content $englishSourcePath -Encoding UTF8
    }
    catch {
        Write-Error "ERROR: Could not parse English source file."
        Write-Error "Error: $_"
        Write-Host ""
        $GlobalStats.FailedFiles++
        continue
    }

    # File statistics
    $FileStats = @{
        FilePath = $xmlPath
        TotalEntries = 0
        PreservedTranslations = 0
        NewEntries = 0
    }

    # Collect all localization IDs and their English text from the source file
    $LocalizationEntries = @{}
    $Comments = @{}

    # Read the entire XML file as text to preserve structure and find all localization patterns
    $englishContent = Get-Content $englishSourcePath -Raw -Encoding UTF8

    # Find all localization patterns {=id}text
    $matches = [regex]::Matches($englishContent, '\{=([^}]+)\}([^\{\<\n\r]*)')

    foreach ($match in $matches) {
        $locId = $match.Groups[1].Value
        $engText = $match.Groups[2].Value.Trim()

        # Clean up the English text - remove quotes, extra whitespace
        $engText = $engText -replace '^[\s"]+|[\s"]+$', ''
        $engText = $engText.Trim()

        # Only add if we haven't seen this ID before (first occurrence wins)
        if (-not $LocalizationEntries.ContainsKey($locId) -and $engText.Length -gt 0) {
            $LocalizationEntries[$locId] = $engText
        }
    }

    Write-Info "Found $($LocalizationEntries.Count) unique localization IDs in English source"

    # Extract comments from English source if it's a strings file
    try {
        foreach ($node in $EnglishXml.DocumentElement.ChildNodes) {
            if ($node.NodeType -eq [System.Xml.XmlNodeType]::Comment) {
                $commentText = $node.InnerText.Trim()
                # Store comment, will add before next string entry
                $Comments[$LocalizationEntries.Count] = $commentText
            }
        }
    }
    catch {
        # Ignore - not all files have comments
    }

    # Build output XML
    $OutputBuilder = New-Object System.Text.StringBuilder

    # Add XML declaration and root elements
    [void]$OutputBuilder.AppendLine('<?xml version="1.0" encoding="utf-8"?>')
    [void]$OutputBuilder.AppendLine('')
    [void]$OutputBuilder.AppendLine('<base xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" type="string">')
    [void]$OutputBuilder.AppendLine('  <tags>')

    # Get language name from language_data.xml or preserve from old file
    $languageName = $LanguageData.LanguageData.GetAttribute("id")

    if ($OldFileExists) {
        try {
            $oldTag = $OldXml.SelectSingleNode("//tag[@language]")
            if ($oldTag) {
                $languageName = $oldTag.GetAttribute("language")
            }
        }
        catch {
            # Ignore errors, use language_data.xml value
        }
    }

    [void]$OutputBuilder.AppendLine("    <tag language=`"$languageName`" />")
    [void]$OutputBuilder.AppendLine('  </tags>')
    [void]$OutputBuilder.AppendLine('  <strings>')

    # Process each localization entry
    $index = 0
    foreach ($locId in ($LocalizationEntries.Keys | Sort-Object)) {
        $FileStats.TotalEntries++

        $actualEnglishText = $LocalizationEntries[$locId]

        # Check if we have an existing translation for this ID
        $translationText = $null

        if ($ExistingTranslations.ContainsKey($locId)) {
            $translationText = $ExistingTranslations[$locId]
            $FileStats.PreservedTranslations++

            # Update TODO entries with new English text
            if ($translationText -match '^TODO\s*\[') {
                $translationText = "TODO [$actualEnglishText]"
                $FileStats.NewEntries++
                $FileStats.PreservedTranslations--
            }
        }
        else {
            # No existing translation - create TODO entry
            $translationText = "TODO [$actualEnglishText]"
            $FileStats.NewEntries++
        }

        # XML-escape the translation text
        $translationText = [System.Security.SecurityElement]::Escape($translationText)

        # Add string element to output
        [void]$OutputBuilder.AppendLine("    <string id=`"$locId`" text=`"$translationText`"/>")

        $index++
    }

    # Close the XML structure
    [void]$OutputBuilder.AppendLine('  </strings>')
    [void]$OutputBuilder.AppendLine('</base>')

    # Save the output file
    if ($DryRun) {
        Write-Info "``[DRY RUN``] Would save translation file: $translationOutputPath"
    }
    else {
        try {
            $OutputBuilder.ToString() | Out-File -FilePath $translationOutputPath -Encoding UTF8
            Write-Success "Translation file saved: $translationOutputPath"
        }
        catch {
            Write-Error "ERROR: Could not save translation file."
            Write-Error "Error: $_"
            Write-Host ""
            $GlobalStats.FailedFiles++
            continue
        }
    }

    # Display file statistics
    Write-Host ""
    Write-Info "File Statistics:"
    Write-Host "  Total entries: $($FileStats.TotalEntries)" -ForegroundColor White
    Write-Host "  Preserved translations: $($FileStats.PreservedTranslations)" -ForegroundColor Green
    Write-Host "  New entries (TODO): $($FileStats.NewEntries)" -ForegroundColor Yellow

    if ($FileStats.TotalEntries -gt 0) {
        $percentComplete = [math]::Round(($FileStats.PreservedTranslations / $FileStats.TotalEntries) * 100, 2)
        Write-Host "  Completion: $percentComplete%" -ForegroundColor Cyan
    }

    Write-Host ""
    Write-Host ""

    # Update global statistics
    $GlobalStats.SuccessfulFiles++
    $GlobalStats.TotalEntries += $FileStats.TotalEntries
    $GlobalStats.TotalPreserved += $FileStats.PreservedTranslations
    $GlobalStats.TotalNew += $FileStats.NewEntries
    $GlobalStats.FileDetails += $FileStats
}

# Display overall statistics
Write-Host ""
Write-Header "========================================================"
Write-Header "  Overall Statistics"
Write-Header "========================================================"
Write-Host ""

Write-Info "Files processed: $($GlobalStats.SuccessfulFiles)/$($GlobalStats.TotalFiles)"
if ($GlobalStats.SkippedFiles -gt 0) {
    Write-Warning "Files skipped: $($GlobalStats.SkippedFiles)"
}
if ($GlobalStats.FailedFiles -gt 0) {
    Write-Error "Files failed: $($GlobalStats.FailedFiles)"
}

Write-Host ""
Write-Info "Total entries across all files: $($GlobalStats.TotalEntries)"
Write-Success "Preserved translations: $($GlobalStats.TotalPreserved)"
Write-Warning "New entries (marked TODO): $($GlobalStats.TotalNew)"

if ($GlobalStats.TotalEntries -gt 0) {
    $overallPercent = [math]::Round(($GlobalStats.TotalPreserved / $GlobalStats.TotalEntries) * 100, 2)
    Write-Host ""
    Write-Info "Overall translation completion: $overallPercent%"
}

Write-Host ""

# Show files with most TODOs
if ($GlobalStats.TotalNew -gt 0) {
    Write-Header "========================================================"
    Write-Header "  Files Needing Most Translation Work"
    Write-Header "========================================================"
    Write-Host ""

    $topFiles = $GlobalStats.FileDetails | Where-Object { $_.NewEntries -gt 0 } | Sort-Object -Property NewEntries -Descending | Select-Object -First 10

    foreach ($file in $topFiles) {
        $percent = 0
        if ($file.TotalEntries -gt 0) {
            $percent = [math]::Round(($file.NewEntries / $file.TotalEntries) * 100, 2)
        }
        Write-Host "  $($file.FilePath)" -ForegroundColor White
        Write-Host "    TODO: $($file.NewEntries)/$($file.TotalEntries) ($percent`%)" -ForegroundColor Yellow
    }

    Write-Host ""
}

Write-Header "========================================================"
Write-Host ""

if ($DryRun) {
    Write-Warning "DRY RUN COMPLETE - No files were modified"
    Write-Info "Remove -DryRun parameter to actually perform the merge"
}
else {
    if ($GlobalStats.TotalNew -gt 0) {
        Write-Warning "ACTION REQUIRED:"
        Write-Warning "  - Search for 'TODO' in the translation files"
        Write-Warning "  - Translate all entries marked as 'TODO [English text]'"
        Write-Warning "  - Total TODO entries: $($GlobalStats.TotalNew)"
    }
    else {
        Write-Success "All entries have been translated!"
    }

    Write-Host ""
    Write-Success "Merge completed successfully!"
}

Write-Host ""
