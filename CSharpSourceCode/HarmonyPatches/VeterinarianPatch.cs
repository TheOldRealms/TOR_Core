using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace TOR_Core.HarmonyPatches;

[HarmonyPatch]
public static class VeterinarianPatch
{
    private const float NORMAL_MOUNT_RECOVERY_CHANCE = 0.01f; // +mounts introduced by tor
    private const float NOBLE_MOUNT_RECOVERY_CHANCE = 0.10f;
    private const float WAR_MOUNT_RECOVERY_CHANCE = 0.25f;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CampaignBattleRecoveryBehavior), "RecoverMountWithChance")]
    private static bool Prefix(TroopRosterElement troopRosterElement, int count, PartyBase party)
    {
        var mountEquipment = troopRosterElement.Character.Equipment[(int)EquipmentIndex.Horse];
        var mountItem = mountEquipment.Item;
        if (mountItem == null)
        {
            return false;
        }

        var mountRecoveryChance = GetMountRecoveryChance(mountItem);

        for (int i = 0; i < count; i++)
        {
            if (MBRandom.RandomFloat < mountRecoveryChance)
            {
                party.ItemRoster.AddToCounts(mountItem, 1);
            }
        }
        return false;
    }

    private static float GetMountRecoveryChance(ItemObject mountItem)
    {

        if (mountItem.ItemCategory == DefaultItemCategories.WarHorse)
        {
            return WAR_MOUNT_RECOVERY_CHANCE;
        }

        if (mountItem.ItemCategory == DefaultItemCategories.NobleHorse)
        {
            return NOBLE_MOUNT_RECOVERY_CHANCE;
        }

        return NORMAL_MOUNT_RECOVERY_CHANCE;
    }

    [HarmonyPatch]
    internal static class VeterinarianPerkDescriptionPatch
    {
        private const string VETERINARIAN_PERK_ID = "MedicineVeterinarian";
        private const string SECONDARY_DESCRIPTION_REPLACEMENT =
            "{=str_tor_veterinarian_description}A variable chance to recover a mount from lost cavalry after battles.";

        private static MethodBase TargetMethod()
        {
            // PerkObject.Initialize overload that takes the descriptions as strings
            foreach (var method in typeof(PerkObject).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.Name != nameof(PerkObject.Initialize))
                {
                    continue;
                }
                var parameters = method.GetParameters();
                if (parameters.Length < 9)
                {
                    continue;
                }

                var looksLikePerkInitialize =
                    parameters[0].ParameterType == typeof(string) &&      // name
                    parameters[1].ParameterType == typeof(SkillObject) && // skill
                    parameters[4].ParameterType == typeof(string) &&      // primaryDescription
                    parameters[8].ParameterType == typeof(string);        // secondaryDescription

                if (looksLikePerkInitialize)
                {
                    return method;
                }
            }
            throw new MissingMethodException(typeof(PerkObject).FullName, "This shouldn't happen, see looksLikePerkInitialize in VeterinarianPatch.cs");
        }

        private static void Prefix(PerkObject __instance, ref string secondaryDescription)
        {
            if (__instance.StringId != VETERINARIAN_PERK_ID)
            {
                return;
            }
            secondaryDescription = SECONDARY_DESCRIPTION_REPLACEMENT;
        }
    }
}


