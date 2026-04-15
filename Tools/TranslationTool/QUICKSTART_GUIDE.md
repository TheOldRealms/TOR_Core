# Translation Merge Tool - Quick Start Guide

## For Windows Users (Most Common)

### Easiest Method: Double-Click the .bat File
1. Navigate to: `TOR_Core\Tools\TranslationTool\`
2. Double-click `merge_translation.bat`
3. Enter your language code (e.g., SP, FR, DE) when prompted
4. Press Enter and done!

### Alternative: PowerShell Method
1. Press `Windows Key + R`
2. Type `powershell` and press Enter
3. Navigate to the TranslationTool folder:
   ```powershell
   cd "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\TOR_Core\Tools\TranslationTool"
   ```
4. Run the script:
```powershell
.\merge_translation.ps1 SP
```
Replace `SP` with your language code:
- `SP` = Spanish
- `FR` = French
- `DE` = German
- `IT` = Italian
- `RU` = Russian
- `PL` = Polish
- `TR` = Turkish
- `PT` = Portuguese
- `CN` = Chinese
- `JP` = Japanese
- `KR` = Korean

### Step 3: If You Get an Error
If you see "execution policy" error, run this command first:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```
Then try Step 2 again.

### Step 4: Translate TODO Entries
1. Open the file: `Languages\<YOUR_LANG>\TOR_Core\tor_strings.xml`
2. Search for "TODO"
3. Replace each "TODO [English text]" with your translation
4. Example:
   ```xml
   <!-- Before -->
   <string id="str_tor_skill_faith" text="TODO Faith"/>

   <!-- After (Spanish) -->
   <string id="str_tor_skill_faith" text="Fe"/>
   ```

### Step 5: Update Language Name (First Time Only)
In the same file, find this line near the top:
```xml
<tag language="Language Name (SP)" />
```

Change it to your language's proper name:
```xml
<!-- Spanish -->
<tag language="Español (LA)" />

<!-- French -->
<tag language="Français" />

<!-- German -->
<tag language="Deutsch" />
```

## For Linux/Mac Users

### Step 1: Install xmllint (if not installed)
```bash
# Ubuntu/Debian
sudo apt-get install libxml2-utils

# Fedora/RHEL
sudo dnf install libxml2

# macOS
brew install libxml2
```

### Step 2: Navigate and Run
```bash
cd "path/to/Mount & Blade II Bannerlord/Modules/TOR_Core/Tools/TranslationTool"
./merge_translation.sh SP
```
Replace `SP` with your language code.

### Step 3: Follow Steps 4-5 from Windows Guide

## What Happens When You Run the Script?

### First Time (No existing translation):
```
========================================
  TOR Translation Merge Tool v1.0
========================================

Source English file: tor_strings.xml
Target language: SP

No existing translation file found. Creating new translation file.

Processing English source file...

Translation file saved: Languages/SP/TOR_Core/tor_strings.xml

========================================
  Merge Statistics
========================================

Total entries in English file: 3452
Preserved translations: 0
New entries (marked TODO): 3452
```

**What to do:** Translate all 3452 entries marked with "TODO"

### Subsequent Runs (After English update):
```
========================================
  TOR Translation Merge Tool v1.0
========================================

Source English file: tor_strings.xml
Target language: SP

Loading existing translation file...
Backup created: tor_strings_backup_20251214_143022.xml

Loaded 3200 existing translations

Processing English source file...

Translation file saved: Languages/SP/TOR_Core/tor_strings.xml

========================================
  Merge Statistics
========================================

Total entries in English file: 3452
Preserved translations: 3200
New entries (marked TODO): 252

Translation completion: 92.70%
```

**What to do:** Only translate the 252 new entries marked with "TODO"

## Important Notes

### Backups Are Automatic
Every time you run the script, it creates a backup with a timestamp:
- `tor_strings_backup_20251214_143022.xml`
- If something goes wrong, restore from the backup

### XML Special Characters
When translating, be careful with these characters:
- Use `&amp;` instead of `&`
- Use `&lt;` instead of `<`
- Use `&gt;` instead of `>`
- Use `&quot;` instead of `"` inside text attributes

**Most text editors will handle this automatically!**

