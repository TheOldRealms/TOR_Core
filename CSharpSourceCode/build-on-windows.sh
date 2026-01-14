#!/bin/bash
#
# TOR_Core Cross-Platform Build Script
# Builds the mod on a Windows VM using a shared folder
#
# Prerequisites:
#   1. Windows VM named "windows-dev" in virt-manager
#   2. OpenSSH Server enabled in Windows
#   3. .NET SDK installed in Windows
#   4. Shared folder set up (see SETUP below)
#
# SETUP - Shared Folder:
#   In virt-manager:
#   1. Open VM settings > Memory > Enable "Enable shared memory"
#   2. Add Hardware > Filesystem:
#      - Driver: virtiofs
#      - Source path: /home/Zerca/.local/share/Steam/steamapps/common/Mount & Blade II Bannerlord
#      - Target path: bannerlord
#   3. In Windows:
#      - Install WinFsp: https://winfsp.dev/rel/
#      - Install virtio-win guest tools (includes virtiofs driver)
#      - Set VirtioFsSvc to auto-start: sc config VirtioFsSvc start=auto
#      - Start the service: sc start VirtioFsSvc
#      - The Z: drive should auto-mount when service starts
#
# Usage:
#   ./build-on-windows.sh              # Build and keep VM running
#   ./build-on-windows.sh --shutdown   # Build and shutdown VM after
#   ./build-on-windows.sh --no-start   # Assume VM is already running
#   ./build-on-windows.sh --help       # Show help
#

set -e

# ============================================================================
# CONFIGURATION - Edit these to match your setup
# ============================================================================

VM_NAME="windows-dev"
VM_CONNECTION="qemu:///system"
WINDOWS_USER="linus"                    # Your Windows username

# Path to Bannerlord on Windows (the mounted shared folder)
# This should be where the virtiofs share is mounted
WINDOWS_BANNERLORD="Z:"

# Project paths on Windows (relative to Bannerlord root)
WINDOWS_PROJECT_PATH="${WINDOWS_BANNERLORD}\\Modules\\TOR_Core\\CSharpSourceCode"

# Local paths (for verification)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT_DLL="$SCRIPT_DIR/../bin/Win64_Shipping_Client/TOR_Core.dll"

# Timeouts
VM_BOOT_TIMEOUT=120      # Seconds to wait for VM to boot
SSH_TIMEOUT=90           # Seconds to wait for SSH to be available

# ============================================================================
# HELPER FUNCTIONS
# ============================================================================

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

