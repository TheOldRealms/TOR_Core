#!/bin/bash
# Force-kill any lingering Bannerlord processes and free port 56000.
#
# Use this when:
#   - Rider's Stop button hangs (Mono Remote can't graceful-shutdown a
#     dead peer, so its TCP disconnect waits forever)
#   - The game window disappeared but a wineserver / Watchdog process
#     is still holding resources (rare, but happens on hard native crashes)
#   - You just want to reset to a clean state
#
# Safe to run when nothing is running (it just prints "nothing to kill").
# Only touches Bannerlord's own process chain — won't hurt other Wine games.

set -uo pipefail

log() { printf '[stop] %s\n' "$*"; }

APPID=261550
killed=0

# 1. The Steam reaper wrapper — kills the whole Proton launch chain
for pid in $(pgrep -af "reaper.*SteamLaunch.*AppId=$APPID" 2>/dev/null | awk '{print $1}'); do
    log "killing reaper pid $pid (whole game chain)"
    kill "$pid" 2>/dev/null && killed=$((killed+1))
done

# Give it a moment to unwind naturally
sleep 1

# 2. Anything left with our specific launcher/native names (Wine side)
for pat in "TaleWorlds\.MountAndBlade\.Launcher\.exe" \
           "Bannerlord\.Native\.exe" \
           "Bannerlord\.exe$" \
           "Watchdog\\.exe" ; do
    for pid in $(pgrep -f "$pat" 2>/dev/null); do
        log "killing $pat pid $pid"
        kill "$pid" 2>/dev/null && killed=$((killed+1))
    done
done

sleep 1

# 3. Force-kill any survivors
for pid in $(pgrep -f "reaper.*SteamLaunch.*AppId=$APPID" 2>/dev/null); do
    log "SIGKILL survivor $pid"
    kill -9 "$pid" 2>/dev/null
done

if [ "$killed" -eq 0 ]; then
    log "nothing to kill — nothing was running"
else
    log "sent SIGTERM to $killed processes"
fi

# 4. Verify port 56000 is freed
if ss -tln 2>/dev/null | grep -qE ":56000\b"; then
    log "WARNING: port 56000 still held (wineserver may need more time; retry in 5s)"
else
    log "port 56000 is free"
fi
