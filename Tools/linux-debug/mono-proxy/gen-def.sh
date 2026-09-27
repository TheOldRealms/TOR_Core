#!/bin/bash
# Generate proxy.def from exports.txt.
#
# Every export becomes a PE forwarder to monosgenorig.dll EXCEPT the ones
# in HOOKED_SYMBOLS, which are implemented locally in proxy.c.
#
# Forwarder targets must be QUOTED — dlltool's .def parser treats dots and
# dashes as separators in the middle of an unquoted identifier, which
# breaks resolution for names like "monosgenorig.mono_free". Wine's
# loader further requires the target basename to have NO dots (that's why
# the original DLL is renamed to monosgenorig.dll, not mono-2.0-sgen-orig.dll).

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
EXPORTS="${1:-$SCRIPT_DIR/exports.txt}"
OUT="${2:-$SCRIPT_DIR/proxy.def}"
ORIG_BASENAME=monosgenorig

HOOKED_SYMBOLS=(
    mono_jit_init_version
    mono_jit_init
    mono_set_dirs
)

is_hooked() {
    local name="$1"
    for h in "${HOOKED_SYMBOLS[@]}"; do
        [[ "$name" == "$h" ]] && return 0
    done
    return 1
}

{
    echo "EXPORTS"
    for h in "${HOOKED_SYMBOLS[@]}"; do
        echo "    $h"
    done
    while IFS= read -r name; do
        [ -z "$name" ] && continue
        is_hooked "$name" && continue
        echo "    $name = \"${ORIG_BASENAME}.$name\""
    done < "$EXPORTS"
} > "$OUT"

echo "Wrote $OUT with $(grep -c '=' "$OUT") forwarders and ${#HOOKED_SYMBOLS[@]} hooks."
