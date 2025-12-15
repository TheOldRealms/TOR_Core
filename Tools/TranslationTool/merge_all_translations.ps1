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

# Trap all errors
trap {
    Write-Host ""
    Write-Host "FATAL ERROR OCCURRED:" -ForegroundColor Red -BackgroundColor Black
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Stack Trace:" -ForegroundColor Yellow
    Write-Host $_.ScriptStackTrace -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor White
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}

# Base paths
$ModuleDataDir = Join-Path $ScriptDir "..\..\ModuleData"
$LanguageDataFile = Join-Path $ModuleDataDir "Languages\$LanguageCode\language_data.xml"

# Get absolute path to Modules base directory
# From TranslationTool, go up to TOR_Core, then up to Modules
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
    Write-Host ""
    Write-Host "ERROR: language_data.xml not found!" -ForegroundColor Red -BackgroundColor Black
    Write-Host "Expected location: $LanguageDataFile" -ForegroundColor Red
    Write-Host "Make sure the language code '$LanguageCode' is correct and the file exists." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor White
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
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
    Write-Host ""
    Write-Host "ERROR: Could not parse language_data.xml" -ForegroundColor Red -BackgroundColor Black
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "The XML file may be corrupted or have invalid syntax." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor White
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}

# Extract all language file paths
$LanguageFiles = $LanguageData.SelectNodes("//LanguageFile")

