using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TOR_Core.HarmonyPatches;

[HarmonyPatch]
public static class InventoryResetPatch
{
    private static readonly Type PartyEquipmentType = AccessTools.Inner(typeof(InventoryLogic), "PartyEquipment");
    private static readonly FieldInfo CharacterEquipmentsBackingField =
        AccessTools.Field(PartyEquipmentType, "<CharacterEquipments>k__BackingField");

    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(PartyEquipmentType, "InitializeCopyFrom", new[] { typeof(MobileParty) });
    }

    private static bool Prefix(object __instance, MobileParty party)
    {
        var characterEquipments = new Dictionary<CharacterObject, Equipment[]>();

        for (var i = 0; i < party.MemberRoster.Count; i++)
        {
            var character = party.MemberRoster.GetElementCopyAtIndex(i).Character;
            if (!character.IsHero)
            {
                continue;
            }

            var hero = character.HeroObject;

            var battleEquipmentSnapshot = new Equipment(character.FirstBattleEquipment);
            battleEquipmentSnapshot.FillFrom(hero.BattleEquipment);

            var civilianEquipmentSnapshot = new Equipment(character.FirstCivilianEquipment);
            civilianEquipmentSnapshot.FillFrom(hero.CivilianEquipment);

            var stealthEquipmentSnapshot = new Equipment(character.FirstStealthEquipment);

            characterEquipments.Add(character, new[]
            {
                battleEquipmentSnapshot,
                civilianEquipmentSnapshot,
                stealthEquipmentSnapshot
             });

        }
        CharacterEquipmentsBackingField.SetValue(__instance, characterEquipments);
        return false;
    }


    [HarmonyPatch]
    public static class InventoryLogicPartyEquipmentResetEquipmentPatch
    {
        private const bool IsLoggingEnabled = false;

        private static readonly Type PartyEquipmentType = AccessTools.Inner(typeof(InventoryLogic), "PartyEquipment");
        private static readonly FieldInfo CharacterEquipmentsBackingField =
            AccessTools.Field(PartyEquipmentType, "<CharacterEquipments>k__BackingField");

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            // ResetEquipment no longer takes a MobileParty parameter in 1.4
            return AccessTools.Method(PartyEquipmentType, "ResetEquipment");
        }

        [HarmonyPrefix]
        private static bool Prefix(object __instance)
        {
            var characterEquipments =
                (Dictionary<CharacterObject, Equipment[]>)CharacterEquipmentsBackingField.GetValue(__instance);

            foreach (var kvp in characterEquipments)
            {
                var character = kvp.Key;
                var snapshotSets = kvp.Value;

                var hero = character.HeroObject;

                hero.BattleEquipment.FillFrom(snapshotSets[0]);
                hero.CivilianEquipment.FillFrom(snapshotSets[1]);

                character.FirstBattleEquipment.FillFrom(snapshotSets[0]);
                character.FirstCivilianEquipment.FillFrom(snapshotSets[1]);
                character.FirstStealthEquipment.FillFrom(snapshotSets[2]);
            }
            return false;
        }
    }
}
