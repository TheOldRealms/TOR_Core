using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TOR_Core.Extensions;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch(typeof(MapEvent), "CanPartyJoinBattle")]
    public static class MapEventJoinRestrictionsPatch
    {
        [HarmonyPostfix]
        public static void CanPartyJoinBattlePostfix(MapEvent __instance, PartyBase party, BattleSideEnum side, ref bool __result)
        {
            if (!__result)
            {
                return;
            }

            var model = Campaign.Current.Models.GetReinforcementRestrictionModel();
            if (model == null)
            {
                return;
            }

            if (!model.CanPartyJoinRequestedSide(__instance, party, side))
            {
                __result = false;
            }
        }
    }
}