using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class AgentPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Equipment), nameof(Equipment.GetRandomEquipmentElements))]
        public static bool FixEquipments(ref Equipment __result, BasicCharacterObject character, bool randomEquipmentModifier, bool isCivilianEquipment = false, int seed = -1)
        {
            Equipment equipment = new(isCivilianEquipment);
            List<Equipment> list = (from eq in character.AllEquipments
                                    where eq.IsCivilian == isCivilianEquipment && !eq.IsEmpty()
                                    select eq).ToList();
            if (list.IsEmpty())
            {
                __result = equipment;
                return false;
            }
            int count = list.Count;
            Random random = new(seed);
            int setNum = MBRandom.RandomInt(count);
            for (int i = 0; i < 12; i++)
            {
                equipment[i] = list[setNum].GetEquipmentFromSlot((EquipmentIndex)i);
                if (randomEquipmentModifier)
                {
                    var modifier = equipment[i].Item?.ItemComponent?.ItemModifierGroup?.GetRandomItemModifierProductionScoreBased();
                    if(modifier != null)
                    {
                        equipment[i].SetModifier(modifier);
                    }
                }
            }
            __result = equipment;
            return false;
        }


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
        [HarmonyPatch(typeof(Agent), "CombatActionsEnabled", MethodType.Getter)]
        public static void CombatActionsEnabledPatch(ref bool __result, Agent __instance)
        {
            if(__instance.IsMainAgent && __result)
            {
                var logic = Mission.Current.GetMissionBehavior<AbilityManagerMissionLogic>();
                if (logic != null && logic.ShouldSuppressCombatActions) __result = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Formation), "GetMedianAgent")]
        public static void MedianAgentPatch(ref Agent __result, Formation __instance)
        {
            if (__result == null)
            {
                List<Agent> units = [];
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
            
            if (__instance == null || !__instance.IsHuman) return true;

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
            if (agent.IsTreeSpirit()) return true;
            if (agent == null || !agent.IsHuman || agent.Character == null || agent.Character.Culture == null || agent.IsFemale || !agent.IsPlayerControlled) return false;
            var cultureId = agent.Character.Culture.StringId;
            if(Game.Current.GameType is Campaign && agent.IsHero)
            {
                cultureId = agent.GetHero().Culture.StringId;
            }
            return cultureId == TORConstants.Cultures.BRETONNIA || cultureId == TORConstants.Cultures.EMPIRE || agent.IsVampire();
        }

        private static int GetRandomVoiceIndexForAgent(Agent agent)
        {
            if (agent.IsVampire())
            {
                return MBRandom.RandomInt(TORConstants.VAMPIRE_VOICE_INDEX_START, TORConstants.VAMPIRE_VOICE_INDEX_START + TORConstants.VAMPIRE_VOICES_COUNT);
            }
            if (agent.IsTreeSpirit())
            {
                return MBRandom.RandomInt(TORConstants.TREESPIRIT_VOICE_INDEX_START, TORConstants.TREESPIRIT_VOICE_INDEX_START + TORConstants.TREESPIRIT_VOICES_COUNT);
            }

            var cultureId = agent.Character.Culture.StringId;
            if (Game.Current.GameType is Campaign)
            {
                cultureId = agent.GetHero().Culture.StringId;
            }

            return cultureId switch
            {
                TORConstants.Cultures.BRETONNIA => MBRandom.RandomInt(TORConstants.BRETONNIA_VOICE_INDEX_START, TORConstants.BRETONNIA_VOICE_INDEX_START + (TORConstants.BRETONNIA_VOICES_COUNT)),
                TORConstants.Cultures.EMPIRE => MBRandom.RandomInt(TORConstants.EMPIRE_VOICE_INDEX_START, TORConstants.EMPIRE_VOICE_INDEX_START + (TORConstants.EMPIRE_VOICES_COUNT)),
                _ => 1,
            };
        }
    }
}
