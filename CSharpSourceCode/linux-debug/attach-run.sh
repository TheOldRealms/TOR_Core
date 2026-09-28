#!/bin/bash
# Prepare Bannerlord for debug attach on Linux via Proton, then launch it.
#
# Steps:
#   1. Verify proxy DLL is installed (fail fast with a helpful message).
#   2. If --restart: kill any running game via stop.sh (needed for the
#      Rider iteration cycle so we always attach to a fresh process with
#      the freshly built TOR_Core.dll loaded).
#   3. Ensure Bannerlord.Native.exe is what Steam launches (rename swap).
#   4. Hide workshop mods whose IDs collide with local dev modules.
#   5. Fire the game via steam://rungameid/261550 (unless already running
#      and --restart wasn't passed — then attach to the existing process).
#   6. Poll TCP :56000 (the Mono soft-debug agent) until it listens or timeout.
#
# Intended use: as the "Before launch → External tool" for a Rider Mono Remote
# run config. Rider launches this, waits for exit 0, then attaches to the
# already-listening agent.
#
# Flags:
#   --restart   kill the running game (if any) before launching, so the
#               freshly built TOR_Core.dll gets loaded. Use this in the
#               Rider Before-Launch chain after "Build Project".
#
# Standalone use: run manually before hitting Debug in Rider.

set -uo pipefail

GAMEBIN="$HOME/.local/share/Steam/steamapps/common/Mount & Blade II Bannerlord/bin/Win64_Shipping_Client"
WSDIR="$HOME/.local/share/Steam/steamapps/workshop/content/261550"
APPID=261550
PORT=56000
TIMEOUT=90

RESTART=0
for arg in "$@"; do
    case "$arg" in
        --restart) RESTART=1 ;;
        *) echo "unknown arg: $arg" >&2; exit 2 ;;
    esac
done

log()  { printf '[attach-run] %s\n' "$*"; }
die()  { printf '[attach-run] ERROR: %s\n' "$*" >&2; exit 1; }

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# --- 1. Verify proxy install ---------------------------------------------
[ -f "$GAMEBIN/monosgenorig.dll" ] || die "proxy not installed. Run:
  cd CSharpSourceCode/linux-debug/mono-proxy && make install"

# --- 1a. Build TOR_Core.CrossPlatform ------------------------------------
# Doing the build here (rather than in Rider's Before Launch chain) lets us
# get by with a single Before Launch task in the Rider run config, which
# sidesteps figuring out the right Build-task XML for .NET SDK-style
# projects. Incremental builds are ~1s if nothing changed.
CSHARP_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
CSPROJ="$CSHARP_DIR/TOR_Core.CrossPlatform.csproj"
if [ -f "$CSPROJ" ]; then
    log "building TOR_Core.CrossPlatform (Debug)"
    if ! dotnet build "$CSPROJ" -c Debug --nologo -v q; then
        die "dotnet build failed. Fix errors above and retry."
    fi
else
    log "WARN: $CSPROJ not found; skipping build"
fi

# --- 1b. --restart: kill running game first ------------------------------
if [ "$RESTART" -eq 1 ]; then
    if pgrep -f "reaper.*SteamLaunch.*AppId=$APPID" >/dev/null 2>&1; then
        log "--restart: killing running game so freshly built DLL gets loaded"
        "$SCRIPT_DIR/stop.sh"
        # Give wineserver a moment to release TCP :$PORT before we start polling
        sleep 2
    fi
fi

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

# "Game is running" detector: pgrep against the Wine-side reaper wrapper
# for AppId 261550. This matches ONLY the Steam-launched game process
# chain, not other tools that happen to have "Bannerlord" in their path
# (e.g. the TOR_Tools MCP host).
if pgrep -af "reaper.*SteamLaunch.*AppId=$APPID" >/dev/null 2>&1; then
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
        # --- 6. Spawn background watcher for auto-cleanup ------------------
        # When the game process dies (crash or normal exit), wineserver keeps
        # holding TCP :$PORT — Rider's Mono Remote session then can't detect
        # that the peer is dead and won't detach cleanly. This watcher runs
        # stop.sh once the game is gone, which force-closes the socket and
        # drops Rider's connection so its debug session finishes.
        (
            # Wait for game process to disappear (it's already up, else we'd
            # not have hit port-open). Poll every 5 s to keep CPU minimal.
            while pgrep -f "reaper.*SteamLaunch.*AppId=$APPID" >/dev/null 2>&1; do
                sleep 5
            done
            # Give wine's own cleanup a moment before we force it.
            sleep 3
            "$SCRIPT_DIR/stop.sh" >/dev/null 2>&1
        ) </dev/null >/dev/null 2>&1 &
        disown $! 2>/dev/null || true
        exit 0
    fi
    sleep 1
done

die "timeout: port $PORT did not open. Check /tmp/monoproxy.log and ~/steam-$APPID.log"
