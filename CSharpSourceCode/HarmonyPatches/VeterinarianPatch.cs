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
}
