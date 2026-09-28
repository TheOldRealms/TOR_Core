# Debugging TOR_Core on Linux (Proton)

This directory contains everything needed to run Bannerlord under Proton with
JetBrains Rider attached over the Mono Soft Debugger — real breakpoints, real
variable inspection, real stack traces. Same workflow as F5-debug on Windows.

## Prerequisites

- **Linux** (developed on Fedora 44; other distros should work with equivalent
  packages).
- **Proton-GE** (or upstream Proton) installed via Steam. Tested against
  `GE-Proton10-34`.
- **JetBrains Rider** — Toolbox install works fine. Rider needs to see the
  project as an SDK-style csproj, which is what `TOR_Core.CrossPlatform.csproj`
  provides.
- **mingw-w64 cross-compiler.** On Fedora:
  ```
  sudo dnf install mingw64-gcc mingw64-binutils
  ```

## One-time setup (initial install)

1. **Build TOR_Core** on Linux so Rider has a portable-PDB DLL to debug against:
   ```
   dotnet build CSharpSourceCode/TOR_Core.CrossPlatform.csproj -c Debug
   ```
   Output lands at `bin/Win64_Shipping_Client/TOR_Core.{dll,pdb}`.

2. **Build & install the mono proxy** (this replaces the game's
   `mono-2.0-sgen.dll` with a shim that activates the soft-debug agent on
   port 56000):
   ```
   cd CSharpSourceCode/linux-debug/mono-proxy && make install
   ```
   The Makefile extracts the export table from your installed mono, generates
   a matching proxy `.def`, cross-compiles the proxy with mingw-w64, backs up
   the original mono, renames it to `monosgenorig.dll`, and installs the proxy.

3. **Set Steam launch options** for Mount & Blade II Bannerlord (right-click
   the game in your library → Properties → Launch Options):
   ```
   PROTON_LOG=1 %command% /singleplayer _MODULES_*Native*SandBoxCore*SandBox*StoryMode*CustomBattle*TOR_Armory*TOR_Environment*TOR_Core*_MODULES_
   ```
   Adjust the module list to whatever mods you want loaded. Do NOT include
   `MONO_SDB_ENV_OPTIONS` or `MONO_ENV_OPTIONS` — TaleWorlds' Mono ignores
   both; the proxy activates the agent directly.

4. **Install the Rider run config and external tool** (one command,
   copies templates into `.idea/` which is gitignored):
   ```
   ./CSharpSourceCode/linux-debug/install-rider-config.sh
   ```
   Then in Rider: **File → Reload All from Disk**. A `Bannerlord Attach`
   entry appears in the top-right run/debug dropdown. Its Before Launch
   chain is already:
   1. **Build Project** — recompiles `TOR_Core.CrossPlatform` so
      `bin/Win64_Shipping_Client/TOR_Core.{dll,pdb}` are fresh.
   2. **External tool "Bannerlord Prepare"** — runs
      `attach-run.sh --restart`, which kills any live game (so the
      fresh DLL gets loaded), applies swap/proxy/mod-hide, launches
      via `steam://`, polls port 56000, exits 0 when ready.
   3. Rider then attaches to `127.0.0.1:56000`.

   Manual alternative if you'd rather set it up by hand:
   - `Run → Edit Configurations → + → Mono Remote`
   - Name: `Bannerlord Attach`, Host: `127.0.0.1`, Port: `56000`
   - Listen for incoming = **unchecked** (game is server, Rider is client)
   - Enable mixed-mode = unchecked
   - Before Launch: **+ Build Project** (for `TOR_Core.CrossPlatform`),
     then **+ Run External tool** pointing at
     `linux-debug/attach-run.sh` with parameter `--restart`.

## Daily workflow

Once set up, one click:

- **Rider → Debug 'Bannerlord Attach'** (Shift+F9)
- `attach-run.sh` fires the Steam URI, waits ~15–25 seconds for the game to
  boot and open the agent port, then exits.
- Rider connects.
- Set breakpoints, inspect, step, evaluate — same as Windows.

## Hot reload

Not supported. Bannerlord loads mod DLLs once at startup; the runtime does
not accept new assemblies at runtime. Iteration loop is:

1. Edit code in Rider.
2. `Ctrl+F9` build.
3. Close the game (Rider disconnects).
4. Debug again — Rider re-launches via `attach-run.sh` in ~25s.

Rider's **runtime variable modification** (right-click in Variables → Set
Value) and the **Immediate window** (Debug tab → the `>` prompt) still work
while paused. Useful for "what if this bool were true here."

## After a Steam update

Steam sometimes overwrites the launcher exe and `mono-2.0-sgen.dll` during
updates. If breakpoints stop working:

```
cd CSharpSourceCode/linux-debug/mono-proxy && make install
```

The Makefile detects a fresh mono, renames it to `monosgenorig.dll` again,
and reinstalls the proxy. `attach-run.sh` re-applies the launcher swap on
its next run.

## When Rider's Stop hangs / everything sticks

```
./CSharpSourceCode/linux-debug/stop.sh
```

