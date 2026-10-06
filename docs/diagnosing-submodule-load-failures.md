# Diagnosing submodule load failures

When TOR fails to load, Visual Studio shows a bare `System.Exception` with no message and no
`InnerException`, at `Module.InitializeSubModuleBases()` line 220. **That stack is identical for
every load failure and contains nothing useful.** `InitializeSubModuleBases` catches your
exception, writes the real detail to the log via `MBDebug.Print` and
`Debug.SetCrashReportCustomString`, then throws a *parameterless* `new Exception()`. The cause is
discarded before you see it, so don't try to read it from the debugger.

## Where the cause actually is

```bash
grep -h "OnSubModuleLoad failed" -A1 \
  "/c/ProgramData/Mount and Blade II Bannerlord/logs/rgl_log_"*.txt | tail -4
```

That yields `OnSubModuleLoad failed for TOR_Core (TOR_Core.dll): <exception>` plus an `Inner:` line
naming the exact patch method, type or member at fault.

In Visual Studio, `Ctrl+Alt+E` → enable **Common Language Runtime Exceptions** (or narrow to
`HarmonyLib.HarmonyException` and `System.ArgumentException`) to break at the real throw site
before the catch swallows it.

## Why this bites TOR in particular

Harmony resolves patch targets at *patch* time, not compile time. A `[HarmonyPatch]` naming a
method the game no longer has compiles cleanly, then throws
`HarmonyException: Undefined target method` during `OnSubModuleLoad` and aborts the entire module —
so one stale patch costs you the whole mod. After a game version bump, audit patch targets by
reflecting over the TaleWorlds assemblies rather than trusting the build. Checking that member
*names* resolve is not sufficient: `MethodType.Constructor` and `argumentTypes` patches select a
specific overload, so their parameter lists must be compared too.

One trap worth spelling out: omitting `argumentTypes` on a `MethodType.Constructor` patch does **not**
mean "whichever constructor exists". `AccessTools.DeclaredConstructor` substitutes `Type[0]` for a
null parameter list, so it looks for a *parameterless* constructor and resolves to null on a type
that has none. Bare ctor patches only work where the target genuinely has a `()` constructor.

## Two traps that look like this

- **Stale binary.** `bin/` is the game's live module directory *and* `TOR_Core.dll` is tracked, so
  `git checkout -- bin/` redeploys an old DLL. Running one built against the previous game version
  throws `ReflectionTypeLoadException`, which surfaces as the same empty exception.
- **Silent deploy failure.** Building while the game is open leaves `obj/` fresh and `bin/` stale:
  the compile reports 0 `CS` errors but the copy fails with `MSB3027`. Grep build output for
  `error`, not `error CS`, and confirm `bin/` actually changed.
