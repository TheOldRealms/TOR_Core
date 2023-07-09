using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class EncounterPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerEncounter), "Init", typeof(PartyBase), typeof(PartyBase), typeof(Settlement))]
        public static void Postfix2(PartyBase attackerParty, PartyBase defenderParty, Settlement settlement = null)
        {
            if (defenderParty.MapEvent != null && settlement != null && defenderParty != MobileParty.MainParty.Party && attackerParty == MobileParty.MainParty.Party)
            {
                var mapEvent = defenderParty.MapEvent;
                if (mapEvent.CanPartyJoinBattle(MobileParty.MainParty.Party, BattleSideEnum.Defender))
                {
                    MobileParty.MainParty.Party.MapEventSide = mapEvent.DefenderSide;
                }
                else if (mapEvent.CanPartyJoinBattle(MobileParty.MainParty.Party, BattleSideEnum.Attacker))
                {
                    MobileParty.MainParty.Party.MapEventSide = mapEvent.AttackerSide;
                }
            }
        }
    }
}
