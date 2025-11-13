using HarmonyLib;
using Helpers;
using SandBox.CampaignBehaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class CharacterObjectPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterHelper), "GetTroopTree")]
        public static void TroopTreePatch(ref IEnumerable<CharacterObject> __result, CharacterObject baseTroop)
        {
            if (!__result.ToList().Any())
            {
                __result = CharacterHelper.GetTroopTree(baseTroop);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterObject), "CreateFrom")]
        public static void TroopTreePatch(ref CharacterObject __result, CharacterObject character)
        {
            __result.Race = character.Race;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CommonTownsfolkCampaignBehavior), "CreateMaleChild")]
        public static bool PreventMaleChild(ref LocationCharacter __result, CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            CharacterObject townsman = culture.Townsman;
            Tuple<string, Monster> randomTownsManActionSetAndMonster = CommonTownsfolkCampaignBehavior.GetRandomTownsManActionSetAndMonster(townsman.Race);
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townsman, out int minValue, out int maxValue, "");
            __result = new LocationCharacter(new AgentData(new SimpleAgentOrigin(townsman, -1, null, default)).Monster(randomTownsManActionSetAndMonster.Item2).Age(MBRandom.RandomInt(minValue, maxValue)), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors), "npc_common_limited", false, relation, randomTownsManActionSetAndMonster.Item1, true, false, null, false, false, true);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CommonTownsfolkCampaignBehavior), "CreateMaleTeenager")]
        public static bool PreventMaleTeenager(ref LocationCharacter __result, CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            CharacterObject townsman = culture.Townsman;
            Tuple<string, Monster> randomTownsManActionSetAndMonster = CommonTownsfolkCampaignBehavior.GetRandomTownsManActionSetAndMonster(townsman.Race);
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townsman, out int minValue, out int maxValue, "");
            __result = new LocationCharacter(new AgentData(new SimpleAgentOrigin(townsman, -1, null, default)).Monster(randomTownsManActionSetAndMonster.Item2).Age(MBRandom.RandomInt(minValue, maxValue)), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors), "npc_common_limited", false, relation, randomTownsManActionSetAndMonster.Item1, true, false, null, false, false, true);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CommonTownsfolkCampaignBehavior), "CreateFemaleChild")]
        public static bool PreventFemaleChild(ref LocationCharacter __result, CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            CharacterObject townswoman = culture.Townswoman;
            Tuple<string, Monster> randomTownsWomanActionSetAndMonster = CommonTownsfolkCampaignBehavior.GetRandomTownsWomanActionSetAndMonster(townswoman.Race);
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townswoman, out int minValue, out int maxValue, "");
            __result = new LocationCharacter(new AgentData(new SimpleAgentOrigin(townswoman, -1, null, default)).Monster(randomTownsWomanActionSetAndMonster.Item2).Age(MBRandom.RandomInt(minValue, maxValue)), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors), "npc_common_limited", false, relation, randomTownsWomanActionSetAndMonster.Item1, true, false, null, false, false, true);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CommonTownsfolkCampaignBehavior), "CreateFemaleTeenager")]
        public static bool PreventFemaleTeenager(ref LocationCharacter __result, CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            CharacterObject townswoman = culture.Townswoman;
            Tuple<string, Monster> randomTownsWomanActionSetAndMonster = CommonTownsfolkCampaignBehavior.GetRandomTownsWomanActionSetAndMonster(townswoman.Race);
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townswoman, out int minValue, out int maxValue, "");
            __result = new LocationCharacter(new AgentData(new SimpleAgentOrigin(townswoman, -1, null, default)).Monster(randomTownsWomanActionSetAndMonster.Item2).Age(MBRandom.RandomInt(minValue, maxValue)), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors), "npc_common_limited", false, relation, randomTownsWomanActionSetAndMonster.Item1, true, false, null, false, false, true);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CommonVillagersCampaignBehavior), "CreateMaleChild")]
        public static bool PreventMaleChild2(ref LocationCharacter __result, CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            CharacterObject townsman = culture.Villager;
            Tuple<string, Monster> randomTownsManActionSetAndMonster = CommonTownsfolkCampaignBehavior.GetRandomTownsManActionSetAndMonster(townsman.Race);
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townsman, out int minValue, out int maxValue, "");
            __result = new LocationCharacter(new AgentData(new SimpleAgentOrigin(townsman, -1, null, default)).Monster(randomTownsManActionSetAndMonster.Item2).Age(MBRandom.RandomInt(minValue, maxValue)), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors), "npc_common_limited", false, relation, randomTownsManActionSetAndMonster.Item1, true, false, null, false, false, true);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CommonVillagersCampaignBehavior), "CreateMaleTeenager")]
        public static bool PreventMaleTeenager2(ref LocationCharacter __result, CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            CharacterObject townsman = culture.Villager;
            Tuple<string, Monster> randomTownsManActionSetAndMonster = CommonTownsfolkCampaignBehavior.GetRandomTownsManActionSetAndMonster(townsman.Race);
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townsman, out int minValue, out int maxValue, "");
            __result = new LocationCharacter(new AgentData(new SimpleAgentOrigin(townsman, -1, null, default)).Monster(randomTownsManActionSetAndMonster.Item2).Age(MBRandom.RandomInt(minValue, maxValue)), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors), "npc_common_limited", false, relation, randomTownsManActionSetAndMonster.Item1, true, false, null, false, false, true);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CommonVillagersCampaignBehavior), "CreateFemaleChild")]
        public static bool PreventFemaleChild2(ref LocationCharacter __result, CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            CharacterObject townswoman = culture.VillageWoman;
            Tuple<string, Monster> randomTownsWomanActionSetAndMonster = CommonTownsfolkCampaignBehavior.GetRandomTownsWomanActionSetAndMonster(townswoman.Race);
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townswoman, out int minValue, out int maxValue, "");
            __result = new LocationCharacter(new AgentData(new SimpleAgentOrigin(townswoman, -1, null, default)).Monster(randomTownsWomanActionSetAndMonster.Item2).Age(MBRandom.RandomInt(minValue, maxValue)), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors), "npc_common_limited", false, relation, randomTownsWomanActionSetAndMonster.Item1, true, false, null, false, false, true);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CommonVillagersCampaignBehavior), "CreateFemaleTeenager")]
        public static bool PreventFemaleTeenager2(ref LocationCharacter __result, CultureObject culture, LocationCharacter.CharacterRelations relation)
        {
            CharacterObject townswoman = culture.VillageWoman;
            Tuple<string, Monster> randomTownsWomanActionSetAndMonster = CommonTownsfolkCampaignBehavior.GetRandomTownsWomanActionSetAndMonster(townswoman.Race);
            Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(townswoman, out int minValue, out int maxValue, "");
            __result = new LocationCharacter(new AgentData(new SimpleAgentOrigin(townswoman, -1, null, default)).Monster(randomTownsWomanActionSetAndMonster.Item2).Age(MBRandom.RandomInt(minValue, maxValue)), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddOutdoorWandererBehaviors), "npc_common_limited", false, relation, randomTownsWomanActionSetAndMonster.Item1, true, false, null, false, false, true);
            return false;
        }
    }
}