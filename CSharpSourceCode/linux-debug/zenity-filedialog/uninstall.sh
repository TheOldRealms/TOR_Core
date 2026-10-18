#!/bin/bash
# Revert the zenity-backed comdlg32.dll replacement — restore whatever
# Proton originally had there (usually a symlink to Proton's own comdlg32).

set -euo pipefail

APPID=1393600
TARGET="$HOME/.steam/steam/steamapps/compatdata/$APPID/pfx/drive_c/windows/system32/comdlg32.dll"
BACKUP="$TARGET.proton-original"

log() { printf '[uninstall-zenity-filedialog] %s\n' "$*"; }

if [ ! -e "$BACKUP" ]; then
    log "no backup at $BACKUP — nothing to restore"
    log "if the installer never ran, there's nothing to undo"
    exit 0
fi

rm -f "$TARGET"
cp -a "$BACKUP" "$TARGET"
rm -f "$BACKUP"
log "restored original comdlg32.dll (was: $(file -b "$TARGET"))"
log "You can also remove WINEDLLOVERRIDES=\"comdlg32=n,b\" from the"
log "Modding Kit's Steam launch options now."