log_info()  { echo -e "${BLUE}[INFO]${NC} $1"; }
log_ok()    { echo -e "${GREEN}[OK]${NC} $1"; }
log_warn()  { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

show_help() {
    grep -A 30 "^# SETUP" "$0" | head -20
    echo
    echo "Usage: $0 [OPTIONS]"
    echo "  --shutdown    Shutdown VM after build"
    echo "  --no-start    Skip starting VM (assume already running)"
    echo "  --help        Show this help"
    exit 0
}

# ============================================================================
# VM MANAGEMENT
# ============================================================================

get_vm_state() {
    virsh -c "$VM_CONNECTION" domstate "$VM_NAME" 2>/dev/null || echo "unknown"
}

start_vm() {
    local state=$(get_vm_state)
    if [[ "$state" == *"running"* ]]; then
        log_ok "VM '$VM_NAME' is already running"
        return 0
    fi

    log_info "Starting VM '$VM_NAME'..."
    virsh -c "$VM_CONNECTION" start "$VM_NAME" >/dev/null
    log_ok "VM start command sent"
}

shutdown_vm() {
    log_info "Shutting down VM '$VM_NAME'..."
    virsh -c "$VM_CONNECTION" shutdown "$VM_NAME" >/dev/null 2>&1 || true
    log_ok "Shutdown signal sent"
}

get_vm_ip() {
    virsh -c "$VM_CONNECTION" domifaddr "$VM_NAME" 2>/dev/null | \
        grep -oE '([0-9]{1,3}\.){3}[0-9]{1,3}' | head -1
}

wait_for_vm_ip() {
    log_info "Waiting for VM to get IP address..."
    local elapsed=0
    local ip=""

    while [[ $elapsed -lt $VM_BOOT_TIMEOUT ]]; do
        ip=$(get_vm_ip)
        if [[ -n "$ip" ]]; then
            echo  # Clear the waiting line
            log_ok "VM IP: $ip"
            VM_IP="$ip"
            return 0
        fi
        sleep 2
        elapsed=$((elapsed + 2))
        echo -ne "\r${BLUE}[INFO]${NC} Waiting... ${elapsed}s / ${VM_BOOT_TIMEOUT}s   "
    done
    echo
    log_error "Timeout waiting for VM IP address"
    return 1
}

wait_for_ssh() {
    log_info "Waiting for SSH on $VM_IP..."
    local elapsed=0

    while [[ $elapsed -lt $SSH_TIMEOUT ]]; do
        if ssh -o ConnectTimeout=3 -o BatchMode=yes -o StrictHostKeyChecking=accept-new \
               "${WINDOWS_USER}@${VM_IP}" "echo ok" >/dev/null 2>&1; then
            echo  # Clear the waiting line
            log_ok "SSH is ready"
            return 0
        fi
        sleep 3
        elapsed=$((elapsed + 3))
        echo -ne "\r${BLUE}[INFO]${NC} Waiting for SSH... ${elapsed}s / ${SSH_TIMEOUT}s   "
    done
    echo
    log_warn "SSH key auth not working - you may need to enter password"
    return 0
}

# ============================================================================
# BUILD PROCESS
# ============================================================================

check_shared_folder() {
    log_info "Checking shared folder access..."

    # Check if the shared folder is accessible
    if ! ssh "${WINDOWS_USER}@${VM_IP}" "if exist \"${WINDOWS_BANNERLORD}\\bin\" (echo ok) else (echo missing)" 2>/dev/null | grep -q "ok"; then
        log_error "Shared folder not accessible at ${WINDOWS_BANNERLORD}"
        echo
        echo "Please ensure:"
        echo "  1. virtiofs shared folder is configured in virt-manager"
        echo "  2. WinFsp is installed in Windows"
        echo "  3. The share is mounted (run as Admin in Windows):"
        echo "     net use ${WINDOWS_BANNERLORD} \\\\?\\GLOBALROOT\\Device\\VirtioFsDevice\\bannerlord"
        echo
        return 1
    fi
    log_ok "Shared folder accessible"
}

run_build() {
    log_info "Building TOR_Core on Windows..."
    echo

    # Run the build via SSH
    # Use CrossPlatform.csproj with BannerlordPath override for Windows shared folder
    ssh -t "${WINDOWS_USER}@${VM_IP}" "cd /d \"${WINDOWS_PROJECT_PATH}\" && dotnet build TOR_Core.CrossPlatform.csproj -c Release -p:BannerlordPath=${WINDOWS_BANNERLORD}\\"

    local result=$?
    echo
    if [[ $result -eq 0 ]]; then
        log_ok "Build completed successfully"
    else
        log_error "Build failed with exit code $result"
        return $result
    fi
}

verify_output() {
    log_info "Verifying build output..."

    if [[ -f "$OUTPUT_DLL" ]]; then
        local size=$(stat -c%s "$OUTPUT_DLL" 2>/dev/null || stat -f%z "$OUTPUT_DLL" 2>/dev/null)
        local modified=$(stat -c%y "$OUTPUT_DLL" 2>/dev/null | cut -d. -f1 || stat -f"%Sm" "$OUTPUT_DLL" 2>/dev/null)
        log_ok "DLL exists: $OUTPUT_DLL"
        echo "      Size: ${size} bytes"
        echo "      Modified: ${modified}"
    else
        log_warn "DLL not found at expected location: $OUTPUT_DLL"
    fi
}

# ============================================================================
# MAIN
# ============================================================================

main() {
    local do_shutdown=false
    local skip_start=false

    # Parse arguments
    for arg in "$@"; do
        case $arg in
            --shutdown)  do_shutdown=true ;;
            --no-start)  skip_start=true ;;
            --help|-h)   show_help ;;
            *)           log_error "Unknown argument: $arg"; show_help ;;
        esac
    done

    echo "========================================"
    echo " TOR_Core Windows VM Build"
    echo "========================================"
    echo

    # Step 1: Start VM (unless skipped)
    if ! $skip_start; then
        start_vm
    fi

    # Step 2: Wait for VM to be accessible
    wait_for_vm_ip || exit 1

    # Step 3: Wait for SSH
    wait_for_ssh

    # Step 4: Check shared folder
    check_shared_folder || exit 1

    # Step 5: Build
    run_build || exit 1

    # Step 6: Verify output
    verify_output

    # Step 7: Clear caches and trigger Rider reload
    log_info "Clearing caches to trigger IDE reload..."
    rm -rf "$SCRIPT_DIR/obj/" 2>/dev/null
    rm -rf "$SCRIPT_DIR/.idea/caches/" 2>/dev/null
    find "$SCRIPT_DIR" -name "*.cs" -exec touch {} \; 2>/dev/null
    touch "$SCRIPT_DIR/TOR_Core.CrossPlatform.csproj"
    log_ok "Caches cleared - IDE should refresh"

    # Step 8: Optionally shutdown
    if $do_shutdown; then
        shutdown_vm
    fi

    echo
    echo "========================================"
    log_ok "Build complete!"
    echo "========================================"
}

VM_IP=""
main "$@"
