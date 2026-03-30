using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches;

[HarmonyPatch(typeof(CaravanPartyComponent), "GetMountAndHarnessVisualIdsForPartyIcon")]
public static class CaravanVisualPatch
{
    [HarmonyPostfix]
    public static void OverrideCaravanVisuals(
        PartyBase party,
        ref string mountStringId,
        ref string harnessStringId)
    {
        string cultureId = party.MapFaction?.Culture?.StringId ?? string.Empty;

        // Override greenskin caravans to use boars instead of camels
        if (cultureId == TORConstants.Cultures.GREENSKIN)
        {
            mountStringId = "tor_greenskin_mount_boar_001";
            harnessStringId = "tor_greenskin_mountarmor_boar_saddle_001";
        }
        // Override vampire counts to use mules instead of camels
        else if (cultureId == TORConstants.Cultures.SYLVANIA)
        {
            mountStringId = "mule";
            switch (party.Index % 3)
            {
                case 0:
                    harnessStringId = "mule_load_a";
                    break;
                case 1:
                    harnessStringId = "mule_load_b";
                    break;
                default:
                    harnessStringId = "mule_load_c";
                    break;
            }
        }
    }
}