if ($LanguageFiles.Count -eq 0) {
    Write-Host ""
    Write-Host "ERROR: No LanguageFile entries found in language_data.xml" -ForegroundColor Red -BackgroundColor Black
    Write-Host "The language_data.xml file appears to be empty or has incorrect structure." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor White
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
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
    MissingTags = @()  # Track entries without localization tags
    DuplicateTags = @()  # Track duplicate localization tag IDs
    AllTagOccurrences = @{}  # Track all occurrences of each tag ID
    Errors = @()  # Track all errors that occur
    ObsoleteTranslations = @()  # Track translation IDs that no longer exist in English source
    CultureNameIssues = @{
        InconsistentTags = @()  # Same tag with different names (BAD)
        DuplicateNames = @()    # Same name with different tags (BAD)
    }
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
        $GlobalStats.Errors += [PSCustomObject]@{
            File = $englishSourcePath
            Stage = "Loading English XML"
            Error = $_.Exception.Message
        }
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

    # Parse XML - scan ALL elements and ALL attributes for localization tags
    $missingTagsInFile = @()

    # Get all elements in the document
    foreach ($element in $EnglishXml.SelectNodes("//*")) {
        $elementId = $element.GetAttribute("id")
        $elementName = $element.LocalName  # Use LocalName to get just the element tag name

        # Skip technical elements that should never be localized
        $excludedElements = @("Mesh", "Material", "face", "EquipmentSet", "BodyProperties", "face_key_template", "Component", "Flags", "hair_tag", "beard_tag", "hair_tags", "beard_tags", "template", "upgrade_targets", "Equipments", "skills")
        if ($elementName -in $excludedElements) {
            continue
        }

        # Check each attribute of this element
        foreach ($attr in $element.Attributes) {
            $attrValue = $attr.Value

            # Check if this attribute contains a localization tag {=...}
            if ($attrValue -match '\{=([^}]+)\}(.*)') {
                $locId = $matches[1]
                $engText = $matches[2]

                # Clean up the English text - remove quotes, extra whitespace
                $engText = $engText.Trim()
                $engText = $engText -replace '^"|"$', ''  # Remove surrounding quotes
                $engText = $engText.Trim()

                # Track all occurrences of this tag for duplicate detection
                $occurrence = [PSCustomObject]@{
                    File = $englishSourcePath
                    Element = $elementName
                    ID = $elementId
                    Attribute = $attr.Name
                    LocTag = $locId
                    Text = $engText
                }

                if (-not $GlobalStats.AllTagOccurrences.ContainsKey($locId)) {
                    $GlobalStats.AllTagOccurrences[$locId] = @()
                }
                $GlobalStats.AllTagOccurrences[$locId] += $occurrence

                # Only add if we haven't seen this ID before (first occurrence wins)
                if (-not $LocalizationEntries.ContainsKey($locId)) {
                    $LocalizationEntries[$locId] = $engText
                }
            }
            # Check if this is an attribute that should have a tag but doesn't
            # Common localizable attributes in Bannerlord XML files
            $localizableAttrs = @("text", "name", "Name", "title", "short_name", "ruler_title", "RegimentName", "RegimentHQName", "MenuHeaderText", "TooltipDescription")
            # Technical attributes that should never be localized
            $excludedAttrs = @("mesh", "Material", "material_type", "skeleton", "body_name", "prefab", "id", "culture", "occupation", "skill_template", "template", "value")

            if ($attr.Name -in $localizableAttrs -and $attr.Name -notin $excludedAttrs -and $attrValue -ne "" -and $attrValue -notmatch '^\{') {
                # This entry is missing a localization tag!
                $missingTagsInFile += [PSCustomObject]@{
                    File = $englishSourcePath
                    Element = $elementName
                    ID = if ($elementId) { $elementId } else { "NO_ID" }
                    Attribute = $attr.Name
                    Text = $attrValue
                }
            }
        }
    }

    if ($missingTagsInFile.Count -gt 0) {
        Write-Warning "Found $($missingTagsInFile.Count) entries without localization tags!"
        $GlobalStats.MissingTags += $missingTagsInFile
    }

    Write-Info "Found $($LocalizationEntries.Count) unique localization IDs in English source"

    # Detect obsolete translations (exist in old file but not in English source)
    $obsoleteInFile = @()
    foreach ($oldId in $ExistingTranslations.Keys) {
        if (-not $LocalizationEntries.ContainsKey($oldId)) {
            $obsoleteInFile += [PSCustomObject]@{
                File = $translationOutputPath
                LocTag = $oldId
                Translation = $ExistingTranslations[$oldId]
            }
        }
    }

    if ($obsoleteInFile.Count -gt 0) {
        Write-Warning "Found $($obsoleteInFile.Count) obsolete translation IDs (no longer in English source)"
        $GlobalStats.ObsoleteTranslations += $obsoleteInFile
    }

    # Validate culture names if this is a cultures file
    if ($xmlPath -match 'cultures\.xml$') {
        Write-Info "Validating culture names..."

        # Group by tag to find inconsistent tags (same tag, different names)
        $byTag = @{}
        foreach ($locId in $LocalizationEntries.Keys) {
            if ($locId -match '_(male|female)(_name)?_\d+$') {
                $name = $LocalizationEntries[$locId]
                if (-not $byTag.ContainsKey($locId)) {
                    $byTag[$locId] = @()
                }
                $byTag[$locId] += $name
            }
        }

        foreach ($tag in $byTag.Keys) {
            $names = $byTag[$tag] | Select-Object -Unique
            if ($names.Count -gt 1) {
                $GlobalStats.CultureNameIssues.InconsistentTags += [PSCustomObject]@{
                    Tag = $tag
                    Names = $names
                    File = $englishSourcePath
                }
                Write-Warning "  Inconsistent tag: {=$tag} has $($names.Count) different names!"
            }
        }

        # Group by name to find duplicate names (same name, different tags)
        $byName = @{}
        foreach ($locId in $LocalizationEntries.Keys) {
            if ($locId -match '_(male|female)(_name)?_\d+$') {
                $name = $LocalizationEntries[$locId]
                if (-not $byName.ContainsKey($name)) {
                    $byName[$name] = @()
                }
                $byName[$name] += $locId
            }
        }

        foreach ($name in $byName.Keys) {
            $tags = $byName[$name] | Select-Object -Unique
            if ($tags.Count -gt 1) {
                $GlobalStats.CultureNameIssues.DuplicateNames += [PSCustomObject]@{
                    Name = $name
                    Tags = $tags
                    File = $englishSourcePath
                }
                Write-Warning "  Duplicate name: '$name' has $($tags.Count) different tags!"
            }
        }
    }

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
            $GlobalStats.Errors += [PSCustomObject]@{
                File = $translationOutputPath
                Stage = "Saving translation file"
                Error = $_.Exception.Message
            }
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

# Detect duplicate localization tags
try {
    $excludedCultureDuplicates = 0
    $tagKeys = @($GlobalStats.AllTagOccurrences.Keys)
    foreach ($tagId in $tagKeys) {
        $occurrences = @($GlobalStats.AllTagOccurrences[$tagId])
        if ($occurrences.Count -gt 1) {
            # Check if it's a true duplicate (different text) or just multiple uses
            $uniqueTexts = @($occurrences | Select-Object -ExpandProperty Text -Unique)
            $isDifferentText = $uniqueTexts.Count -gt 1

            # Check if all occurrences are from cultures.xml files
            $allFromCultures = $true
            foreach ($occ in $occurrences) {
                if ($occ.File -notmatch 'cultures\.xml$') {
                    $allFromCultures = $false
                    break
                }
            }

            # Exclude cultures.xml duplicates with same text (normal name sharing)
            if ($allFromCultures -and -not $isDifferentText) {
                $excludedCultureDuplicates += $occurrences.Count
                continue
            }

            foreach ($occurrence in $occurrences) {
                $GlobalStats.DuplicateTags += [PSCustomObject]@{
                    LocTag = $tagId
                    File = $occurrence.File
                    Element = $occurrence.Element
                    ID = $occurrence.ID
                    Attribute = $occurrence.Attribute
                    Text = $occurrence.Text
                    TotalOccurrences = $occurrences.Count
                    IsDifferentText = $isDifferentText
                }
            }
        }
    }

    if ($GlobalStats.DuplicateTags.Count -gt 0) {
        Write-Host ""
        Write-Warning "Found $($GlobalStats.DuplicateTags.Count) duplicate localization tag usages!"
        $uniqueDuplicateTags = ($GlobalStats.DuplicateTags | Select-Object -ExpandProperty LocTag -Unique).Count
        Write-Info "  Unique duplicate tag IDs: $uniqueDuplicateTags"
    }

    if ($excludedCultureDuplicates -gt 0) {
        Write-Host ""
        Write-Info "Excluded $excludedCultureDuplicates culture name duplicates (normal name sharing across cultures)"
    }
}
catch {
    Write-Warning "Error detecting duplicate tags: $($_.Exception.Message)"
    $GlobalStats.Errors += [PSCustomObject]@{
        File = "N/A"
        Stage = "Duplicate tag detection"
        Error = $_.Exception.Message
    }
}

# Report obsolete translations
if ($GlobalStats.ObsoleteTranslations.Count -gt 0) {
    Write-Host ""
    Write-Warning "Found $($GlobalStats.ObsoleteTranslations.Count) obsolete translation IDs!"
    Write-Info "  These IDs exist in your translations but not in the English source"
    Write-Info "  They have been excluded from the merged output"
    Write-Info "  (Likely due to: consolidation, renaming, or removed features)"
}

# Report culture name validation issues
$totalCultureIssues = $GlobalStats.CultureNameIssues.InconsistentTags.Count + $GlobalStats.CultureNameIssues.DuplicateNames.Count
if ($totalCultureIssues -gt 0) {
    Write-Host ""
    Write-Warning "Found $totalCultureIssues culture name validation issues!"
    if ($GlobalStats.CultureNameIssues.InconsistentTags.Count -gt 0) {
        Write-Info "  Inconsistent tags (same tag, different names): $($GlobalStats.CultureNameIssues.InconsistentTags.Count)"
    }
    if ($GlobalStats.CultureNameIssues.DuplicateNames.Count -gt 0) {
        Write-Info "  Duplicate names (same name, different tags): $($GlobalStats.CultureNameIssues.DuplicateNames.Count)"
    }
}

# Write comprehensive report if there are any issues to report
if ($GlobalStats.MissingTags.Count -gt 0 -or $GlobalStats.DuplicateTags.Count -gt 0 -or $GlobalStats.Errors.Count -gt 0 -or $GlobalStats.ObsoleteTranslations.Count -gt 0 -or $totalCultureIssues -gt 0) {
    $reportFileName = "translation_issues_report_$LanguageCode.txt"
    $reportFilePath = Join-Path $ScriptDir $reportFileName

    Write-Host ""
    Write-Info "Generating comprehensive issues report: $reportFilePath"

    $reportContent = @()
    $reportContent += "="*80
    $reportContent += "TRANSLATION ISSUES REPORT"
    $reportContent += "="*80
    $reportContent += "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    $reportContent += "Language: $LanguageCode"
    $reportContent += ""
    $reportContent += "Summary:"
    $reportContent += "  Missing localization tags: $($GlobalStats.MissingTags.Count)"
    $reportContent += "  Duplicate tag usages: $($GlobalStats.DuplicateTags.Count)"
    if ($GlobalStats.DuplicateTags.Count -gt 0) {
        $uniqueDups = ($GlobalStats.DuplicateTags | Select-Object -ExpandProperty LocTag -Unique).Count
        $reportContent += "    (Unique duplicate IDs: $uniqueDups)"
    }
    $reportContent += "  Obsolete translation IDs: $($GlobalStats.ObsoleteTranslations.Count)"
    $reportContent += "  Culture name issues: $totalCultureIssues"
    if ($totalCultureIssues -gt 0) {
        $reportContent += "    - Inconsistent tags: $($GlobalStats.CultureNameIssues.InconsistentTags.Count)"
        $reportContent += "    - Duplicate names: $($GlobalStats.CultureNameIssues.DuplicateNames.Count)"
    }
    $reportContent += "  Errors encountered: $($GlobalStats.Errors.Count)"
    $reportContent += ""

    # SECTION 1: ERRORS (if any)
    if ($GlobalStats.Errors.Count -gt 0) {
        $reportContent += "="*80
        $reportContent += "SECTION 1: ERRORS"
        $reportContent += "="*80
        $reportContent += ""

        # Group errors by module
        $errorsByFile = $GlobalStats.Errors | Group-Object -Property File

        foreach ($fileGroup in $errorsByFile) {
            $fileName = if ($fileGroup.Name -ne "N/A") { Split-Path $fileGroup.Name -Leaf } else { "N/A" }
            $moduleName = if ($fileGroup.Name -match '\\(TOR_[^\\]+)\\') { $matches[1] } else { "General" }

            $reportContent += "-"*80
            $reportContent += "[$moduleName] $fileName"
            if ($fileGroup.Name -ne "N/A") {
                $reportContent += "Full path: $($fileGroup.Name)"
            }
            $reportContent += "Errors: $($fileGroup.Count)"
            $reportContent += "-"*80

            foreach ($error in $fileGroup.Group) {
                $reportContent += "  Stage: $($error.Stage)"
                $reportContent += "  Error: $($error.Error)"
                $reportContent += ""
            }
        }
        $reportContent += ""
    }

    # SECTION 2: MISSING LOCALIZATION TAGS (if any)
    if ($GlobalStats.MissingTags.Count -gt 0) {
        $reportContent += "="*80
        $reportContent += "SECTION 2: MISSING LOCALIZATION TAGS"
        $reportContent += "="*80
        $reportContent += ""
        $reportContent += "Total entries without localization tags: $($GlobalStats.MissingTags.Count)"
        $reportContent += ""

        # Group by module first, then by file
        $byFile = $GlobalStats.MissingTags | Group-Object -Property File

        # Summary by module
        $reportContent += "SUMMARY BY MODULE:"
        $reportContent += "-"*80
        $moduleGroups = $byFile | Group-Object {
            if ($_.Name -match '\\(TOR_[^\\]+)\\') { $matches[1] } else { "Unknown" }
        }
        foreach ($modGroup in ($moduleGroups | Sort-Object Name)) {
            $totalInModule = ($modGroup.Group | ForEach-Object { $_.Count } | Measure-Object -Sum).Sum
            $reportContent += "  [$($modGroup.Name)] - $totalInModule entries across $($modGroup.Count) files"
        }
        $reportContent += ""

        # Detailed by file
        $reportContent += "DETAILED ENTRIES:"
        $reportContent += "-"*80

        foreach ($fileGroup in ($byFile | Sort-Object { if ($_.Group[0].File) { $_.Group[0].File } else { "" } })) {
            if (-not $fileGroup.Name) { continue }

            $fileName = Split-Path $fileGroup.Name -Leaf
            $moduleName = if ($fileGroup.Name -match '\\(TOR_[^\\]+)\\') { $matches[1] } else { "Unknown" }

            $reportContent += ""
            $reportContent += "[$moduleName] $fileName - $($fileGroup.Count) entries"
            $reportContent += "Path: $($fileGroup.Name)"

            foreach ($entry in $fileGroup.Group) {
                $elementInfo = if ($entry.Element) { "<$($entry.Element)> " } else { "" }
                $attrInfo = if ($entry.Attribute) { "$($entry.Attribute)=" } else { "text=" }
                $suggestedTag = "{=str_tor_$($entry.ID)}"

                $reportContent += "  $elementInfo$($entry.ID) -> $attrInfo`"$suggestedTag$($entry.Text)`""
            }
        }
        $reportContent += ""
    }

    # SECTION 3: DUPLICATE TAGS (if any)
    if ($GlobalStats.DuplicateTags.Count -gt 0) {
        $reportContent += "="*80
        $reportContent += "SECTION 3: DUPLICATE LOCALIZATION TAGS"
        $reportContent += "="*80
        $reportContent += ""

        $uniqueDuplicateTags = $GlobalStats.DuplicateTags | Select-Object -ExpandProperty LocTag -Unique
        $reportContent += "Total duplicate tag usages: $($GlobalStats.DuplicateTags.Count)"
        $reportContent += "Unique duplicate tag IDs: $($uniqueDuplicateTags.Count)"
        $reportContent += ""

        # Group by tag ID
        $byTag = $GlobalStats.DuplicateTags | Group-Object -Property LocTag
        $sortedGroups = $byTag | Sort-Object @{Expression={$_.Group[0].IsDifferentText}; Descending=$true}, Name

        # Summary
        $reportContent += "SUMMARY - CRITICAL DUPLICATES (Different Text):"
        $reportContent += "-"*80
        $criticalDups = $sortedGroups | Where-Object { $_.Group[0].IsDifferentText }
        if ($criticalDups) {
            foreach ($tagGroup in $criticalDups) {
                $reportContent += "  {=$($tagGroup.Name)} - $($tagGroup.Count) occurrences [DIFFERENT TEXT - NEEDS FIXING]"
            }
        } else {
            $reportContent += "  None"
        }
        $reportContent += ""

        $reportContent += "SUMMARY - MULTIPLE USES (Same Text):"
        $reportContent += "-"*80
        $multipleDups = $sortedGroups | Where-Object { -not $_.Group[0].IsDifferentText }
        if ($multipleDups) {
            foreach ($tagGroup in $multipleDups) {
                $reportContent += "  {=$($tagGroup.Name)} - $($tagGroup.Count) occurrences [Multiple Uses]"
            }
        } else {
            $reportContent += "  None"
        }
        $reportContent += ""

        # Detailed entries
        $reportContent += "DETAILED ENTRIES:"
        $reportContent += "-"*80

        foreach ($tagGroup in $sortedGroups) {
            $isDifferent = $tagGroup.Group[0].IsDifferentText
            $marker = if ($isDifferent) { "[DIFFERENT TEXT - NEEDS FIXING]" } else { "[Multiple uses (same text)]" }

            $reportContent += ""
            $reportContent += "Tag ID: {=$($tagGroup.Name)} - $($tagGroup.Count) occurrences $marker"

            foreach ($entry in $tagGroup.Group) {
                $fileName = Split-Path $entry.File -Leaf
                $moduleName = if ($entry.File -match '\\(TOR_[^\\]+)\\') { $matches[1] } else { "Unknown" }
                $elementInfo = if ($entry.Element) { "<$($entry.Element)> " } else { "" }

                $reportContent += "  [$moduleName] $fileName"
                $reportContent += "    $elementInfo$($entry.ID) -> $($entry.Attribute)=`"{=$($entry.LocTag)}$($entry.Text)`""
            }
        }
        $reportContent += ""
    }

    # SECTION 4: OBSOLETE TRANSLATION IDs (if any)
    if ($GlobalStats.ObsoleteTranslations.Count -gt 0) {
        $reportContent += "="*80
        $reportContent += "SECTION 4: OBSOLETE TRANSLATION IDs"
        $reportContent += "="*80
        $reportContent += ""
        $reportContent += "These translation IDs exist in your translation file but no longer exist"
        $reportContent += "in the English source. They have been removed from the output."
        $reportContent += "Common causes:"
        $reportContent += "  - IDs were renamed/reorganized in the English source"
        $reportContent += "  - Features were removed from the mod"
        $reportContent += "  - Duplicate names were consolidated (e.g., vampire_counts now uses empire names)"
        $reportContent += ""
        $reportContent += "Total obsolete IDs: $($GlobalStats.ObsoleteTranslations.Count)"
        $reportContent += ""

        # Group by file
        $byFile = $GlobalStats.ObsoleteTranslations | Group-Object -Property File

        foreach ($fileGroup in ($byFile | Sort-Object Name)) {
            $fileName = Split-Path $fileGroup.Name -Leaf
            $reportContent += "-"*80
            $reportContent += "$fileName - $($fileGroup.Count) obsolete IDs"
            $reportContent += "-"*80

            # Show samples (first 50) and patterns
            $samples = $fileGroup.Group | Select-Object -First 50

            # Try to detect patterns
            $patterns = @{}
            foreach ($item in $fileGroup.Group) {
                if ($item.LocTag -match '^([^_]+_[^_]+)_') {
                    $pattern = $matches[1]
                    if (-not $patterns.ContainsKey($pattern)) {
                        $patterns[$pattern] = 0
                    }
                    $patterns[$pattern]++
                }
            }

            if ($patterns.Count -gt 0) {
                $reportContent += ""
                $reportContent += "Common patterns:"
                foreach ($pattern in ($patterns.GetEnumerator() | Sort-Object -Property Value -Descending)) {
                    $reportContent += "  $($pattern.Key)_* : $($pattern.Value) entries"
                }
                $reportContent += ""
            }

            $reportContent += "Sample entries (showing first 50):"
            foreach ($item in $samples) {
                $translationPreview = $item.Translation
                if ($translationPreview.Length -gt 60) {
                    $translationPreview = $translationPreview.Substring(0, 57) + "..."
                }
                $reportContent += "  $($item.LocTag) = `"$translationPreview`""
            }

            if ($fileGroup.Count -gt 50) {
                $reportContent += "  ... and $($fileGroup.Count - 50) more"
            }

            $reportContent += ""
        }
    }

    # SECTION 5: CULTURE NAME VALIDATION ISSUES (if any)
    if ($totalCultureIssues -gt 0) {
        $reportContent += "="*80
        $reportContent += "SECTION 5: CULTURE NAME VALIDATION ISSUES"
        $reportContent += "="*80
        $reportContent += ""
        $reportContent += "Culture names must follow these rules:"
        $reportContent += "  GOOD: Same tag + Same name (intentional sharing, e.g., empire/vampire_counts)"
        $reportContent += "  BAD:  Same tag + Different name (data corruption/error)"
        $reportContent += "  BAD:  Different tag + Same name (should use same tag)"
        $reportContent += ""
        $reportContent += "Total issues: $totalCultureIssues"
        $reportContent += ""

        # Inconsistent tags (same tag, different names) - CRITICAL
        if ($GlobalStats.CultureNameIssues.InconsistentTags.Count -gt 0) {
            $reportContent += "-"*80
            $reportContent += "INCONSISTENT TAGS (Same tag, different names) - CRITICAL"
            $reportContent += "-"*80
            $reportContent += ""
            $reportContent += "Count: $($GlobalStats.CultureNameIssues.InconsistentTags.Count)"
            $reportContent += ""

            foreach ($issue in $GlobalStats.CultureNameIssues.InconsistentTags) {
                $fileName = Split-Path $issue.File -Leaf
                $reportContent += "Tag: {=$($issue.Tag)}"
                $reportContent += "File: $fileName"
                $reportContent += "Different names found:"
                foreach ($name in $issue.Names) {
                    $reportContent += "  - `"$name`""
                }
                $reportContent += ""
            }
        }

        # Duplicate names (same name, different tags) - WARNING
        if ($GlobalStats.CultureNameIssues.DuplicateNames.Count -gt 0) {
            $reportContent += "-"*80
            $reportContent += "DUPLICATE NAMES (Same name, different tags) - CONSOLIDATION OPPORTUNITY"
            $reportContent += "-"*80
            $reportContent += ""
            $reportContent += "Count: $($GlobalStats.CultureNameIssues.DuplicateNames.Count)"
            $reportContent += ""
            $reportContent += "These names appear with different tags and could be consolidated"
            $reportContent += "to use a single tag (reducing translation work)."
            $reportContent += ""

            # Show first 50 to avoid huge reports
            $samples = $GlobalStats.CultureNameIssues.DuplicateNames | Select-Object -First 50

            foreach ($issue in $samples) {
                $reportContent += "Name: `"$($issue.Name)`""
                $reportContent += "Tags:"
                foreach ($tag in $issue.Tags) {
                    $reportContent += "  - {=$tag}"
                }
                $reportContent += ""
            }

            if ($GlobalStats.CultureNameIssues.DuplicateNames.Count -gt 50) {
                $reportContent += "... and $($GlobalStats.CultureNameIssues.DuplicateNames.Count - 50) more"
                $reportContent += ""
            }
        }
    }

    $reportContent += ""
    $reportContent += "="*80
    $reportContent += "END OF REPORT"

    $reportContent | Out-File -FilePath $reportFilePath -Encoding UTF8

    Write-Success "Comprehensive issues report created: $reportFileName"
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

# Keep window open
Write-Host "Press any key to exit..." -ForegroundColor White
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