### Placeholders
Keep game placeholders unchanged:
```xml
<!-- Correct -->
<string id="str_tor_career_free_points_label" text="Puntos de carrera libres: {FREE_CAREERPOINTS}"/>

<!-- Wrong - don't translate {FREE_CAREERPOINTS} -->
<string id="str_tor_career_free_points_label" text="Puntos de carrera libres: {PUNTOS_DE_CARRERA_LIBRES}"/>
```

Common placeholders:
- `{a0}`, `{a1}`, `{a2}` - Numbers or values
- `{newline}` - Line break
- `{GOLD_ICON}`, `{INFLUENCE_ICON}` - Icons
- `{SETTLEMENT}`, `{HERO}`, `{PARTY}` - Game entities
- `{REWARD}`, `{AMOUNT}`, `{COUNT}` - Dynamic values

## Troubleshooting

### Problem: "File not found"
**Solution:** Make sure you're in the correct directory. Use `cd` to navigate to the TranslationTool folder.

### Problem: "Cannot load existing translation"
**Solution:** Your translation file might have XML errors. Restore from backup and try again.

### Problem: Script runs but creates empty file
**Solution:** Make sure the English `tor_strings.xml` file exists and is not corrupted.

### Problem: Game doesn't show my translations
**Solution:**
1. Verify file is at: `ModuleData/Languages/<LANG>/TOR_Core/tor_strings.xml`
2. Check for XML errors (malformed tags, unescaped characters)
3. Make sure language is selected in game settings
4. Restart the game

## Recommended Text Editors

- **Windows:**
  - Notepad++ (free) - https://notepad-plus-plus.org/
  - Visual Studio Code (free) - https://code.visualstudio.com/

- **Mac:**
  - Visual Studio Code (free)
  - TextMate (free)
  - BBEdit (paid, but has free mode)

- **Linux:**
  - Visual Studio Code (free)
  - Kate (free)
  - gedit (free)

**Why use these?** They:
- Have XML syntax highlighting
- Show line numbers
- Can search/replace with "TODO"
- Auto-format XML
- Handle UTF-8 encoding properly

## Getting Help

If you're stuck:

1. Read the full documentation: `TRANSLATION_MERGE_README.md`
2. Check for typos in your command
3. Verify all file paths are correct
4. Look at the backup files if something went wrong
5. Contact the TOR mod team for support

## Tips for Efficient Translation

### Use Search and Replace Wisely
If translating a common word that appears many times:
1. Search for: `text="TODO Faith`
2. Replace with: `text="Fe` (or your translation)
3. Be careful with partial matches!

### Translate in Batches
Group similar entries:
1. First: All skill names
2. Then: All skill descriptions
3. Next: All perk names
4. Finally: Dialogue and UI text

### Test Incrementally
Don't translate everything before testing:
1. Translate 50-100 entries
2. Test in-game
3. Fix any issues
4. Continue translating

### Keep a Glossary
Maintain consistency by documenting your choices:
- "Faith" → "Fe" (not "Fé" or "Creencia")
- "Gunpowder" → "Pólvora" (not "Polvo Negro")
- "Spellcraft" → "Hechicería" (not "Magia")

## Example Workflow

### Initial Translation (First Time)
```
1. Run: .\merge_translation.ps1 SP
2. Open: Languages\SP\TOR_Core\tor_strings.xml
3. Search: "TODO"
4. Translate: First 100 entries
5. Save file
6. Test in game
7. Continue until complete
```

### Update Translation (After English Changes)
```
1. Backup your work (optional, script does this)
2. Run: .\merge_translation.ps1 SP
3. Check statistics: "252 new entries"
4. Open: Languages\SP\TOR_Core\tor_strings.xml
5. Search: "TODO"
6. Translate: Only the 252 new entries
7. Save and test
```

## Success Checklist

Before sharing your translation:
- [ ] All "TODO" entries have been translated
- [ ] Language tag updated with proper name
- [ ] File is valid XML (no syntax errors)
- [ ] Game launches with your language selected
- [ ] Spot-check translations in-game
- [ ] Special characters display correctly
- [ ] Placeholders (like `{a0}`) are preserved
- [ ] No English text leaking through

---

**Ready to start?** Jump to the instructions for your platform above!

**Need more details?** Read the full guide: `TRANSLATION_MERGE_README.md`

**Happy Translating!**
