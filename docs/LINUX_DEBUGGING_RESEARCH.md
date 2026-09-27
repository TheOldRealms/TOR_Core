# Debugging TOR_Core / Bannerlord on Linux via Proton

## Research Document for Setting Up .NET Debugging Under Wine/Proton

**Date:** September 2026
**Purpose:** Enable managed .NET debugging of Bannerlord mods running under Proton on Linux
**Status:** Research compiled - experimental approaches documented

---

## Executive Summary

Debugging .NET/C# code running inside Wine/Proton is challenging but **has been proven possible**. The key insight is that **vsdbg (the .NET debugger) must run inside Wine**, not natively on Linux. This document compiles all known approaches and community experiences.

---

## Table of Contents

1. [Background & Challenges](#background--challenges)
2. [Approach 1: VS Code + vsdbg in Wine (Most Proven)](#approach-1-vs-code--vsdbg-in-wine-most-proven)
3. [Approach 2: dnSpy Under Wine](#approach-2-dnspy-under-wine)
4. [Approach 3: JetBrains Rider (Current Limitations)](#approach-3-jetbrains-rider-current-limitations)
5. [Approach 4: Native Code Debugging with GDB/winedbg](#approach-4-native-code-debugging-with-gdbwinedbg)
6. [Proton-Specific Configuration](#proton-specific-configuration)
7. [Fallback: Enhanced Logging](#fallback-enhanced-logging)
8. [Known Issues & Limitations](#known-issues--limitations)
9. [Community Resources](#community-resources)
10. [Step-by-Step Setup Guide](#step-by-step-setup-guide)

---

## Background & Challenges

### Why This Is Hard

1. **Wine/Proton creates a Windows environment** - The .NET runtime is the Windows version running inside Wine
2. **Linux debuggers can't attach to Wine's .NET** - Native Linux tools (including Rider's normal attach) see Wine processes, not the managed .NET code inside
3. **PID mismatch** - You must use the **Wine PID** (from `wine taskmgr`), not the Linux process ID
4. **Proton adds complexity** - Steam's runtime container wraps everything in additional layers

### What Bannerlord Uses

- **Runtime:** .NET 6 (CoreCLR) - This is good news, better debugging support than old .NET Framework
- **Mod Framework:** Harmony for runtime patching
- **Platform:** TaleWorlds custom engine wrapping .NET

---

## Approach 1: VS Code + vsdbg in Wine (Most Proven)

This approach has been **confirmed working** by users on WineHQ forums for WPF applications.

### Source
> "I got this to work and it is GREAT. I can use Visual Studio to remotely attach to my WPF app running under Wine (Ubuntu) and do normal managed debugging as if the app were running locally in Windows."
> — [WineHQ Forums](https://forum.winehq.org/viewtopic.php?t=34925)

### Prerequisites

1. **Windows vsdbg** (not Linux version)
2. **VS Code** with C# extension on Linux
3. **Working Proton/Wine prefix** for Bannerlord

### Step 1: Obtain Windows vsdbg

On a Windows machine (or VM):
```powershell
# Install VS Code + C# extension, then copy:
%USERPROFILE%\.vscode\extensions\ms-dotnettools.csharp-*\.debugger\
```

Or download directly:
```bash
# These are Windows binaries - will run under Wine
curl -L "https://vsdebugger.azureedge.net/vsdbg-17-7-10808-2/vsdbg-win7-x64.zip" -o vsdbg.zip
unzip vsdbg.zip -d vsdbg-win
```

### Step 2: Place vsdbg in Wine Prefix

```bash
# Bannerlord's Proton prefix
export WINEPREFIX=~/.steam/steam/steamapps/compatdata/261550/pfx

# Create directory and copy
mkdir -p "$WINEPREFIX/drive_c/vsdbg"
cp -r vsdbg-win/* "$WINEPREFIX/drive_c/vsdbg/"
```

### Step 3: Create VS Code Configuration

**`.vscode/launch.json`:**
```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Attach to Bannerlord (Wine/Proton)",
            "type": "coreclr",
            "request": "attach",
            "processId": "${command:pickProcess}",
            "pipeTransport": {
                "pipeProgram": "bash",
                "pipeArgs": [
                    "-c",
                    "WINEPREFIX=~/.steam/steam/steamapps/compatdata/261550/pfx wine"
                ],
                "debuggerPath": "C:\\vsdbg\\vsdbg.exe",
                "pipeCwd": "${workspaceFolder}",
                "quoteArgs": false
            },
            "sourceFileMap": {
                "/home/YOUR_USER/path/to/TOR_Core": "${workspaceFolder}"
            },
            "logging": {
                "engineLogging": true
            }
        }
    ]
}
```

### Step 4: Get Wine PID (Critical!)

```bash
# Start Wine's task manager to get the correct PID
WINEPREFIX=~/.steam/steam/steamapps/compatdata/261550/pfx wine taskmgr

# Or list processes via Wine
WINEPREFIX=~/.steam/steam/steamapps/compatdata/261550/pfx wine cmd /c "tasklist"
```

**Important:** The process ID you attach to must be the **Wine PID**, not the Linux PID from `ps aux`.

---

## Approach 2: dnSpy Under Wine

dnSpy can run under Wine and attach to .NET processes also running under Wine.

### Source
> "One debugger that does work is dnSpy. It can run under Wine and debug .NET Core applications that are also running under Wine."
> — [Community Guide](https://ccifra.github.io/PortingWPFAppsToLinux/Overview.html)

### Setup

```bash
# Download dnSpy (Windows version)
# https://github.com/dnSpy/dnSpy/releases

# Run under Wine (same prefix as Bannerlord)
export WINEPREFIX=~/.steam/steam/steamapps/compatdata/261550/pfx
wine dnSpy.exe
```

### Attaching

1. In dnSpy: **Debug → Attach to Process**
2. Select the Bannerlord process (by Wine PID)
3. Set breakpoints in TOR_Core.dll

### For Unity-Based Games (Reference)

If debugging Unity games, you may need to enable debug mode:
```ini
# In boot.config
player-connection-debug=1
```

---

## Approach 3: JetBrains Rider (Current Limitations)

### Current Status: Limited Support

Rider currently **cannot attach to native processes to debug managed code within them**. This is tracked as:
- **RIDER-11810** on JetBrains YouTrack

### What Works

- **SSH Remote Debugging** - For native .NET on Linux (not Wine)
- **Docker debugging** - For containerized .NET apps
- **Rider 2025.2+** - Has improved remote native debugging

### Potential Workaround (Experimental)

Create a wrapper script that makes vsdbg accessible via Rider's remote debugging:

```bash
#!/bin/bash
# vsdbg-wine-wrapper.sh
export WINEPREFIX=~/.steam/steam/steamapps/compatdata/261550/pfx
wine C:\\vsdbg\\vsdbg.exe "$@"
```

Then configure Rider SSH remote debugging to use this wrapper. **This is untested and experimental.**

### Future Hope

JetBrains is actively developing:
- Mixed-mode debugging (.NET + native)
- Remote Windows development from Linux
- Better cross-platform debugging

Check [Rider 2025 roadmap](https://blog.jetbrains.com/dotnet/2025/07/24/the-rider-2025-2-release-candidate/) for updates.

---

## Approach 4: Native Code Debugging with GDB/winedbg

For debugging native (C++) code or low-level issues, not managed .NET code.

### Proton Debug Build

```bash
# In Steam: Proton Experimental → Properties → Betas → "debug - unstripped"
```

### Environment Variables

Add to Steam launch options:
```bash
# Wait for debugger attachment
PROTON_WAIT_ATTACH=1 %command%

# Generate debug command scripts
PROTON_DUMP_DEBUG_COMMANDS=1 %command%
# Check /tmp/proton_$USER/ for generated scripts

# Enable logging
PROTON_LOG=1 %command%
```

### Attaching GDB

```bash
# Get the PID
ps aux | grep Bannerlord

# Attach
gdb
(gdb) attach [PID]
```

### Using winedbg

```bash
# Access the Proton runtime shell first
# (see Proton-Specific Configuration below)
wine winedbg
```

### Symbol Store (Proton 9+)

Proton uploads debug symbols to:
```
https://proton-archive.steamos.cloud/
```

Configure debuggers (VS, WinDbg) to use this symbol server.

---

## Proton-Specific Configuration

### Accessing Proton's Runtime Shell

```bash
# Set launch options:
PROTON_LOG=1 STEAM_COMPAT_LAUNCHER_SERVICE=proton %command%

# Check the log for steam-runtime-launch-client command
cat ~/steam-261550.log | grep "steam-runtime-launch-client"

# Example command (your bus name will differ):
~/.local/share/Steam/steamapps/common/SteamLinuxRuntime_sniper/pressure-vessel/bin/steam-runtime-launch-client \
    --bus-name=:1.XXX \
    --directory='' \
    -- bash
```

### Alternative: Pressure-Vessel Shell

```bash
# Launch options:
PRESSURE_VESSEL_SHELL=instead %command%
```

This spawns an xterm with the Proton environment ready.

### .NET Framework Installation (if needed)

```bash
# Using protontricks
protontricks 261550 dotnet472

# If it fails, disable sync first:
export WINEESYNC=0
export WINEFSYNC=0
export PROTON_NO_ESYNC=1
export PROTON_NO_FSYNC=1
protontricks 261550 dotnet472
```

### .NET Diagnostic Environment Variables

```bash
# Enable diagnostics (default)
DOTNET_EnableDiagnostics=1

# Or disable if causing issues
DOTNET_EnableDiagnostics=0

# Fine-grained control (.NET 8+)
DOTNET_EnableDiagnostics_Debugger=1
DOTNET_EnableDiagnostics_Profiler=0
```

---

## Fallback: Enhanced Logging

When debugging doesn't work, robust logging is your friend.

### TaleWorlds Debug Logging

```csharp
// In your mod code
TaleWorlds.Library.Debug.Print($"[TOR_DEBUG] Value: {myVariable}");
```

### Log Locations

```bash
# Bannerlord logs in Proton prefix
~/.steam/steam/steamapps/compatdata/261550/pfx/drive_c/ProgramData/Mount and Blade II Bannerlord/logs/

# Proton logs (if PROTON_LOG=1)
~/steam-261550.log
```

### ButterLib Crash Dumps

BUTR's ButterLib provides `.dmp` crash dump files that can be analyzed:
1. Open in Visual Studio (on Windows/VM)
2. Debug → Start Debugging (Managed Only)
3. Point to your .dll and .pdb files

---

## Known Issues & Limitations

### Critical: Exception Crash Bug

> "Exceptions thrown while the debugger is attached cause crashes, even though the same build runs fine without debugging."
> — [apple1417.dev](https://apple1417.dev/posts/2023-05-18-debugging-proton)

**Workarounds:**
- Disable first-chance exception catching in debugger settings
- Use conditional breakpoints instead of exception breakpoints
- Build with try-catch around suspect code for debugging sessions

### Harmony Patches on Linux

Certain Harmony patches cause crashes on Linux but work on Windows:
> "The problem seems to be linked to the particular method that was patched."
> — [Harmony Issue #198](https://github.com/pardeike/Harmony/issues/198)

**Workaround:** Use `cecil` backend instead of default:
```ini
# BepInEx config
[Preloader]
HarmonyBackend = cecil
```

### TaleWorlds Debugger Conflict

TaleWorlds has their own debugger attached:
> "You could kill the Unity debugger to use your own, but now they tied it to the launcher."

**BLSE workaround:**
```bash
# Launch options to enable BLSE's exception interceptor when debugger is attached
/enablecrashhandlerwhendebuggerisattached
```

---

## Community Resources

### Official & Semi-Official

- [Proton Debugging Docs](https://github.com/ValveSoftware/Proton/blob/proton_10.0/docs/DEBUGGING-LINUX.md)
- [TaleWorlds Mod Docs](https://moddocs.bannerlord.com/)
- [BUTR GitHub](https://github.com/BUTR) - Bannerlord Unofficial Tools

### Debugging Guides

- [apple1417.dev - Debugging Under Proton](https://apple1417.dev/posts/2023-05-18-debugging-proton)
- [VS Code Remote Debugging](https://github.com/dotnet/vscode-csharp/blob/main/docs/debugger/Attaching-to-remote-processes.md)
- [WineHQ Managed Debugging Thread](https://forum.winehq.org/viewtopic.php?t=34925)

### Tools

- [dnSpy](https://github.com/dnSpy/dnSpy) - .NET debugger (archived but works)
- [dnSpyEx](https://github.com/dnSpyEx/dnSpy) - Active fork of dnSpy
- [netcoredbg](https://github.com/Samsung/netcoredbg) - Open-source .NET debugger
- [BLSE](https://github.com/BUTR/Bannerlord.BLSE) - Bannerlord Software Extender

### Community Discussions

- [Bannerlord Linux Steam Discussion](https://steamcommunity.com/app/261550/discussions/0/2144217924392222478/)
- [ProtonDB Bannerlord Page](https://www.protondb.com/app/261550)
- [Bannerlord Nexus - Debug Guide](https://www.nexusmods.com/mountandblade2bannerlord/mods/2667)

---

## Step-by-Step Setup Guide

### Phase 1: Verify Basics

```bash
# 1. Check Wine prefix works
export WINEPREFIX=~/.steam/steam/steamapps/compatdata/261550/pfx
wine --version

# 2. Check .NET is present
ls "$WINEPREFIX/drive_c/windows/Microsoft.NET/"

# 3. Verify Bannerlord runs normally
# (Launch via Steam, confirm it works)
```

### Phase 2: Test PROTON_WAIT_ATTACH

```bash
# Steam launch options:
PROTON_WAIT_ATTACH=1 %command%

# Launch game - should pause before starting
# If it pauses, the debug infrastructure works
```

### Phase 3: Setup vsdbg

```bash
# 1. Get Windows vsdbg
# (Download or copy from Windows machine)

# 2. Place in prefix
mkdir -p "$WINEPREFIX/drive_c/vsdbg"
cp -r vsdbg-win/* "$WINEPREFIX/drive_c/vsdbg/"

# 3. Test vsdbg runs
wine C:\\vsdbg\\vsdbg.exe --help
```

### Phase 4: Configure VS Code

1. Install C# extension (C# Dev Kit or OmniSharp)
2. Create `.vscode/launch.json` (see Approach 1 above)
3. Open TOR_Core workspace

### Phase 5: Attempt Attach

1. Launch Bannerlord with `PROTON_WAIT_ATTACH=1`
2. Get Wine PID via `wine taskmgr`
3. In VS Code: Run → Attach to Process
4. Enter the Wine PID
5. If successful, set breakpoints and continue execution

### Phase 6: Troubleshoot

If attach fails:
- Enable VS Code logging: `"logging": {"engineLogging": true}`
- Check Wine compatibility: try newer Wine/Proton version
- Verify PDB files are present alongside DLLs
- Try dnSpy under Wine as alternative

---

## Appendix: Environment Variable Reference

```bash
# Proton Debug
PROTON_LOG=1                    # Enable logging
PROTON_WAIT_ATTACH=1            # Pause for debugger
PROTON_DUMP_DEBUG_COMMANDS=1    # Generate debug scripts

# Wine
WINEPREFIX=/path/to/prefix      # Wine prefix location
WINEDEBUG=+mono                 # Mono debug output
WINEESYNC=0                     # Disable esync
WINEFSYNC=0                     # Disable fsync

# .NET
DOTNET_EnableDiagnostics=1      # Enable .NET diagnostics
DOTNET_EnableDiagnostics_Debugger=1  # Enable debugger specifically

# Proton sync disable (for troubleshooting)
PROTON_NO_ESYNC=1
PROTON_NO_FSYNC=1
```

---

## Conclusion

The most promising path is **VS Code + vsdbg running inside Wine**. While not as polished as native Windows debugging or Rider, it has been proven to work for .NET apps under Wine.

**Realistic expectations:**
- Basic breakpoint debugging: **Should work**
- Variable inspection: **Should work**
- Exception debugging: **May cause crashes** (known issue)
- Full Rider integration: **Not currently possible**

Good luck, and please report back any successes or new findings to help the Linux modding community!
