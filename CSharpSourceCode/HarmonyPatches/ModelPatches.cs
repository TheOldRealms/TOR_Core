using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;

namespace TOR_Core.HarmonyPatches;

[HarmonyPatch]
public static class ModelPatches
{
    
    // this patch ensures that the randomly AI hired  mercenary costs , while being a vassal in a kingdom, are not getting crazy high. Especially in smaller kingdoms.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DefaultClanFinanceModel), "CalculateShareFactor")]
    public static bool Prefix(ref float __result, Clan clan)
    {
        if (clan == Clan.PlayerClan &&  clan.Kingdom !=null && clan.Kingdom.RulingClan != Clan.PlayerClan)
        {
            __result = 0;
            return false;
        }

        return true;
    }
}