#!/bin/bash
# Kill Rider's .NET backend process to clear a stuck Mono Remote debug
# session UI (RIDER-45772). The main Rider IDE process (Java) stays
# alive — editor tabs, open files, and search indices are preserved.
# Rider auto-respawns the backend; takes a few seconds. All in-progress
# debug sessions are reset, which is exactly what we want when the UI
# is stuck showing "attached" with no OS-level process behind it.
#
# Usage:
#   ./reset-rider-backend.sh           kill Rider.Backend and exit
#   ./reset-rider-backend.sh --force   skip the "are you sure" prompt
#
# Safety: only matches processes whose command line contains
# "Rider.Backend" AND whose binary path lives under a JetBrains/Toolbox
# directory — won't kill arbitrary dotnet processes or the main Rider
# window.

set -uo pipefail

FORCE=0
for arg in "$@"; do
    case "$arg" in
        --force|-f) FORCE=1 ;;
        -h|--help)
            sed -n '2,/^$/p' "$0" | sed 's/^# \{0,1\}//'
            exit 0 ;;
        *) echo "unknown arg: $arg (see --help)" >&2; exit 2 ;;
    esac
done

log() { printf '[reset-rider-backend] %s\n' "$*"; }

# Find Rider.Backend processes. We require BOTH "Rider.Backend" in the
# cmdline AND a JetBrains path, so we don't accidentally kill arbitrary
# .NET apps that someone named Rider.Backend.
mapfile -t PIDS < <(
    pgrep -f "Rider\.Backend" 2>/dev/null | while read -r pid; do
        args=$(ps -o args= -p "$pid" 2>/dev/null || echo)
        if echo "$args" | grep -qE "(JetBrains|Toolbox/apps/rider)"; then
            echo "$pid"
        fi
    done
)

if [ ${#PIDS[@]} -eq 0 ]; then
    log "no Rider.Backend process found — nothing to reset"
    log "(the main Rider IDE might not be running, or hasn't started a backend yet)"
    exit 0
fi

log "found ${#PIDS[@]} Rider.Backend process(es):"
for pid in "${PIDS[@]}"; do
    cmd=$(ps -o cmd= -p "$pid" 2>/dev/null | cut -c-120)
    log "  pid $pid: $cmd..."
done

if [ "$FORCE" -ne 1 ]; then
    echo
    read -p "Kill them? [y/N] " answer
    case "$answer" in
        y|Y|yes|YES) ;;
        *) log "cancelled"; exit 0 ;;
    esac
fi

for pid in "${PIDS[@]}"; do
    log "SIGTERM pid $pid"
    kill "$pid" 2>/dev/null
done

# Give it 2 seconds to exit gracefully
sleep 2

for pid in "${PIDS[@]}"; do
    if kill -0 "$pid" 2>/dev/null; then
        log "SIGKILL survivor pid $pid"
        kill -9 "$pid" 2>/dev/null
    fi
done

log "done. Rider main IDE will respawn Rider.Backend in a few seconds."
log "Debug session UI should be reset. Editor tabs/files are preserved."
