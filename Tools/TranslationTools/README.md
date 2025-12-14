# TOR_Core Translation Tools

This folder contains tools to help translators manage and update translation files for The Old Realms mod.

## Quick Start

**Easiest method for Windows users:**
1. Double-click `merge_translation.bat`
2. Enter your language code (SP, FR, DE, etc.)
3. Press Enter
4. Done!

## What's in This Folder

### Scripts
- **`merge_translation.bat`** - Double-click to run (easiest!)
- **`merge_translation.ps1`** - PowerShell script (called by .bat)
- **`merge_all_translations.ps1`** - Advanced: Merge ALL translation files across all modules

### Documentation
- **`QUICKSTART_GUIDE.md`** - Step-by-step instructions
- **`README.md`** - This file

## What These Tools Do

### Basic Tool (merge_translation.ps1)
The basic translation merge tool:
- ✅ Updates your translation file when the English text changes
- ✅ Preserves all your existing translations
- ✅ Adds new entries as "TODO [English text]"
- ✅ Creates automatic backups
- ✅ Shows statistics about your translation progress

### Advanced Tool (merge_all_translations.ps1)
The advanced merge tool processes ALL translation files across ALL modules (TOR_Core, TOR_Armory, TOR_Environment):

**Features:**
- ✅ Reads language_data.xml to find all translation files
- ✅ Extracts localization IDs from any XML attribute or element
- ✅ Handles different XML structures automatically (strings, items, characters, etc.)
- ✅ Matches existing translations by localization ID
- ✅ Preserves all existing translations
- ✅ Creates TODO entries for missing translations
- ✅ Creates automatic backups before overwriting
- ✅ Creates proper folder structure
- ✅ Generates comprehensive statistics
- ✅ Shows top 10 files needing most translation work
- ✅ Supports dry-run mode to preview changes
- ✅ Color-coded progress output

**Usage:**
```powershell
# Preview changes (dry run - RECOMMENDED FIRST!)
.\merge_all_translations.ps1 -LanguageCode SP -DryRun

# Apply changes to all files
.\merge_all_translations.ps1 -LanguageCode SP

# Examples for other languages
.\merge_all_translations.ps1 -LanguageCode FR -DryRun
.\merge_all_translations.ps1 -LanguageCode DE
```

**What It Does:**
1. Reads `ModuleData/Languages/[LANG]/language_data.xml` to get list of files
2. For each file:
   - Finds English source in correct module (TOR_Core, TOR_Armory, or TOR_Environment)
   - Scans for all `{=localization_id}English Text` patterns
   - Loads existing translations if file exists
   - Matches by localization ID
   - Preserves existing translations
   - Marks missing translations as "TODO [English text]"
   - Creates backup before saving
3. Shows detailed statistics and files needing most work

**Example Output:**
```
Found 39 files to process
Total entries across all files: 15094
Preserved translations: 6342
New entries (marked TODO): 8752
Overall translation completion: 42.02%

Files Needing Most Translation Work:
  SP/TOR_Armory/ModuleData/xslt_changes.xml
    TODO: 1122/1167 (96.14%)
  SP/TOR_Core/ModuleData/tor_strings.xml
    TODO: 3162/3327 (95.04%)
```

## Language Codes

Common language codes:
- `SP` - Spanish (Español)
- `FR` - French (Français)
- `DE` - German (Deutsch)
- `IT` - Italian (Italiano)
- `RU` - Russian (Русский)
- `PT` - Portuguese (Português)
- `PL` - Polish (Polski)
- `TR` - Turkish (Türkçe)
- `CN` - Chinese (Simplified)
- `JP` - Japanese (日本語)
- `KR` - Korean (한국어)

## Need Help?

1. Check `QUICKSTART_GUIDE.md` for detailed instructions
2. Contact the TOR Mod Team if you need assistance

## File Structure

```
TOR_Core/
├── Tools/
│   └── TranslationTools/           (You are here)
│       ├── merge_translation.bat   ← Double-click this!
│       ├── merge_translation.ps1
│       ├── README.md
│       └── QUICKSTART_GUIDE.md
└── ModuleData/
    ├── tor_strings.xml             (English source)
    └── Languages/
        ├── SP/TOR_Core/tor_strings.xml  (Spanish)
        ├── FR/TOR_Core/tor_strings.xml  (French)
        └── ...
```

Happy translating! 🌍
