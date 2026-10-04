#!/bin/bash
# Install shared Rider run configuration + external tool into the local
# .idea/ directory. .idea/ itself is gitignored, so the source-of-truth
# XML lives under rider-config/ and this script copies it into place.
#
# Rerun safely: overwrites existing files with the same names.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CSHARP_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"      # CSharpSourceCode/

# Rider stores per-project state in one of two places depending on the
# project layout. For a bare .csproj (no .sln), it creates a nested
# "virtual solution" directory: .idea/.idea.<Name>.dir/.idea/. For a .sln,
# it uses .idea/ directly. Find whichever is real by looking for the
# workspace.xml file.

nested_idea() {
    # Rider creates nested .idea/.idea.<name>[.dir]/.idea/ layouts:
    #   - For bare .csproj opens: .idea.<ProjectName>.dir/
    #   - For .sln opens:         .idea.<SolutionName>/   (no .dir suffix)
    # When multiple exist (because the project was opened both ways), pick
    # the one with the most recently modified workspace.xml — that's the
    # live Rider session.
    local best="" best_mtime=0
    for d in "$CSHARP_DIR"/.idea/.idea.*/.idea; do
        [ -f "$d/workspace.xml" ] || continue
        local mt
        mt=$(stat -c %Y "$d/workspace.xml" 2>/dev/null)
        [ -z "$mt" ] && continue
        if [ "$mt" -gt "$best_mtime" ]; then
            best="$d"
            best_mtime="$mt"
        fi
    done
    [ -n "$best" ] && { echo "$best"; return 0; }
    return 1
}

if nested=$(nested_idea) && [ -n "$nested" ]; then
    IDEA_DIR="$nested"
    echo "[install-rider-config] detected nested Rider layout: $IDEA_DIR"
elif [ -f "$CSHARP_DIR/.idea/workspace.xml" ]; then
    IDEA_DIR="$CSHARP_DIR/.idea"
    echo "[install-rider-config] detected flat Rider layout: $IDEA_DIR"
else
    echo "[install-rider-config] No Rider project state found under $CSHARP_DIR/.idea/"
    echo "  Open CSharpSourceCode/TOR_Core.CrossPlatform.csproj in Rider first,"
    echo "  then re-run this script."
    exit 1
fi

# Clean up any stale files at the WRONG level (older install script wrote there)
if [ -f "$CSHARP_DIR/.idea/runConfigurations/Bannerlord_Attach.xml" ] && [ "$IDEA_DIR" != "$CSHARP_DIR/.idea" ]; then
    rm -f "$CSHARP_DIR/.idea/runConfigurations/Bannerlord_Attach.xml"
    rmdir "$CSHARP_DIR/.idea/runConfigurations" 2>/dev/null || true
    echo "[install-rider-config] cleaned stale Bannerlord_Attach.xml at wrong .idea level"
fi
if [ -f "$CSHARP_DIR/.idea/tools/External Tools.xml" ] && [ "$IDEA_DIR" != "$CSHARP_DIR/.idea" ]; then
    rm -f "$CSHARP_DIR/.idea/tools/External Tools.xml"
    rmdir "$CSHARP_DIR/.idea/tools" 2>/dev/null || true
    echo "[install-rider-config] cleaned stale External Tools.xml at wrong .idea level"
fi

# 1. Run configuration
mkdir -p "$IDEA_DIR/runConfigurations"
install -m 0644 "$SCRIPT_DIR/rider-config/runConfigurations/Bannerlord_Attach.xml" \
    "$IDEA_DIR/runConfigurations/Bannerlord_Attach.xml"
echo "[install-rider-config] installed runConfigurations/Bannerlord_Attach.xml"

# 2. External Tools definition
#
# JetBrains stores External Tools at the USER level, not project level —
# they live at ~/.config/JetBrains/Rider<VERSION>/tools/External Tools.xml.
# The template in rider-config/tools/External Tools.xml is the source of
# truth for OUR tool; we merge it into each detected Rider install's file,
# preserving other tools the user may have created.
#
# We use Python because ElementTree handles XML merge/dedupe cleanly.
# The stale .idea/tools/External Tools.xml (left over from an earlier
# incorrect attempt) is removed.
if [ -f "$IDEA_DIR/tools/External Tools.xml" ]; then
    rm -f "$IDEA_DIR/tools/External Tools.xml"
    rmdir "$IDEA_DIR/tools" 2>/dev/null || true
    echo "[install-rider-config] removed stale project-level External Tools.xml"
fi

RIDER_INSTALLS=()
while IFS= read -r -d '' d; do
    RIDER_INSTALLS+=("$d")
done < <(find "$HOME/.config/JetBrains" -maxdepth 1 -type d -name "Rider*" -print0 2>/dev/null)

if [ ${#RIDER_INSTALLS[@]} -eq 0 ]; then
    echo "[install-rider-config] WARN: no Rider install found under ~/.config/JetBrains/"
    echo "  Open the project in Rider once (it'll create the config dir), then re-run."
else
    for rider in "${RIDER_INSTALLS[@]}"; do
        tools_dir="$rider/tools"
        tools_file="$tools_dir/External Tools.xml"
        mkdir -p "$tools_dir"

        python3 - "$tools_file" "$SCRIPT_DIR/rider-config/tools/External Tools.xml" <<'PY'
import sys, os
import xml.etree.ElementTree as ET

dst_path, src_path = sys.argv[1], sys.argv[2]

# Load our source tool(s) from the shared template
src = ET.parse(src_path).getroot()

# Load or create destination toolSet
if os.path.exists(dst_path):
    dst = ET.parse(dst_path).getroot()
else:
    dst = ET.Element("toolSet", attrib={"name": "External Tools"})

# For each tool in source, replace-or-append in destination
for src_tool in src.findall("tool"):
    name = src_tool.get("name")
    # remove existing with same name
    for existing in list(dst.findall("tool")):
        if existing.get("name") == name:
            dst.remove(existing)
    dst.append(src_tool)

ET.ElementTree(dst).write(dst_path, encoding="utf-8", xml_declaration=False)
PY
        echo "[install-rider-config] merged Bannerlord Prepare into $tools_file"
    done
fi

echo ""
echo "[install-rider-config] Done. In Rider:"
echo "  - Reload the project (File -> Reload All from Disk) so Rider picks up the new configs."
echo "  - The run config appears as 'Bannerlord Attach' in the top-right dropdown."
echo "  - Hitting Debug on it will: build -> restart game -> attach when port 56000 opens."
