using System.Collections.Generic;
using TaleWorlds.Library;

namespace TOR_Core.Utilities.Profiling;

/// <summary>
/// Console-command bindings for the TOR profiler harness. Grouped under the
/// <c>tor.</c> prefix to match the existing TOR command namespace.
/// </summary>
public static class TORProfilerConsoleCommands
{
    [CommandLineFunctionality.CommandLineArgumentFunction("profiler_enable", "tor")]
    public static string EnableCmd(List<string> arguments)
    {
        TORCampaignProfiler.Enable();
        return "TOR profiler enabled.";
    }

    [CommandLineFunctionality.CommandLineArgumentFunction("profiler_disable", "tor")]
    public static string DisableCmd(List<string> arguments)
    {
        TORCampaignProfiler.Disable();
        return "TOR profiler disabled.";
    }

    [CommandLineFunctionality.CommandLineArgumentFunction("profiler_dump", "tor")]
    public static string DumpCmd(List<string> arguments)
    {
        TORCampaignProfiler.Dump();
        return "TOR profiler stats written to the TOR log file.";
    }

    [CommandLineFunctionality.CommandLineArgumentFunction("profiler_reset", "tor")]
    public static string ResetCmd(List<string> arguments)
    {
        TORCampaignProfiler.Reset();
        return "TOR profiler stats reset.";
    }
}
