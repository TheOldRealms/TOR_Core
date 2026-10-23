#!/bin/bash
# Regenerate the sprite atlas for a module on Linux via Wine.
#
# The Modding Kit ships `TaleWorlds.TwoDimension.SpriteSheetGenerator.exe`
# under `bin/Win64_Shipping_wEditor/`. It's a tiny CLI around
# `TaleWorlds.TwoDimension.SpriteSheetGenerator.Library.dll`, which uses
# `StbSharp` (pure-managed) — no `System.Drawing` / GDI dependencies, so it
# runs cross-platform in theory.
#
# Two Linux-specific gotchas forced the Wine wrapper + symlink dance:
#
#   1. The library's paths use literal `"\\"` Windows separators (grep the
#      decompile: `_moduleFolderPath + "SpriteParts\\"`). Mono on Linux does
#      NOT translate these, so `Directory.Exists(".../SpriteParts\\")` is
#      false and the generator finds zero files, reports success, writes
#      nothing. Running the EXE via Wine honours backslashes natively.
#
#   2. In `SingleModule` mode the generator writes `<DirName>SpriteData.xml`
#      where `<DirName>` is the last component of the input path. Pointing
#      straight at `<module>/GUI` yields `GUISpriteData.xml` — wrong. Workaround:
#      use `AllAvailableModules` against a tmp parent dir that contains ONLY
#      a symlink to the module we want packed. The symlink's filename
#      becomes `<ModuleName>`, so the output XML name matches what the
#      module's `SubModule.xml` expects to load.
#
# Side effect of each run: the generator wipes and re-packs ALL atlas sheets
# in `<module>/AssetSources/GauntletUI/`, so even unchanged sheets show as
# modified in git. Expected; TOR_Armory already accepts this.
#
# Usage:
#   ./regenerate-sprites.sh                # packs TOR_Armory (default)
#   ./regenerate-sprites.sh TOR_Core       # packs a different module
#
# Hooking into Steam launch options for the Modding Kit (AppID 1393600):
#
#   "/absolute/path/to/CSharpSourceCode/linux-debug/regenerate-sprites.sh" && %command%
#
# This repacks sprites before every Kit launch — useful during active icon
# work. Omit the hook once art is stable; running manually when PNGs change
# is cheaper than the ~1s cost per Kit boot.

set -euo pipefail

MODULE="${1:-TOR_Armory}"
GAMEBIN="$HOME/.local/share/Steam/steamapps/common/Mount & Blade II Bannerlord/bin/Win64_Shipping_wEditor"
MODULES_DIR="$HOME/.local/share/Steam/steamapps/common/Mount & Blade II Bannerlord/Modules"
MODULE_PATH="$MODULES_DIR/$MODULE"
TMP_PARENT="/tmp/tor-spritegen-$MODULE"

log() { printf '[regenerate-sprites] %s\n' "$*"; }

if [ ! -d "$MODULE_PATH" ]; then
    log "ERROR: module not found at $MODULE_PATH"
    exit 1
fi

GENERATOR="$GAMEBIN/TaleWorlds.TwoDimension.SpriteSheetGenerator.exe"
if [ ! -f "$GENERATOR" ]; then
    log "ERROR: generator not found at $GENERATOR"
    log "       (install the Bannerlord Modding Kit via Steam, AppID 1393600)"
    exit 1
fi

if ! command -v wine >/dev/null 2>&1; then
    log "ERROR: 'wine' not on PATH. install wine or wine-staging."
    exit 1
fi

# Set up tmp parent with symlink → generator sees it as a single module named
# $MODULE (not "GUI" or similar) so the output XML lands at the right filename.
rm -rf "$TMP_PARENT"
mkdir -p "$TMP_PARENT"
ln -s "$MODULE_PATH" "$TMP_PARENT/$MODULE"
trap 'rm -rf "$TMP_PARENT"' EXIT

# Convert the Linux tmp path to a Wine Z:\ path.
WINE_SRC="Z:$(echo "$TMP_PARENT" | tr '/' '\\')"

log "packing $MODULE from $MODULE_PATH"
cd "$GAMEBIN"
# Pipe empty input so the generator's final `Console.ReadKey()` doesn't block.
WINEDEBUG=-all echo "" | wine "$(basename "$GENERATOR")" \
    "SourceDirectory=$WINE_SRC" \
    "CollectionType=AllAvailableModules" \
    "OutputType=Engine" \
    2>&1 \
    | grep -vE '^[0-9a-f]+:fixme:|wine-staging|Vulkan|urlmon:|file:NtLockFile|ntdll:NtQuery|wineusb:|msi:' \
    || true

# Post-processing: re-inject <AlwaysLoad /> on categories that have it in
# Config.xml. The generator's LoadConfig() silently fails on Linux (path
# resolution through the Wine symlink), so _configs stays empty and every
# category's AlwaysLoad flag gets dropped from the sprite data XML. Without
# AlwaysLoad on ui_custom_fonts, fonts fall through to lazy-load and all
# text in the HUD/menus misses on first draw. Re-add the flag post-hoc.
SPRITE_DATA="$MODULE_PATH/GUI/${MODULE}SpriteData.xml"
CONFIG_XML="$MODULE_PATH/GUI/SpriteParts/Config.xml"

if [ -f "$SPRITE_DATA" ] && [ -f "$CONFIG_XML" ]; then
    log "re-wiring AlwaysLoad tags from Config.xml"
    python3 - "$CONFIG_XML" "$SPRITE_DATA" <<'PY'
import sys
import xml.etree.ElementTree as ET

config_path, data_path = sys.argv[1], sys.argv[2]

# Config.xml: category Name -> True if it has an <AlwaysLoad /> child.
cfg_tree = ET.parse(config_path)
always_load = {
    c.find("./").text if False else c.attrib.get("Name"): c.find("AlwaysLoad") is not None
    for c in cfg_tree.getroot().findall("SpriteCategory")
}

data_tree = ET.parse(data_path)
inserted = 0
for cat in data_tree.getroot().find("SpriteCategories").findall("SpriteCategory"):
    name_el = cat.find("Name")
    if name_el is None:
        continue
    name = (name_el.text or "").strip()
    if not always_load.get(name, False):
        continue
    if cat.find("AlwaysLoad") is not None:
        continue  # already present; nothing to do
    # Insert <AlwaysLoad /> right after <Name>, matching generator output order.
    name_idx = list(cat).index(name_el)
    cat.insert(name_idx + 1, ET.Element("AlwaysLoad"))
    inserted += 1

data_tree.write(data_path, encoding="UTF-8", xml_declaration=True)
print(f"[regenerate-sprites] re-inserted AlwaysLoad on {inserted} categor{'y' if inserted==1 else 'ies'}")
PY
fi

log "done — atlas at $MODULE_PATH/AssetSources/GauntletUI/, XML at $MODULE_PATH/GUI/${MODULE}SpriteData.xml"
