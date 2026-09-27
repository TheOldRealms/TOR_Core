#!/bin/bash
# Prepare Bannerlord for debug attach on Linux via Proton, then launch it.
#
# Steps:
#   1. Verify proxy DLL is installed (fail fast with a helpful message).
#   2. Ensure Bannerlord.Native.exe is what Steam launches (rename swap).
#   3. Hide workshop mods whose IDs collide with local dev modules.
#   4. Fire the game via steam://rungameid/261550.
#   5. Poll TCP :56000 (the Mono soft-debug agent) until it listens or timeout.
#
# Intended use: as the "Before launch → External tool" for a Rider Mono Remote
# run config. Rider launches this, waits for exit 0, then attaches to the
# already-listening agent.
#
# Standalone use: run manually before hitting Debug in Rider.

set -uo pipefail

GAMEBIN="$HOME/.local/share/Steam/steamapps/common/Mount & Blade II Bannerlord/bin/Win64_Shipping_Client"
WSDIR="$HOME/.local/share/Steam/steamapps/workshop/content/261550"
APPID=261550
PORT=56000
TIMEOUT=90

log()  { printf '[attach-run] %s\n' "$*"; }
die()  { printf '[attach-run] ERROR: %s\n' "$*" >&2; exit 1; }

# --- 1. Verify proxy install ---------------------------------------------
[ -f "$GAMEBIN/monosgenorig.dll" ] || die "proxy not installed. Run:
  cd Tools/linux-debug/mono-proxy && make install"

# --- 2. Ensure native launcher swap --------------------------------------
LAUNCHER="$GAMEBIN/TaleWorlds.MountAndBlade.Launcher.exe"
NATIVE="$GAMEBIN/Bannerlord.Native.exe"
LAUNCHER_BAK="$GAMEBIN/TaleWorlds.MountAndBlade.Launcher.exe.original"

if [ ! -f "$LAUNCHER_BAK" ]; then
    [ -f "$NATIVE" ] || die "Bannerlord.Native.exe missing; can't apply swap"
    log "applying launcher swap: Bannerlord.Native.exe -> TaleWorlds.MountAndBlade.Launcher.exe"
    mv "$LAUNCHER" "$LAUNCHER_BAK" || die "swap: mv failed (game running?)"
    cp "$NATIVE" "$LAUNCHER" || die "swap: cp failed"
fi

# --- 3. Hide known-conflicting workshop mods ----------------------------
# Workshop TOR_Core (3025574678) and workshop Harmony (2859188632) duplicate
# local IDs; direct-launch of Bannerlord.Native.exe crashes during module
# registration when duplicates are discovered.
CONFLICTS=(2859188632 3025574678)
for id in "${CONFLICTS[@]}"; do
    sub="$WSDIR/$id/SubModule.xml"
    if [ -f "$sub" ]; then
        log "hiding workshop mod $id"
        mv "$sub" "$sub.disabled"
    fi
done

# --- 4. Launch via Steam -------------------------------------------------
# Steam applies whatever launch options are set in the app properties.
# For the debug workflow the recommended launch option is just:
#   PROTON_LOG=1 %command% /singleplayer _MODULES_*Native*SandBoxCore*SandBox*StoryMode*CustomBattle*TOR_Armory*TOR_Environment*TOR_Core*_MODULES_

if pgrep -af "Bannerlord|Launcher\.exe.*/singleplayer" >/dev/null 2>&1; then
    log "game already running — skipping steam:// launch"
else
    log "launching Steam game $APPID"
    xdg-open "steam://rungameid/$APPID" >/dev/null 2>&1 &
fi

# --- 5. Poll for debug port ---------------------------------------------
log "waiting up to ${TIMEOUT}s for TCP :$PORT..."
for i in $(seq 1 "$TIMEOUT"); do
    if ss -tln 2>/dev/null | grep -qE ":${PORT}\b"; then
        log "port $PORT is listening ($i s). ready for Rider attach."
        exit 0
    fi
    sleep 1
done

die "timeout: port $PORT did not open. Check /tmp/monoproxy.log and ~/steam-$APPID.log"
