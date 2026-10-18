#!/bin/bash
# Discovery helper for AssertAutoIgnore port.
# Prints every visible X11 window (incl. ones Wine renders under XWayland),
# then watches for NEW windows and announces them. Use it while the
# editor is running: trigger an assertion dialog, and the dialog's title
# will appear here. Ctrl+C to exit.

set -u

snapshot() {
    xdotool search --onlyvisible --name "" 2>/dev/null | sort -u
}

winfo() {
    local w="$1"
    local name wmclass pid
    name=$(xdotool getwindowname "$w" 2>/dev/null)
    wmclass=$(xprop -id "$w" WM_CLASS 2>/dev/null | sed 's/WM_CLASS(STRING) = //')
    pid=$(xdotool getwindowpid "$w" 2>/dev/null)
    printf '  wid=%s pid=%s class=%s\n   title="%s"\n' "$w" "$pid" "$wmclass" "$name"
}

echo "== snapshot: all currently visible windows =="
baseline=$(snapshot)
for w in $baseline; do winfo "$w"; done
echo ""
echo "== watching for new/changed windows (Ctrl+C to stop) =="
seen="$baseline"
while true; do
    current=$(snapshot)
    new=$(comm -13 <(echo "$seen") <(echo "$current"))
    if [ -n "$new" ]; then
        for w in $new; do
            printf '[new window at %s]\n' "$(date +%H:%M:%S)"
            winfo "$w"
        done
    fi
    seen="$current"
    sleep 0.4
done
