#!/bin/bash
# Linux port of Windows' AssertAutoIgnore for the Bannerlord Modding Kit.
#
# Reason: TaleWorlds' editor fires native Win32 MessageBox dialogs for
# RGL CONTENT WARNINGs and ASSERTs that were never fixed. On Windows,
# a WindowsFormsApp1-style UI Automation tool silently clicks "Ignore".
# Under Wine, UI Automation is heavily stubbed — the original exe runs
# but can't actually find the button to click. This replacement uses
# xdotool against XWayland/X11 instead, which does work for Wine's
# dialog windows.
#
# What it does:
#   - Polls for visible X11 windows whose class is "steam_app_1393600"
#     (the Modding Kit's Steam AppID) and whose title matches a known
#     assertion-dialog pattern.
#   - Sends the key that activates the "proceed" / "ignore" button:
#     Return for most (default button is the dismiss one), the Y
#     accelerator for "Always Ignore?" to permanently suppress.
#   - Logs a count of dismissed dialogs (parity with the Windows exe's
#     KillCount tracking).
#
# Usage:
#   ./auto-dismiss-asserts.sh &            run in background
#   ./auto-dismiss-asserts.sh --once       zap current dialogs and exit
#   ./auto-dismiss-asserts.sh --verbose    log every poll cycle
#
# Stop: Ctrl+C or `pkill -f auto-dismiss-asserts.sh`.

set -uo pipefail

APPID_CLASS="steam_app_1393600"     # Modding Kit Steam AppID
POLL=0.1                             # seconds between polls (3x faster than default)

ONCE=0
VERBOSE=0
for arg in "$@"; do
    case "$arg" in
        --once)    ONCE=1 ;;
        --verbose) VERBOSE=1 ;;
        -h|--help)
            sed -n '2,/^$/p' "$0" | sed 's/^# \{0,1\}//'
            exit 0 ;;
        *) echo "unknown arg: $arg (see --help)" >&2; exit 2 ;;
    esac
done

command -v xdotool >/dev/null || { echo "xdotool missing. sudo dnf install xdotool"; exit 1; }

log() { printf '[auto-dismiss] %s\n' "$*"; }

# Patterns: window-title regex -> dismiss method.
#
# Dismiss method can be either:
#   - "windowclose": sends WM_DELETE_WINDOW to the dialog. Wine's default
#     handler closes the dialog, picking whichever button is bound to the
#     close action (typically "No"/"Cancel" for Yes/No dialogs). Reliable
#     — works regardless of button layout and doesn't need focus — but
#     we lose control over which button is activated.
#   - any xdotool key spec (e.g. "Return", "space", "alt+i"): sent via
#     `xdotool key --window $wid`. Works for simple keys (Return/space),
#     unreliable for alt+modifier combos because xdotool's --window flag
#     doesn't deliver modifier state to Wine dialogs. Known working
#     cases: RGL [CONTENT] WARNING + Return (closes the one OK button).
#
# Why windowclose for ASSERT / Always Ignore?:
#   The alt+modifier approach doesn't work via --window and the only
#   alternative — windowactivate+key — steals keyboard focus every poll,
#   making the rest of the system unusable. windowclose avoids both
#   problems at the cost of not being able to pick "Ignore" specifically.
#   For most assertion dialogs that just means the next identical one
#   will re-appear (which we also auto-dismiss), rather than being
#   suppressed for the session.
declare -a PATTERNS=(
    # For RGL warnings and Always Ignore?, Return hits the default button
    # which is a REAL engine response (unlike windowclose which only
    # destroys the X11 window and leaves the engine's modal pump stuck).
    # Even if "No" is the default for Always Ignore?, the engine progresses
    # and generates the next warning. Progress > suppression.
    '^RGL WARNING$|Return'
    '^RGL CONTENT WARNING$|Return'
    '^Always Ignore\?$|Return'
    # Classic MSVC CRT Abort/Retry/Ignore dialog. Default (Return) = Abort
    # which kills the game. Alt+I accelerator doesn't deliver via --window.
    # Workaround: Tab Tab Return cycles focus Abort -> Retry -> Ignore then
    # activates Ignore. Needs each key as separate --window send to work
    # reliably with Wine's dialog focus plumbing.
    '^Assertion Failed!$|Tab Tab Return'
    '^ASSERT$|Tab Tab Return'
)

total=0

# Track dismiss attempts per-wid so we can detect "same window not closing"
# and log a diagnostic hierarchy dump exactly once per stuck window.
declare -A ATTEMPTS

dismiss_round() {
    local round=0
    for entry in "${PATTERNS[@]}"; do
        local pat="${entry%|*}"
        local key="${entry##*|}"
        local wids
        wids=$(xdotool search --name "$pat" 2>/dev/null) || continue
        [ -z "$wids" ] && continue
        for wid in $wids; do
            # Only touch windows that belong to the Modding Kit process.
            if xprop -id "$wid" WM_CLASS 2>/dev/null | grep -q "$APPID_CLASS"; then
                local title
                title=$(xdotool getwindowname "$wid" 2>/dev/null)

                # Deliver dismiss event to the target window WITHOUT stealing
                # focus. "windowclose" sends WM_DELETE_WINDOW (reliable but
                # can't pick a specific button); otherwise `xdotool key
                # --window` sends keystrokes (works for Return/space, not
                # for alt+modifier combos on Wine dialogs).
                if [ "$key" = "windowclose" ]; then
                    xdotool windowclose "$wid" 2>/dev/null
                    ok=$?
                else
                    # Multi-key sequences (e.g. "Tab Tab Return") must be
                    # sent as separate --window invocations so Wine's dialog
                    # processes each key event + focus change in order.
                    ok=0
                    for k in $key; do
                        xdotool key --window "$wid" "$k" 2>/dev/null || { ok=$?; break; }
                    done
                fi
                if [ "$ok" -eq 0 ]; then :; else continue; fi
                {
                    total=$((total + 1))
                    round=$((round + 1))
                    ATTEMPTS[$wid]=$((${ATTEMPTS[$wid]:-0} + 1))
                    log "dismissed wid=$wid key=$key title=\"$title\" (attempt ${ATTEMPTS[$wid]})"
                }

                # If we've tried this window 5+ times and it's STILL open,
                # the key isn't closing it. Dump hierarchy ONCE to help
                # figure out the real button label / accelerator.
                if [ "${ATTEMPTS[$wid]:-0}" -eq 5 ]; then
                    log "WARN: wid=$wid not closing after 5 attempts; dumping hierarchy:"
                    xwininfo -tree -id "$wid" 2>&1 | head -20 | sed 's/^/  /' | tee -a "${0%.sh}.log" >&2 || true
                fi
            fi
        done
    done
    return $round
}

if [ "$ONCE" -eq 1 ]; then
    dismiss_round || true
    log "one-shot complete. total dismissed: $total"
    exit 0
fi

log "watching for modding-kit assertion dialogs (poll=${POLL}s). total: 0"
trap 'log "stopping; total dismissed: $total"; exit 0' INT TERM
while true; do
    dismiss_round || true
    [ "$VERBOSE" -eq 1 ] && log "poll (total: $total)"
    sleep "$POLL"
done
