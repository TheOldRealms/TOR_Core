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
using TOR_Core.Extensions;

namespace TOR_Core.HarmonyPatches
{
    /// <summary>Patches that filter monstrous units out of garrisons, double town/village NPC spawn rates, race-preserve CharacterObject.CreateFrom, and reskin townsfolk children.</summary>
    [HarmonyPatch]
    public static class CharacterObjectPatches
    {
        /// <summary>Removes monstrous troops from the garrison roster so towns and villages don't spawn them as guards.</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GuardsCampaignBehavior), "InitializeGarrisonCharacters")]
        public static void ExcludeMonstersFromSettlementGuards(List<(CharacterObject, int)> ____garrisonTroops)
        {
            ____garrisonTroops.RemoveAll(troop => troop.Item1.IsMonstrous());
        }

        /// <summary>Retries the troop-tree lookup when vanilla returns an empty list.</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterHelper), "GetTroopTree")]
        public static void TroopTreePatch(ref IEnumerable<CharacterObject> __result, CharacterObject baseTroop)
        {
            if (!__result.ToList().Any())
            {
                __result = CharacterHelper.GetTroopTree(baseTroop);
            }
        }

        /// <summary>Preserves the source character's Race on CharacterObject.CreateFrom clones.</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterObject), "CreateFrom")]
        public static void TroopTreePatch(ref CharacterObject __result, CharacterObject character)
        {
            __result.Race = character.Race;
        }

        /// <summary>Replaces vanilla's male-child townsfolk with an adult townsman so monstrous races don't produce child models.</summary>
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
        
        /// <summary>Doubles the townsfolk NPC spawn rate.</summary>
        [HarmonyPatch(typeof(CommonTownsfolkCampaignBehavior), "GetSpawnRate")]
        public static class CommonTownsfolkCampaignBehavior_GetSpawnRate_Patch
        {
            static void Postfix(ref float __result)
            {
                __result *= 2;
            }
        }

        /// <summary>Doubles the villager NPC spawn rate.</summary>
        [HarmonyPatch(typeof(CommonVillagersCampaignBehavior), "GetSpawnRate")]
        public static class CommonVillagersCampaignBehavior_GetSpawnRate_Patch
        {
            static void Postfix(ref float __result)
            {
                __result *= 2;
            }
        }

        /// <summary>Replaces vanilla's male-teen townsfolk with an adult townsman.</summary>
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

        /// <summary>Replaces vanilla's female-child townsfolk with an adult townswoman.</summary>
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

        /// <summary>Replaces vanilla's female-teen townsfolk with an adult townswoman.</summary>
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

        /// <summary>Replaces vanilla's male-child villager with an adult villager.</summary>
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

        /// <summary>Replaces vanilla's male-teen villager with an adult villager.</summary>
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

        /// <summary>Replaces vanilla's female-child villager with an adult villager.</summary>
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

        /// <summary>Replaces vanilla's female-teen villager with an adult villager.</summary>
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