Force-kills any lingering Bannerlord/Wine processes and frees TCP 56000.
Useful because Rider's Mono Remote "Stop" tries to negotiate a graceful
`VM_DISPOSE` over TCP; if the game process is already dead, that
negotiation hangs forever. In Rider itself, the ▾ next to the red Stop
square offers **Detach** — use that instead of Terminate for remote
attaches, it closes only Rider's side of the TCP connection without
waiting for a peer response.

## Reverting

```
./CSharpSourceCode/linux-debug/undo.sh
```

Undoes the launcher swap, restores workshop mod SubModule.xml files, and
uninstalls the proxy. Game boots normally via the vanilla managed launcher
afterwards. Safe to re-install anytime.

## How this works (short version)

- **The problem.** Bannerlord under Proton has two runtimes: the managed
  launcher `TaleWorlds.MountAndBlade.Launcher.exe` runs on Wine Mono (which
  ships without the soft-debug agent — `debugger-agent-stubs.c`, calling
  `mono_debugger_agent_parse_options` there just `g_error`s). The native
  `Bannerlord.Native.exe` embeds CoreCLR and legacy mono via
  `TaleWorlds.Native.dll`, which imports from `mono-2.0-sgen.dll`. That
  bundled mono **has** the soft-debug agent compiled in — but nobody
  activates it.
- **The fix.** Replace `mono-2.0-sgen.dll` with a proxy shim that
  forwards ~1065 exports to a renamed copy of the original and hooks the
  three init functions actually called by TW.Native.dll
  (`mono_set_dirs`, `mono_jit_init_version`, `mono_jit_init`). In the
  hooks, before delegating, we call `mono_debug_init(MONO_DEBUG_FORMAT_MONO)`
  and `mono_jit_parse_options("--debugger-agent=...")`. Now the game's own
  Mono activates the agent on TCP 56000 during runtime bootstrap.
- **The gotchas.** Wine's PE loader doesn't auto-load forwarder targets like
  Windows does, so the proxy has to `LoadLibrary` the renamed original in
  its `DllMain`. Wine's forwarder-name parser also gets confused by dots
  in the target module name, so the renamed original can't be called
  `mono-2.0-sgen-orig.dll` — it must be `monosgenorig.dll`. And the
  managed launcher must be bypassed (a copy of `Bannerlord.Native.exe`
  renamed over it) or the game runs on Wine Mono instead of the bundled
  Mono, and our proxy is inert.

See `mono-proxy/proxy.c` for the full narrated implementation.

## Known issues

- Some crashes on "Enter the Old World" don't trip a Rider breakpoint —
  they're probably native (Wine-side) faults that the Mono debugger can't
  see. Enable **Debug → Break on Exceptions → System.Exception (Break when
  thrown)** to catch managed first-chance exceptions before the game
  terminates. If nothing pauses, the crash is native; check
  `~/.steam/steam/steamapps/compatdata/261550/pfx/drive_c/ProgramData/Mount and Blade II Bannerlord/logs/rgl_log_*.txt`.
- First-chance managed exceptions with the debugger attached occasionally
  crash the game (a Mono limitation). If you hit this, disable
  "Break when thrown" and only break on unhandled exceptions.
- **Rider gets stuck in a "pseudo-attached" state after session ends** —
  Rider's Mono Remote UI shows the debug session as active even after the
  game/TCP/backend process are all gone. External kill scripts (`stop.sh`)
  can't reach it because there's no OS-level state left to kill. This is
  [RIDER-45772](https://youtrack.jetbrains.com/issue/RIDER-45772) — an
  unresolved JetBrains bug, not caused by our setup. Workarounds:
  - Right-click the session tab → Close / Terminate (works in some
    Rider versions)
  - Kill `Rider.Backend` process — resets Rider's debug state without
    losing your editor tabs
  - Full Rider restart (nuclear but reliable)

## Why the Mono soft-debug path was necessary (state of the art)

We researched alternatives before settling on the Mono proxy approach:

- **Wine Mono** (Proton's default .NET runtime) is compiled with the
  soft-debug agent stubbed out (`debugger-agent-stubs.c`) — calling
  `mono_debugger_agent_parse_options` there `g_error`s. So Wine Mono
  cannot be a debug target. Rebuilding Wine Mono with soft-debug enabled
  requires porting the agent's socket/threading layer to Windows API —
  documented as multi-day work by the community.
- **CoreCLR + vsdbg** would only apply to Bannerlord's Xbox Game Pass
  build (Steam is Mono-hosted). Also, Rider's CoreCLR remote attach is
  SSH-only and can't reach a Windows-PE CoreCLR inside Wine.
- **Bypassing the launcher and running `Bannerlord.Native.exe`** was the
  only way to get TaleWorlds' bundled Mono (which has the agent) to be
  the JIT of record; the managed launcher path uses Wine Mono.

The proxy DLL is the only Bannerlord+Linux+Rider recipe published as of
2026 — no BUTR/BLSE/community solution exists. RIDER-45772 is the last
rough edge; everything else works.
