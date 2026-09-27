#!/bin/bash
# Revert everything Tools/linux-debug set up.
#   - Restore the managed launcher (undo the Bannerlord.Native.exe swap)
#   - Restore workshop SubModule.xml files
#   - Uninstall the proxy DLL (via Makefile)
#
# Safe to run repeatedly; each step is idempotent.

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
GAMEBIN="$HOME/.local/share/Steam/steamapps/common/Mount & Blade II Bannerlord/bin/Win64_Shipping_Client"
WSDIR="$HOME/.local/share/Steam/steamapps/workshop/content/261550"

log() { printf '[undo] %s\n' "$*"; }

if pgrep -f "Bannerlord|Launcher\.exe.*/singleplayer" >/dev/null 2>&1; then
    log "WARNING: game is running. Close it before running undo."
    exit 1
fi

# --- Restore managed launcher ------------------------------------------
LAUNCHER_BAK="$GAMEBIN/TaleWorlds.MountAndBlade.Launcher.exe.original"
if [ -f "$LAUNCHER_BAK" ]; then
    log "restoring managed launcher"
    rm -f "$GAMEBIN/TaleWorlds.MountAndBlade.Launcher.exe"
    mv "$LAUNCHER_BAK" "$GAMEBIN/TaleWorlds.MountAndBlade.Launcher.exe"
else
    log "launcher already restored (no .original present)"
fi

# --- Restore workshop SubModule.xml files ------------------------------
for id in 2859188632 3025574678; do
    disabled="$WSDIR/$id/SubModule.xml.disabled"
    if [ -f "$disabled" ]; then
        log "restoring workshop $id"
        mv "$disabled" "$WSDIR/$id/SubModule.xml"
    fi
done

# --- Uninstall proxy ---------------------------------------------------
log "uninstalling proxy"
make -C "$SCRIPT_DIR/mono-proxy" uninstall

log "done. Game will boot normally via the managed launcher again."
