using System.Collections.Generic;
using System.Linq;
using NLog;

namespace TOR_Core.Utilities.Profiling;

/// <summary>
/// Lightweight in-mod stopwatch profiler for TOR campaign-event handlers. Not a
/// replacement for BCA; a cheap tool to confirm optimisation deltas during
/// playtesting and catch freezes via a frame-time watchdog.
/// </summary>
/// <remarks>
/// Disabled by default. Enable via the <c>tor.profiler_enable</c> console command or
/// the <c>--tor-profiler</c> launch argument. Usage:
/// <code>
/// private void HourlyTick()
/// {
///     using var _scope = TORCampaignProfiler.Scope("CustomEvents.HourlyTick");
///     // existing body
/// }
/// </code>
/// Campaign events fire on the main thread, so the stats dictionary is not locked.
/// <see cref="Dump"/> writes a sorted report to the TOR NLog file via
/// <see cref="TORCommon.Log(NLog.LogLevel, string)"/>.
/// </remarks>
public static class TORCampaignProfiler
{
    /// <summary>
    /// Whether scopes should actually time themselves. When false,
    /// <see cref="Scope"/> returns a no-op <see cref="ProfilerScope"/>.
    /// </summary>
    public static bool Enabled { get; private set; }

    /// <summary>
    /// Any frame whose dt exceeds this threshold (in milliseconds) will be logged
    /// by <see cref="OnFrame"/>. 100 ms = visible stutter.
    /// </summary>
    public static double FrameWatchdogThresholdMs { get; set; } = 100.0;

    private sealed class ScopeStats
    {
        public long Count;
        public double TotalMs;
        public double PeakMs;
    }

    private static readonly Dictionary<string, ScopeStats> _scopes = [];

    /// <summary>
    /// Returns a disposable timer for the named scope. Caller is expected to
    /// <c>using var _ = ...</c> at the top of the method being measured.
    /// </summary>
    public static ProfilerScope Scope(string name) => new ProfilerScope(name, Enabled);

    /// <summary>
    /// Records a completed scope measurement. Called by <see cref="ProfilerScope.Dispose"/>.
    /// Main-thread only.
    /// </summary>
    internal static void Record(string name, double elapsedMs)
    {
        if (!_scopes.TryGetValue(name, out var stats))
        {
            stats = new ScopeStats();
            _scopes[name] = stats;
        }
        stats.Count++;
        stats.TotalMs += elapsedMs;
        if (elapsedMs > stats.PeakMs) stats.PeakMs = elapsedMs;
    }

    /// <summary>
    /// Enables measurement. Subsequent <see cref="Scope"/> calls will time themselves.
    /// Safe to call repeatedly.
    /// </summary>
    public static void Enable()
    {
        Enabled = true;
        TORCommon.Say("TOR profiler enabled.");
    }

    /// <summary>
    /// Disables measurement. Accumulated stats are kept; call <see cref="Reset"/> to clear.
    /// </summary>
    public static void Disable()
    {
        Enabled = false;
        TORCommon.Say("TOR profiler disabled.");
    }

    /// <summary>
    /// Clears all accumulated per-scope stats. Does not change the enabled state.
    /// </summary>
    public static void Reset()
    {
        _scopes.Clear();
        TORCommon.Say("TOR profiler stats reset.");
    }

    /// <summary>
    /// Called once per application tick from <c>SubModule.OnApplicationTick</c>.
    /// Logs a watchdog warning when <paramref name="dt"/> exceeds
    /// <see cref="FrameWatchdogThresholdMs"/>. No-op when the profiler is disabled.
    /// </summary>
    public static void OnFrame(float dt)
    {
        if (!Enabled) return;
        double ms = dt * 1000.0;
        if (ms < FrameWatchdogThresholdMs) return;
        TORCommon.Log(LogLevel.Warn, $"[TORProfiler] frame-time watchdog: {ms:F1} ms frame");
    }

    /// <summary>
    /// Writes a per-scope report (total, count, peak, mean) to the TOR NLog file,
    /// sorted by total elapsed time descending.
    /// </summary>
    public static void Dump()
    {
        var ordered = _scopes.OrderByDescending(kv => kv.Value.TotalMs).ToList();
        TORCommon.Log(LogLevel.Info, "=== TOR profiler dump (sorted by total ms) ===");
        TORCommon.Log(LogLevel.Info, $"{"Scope",-50} {"total(ms)",12} {"count",10} {"peak(ms)",10} {"mean(ms)",10}");
        foreach (var kv in ordered)
        {
            var s = kv.Value;
            double mean = s.Count == 0 ? 0 : s.TotalMs / s.Count;
            TORCommon.Log(LogLevel.Info, $"{kv.Key,-50} {s.TotalMs,12:F2} {s.Count,10} {s.PeakMs,10:F3} {mean,10:F4}");
        }
        TORCommon.Log(LogLevel.Info, "=== end dump ===");
        TORCommon.Say($"TOR profiler: dumped {_scopes.Count} scopes to log.");
    }
}
