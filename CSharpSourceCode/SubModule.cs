using HarmonyLib;
using NLog;
using NLog.Config;
using NLog.Targets;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade;
using TOR_Core.Utilities;

namespace TOR_Core
{
    public class SubModule : MBSubModuleBase
    {
        public static Harmony HarmonyInstance { get; private set; }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            TORCommon.Say("TOR Core loaded.");
        }

        protected override void OnSubModuleLoad()
        {
            ConfigureLogging();
            HarmonyInstance = new Harmony("mod.harmony.theoldrealms");
            HarmonyInstance.PatchAll();
            UIConfig.DoNotUseGeneratedPrefabs = true;
        }

        private static void ConfigureLogging()
        {
            var config = new LoggingConfiguration();

            // Log debug/exception info to the log file
            var logfile = new FileTarget("logfile") { FileName = TORPaths.TORLogPath };
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);

            // Log info and higher to the VS debugger
            var logdebugger = new DebuggerTarget("logdebugger");
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logdebugger);

            LogManager.Configuration = config;
        }
    }
}
