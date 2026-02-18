using HarmonyLib;
using Helpers;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.PostBattleLoot;
using TOR_Core.CampaignMechanics.ServeAsAHireling;
using TOR_Core.Extensions;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class EncounterPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(VillageHostileActionCampaignBehavior), "village_raid_game_menu_init")]
        public static bool VillageRaidGameMenuInitPrefix(MenuCallbackArgs args)
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
        public static bool WaitMenuStartRaidingOnConditionPrefix(MenuCallbackArgs args, ref bool __result)
        {
            if (Hero.MainHero.IsEnlisted())
            {
                __result = false;

                GameMenu.SwitchToMenu("hireling_menu");

                return false;
            }

            return true;
        }



        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameMenu), "ActivateGameMenu")]
        public static bool ActivateGameMenuPrefix(ref string menuId)
        {
            // When enlisted, intercept encounter menu activation
            if (menuId == "encounter" && Hero.MainHero.IsEnlisted())
            {
                // Let encounter through if player clicked "Join Battle"
                if (ServeAsAHirelingCampaignBehavior.IsStartingBattle)
                {
                    return true;
                }

                var hasActiveBattle = Hero.MainHero.PartyBelongedTo?.MapEvent != null;
                if (hasActiveBattle)
                {
                    menuId = "hireling_battle_menu";
                }
                else
                {
                    menuId = "hireling_menu";
                }
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerEncounter), "Finish")]
        public static bool PlayerEncounterFinishPrefix()
        {
            // Only block Finish during post-battle transitions when enlisted
            // This prevents crashes from AI party ticks while allowing normal siege/encounter flow
            if (Hero.MainHero.IsEnlisted() && ServeAsAHirelingCampaignBehavior.InPostBattleTransition)
            {
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapEvent), "GetMemberRosterReceivingLootShare")]
        public static void GetMemberRosterPostfix(TroopRoster __result)
        {
            if (__result != null && PendingLootedTroopManager.HasPendingModifications)
                PendingLootedTroopManager.ApplyMemberModifications(__result);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapEvent), "GetPrisonerRosterReceivingLootShare")]
        public static void GetPrisonerRosterPostfix(TroopRoster __result)
        {
            if (__result != null && PendingLootedTroopManager.HasPendingModifications)
                PendingLootedTroopManager.ApplyPrisonerModifications(__result);
        }
    }

    [HarmonyPatch(typeof(PartyBaseHelper), "DoesSurrenderIsLogicalForParty")] // villager parties and caravans will no longer surrender if there's a lord helping them
    internal static class CaravanSurrenderPatches
    {
        [HarmonyPrefix]
        private static bool Prefix(MobileParty ourParty, MobileParty enemyParty, float acceptablePowerRatio, ref bool __result)
        {
            if (ourParty == null || (!ourParty.IsCaravan && !ourParty.IsVillager))
            {
                return true;
            }

            var mapEvent = ourParty.MapEvent;
            if (mapEvent == null)
            {
                return true;
            }

            if (!TryGetPartySide(mapEvent, ourParty.Party, out var caravanSide))
            {
                return true;
            }

            foreach (var mapEventParty in mapEvent.PartiesOnSide(caravanSide))
            {
                var mobileParty = mapEventParty.Party?.MobileParty;
                if (mobileParty != null && mobileParty != ourParty && mobileParty.IsLordParty)
                {
                    __result = false;
                    return false;
                }
            }

            return true;
        }

        private static bool TryGetPartySide(MapEvent mapEvent, PartyBase partyBase, out BattleSideEnum side)
        {
            foreach (var attackerParty in mapEvent.PartiesOnSide(BattleSideEnum.Attacker))
            {
                if (attackerParty.Party == partyBase)
                {
                    side = BattleSideEnum.Attacker;
                    return true;
                }
            }

            foreach (var defenderParty in mapEvent.PartiesOnSide(BattleSideEnum.Defender))
            {
                if (defenderParty.Party == partyBase)
                {
                    side = BattleSideEnum.Defender;
                    return true;
                }
            }

            side = default;
            return false;
        }
    }
}