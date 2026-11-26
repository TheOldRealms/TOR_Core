using System;
using System.Diagnostics;

namespace TOR_Core.Utilities.Profiling;

/// <summary>
/// Disposable stopwatch for a single <see cref="TORCampaignProfiler"/> measurement.
/// Returned by <see cref="TORCampaignProfiler.Scope(string)"/>; intended for use in a
/// <c>using</c> declaration at the top of the measured method.
/// </summary>
/// <remarks>
/// Struct, not class: a using declaration dispatches Dispose directly on the struct
/// without boxing, so measurement overhead stays on the stack. When the profiler is
/// disabled, <see cref="Dispose"/> short-circuits with a single bool check.
/// </remarks>
public readonly struct ProfilerScope : IDisposable
{
    private readonly string _name;
    private readonly long _startTicks;
    private readonly bool _active;

    internal ProfilerScope(string name, bool active)
    {
        _name = name;
        _active = active;
        _startTicks = active ? Stopwatch.GetTimestamp() : 0;
    }

    public void Dispose()
    {
        if (!_active) return;
        var elapsedTicks = Stopwatch.GetTimestamp() - _startTicks;
        var elapsedMs = elapsedTicks * 1000.0 / Stopwatch.Frequency;
        TORCampaignProfiler.Record(_name, elapsedMs);
    }
}
