using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Companions
{
    public class TORAICompanionCampaignBehavior : CampaignBehaviorBase
    {
        private const string Keep = "lordshall";
        public override void RegisterEvents()
        {
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
            CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, OnPartyCreated);
            CampaignEvents.OnPartyDisbandedEvent.AddNonSerializedListener(this, OnPartyDisbanded);
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnPartyDestroyed);
            CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.CanHeroBecomePrisonerEvent.AddNonSerializedListener(this, new ReferenceAction<Hero, bool>(this.CanHeroBecomePrisoner));
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
            return;

            void OnMapEventEnded(MapEvent mapEvent)
            {
                var side = mapEvent.DefeatedSide;
                var parties = mapEvent.InvolvedParties;

                foreach (var party in parties.Where(party => party.IsMobile&& party.MobileParty.IsLordParty))
                {
                    if (party.MobileParty.Party.NumberOfAllMembers <= party.MobileParty.GetMemberHeroes().Count)
                    {
                        HandleRemoveParty(party.MobileParty);
                    }
                    
                }
            }
        }

        /// <remarks>
        /// Don't feel like playing hide and seek with the prisoner taking and loot distribution after battles and the companions being teleported away, so let's attempt to avoid that completely.
        /// </remarks>
        private void CanHeroBecomePrisoner(Hero hero, ref bool result)
        {
            if (hero.IsAICompanion()) result = false;
        }

        private void OnNewGameCreated(CampaignGameStarter starter, int index)
        {
            if (index == CampaignEvents.OnNewGameCreatedPartialFollowUpEventMaxIndex - 1)
            {
                foreach(var party in MobileParty.AllLordParties)
                {
                    OnPartyCreated(party);
                }
            }
        }

        private void OnPartyDestroyed(MobileParty party, PartyBase destroyer) => HandleRemoveParty(party);

        private void OnPartyDisbanded(MobileParty party, Settlement settlement) => HandleRemoveParty(party);

        private void HandleRemoveParty(MobileParty party)
        {
            //CanBecomePrisoner returning false hopefully makes this redundant. No, breakpoints are still hit at least in the case of ai party vs ai party. Is CBP actually making this guaranteed to be hit because they can't be removed from the party by becoming a prisoner?
            //has the hero already been added to the prison roster to divide up after battle? what resolves first, the forced settlement entrance or the loot distribution?
            if (party.GetMemberHeroes().Any(x => x.IsAICompanion()))
            {
                foreach (var companion in party.GetMemberHeroes().Where(x => x.IsAICompanion()))
                {
                    if (party.MemberRoster.Contains(companion.CharacterObject)) party.MemberRoster.AddToCounts(companion.CharacterObject, -1);
                    MoveHeroToHomeTown(companion);
                }
            }
        }

        private void MoveHeroToHomeTown(Hero hero)
        {
            //HeroSpawnCampaignBehavior.OnHeroDailyTick : is this implicated in some way?
            var town = hero?.Clan?.Leader?.HomeSettlement?.Town;
            if (town == null) town = Town.AllTowns.GetRandomElementWithPredicate(x => x.Settlement.Culture == hero.Culture);
            if (town == null) town = Town.AllTowns.GetRandomElementInefficiently();
            TeleportHeroAction.ApplyImmediateTeleportToSettlement(hero, town.Settlement); //this is blocked if the hero is in a party that's in a map event, but hopefully CanBecomePrisoner will prevent that companions from entering a bugged state and this isn't needed
            //EnterSettlementAction.ApplyForCharacterOnly(hero, town.Settlement);
        }

        private void OnPartyCreated(MobileParty party)
        {
            if(party != null && party.LeaderHero != null && party.LeaderHero.Clan != null && party.LeaderHero.Clan.Leader != null && party.LeaderHero.Clan.Leader == party.LeaderHero)
            {
                var companions = party.LeaderHero.Clan.Heroes.Where(x => x.IsAICompanion());
                foreach(var companion in companions)
                {
                    if (!party.GetMemberHeroes().Contains(companion)) AddHeroToPartyAction.Apply(companion, party);
                }
            }
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (!settlement.IsFortification || party != MobileParty.MainParty) return;
            var location = settlement.LocationComplex?.GetLocationWithId(Keep);
            if (location == null) return;
            List<Hero> companions = new List<Hero>();
            foreach(var partyInSettlement in settlement.Parties)
            {
                foreach (var companion in partyInSettlement.GetMemberHeroes().Where(x => x.IsAICompanion()))
                {
                    companions.Add(companion);
                }
            }
            foreach(var companion in settlement.HeroesWithoutParty.Where(x=>x.IsAICompanion()))
            {
                companions.Add(companion);
            }
            foreach(var companion in companions)
            {
                AddLocationCharacterToLocation(companion, location);
            }
        }

        private void OnSettlementLeft(MobileParty party, Settlement settlement)
        {
            if (!settlement.IsTown) return;
            if (party != null && party.GetMemberHeroes().Any(x => x.IsAICompanion()))
            {
                foreach (var companion in party.GetMemberHeroes().Where(x => x.IsAICompanion()))
                {
                    settlement.LocationComplex.RemoveCharacterIfExists(companion);
                }
            }
        }

        private static void AddLocationCharacterToLocation(Hero hero, Location location)
        {
            LocationCharacter locationCharacter = new LocationCharacter(new AgentData(new SimpleAgentOrigin(hero.CharacterObject)).Monster(Game.Current.DefaultMonster),
                SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "npc_common", true, LocationCharacter.CharacterRelations.Neutral, null, true);
            location.AddCharacter(locationCharacter);
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}