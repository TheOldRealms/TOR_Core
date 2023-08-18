using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class AgentPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Agent), "GetBattleImportance")]
        public static void BattleImportancePatch(ref float __result, Agent __instance)
        {
            if (__instance.IsExpendable())
            {
                __result = 0;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Formation), "GetMedianAgent")]
        public static void MedianAgentPatch(ref Agent __result, Formation __instance)
        {
            if (__result == null)
            {
                List<Agent> units = new List<Agent>();
                foreach (var unit in __instance.Arrangement.GetAllUnits())
                {
                    units.Add((Agent)unit);
                }
                __result = units.FirstOrDefault();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Agent), "MakeVoice")]
        public static bool VoiceVariationPatch(Agent __instance, SkinVoiceManager.SkinVoiceType voiceType)
        {
            
            if (__instance == null || !__instance.IsHuman || !__instance.IsHero || __instance.Controller != Agent.ControllerType.Player) return true;

            string className = __instance.Monster.SoundAndCollisionInfoClassName;
            if (className != "human") return true;
            if (ShouldGetRandomizedVoice(__instance))
            {
                int count = SkinVoiceManager.GetVoiceDefinitionCountWithMonsterSoundAndCollisionInfoClassName(className);
                int[] array = new int[count];
                SkinVoiceManager.GetVoiceDefinitionListWithMonsterSoundAndCollisionInfoClassName(className, array);
                __instance.AgentVisuals.SetVoiceDefinitionIndex(GetRandomVoiceIndexForAgent(__instance), 0);
            }
            
            return true;
        }

        private static bool ShouldGetRandomizedVoice(Agent agent)
        {
            if (agent == null || !agent.IsHuman || agent.Character == null || agent.Character.Culture == null || agent.IsFemale) return false;
            var cultureId = agent.Character.Culture.StringId;
            if(Game.Current.GameType is Campaign)
            {
                cultureId = agent.GetHero().Culture.StringId;
            }
            return cultureId == "vlandia" || cultureId == "empire" || agent.IsVampire();
        }

        private static int GetRandomVoiceIndexForAgent(Agent agent)
        {
            if(agent.IsVampire()) return MBRandom.RandomInt(TORConstants.VAMPIRE_VOICE_INDEX_START, TORConstants.VAMPIRE_VOICE_INDEX_START + (TORConstants.VAMPIRE_VOICES_COUNT));

            var cultureId = agent.Character.Culture.StringId;
            if (Game.Current.GameType is Campaign)
            {
                cultureId = agent.GetHero().Culture.StringId;
            }

            switch (cultureId)
            {
                case "vlandia":
                    return MBRandom.RandomInt(TORConstants.BRETONNIA_VOICE_INDEX_START, TORConstants.BRETONNIA_VOICE_INDEX_START + (TORConstants.BRETONNIA_VOICES_COUNT));
                case "empire":
                    return MBRandom.RandomInt(TORConstants.EMPIRE_VOICE_INDEX_START, TORConstants.EMPIRE_VOICE_INDEX_START + (TORConstants.EMPIRE_VOICES_COUNT));
                default:
                    return 1;
            }
        }
    }
}
