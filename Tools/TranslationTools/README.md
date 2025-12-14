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

### Documentation
- **`QUICKSTART_GUIDE.md`** - Step-by-step instructions
- **`README.md`** - This file

## What These Tools Do

The translation merge tool:
- ✅ Updates your translation file when the English text changes
- ✅ Preserves all your existing translations
- ✅ Adds new entries as "TODO [English text]"
- ✅ Creates automatic backups
- ✅ Shows statistics about your translation progress

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
