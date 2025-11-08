using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Assimilation
{
    public class AssimilationCampaignBehavior : CampaignBehaviorBase
    {
        private Dictionary<Settlement, CultureObject> _settlementCulturePairs = [];
        private Dictionary<Settlement, CultureObject> _originalSettlementCulturePairs = [];

        public static CultureObject GetOriginalCultureForSettlement(Settlement settlement)
        {
            if (Campaign.Current == null || Campaign.Current.GetCampaignBehavior<AssimilationCampaignBehavior>() == null) return settlement.Culture;
            
            var instance = Campaign.Current.GetCampaignBehavior<AssimilationCampaignBehavior>();
            if (instance._originalSettlementCulturePairs.ContainsKey(settlement)) return instance._originalSettlementCulturePairs[settlement];
            else return settlement.Culture;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, SettlementOwnerChanged);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, OnNewGameStart);
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, BeforeSave);
            CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, OnTroopRecruited);
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, DailyTickSettlement);
            CampaignEvents.OnLootDistributedToPartyEvent.AddNonSerializedListener(this, OnDistributeLootToParty);
        }

        private void OnDistributeLootToParty(PartyBase winnerParty, PartyBase defeatedParty, ItemRoster roster)
        {
            foreach (var rosterElement in winnerParty.MemberRoster.GetTroopRoster().ToList())
            {
                SwapTroopsIfNeeded(winnerParty.LeaderHero, winnerParty.MemberRoster, rosterElement.Character, rosterElement.Number);
            }
        }

        //Sly : this can instead be running on the TroopGivenToSettlement event. Initial swap on ownership changing, then donated troops from parties. The issue that would arise is the settlement's garrison recruiting troops in the days following the ownership change as the troops offered by notables aren't currently updated to the incoming culture. Though, the notables also have the issue where their culture property is changed, but not their appearance, so there's a handful of changes in tandem which need fixing to smooth out the cultural change. Also, the SettlementOwnerChanged method would need to make use of SwapTroopsIfNeeded.
        private void DailyTickSettlement(Settlement settlement)
        {
            if (settlement?.Town?.GarrisonParty?.MemberRoster == null || settlement?.Owner == null) return;

            if (settlement.IsUnderSiege || settlement.InRebelliousState) return;

            if (settlement.OwnerClan.Equals(Clan.PlayerClan)) return;

            foreach (var rosterElement in settlement.Town.GarrisonParty.MemberRoster.GetTroopRoster().ToList())
            {
                SwapTroopsIfNeeded(settlement.Owner, settlement.Town.GarrisonParty?.MemberRoster, rosterElement.Character, rosterElement.Number);
            }
        }

        //this occurs after same event in TORAIRecruitmentCampaignBehavior
        private void OnTroopRecruited(Hero recruiter, Settlement settlement, Hero recruitmentSource, CharacterObject troop, int count)
        {
            SwapTroopsIfNeeded(recruiter, recruiter?.PartyBelongedTo?.MemberRoster, troop, count);
        }

        private void OnNewGameStart(CampaignGameStarter starter, int index)
        {
            if(index == 0)
            {
                foreach (var settlement in Settlement.All)
                {
                    _settlementCulturePairs.Add(settlement, settlement.Culture);
                    _originalSettlementCulturePairs.Add(settlement, settlement.Culture);
                }
            }
        }

        private void BeforeSave()
        {
            foreach(var settlement in Settlement.All)
            {
                if (_settlementCulturePairs.ContainsKey(settlement))
                {
                    _settlementCulturePairs[settlement] = settlement.Culture;
                }
                else
                {
                    _settlementCulturePairs.Add(settlement, settlement.Culture);
                }
            }
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            foreach(var settlement in _settlementCulturePairs.Keys)
            {
                CultureObject settlementCulture = _settlementCulturePairs[settlement];
                if (settlement.Culture != settlementCulture)
                {
                    settlement.Culture = settlementCulture;
                    foreach(var notable in settlement.Notables)
                    {
                        if (notable.Culture != settlement.Culture) notable.Culture = settlement.Culture;
                    }
                }
            }
        }

        //the owner is the party leader for a mobile party, or the clan leader for a garrison
        //maybe base this on the culture of the clan leader so that the player recruiting wanderers from other cultures won't convert troops into those cultures leading to armies with varied cultures?
        private void SwapTroopsIfNeeded(Hero owner, TroopRoster roster, CharacterObject troop, int count)
        {
            if (troop.Culture == owner?.Culture) return;

            if (owner == null) return;

            if ((troop.Culture.StringId == TORConstants.Cultures.SYLVANIA || troop.Culture.StringId == TORConstants.Cultures.MOUSILLON) && 
                (owner.Culture.StringId == TORConstants.Cultures.SYLVANIA || owner.Culture.StringId == TORConstants.Cultures.MOUSILLON))
            {
                return;
            }

            if (owner.Clan == null || owner == Hero.MainHero || owner.PartyBelongedTo?.Party == null || roster == null || troop.IsHero)
            {
                return;
            }

            roster.ValidateTroopListCache(); //Sly : the roster passed in has its properties changed by the Add and Remove further below, but the underlying data cache for the elements is unchanged. Performing the validation forces the cached values to be updated so that the TroopCount below is not counting a stale copy and avoids the issue of a noble recruiting multiple troops simultaneosuly and the subsequent troop swaps using stale values
            var verifiedAmount = roster.GetTroopCount(troop); 
            if (verifiedAmount < 1) return;
            count = (int)verifiedAmount; //Sly : if multiple behaviours remove troops (which makes use of the roster index), then the roster index may vary and a troop with no valid index causes the troop roster to fetch data[-1], ie. TORAIRecruitmentCampaignBehavior



            //Sly : in the future, this should be cached on load so that all of these replacements aren't calculated constantly. Recruitments from notables by the AI are a series of single troops recruited so if they recruit 10 troops, this is passed through 10 times. Cache and performing lookups is probably going to be much faster for a small increase in memory.
            if (owner.Clan.DefaultPartyTemplate == null) return;

            List<CharacterObject> templateCharacters = [];
            foreach (PartyTemplateStack stack in owner.Clan.DefaultPartyTemplate.Stacks)
            {
                templateCharacters = [.. templateCharacters, .. CharacterHelper.GetTroopTree(stack.Character)];
            }

            FormationClass troopClass = FormationClassExtensions.FallbackClass(troop.DefaultFormationClass);
            List<CharacterObject> ofSameFormation = templateCharacters.Where(c => FormationClassExtensions.FallbackClass(c.DefaultFormationClass) == troopClass).ToList();

            // first check for a template character of the same formation and same elite status
            CharacterObject replacement = DetermineReplacement(ofSameFormation, troop.Tier, IsEliteTroop(troop));

            // next check for template character of any formation and same elite status
            replacement ??= DetermineReplacement(templateCharacters, troop.Tier, IsEliteTroop(troop));

            // next check for a template character of the same formation and the opposite elite status
            replacement ??= DetermineReplacement(ofSameFormation, troop.Tier, !IsEliteTroop(troop));

            // finally check for template character of any formation and the opposite elite status
            replacement ??= DetermineReplacement(templateCharacters, troop.Tier, !IsEliteTroop(troop));

            if(replacement != null)
            {
                roster.RemoveTroop(troop, count); //Sly : will use FindIndexOfTroop which returns -1 if the troop is not in the roster, but it passes it blindly into AddToCountsAtIndex which naively manipulates it trying to access the data[-1]
                roster.AddToCounts(replacement, count);
                roster.RemoveZeroCounts();

                if (replacement.Tier != troop.Tier || IsEliteTroop(replacement) != IsEliteTroop(troop))
                {
                    // adjust recruitment gold
                    int troopCost = (int)Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(troop, owner).ResultNumber;
                    int replacementCost = (int)Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(replacement, owner).ResultNumber;
                    GiveGoldAction.ApplyBetweenCharacters(null, owner, (troopCost - replacementCost) * count);
                }
            }
        }

        private CharacterObject DetermineReplacement(List<CharacterObject> templateCharacters, int troopTier, bool useElite)
        {
            CharacterObject replacement = null;
            replacement = templateCharacters.Where(t => t.Tier == troopTier && IsEliteTroop(t) == useElite).TakeRandom(1).FirstOrDefault();
            replacement ??= templateCharacters.Where(t => t.Tier == troopTier).TakeRandom(1).FirstOrDefault();
            replacement ??= templateCharacters.TakeRandom(1).FirstOrDefault();

            return replacement;
        }

        private bool IsEliteTroop(CharacterObject unit)
        {
            var tree = CharacterHelper.GetTroopTree(unit.Culture.EliteBasicTroop);
            return tree.Contains(unit);
        }

        private void SettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            if(IsSpecialSettlement(settlement))
                return;
            
            if(newOwner.MapFaction != null && oldOwner.MapFaction != null)
            {
                if (newOwner.MapFaction.Culture != settlement.Culture)
                {
                    settlement.Culture = newOwner.MapFaction.Culture;
                    foreach(var notable in settlement.Notables)
                    {
                        if (notable.Culture != settlement.Culture) notable.Culture = settlement.Culture;
                    }
                    if(settlement.BoundVillages != null && settlement.BoundVillages.Count > 0)
                    {
                        foreach(var village in settlement.BoundVillages)
                        {
                            village.Settlement.Culture = settlement.Culture;
                            foreach(var villageNotable in village.Settlement.Notables)
                            {
                                if (villageNotable.Culture != settlement.Culture) villageNotable.Culture = settlement.Culture;
                            }
                        }
                    }
                }
            }
        }

        private bool IsSpecialSettlement(Settlement settlement)
        {
            return settlement.StringId == "castle_BK1";
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_settlementCulturePairs", ref _settlementCulturePairs);
            dataStore.SyncData("_originalSettlementCulturePairs", ref _originalSettlementCulturePairs);
        }
    }
}
