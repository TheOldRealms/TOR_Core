using System.Windows.Forms;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class EncounterPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerEncounter), "Init", typeof(PartyBase), typeof(PartyBase), typeof(Settlement))]
        public static void Postfix1(PartyBase attackerParty, PartyBase defenderParty, Settlement settlement = null)
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(VillageHostileActionCampaignBehavior), "hostile_action_village_on_init")]
        public static bool Prefix1()
        {
            if (PlayerEncounter.EncounterSettlement == null)
            {
                if (Hero.MainHero.IsEnlisted())
                {
                    var lord = Hero.MainHero.GetEnlistingHero();

                    var t = MapEvent.PlayerMapEvent;
                    var settlement = lord.CurrentSettlement;
                    if (settlement != null)
                    {
                        MBTextManager.SetTextVariable("VILLAGE_NAME", settlement.Name, false);
                    }
                }
                else MBTextManager.SetTextVariable("VILLAGE_NAME", "unknown_settlement", false);
                return false;
            }
            else return true;
        }
        
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(VillageHostileActionCampaignBehavior), "wait_menu_start_raiding_on_condition")]
        public static bool Prefix1(ref bool __result)
        {
            if (Hero.MainHero.IsEnlisted())
            {
                __result = false;

                GameMenu.SwitchToMenu("hireling_menu");

                return false;
            }

            return true;
        }
    }
}
