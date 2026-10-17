#!/bin/bash
# Build + install a zenity-backed comdlg32.dll replacement for the Modding
# Kit's Proton prefix. Replaces Wine's hideous Win95-style file-open dialog
# with a native GTK file picker via zenity.
#
# Based on Repeerc/wine_comdlg32_to_zenity (MIT; 2 commits, Sep 2025) — the
# only known maintained project doing this. We clone it, build for x86_64,
# install the DLL into the Modding Kit's Proton prefix.
#
# Caveats baked in:
#   - Only hooks GetOpenFileName[AW] + GetSaveFileName[AW] (the Win16-era
#     C API). COM IFileOpenDialog is NOT hooked — if an app uses the modern
#     API, that dialog stays Wine's default (which is actually tolerable).
#   - Other comdlg32 funcs (ChooseColor, PrintDlg, ChooseFont, FindText)
#     become empty stubs returning 0/NULL. Scene editor doesn't use them.
#   - Needs zenity installed on host (dnf install zenity).
#
# After running this script, add to the Modding Kit's Steam launch options:
#   WINEDLLOVERRIDES="comdlg32=n,b" %command%
# ("n,b" = prefer our native DLL, fall back to Wine builtin if it fails to
# load. Safer than plain "n".)

set -euo pipefail

APPID=1393600                 # Mount & Blade II: Bannerlord - Modding Kit
HERE="$(cd "$(dirname "$0")" && pwd)"
REPO="$HERE/repo"
UPSTREAM="https://github.com/Repeerc/wine_comdlg32_to_zenity.git"

log() { printf '[install-zenity-filedialog] %s\n' "$*"; }

# -------- 1. host prerequisites --------
missing=()
for cmd in zenity x86_64-w64-mingw32-gcc git; do
    command -v "$cmd" >/dev/null || missing+=("$cmd")
done
if [ "${#missing[@]}" -gt 0 ]; then
    log "ERROR: missing commands: ${missing[*]}"
    log "       Fedora: sudo dnf install zenity mingw64-gcc mingw64-binutils git"
    exit 1
fi

# -------- 2. clone upstream (idempotent) --------
if [ ! -d "$REPO/.git" ]; then
    log "cloning $UPSTREAM"
    git clone --depth 1 "$UPSTREAM" "$REPO"
else
    log "repo exists; skipping clone"
fi

# -------- 3. build 64-bit DLL --------
#
# Upstream's CMakeLists.txt is hardcoded for i686 (32-bit); Bannerlord is x64
# only, so we skip CMake and use the raw mingw command from CMakeLists.txt's
# comment, adapted for x86_64. The warnings about dllimport-vs-dllexport
# redeclaration on FindTextA/ReplaceTextA/GetFileTitleA are harmless (mingw
# headers forward-declare these as dllimport; we override as dllexport).
BUILT="$REPO/comdlg32.dll"
log "compiling $BUILT (64-bit)"
cd "$REPO"
# Suppress the expected mingw warnings about FindTextA/ReplaceTextA/
# GetFileTitleA being "redeclared without dllimport" — upstream headers
# forward-declare them as dllimport, our DLL_FUNC_EXPORT macro overrides
# as dllexport. Harmless. Just send stderr to /dev/null; if the build
# actually fails, the missing output file below catches it.
x86_64-w64-mingw32-gcc -shared -O2 -o comdlg32.dll hook.c d.def \
    -municode -Wl,--enable-stdcall-fixup -Wl,--kill-at 2>/dev/null || true

if [ ! -f "$BUILT" ]; then
    log "ERROR: build failed, $BUILT missing"
    exit 1
fi

# Sanity-check: the 4 file dialog symbols must be exported. Collect
# nm output into a variable first so the regex check happens in-shell
# (no pipeline → no subshell → avoids `set -u` quirks in child envs).
nm_syms=$(x86_64-w64-mingw32-nm --defined-only "$BUILT" 2>/dev/null)
for sym in GetOpenFileNameW GetOpenFileNameA GetSaveFileNameW GetSaveFileNameA; do
    if [[ ! $nm_syms =~ (^|$'\n')[^$'\n']*" T $sym"($'\n'|$) ]]; then
        log "ERROR: $sym missing from built DLL"
        exit 1
    fi
done
log "built OK ($(stat -c '%s' "$BUILT") bytes, 4/4 file dialog exports present)"

# -------- 4. install into Modding Kit's Proton prefix --------
PFX="$HOME/.steam/steam/steamapps/compatdata/$APPID/pfx"
if [ ! -d "$PFX" ]; then
    log "ERROR: Modding Kit prefix not found at $PFX"
    log "       Launch the Modding Kit via Steam at least once to create it."
    exit 1
fi
SYS32="$PFX/drive_c/windows/system32"
TARGET="$SYS32/comdlg32.dll"
BACKUP="$TARGET.proton-original"

if [ ! -e "$BACKUP" ]; then
    log "backing up original comdlg32.dll -> $(basename "$BACKUP")"
    cp -a "$TARGET" "$BACKUP"   # preserves symlink if it was one
else
    log "backup $(basename "$BACKUP") already present; preserving it"
fi

rm -f "$TARGET"
cp "$BUILT" "$TARGET"
log "installed to $TARGET"

# -------- 5. remind the user about the Steam launch options --------
cat <<EOF

NEXT STEP (manual, one-time per Steam install):

  1. In Steam, right-click "Mount & Blade II: Bannerlord - Modding Kit"
     → Properties → Launch Options
  2. Add BEFORE %command%:

       WINEDLLOVERRIDES="comdlg32=n,b" %command%

     (n,b = prefer our native DLL, fall back to Wine builtin if load fails.)

  3. Launch the Modding Kit. Open a File → Open dialog. You should see a
     native GTK file picker instead of the Win95 one.

If the dialog is still the old one, the override isn't being picked up.
Check Proton logs: PROTON_LOG=1 in launch options, then look for
"comdlg32" in \$HOME/steam-*.log.

To revert: ./uninstall.sh (restores the Proton-original symlink).
EOF
