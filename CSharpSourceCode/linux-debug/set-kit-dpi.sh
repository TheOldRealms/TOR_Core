#!/bin/bash
# Set the Wine DPI (LogPixels) for the Modding Kit's Proton prefix.
# Wine's default is 96 DPI, which produces tiny text on high-DPI monitors.
# Bumping LogPixels scales the whole UI (fonts, dialogs, chrome) uniformly.
#
# Common values:
#   96  = 1.00x  (Wine default, often too small on 4K+)
#   120 = 1.25x
#   144 = 1.50x  (comfortable on most 4K monitors)
#   168 = 1.75x
#   192 = 2.00x  (big; for 5K+ or if you really want chunky text)
#
# Writes to two locations because modern Wine honors both:
#   [Control Panel\Desktop]          LogPixels  (canonical)
#   [Software\Wine\Fonts]            LogPixels  (legacy cache)
#
# IMPORTANT: Modding Kit (editor AND launcher) must be fully closed before
# running, or Wine's wineserver will overwrite the edit when it exits.
#
# Usage:
#   ./set-kit-dpi.sh 144
#   ./set-kit-dpi.sh         # defaults to 144
#   ./set-kit-dpi.sh --revert # restore pre-edit backup

set -euo pipefail

APPID=1393600
PFX="$HOME/.steam/steam/steamapps/compatdata/$APPID/pfx"
REG="$PFX/user.reg"
BACKUP="$REG.pre-dpi-bump"

log() { printf '[set-kit-dpi] %s\n' "$*"; }

if [ ! -f "$REG" ]; then
    log "ERROR: $REG not found. Launch Modding Kit via Steam at least once to create the prefix."
    exit 1
fi

# Refuse to run if the Kit's wineserver is alive — our edit would be lost.
if ps -eo pid,comm,args 2>/dev/null | awk -v appid="$APPID" '/wineserver/ && $0 ~ appid' | grep -q .; then
    log "ERROR: wineserver for the Modding Kit prefix is running."
    log "       Fully close the Kit (editor + launcher window) and retry."
    exit 1
fi
# Also check for the launcher chain specifically.
if ps -eo pid,args 2>/dev/null | awk -v appid="$APPID" '/SteamLaunch.*AppId=/ && $0 ~ "AppId="appid' | grep -q .; then
    log "ERROR: Modding Kit launch chain still running (editor or launcher)."
    log "       Close the launcher window too and retry."
    exit 1
fi

if [ "${1:-}" = "--revert" ]; then
    if [ ! -f "$BACKUP" ]; then
        log "no backup at $BACKUP to revert to"
        exit 1
    fi
    cp "$BACKUP" "$REG"
    log "restored $REG from backup"
    exit 0
fi

DPI="${1:-144}"
if ! [[ $DPI =~ ^[0-9]+$ ]] || [ "$DPI" -lt 72 ] || [ "$DPI" -gt 300 ]; then
    log "ERROR: DPI must be an integer in [72, 300] (got '$DPI')"
    exit 2
fi

# Format DPI as 8-char lowercase hex for the registry
HEX=$(printf '%08x' "$DPI")

# One-shot backup so --revert works
[ -f "$BACKUP" ] || cp "$REG" "$BACKUP"

# Patch [Software\Wine\Fonts] -> LogPixels (the one Wine always writes)
# Match: "LogPixels"=dword:XXXXXXXX
if grep -q '^"LogPixels"=dword:' "$REG"; then
    sed -i "s|^\"LogPixels\"=dword:[0-9a-f]\{8\}|\"LogPixels\"=dword:$HEX|g" "$REG"
    log "updated existing LogPixels entries -> dword:$HEX"
else
    log "WARN: no existing LogPixels entry to update"
fi

# Ensure [Control Panel\Desktop] has LogPixels too. Insert alphabetically
# after IconTitleWrap if the key isn't there yet. (If a user already
# bumped it via winecfg, the sed above handled it.)
if ! awk '/^\[Control Panel\\\\Desktop\]/,/^\[/' "$REG" | grep -q '^"LogPixels"='; then
    # Insert LogPixels after IconTitleWrap in the Desktop section
    sed -i '/^\[Control Panel\\\\Desktop\]/,/^\[/ {
        /^"IconTitleWrap"=/a\
"LogPixels"=dword:'"$HEX"'
    }' "$REG"
    log "inserted LogPixels=dword:$HEX in [Control Panel\\Desktop]"
fi

echo ""
log "DPI set to $DPI (0x$HEX). Launch the Modding Kit — fonts should be larger."
log "To revert: $0 --revert"
