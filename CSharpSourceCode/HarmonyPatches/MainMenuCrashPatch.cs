using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;
using TWModule = TaleWorlds.MountAndBlade.Module;

namespace TOR_Core.HarmonyPatches
{
    // a reliable main menu hook, avoid flushing so it doesnt stale
    [HarmonyPatch(typeof(TWModule), "GetInitialStateOptions")]
    internal static class MainMenuDeferredClearAllCleanup
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.First)]
        private static void Postfix(ref IEnumerable<InitialStateOption> __result)
        {
            ToMainMenuClearAllFix.TryFlushDeferredFromMainMenu();
        }
    }

    // ClearAll can hit during MapScreen finalize and crash so defer and replay after finalize
    // if still queued after CleanScreens, drop (stale ptrs anyway)
    [HarmonyPatch]
    internal static class ToMainMenuClearAllFix
    {
        private const string MAP_SCREEN_TYPE_NAME = "SandBox.View.Map.MapScreen";
        private const string SCENE_CALLBACKS_TYPE_NAME = "ManagedCallbacks.ScriptingInterfaceOfIScene";
        private const string SCENEVIEW_CALLBACKS_TYPE_NAME = "ManagedCallbacks.ScriptingInterfaceOfISceneView";

        private static int _endGameInProgress;
        private static int _cleanScreensDepth;
        private static int _mapScreenFinalizeDepth;
        private static int _flushRequested;
        private static int _isFlushing;

        private enum DeferredCallKind
        {
            SceneClearAll,
            SceneViewClearAll
        }

        private struct DeferredClearAllCall
        {
            public DeferredCallKind Kind;
            public ulong Pointer;

            // SceneView only
            public bool ClearScene;
            public bool RemoveTerrain;
        }

        private static readonly object _pendingLock = new object();
        private static readonly List<DeferredClearAllCall> _pendingCallsOrdered = new List<DeferredClearAllCall>(16);

        private static readonly HashSet<ulong> _scenePointers = new HashSet<ulong>(32);
        private static readonly Dictionary<ulong, int> _sceneViewIndexByPointer = new Dictionary<ulong, int>(16);

        private static MethodInfo _sceneClearAllMethod;
        private static MethodInfo _sceneViewClearAllMethod;

        private static bool IsInDangerWindow =>
            Volatile.Read(ref _isFlushing) == 0 &&
            Volatile.Read(ref _cleanScreensDepth) > 0 &&
            Volatile.Read(ref _mapScreenFinalizeDepth) > 0;

        // drop pending calls
        internal static void TryFlushDeferredFromMainMenu()
        {
            if (Volatile.Read(ref _endGameInProgress) != 0) return;
            if (Volatile.Read(ref _cleanScreensDepth) > 0) return;
            if (Volatile.Read(ref _mapScreenFinalizeDepth) > 0) return;

            DropPendingCalls();
        }

        // EndGame

        [HarmonyPatch(typeof(MBGameManager), "EndGame")]
        [HarmonyPrefix]
        private static void MBGameManager_EndGame_Prefix()
        {
            Interlocked.Exchange(ref _endGameInProgress, 1);
            Interlocked.Exchange(ref _flushRequested, 0);
            Interlocked.Exchange(ref _isFlushing, 0);
            Interlocked.Exchange(ref _mapScreenFinalizeDepth, 0);

            lock (_pendingLock)
            {
                _pendingCallsOrdered.Clear();
                _scenePointers.Clear();
                _sceneViewIndexByPointer.Clear();
            }
        }

        // CleanScreens Depth

        [HarmonyPatch(typeof(ScreenManager), "CleanScreens")]
        [HarmonyPrefix]
        private static void ScreenManager_CleanScreens_Prefix()
        {
            Interlocked.Increment(ref _cleanScreensDepth);
        }

        [HarmonyPatch(typeof(ScreenManager), "CleanScreens")]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void ScreenManager_CleanScreens_Postfix()
        {
            if (Volatile.Read(ref _flushRequested) == 0)
            {
                return;
            }

            if (Volatile.Read(ref _mapScreenFinalizeDepth) > 0)
            {
                return;
            }

            FlushDeferredIfSafe();
        }

        [HarmonyPatch(typeof(ScreenManager), "CleanScreens")]
        [HarmonyFinalizer]
        private static Exception ScreenManager_CleanScreens_Finalizer(Exception __exception)
        {
            var newDepth = Interlocked.Decrement(ref _cleanScreensDepth);

            if (newDepth <= 0)
            {
                // EndGame window is closed
                Interlocked.Exchange(ref _endGameInProgress, 0);
                Interlocked.Exchange(ref _flushRequested, 0);

                // after CleanScreens, stale pointer goes wild so just drop
                DropPendingCalls();
            }

            return __exception;
        }

        // MapScreen finalize depth tracking
        [HarmonyPatch(typeof(ScreenBase), "HandleFinalize")]
        private static class ScreenBase_HandleFinalize_MapScreenDepthPatch
        {
            [HarmonyPrefix]
            private static void Prefix(ScreenBase __instance, ref bool __state)
            {
                __state =
                    Volatile.Read(ref _cleanScreensDepth) > 0 &&
                    __instance.GetType().FullName == MAP_SCREEN_TYPE_NAME;

                if (__state)
                {
                    Interlocked.Increment(ref _mapScreenFinalizeDepth);
                }
            }

            [HarmonyFinalizer]
            private static Exception Finalizer(ScreenBase __instance, bool __state, Exception __exception)
            {
                if (__state)
                {
                    var newDepth = Interlocked.Decrement(ref _mapScreenFinalizeDepth);

                    if (newDepth <= 0 && Volatile.Read(ref _flushRequested) != 0)
                    {
                        FlushDeferredIfSafe();
                    }
                }

                return __exception;
            }
        }

        [HarmonyPatch(typeof(ScreenBase), "HandleDeactivate")]
        private static class ScreenBase_HandleDeactivate_MapScreenDepthPatch
        {
            [HarmonyPrefix]
            private static void Prefix(ScreenBase __instance, ref bool __state)
            {
                __state =
                    Volatile.Read(ref _cleanScreensDepth) > 0 &&
                    __instance.GetType().FullName == MAP_SCREEN_TYPE_NAME;

                if (__state)
                {
                    Interlocked.Increment(ref _mapScreenFinalizeDepth);
                }
            }

            [HarmonyFinalizer]
            private static Exception Finalizer(ScreenBase __instance, bool __state, Exception __exception)
            {
                if (__state)
                {
                    var newDepth = Interlocked.Decrement(ref _mapScreenFinalizeDepth);

                    if (newDepth <= 0 && Volatile.Read(ref _flushRequested) != 0)
                    {
                        FlushDeferredIfSafe();
                    }
                }

                return __exception;
            }
        }

        // Callbacks
        [HarmonyPatch]
        private static class ScriptingInterfaceOfIScene_ClearAll_DeferPatch
        {
            private static bool Prepare()
            {
                var type = AccessTools.TypeByName(SCENE_CALLBACKS_TYPE_NAME);
                return type != null && AccessTools.Method(type, "ClearAll", new[] { typeof(UIntPtr) }) != null;
            }

            private static MethodBase TargetMethod()
            {
                var type = AccessTools.TypeByName(SCENE_CALLBACKS_TYPE_NAME);
                return type == null ? null : AccessTools.Method(type, "ClearAll", new[] { typeof(UIntPtr) });
            }

            [HarmonyPrefix]
            private static bool Prefix(UIntPtr __0)
            {
                return SceneClearAllOrDefer(__0);
            }
        }

        // if multiple calls arrive for the same pointer combine flags so the flush replays better requested cleanup
        [HarmonyPatch]
        private static class ScriptingInterfaceOfISceneView_ClearAll_DeferPatch
        {
            private static bool Prepare()
            {
                var type = AccessTools.TypeByName(SCENEVIEW_CALLBACKS_TYPE_NAME);
                return type != null && AccessTools.Method(type, "ClearAll", new[] { typeof(UIntPtr), typeof(bool), typeof(bool) }) != null;
            }

            private static MethodBase TargetMethod()
            {
                var type = AccessTools.TypeByName(SCENEVIEW_CALLBACKS_TYPE_NAME);
                return type == null ? null : AccessTools.Method(type, "ClearAll", new[] { typeof(UIntPtr), typeof(bool), typeof(bool) });
            }

            [HarmonyPrefix]
            private static bool Prefix(UIntPtr __0, bool __1, bool __2)
            {
                return SceneViewClearAllOrDefer(__0, __1, __2);
            }
        }

        private static bool SceneClearAllOrDefer(UIntPtr scenePointer)
        {
            if (!IsInDangerWindow)
            {
                return true;
            }

            var ptrValue = scenePointer.ToUInt64();
            if (ptrValue == 0)
            {
                // null pointer
                return true;
            }

            lock (_pendingLock)
            {
                // dedupe pointer to block replaying unnecessary ClearAll calls
                if (_scenePointers.Add(ptrValue))
                {
                    _pendingCallsOrdered.Add(new DeferredClearAllCall
                    {
                        Kind = DeferredCallKind.SceneClearAll,
                        Pointer = ptrValue
                    });
                }
            }

            Interlocked.Exchange(ref _flushRequested, 1);
            return false;
        }

        private static bool SceneViewClearAllOrDefer(UIntPtr sceneViewPointer, bool clearScene, bool removeTerrain)
        {
            if (!IsInDangerWindow)
            {
                return true;
            }

            var ptrValue = sceneViewPointer.ToUInt64();
            if (ptrValue == 0)
            {
                return true;
            }

            lock (_pendingLock)
            {
                // combine flags each pointer. cleanups differ between calls
                if (_sceneViewIndexByPointer.TryGetValue(ptrValue, out var existingIndex))
                {
                    var existing = _pendingCallsOrdered[existingIndex];
                    existing.ClearScene |= clearScene;
                    existing.RemoveTerrain |= removeTerrain;
                    _pendingCallsOrdered[existingIndex] = existing;
                }
                else
                {
                    _sceneViewIndexByPointer[ptrValue] = _pendingCallsOrdered.Count;
                    _pendingCallsOrdered.Add(new DeferredClearAllCall
                    {
                        Kind = DeferredCallKind.SceneViewClearAll,
                        Pointer = ptrValue,
                        ClearScene = clearScene,
                        RemoveTerrain = removeTerrain
                    });
                }
            }

            Interlocked.Exchange(ref _flushRequested, 1);
            return false;
        }

        // flush deferred calls once MapScreen finalize is fully done
        // same engine path, just delayed to safer timing
        private static void FlushDeferredIfSafe()
        {
            if (Volatile.Read(ref _cleanScreensDepth) <= 0)
            {
                return;
            }

            if (Volatile.Read(ref _mapScreenFinalizeDepth) > 0)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _isFlushing, 1, 0) != 0)
            {
                return;
            }

            try
            {
                EnsureClearAllMethodsResolved();
                if (_sceneClearAllMethod == null && _sceneViewClearAllMethod == null)
                {
                    // if resolve fails, keep the queue and dont eat failure
                    return;
                }

                DeferredClearAllCall[] callsToFlush;

                lock (_pendingLock)
                {
                    if (_pendingCallsOrdered.Count == 0)
                    {
                        Interlocked.Exchange(ref _flushRequested, 0);
                        return;
                    }

                    callsToFlush = _pendingCallsOrdered.ToArray();
                    _pendingCallsOrdered.Clear();
                    _scenePointers.Clear();
                    _sceneViewIndexByPointer.Clear();
                }

                Interlocked.Exchange(ref _flushRequested, 0);

                foreach (var call in callsToFlush)
                {
                    switch (call.Kind)
                    {
                        case DeferredCallKind.SceneViewClearAll:
                            _sceneViewClearAllMethod?.Invoke(null, new object[]
                            {
                                new UIntPtr(call.Pointer),
                                call.ClearScene,
                                call.RemoveTerrain
                            });
                            break;

                        case DeferredCallKind.SceneClearAll:
                            _sceneClearAllMethod?.Invoke(null, new object[]
                            {
                                new UIntPtr(call.Pointer)
                            });
                            break;
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref _isFlushing, 0);
            }
        }

        // resolve managed ClearAll callbacks via reflection and cache it to keep results stable, its still a risk
        private static void EnsureClearAllMethodsResolved()
        {
            if (_sceneClearAllMethod != null && _sceneViewClearAllMethod != null)
            {
                return;
            }

            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            if (_sceneClearAllMethod == null)
            {
                var sceneType = AccessTools.TypeByName(SCENE_CALLBACKS_TYPE_NAME);
                _sceneClearAllMethod = sceneType?.GetMethod(
                    "ClearAll",
                    flags,
                    binder: null,
                    types: new[] { typeof(UIntPtr) },
                    modifiers: null);
            }

            if (_sceneViewClearAllMethod == null)
            {
                var sceneViewType = AccessTools.TypeByName(SCENEVIEW_CALLBACKS_TYPE_NAME);
                _sceneViewClearAllMethod = sceneViewType?.GetMethod(
                    "ClearAll",
                    flags,
                    binder: null,
                    types: new[] { typeof(UIntPtr), typeof(bool), typeof(bool) },
                    modifiers: null);
            }
        }

        private static void DropPendingCalls()
        {
            lock (_pendingLock)
            {
                _pendingCallsOrdered.Clear();
                _scenePointers.Clear();
                _sceneViewIndexByPointer.Clear();
            }
        }
    }
}
