#!/bin/bash
# Install shared Rider run configuration + external tool into the local
# .idea/ directory. .idea/ itself is gitignored, so the source-of-truth
# XML lives under rider-config/ and this script copies it into place.
#
# Rerun safely: overwrites existing files with the same names.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CSHARP_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"      # CSharpSourceCode/
IDEA_DIR="$CSHARP_DIR/.idea"

if [ ! -d "$IDEA_DIR" ]; then
    echo "[install-rider-config] $IDEA_DIR does not exist."
    echo "  Open CSharpSourceCode/TOR_Core.CrossPlatform.csproj in Rider first,"
    echo "  then re-run this script."
    exit 1
fi

# 1. Run configuration
mkdir -p "$IDEA_DIR/runConfigurations"
install -m 0644 "$SCRIPT_DIR/rider-config/runConfigurations/Bannerlord_Attach.xml" \
    "$IDEA_DIR/runConfigurations/Bannerlord_Attach.xml"
echo "[install-rider-config] installed runConfigurations/Bannerlord_Attach.xml"

# 2. External Tools definition
mkdir -p "$IDEA_DIR/tools"
install -m 0644 "$SCRIPT_DIR/rider-config/tools/External Tools.xml" \
    "$IDEA_DIR/tools/External Tools.xml"
echo "[install-rider-config] installed tools/External Tools.xml"

echo ""
echo "[install-rider-config] Done. In Rider:"
echo "  - Reload the project (File -> Reload All from Disk) so Rider picks up the new configs."
echo "  - The run config appears as 'Bannerlord Attach' in the top-right dropdown."
echo "  - Hitting Debug on it will: build -> restart game -> attach when port 56000 opens."
