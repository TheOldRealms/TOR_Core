using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TOR_Core.HarmonyPatches;

[HarmonyPatch]
public static class VeterinarianPatch
{
    private const float NORMAL_MOUNT_RECOVERY_CHANCE = 0.05f; // +mounts introduced by tor
    private const float NOBLE_MOUNT_RECOVERY_CHANCE = 0.25f;
    private const float WAR_MOUNT_RECOVERY_CHANCE = 0.5f;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CampaignBattleRecoveryBehavior), "RecoverMountWithChance")]
    private static bool Prefix(TroopRosterElement troopRosterElement, int count, PartyBase party)
    {
        EquipmentElement equipmentElement = troopRosterElement.Character.Equipment[10];
        ItemObject mountItem = equipmentElement.Item;

        if (mountItem == null || !mountItem.IsMountable)
        {
            return false;
        }

        float chance = GetMountRecoveryChance(mountItem);

        if (chance < 0f)
        {
            return true;
        }

        for (int i = 0; i < count; i++)
        {
            if (MBRandom.RandomFloat < chance)
            {
                party.ItemRoster.AddToCounts(mountItem, 1);
            }
        }
        return false;
    }

    private static float GetMountRecoveryChance(ItemObject mountItem)
    {
        ItemCategory category = mountItem.ItemCategory;

        if (category == DefaultItemCategories.Horse)
        {
            return NORMAL_MOUNT_RECOVERY_CHANCE;
        }
        if (category == DefaultItemCategories.NobleHorse)
        {
            return NOBLE_MOUNT_RECOVERY_CHANCE;
        }
        if (category == DefaultItemCategories.WarHorse)
        {
            return WAR_MOUNT_RECOVERY_CHANCE;
        }
        return -1f;
    }
}
