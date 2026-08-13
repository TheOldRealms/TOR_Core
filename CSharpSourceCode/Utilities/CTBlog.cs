#if TOR_CTB_LOG
using NLog;
using System;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Utilities
{
    internal static class CrushThroughDecisionTrace
    {
        private const bool ENABLED = false;

        private const bool WRITE_TO_HUD = false;
        private const bool WRITE_TO_LOG = false;

        private const bool ONLY_WHEN_PLAYER_INVOLVED = true;

        private const int HUD_THROTTLE_MS = 250;
        private const int LOG_THROTTLE_MS = 0;

        private static int _lastHudTick;
        private static int _lastLogTick;

        public static bool ShouldTrace(Agent attackerAgent, Agent defenderAgent)
        {
            if (!ENABLED)
            {
                return false;
            }

            if (!ONLY_WHEN_PLAYER_INVOLVED)
            {
                return true;
            }

            Agent main = Agent.Main;
            return main != null && (attackerAgent == main || defenderAgent == main);
        }

        public static void Trace(Agent attackerAgent, Agent defenderAgent, string tag, string details, bool forceHud = false)
        {
            if (!ShouldTrace(attackerAgent, defenderAgent))
            {
                return;
            }

            int nowTick = Environment.TickCount;
            string line = $"[CTB] {tag} | {details}";

            if (WRITE_TO_LOG && nowTick - _lastLogTick >= LOG_THROTTLE_MS)
            {
                TORCommon.Log(line, LogLevel.Info);
                _lastLogTick = nowTick;
            }

            if (WRITE_TO_HUD && (forceHud || nowTick - _lastHudTick >= HUD_THROTTLE_MS))
            {
                TORCommon.Say(line);
                _lastHudTick = nowTick;
            }
        }
    }
}
#endif
