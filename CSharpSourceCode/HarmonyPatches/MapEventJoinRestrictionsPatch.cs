using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TOR_Core.Utilities;

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

            if (!BattleJoinCultureRules.CanPartyJoinRequestedSide(__instance, party, side))
            {
                __result = false;
            }
        }

        private static class BattleJoinCultureRules
        {
            public static bool CanPartyJoinRequestedSide(MapEvent mapEvent, PartyBase joiningParty, BattleSideEnum side)
            {
                if (mapEvent == null || joiningParty == null)
                {
                    return true;
                }

                string joiningCultureId = joiningParty.MapFaction?.Culture?.StringId;
                if (!IsRestrictedCulture(joiningCultureId))
                {
                    return true;
                }

                foreach (MapEventParty sideMember in mapEvent.GetMapEventSide(side).Parties)
                {
                    PartyBase sideParty = sideMember.Party;
                    if (sideParty == null || sideParty == joiningParty || !sideParty.IsActive)
                    {
                        continue;
                    }

                    if (HasDirectJoinOverride(joiningParty, sideParty))
                    {
                        continue;
                    }

                    string sideCultureId = sideParty.MapFaction?.Culture?.StringId;
                    if (!IsRestrictedCulture(sideCultureId))
                    {
                        continue;
                    }

                    if (!CanCultureHelpCulture(joiningCultureId, sideCultureId))
                    {
                        return false;
                    }
                }

                return true;
            }

            private static bool HasDirectJoinOverride(PartyBase joiningParty, PartyBase sideParty)
            {
                IFaction joiningFaction = joiningParty.MapFaction;
                IFaction sideFaction = sideParty.MapFaction;

                if (joiningFaction == null || sideFaction == null)
                {
                    return false;
                }

                if (joiningFaction == sideFaction)
                {
                    return true;
                }

                Kingdom joiningKingdom = joiningFaction as Kingdom;
                Kingdom sideKingdom = sideFaction as Kingdom;

                return joiningKingdom != null
                    && sideKingdom != null
                    && joiningKingdom.IsAllyWith(sideKingdom);
            }

            private static bool IsRestrictedCulture(string cultureId)
            {
                return cultureId == TORConstants.Cultures.EMPIRE
                    || cultureId == TORConstants.Cultures.BRETONNIA
                    || cultureId == TORConstants.Cultures.SYLVANIA
                    || cultureId == TORConstants.Cultures.MOUSILLON
                    || cultureId == TORConstants.Cultures.ASRAI
                    || cultureId == TORConstants.Cultures.EONIR
                    || cultureId == TORConstants.Cultures.DAWI
                    || cultureId == TORConstants.Cultures.GREENSKIN
                    || cultureId == TORConstants.Cultures.CHAOS;
            }

            private static bool CanCultureHelpCulture(string helperCultureId, string targetCultureId)
            {
                switch (helperCultureId)
                {
                    case TORConstants.Cultures.EMPIRE:
                        return targetCultureId == TORConstants.Cultures.EMPIRE
                            || targetCultureId == TORConstants.Cultures.BRETONNIA
                            || targetCultureId == TORConstants.Cultures.EONIR
                            || targetCultureId == TORConstants.Cultures.DAWI
                            || targetCultureId == TORConstants.Cultures.SYLVANIA
                            || targetCultureId == TORConstants.Cultures.MOUSILLON
                            || targetCultureId == TORConstants.Cultures.ASRAI;

                    case TORConstants.Cultures.BRETONNIA:
                        return targetCultureId == TORConstants.Cultures.BRETONNIA
                            || targetCultureId == TORConstants.Cultures.EMPIRE
                            || targetCultureId == TORConstants.Cultures.EONIR
                            || targetCultureId == TORConstants.Cultures.DAWI
                            || targetCultureId == TORConstants.Cultures.MOUSILLON;

                    case TORConstants.Cultures.DAWI:
                        return targetCultureId == TORConstants.Cultures.DAWI
                            || targetCultureId == TORConstants.Cultures.EMPIRE
                            || targetCultureId == TORConstants.Cultures.BRETONNIA;

                    case TORConstants.Cultures.SYLVANIA:
                        return targetCultureId == TORConstants.Cultures.SYLVANIA
                            || targetCultureId == TORConstants.Cultures.EMPIRE
                            || targetCultureId == TORConstants.Cultures.MOUSILLON;

                    case TORConstants.Cultures.MOUSILLON:
                        return targetCultureId == TORConstants.Cultures.MOUSILLON
                            || targetCultureId == TORConstants.Cultures.EMPIRE
                            || targetCultureId == TORConstants.Cultures.BRETONNIA
                            || targetCultureId == TORConstants.Cultures.SYLVANIA;

                    case TORConstants.Cultures.ASRAI:
                        return targetCultureId == TORConstants.Cultures.ASRAI
                            || targetCultureId == TORConstants.Cultures.EONIR
                            || targetCultureId == TORConstants.Cultures.EMPIRE;

                    case TORConstants.Cultures.EONIR:
                        return targetCultureId == TORConstants.Cultures.EONIR
                            || targetCultureId == TORConstants.Cultures.EMPIRE
                            || targetCultureId == TORConstants.Cultures.BRETONNIA
                            || targetCultureId == TORConstants.Cultures.ASRAI;

                    case TORConstants.Cultures.GREENSKIN:
                        return targetCultureId == TORConstants.Cultures.GREENSKIN;

                    case TORConstants.Cultures.CHAOS:
                        return targetCultureId == TORConstants.Cultures.CHAOS;

                    default:
                        return true;
                }
            }
        }
    }
}