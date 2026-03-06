using Helpers;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Chaos
{
    public class ChaosCampaignBehavior : CampaignBehaviorBase
    {
        [SaveableField(0)]
        private double _lastUprisingTime = default;
        private const int _minimumElapsedDaysBetweenUprisings = 126;
        private bool _hasTriggered = false;
        private const int _maxNumberOfChaosLords = 10;
        private const int _numberOfChaosLordsToSpawnPerUprising = 4;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, EnforceWarWithChaos);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
            CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, OnNewGameStarted);
        }

        private void OnNewGameStarted(CampaignGameStarter starter)
        {
            _lastUprisingTime = CampaignTime.Now.ToDays;
        }

        private void DailyTick() //this could be put into a weekly tick probably if we aren't particularly picky about which day in the week it occurs on
        {
            if (CampaignTime.Now.ToDays > _lastUprisingTime + _minimumElapsedDaysBetweenUprisings && !_hasTriggered)
            {
                var allEligibleKingdoms = Kingdom.All.WhereQ(x => (x.Culture.StringId == TORConstants.Cultures.EMPIRE || x.Culture.StringId == TORConstants.Cultures.BRETONNIA) && x.Fiefs.WhereQ(f => f.IsTown).AnyQ());
                if (!allEligibleKingdoms.AnyQ()) return;
                var mostPowerfulKingdom = allEligibleKingdoms.MaxBy(x => x.CurrentTotalStrength);

                var eligibleSettlements = mostPowerfulKingdom.Fiefs.WhereQ(x =>
                x.IsTown &&
                !x.IsUnderSiege &&
                !x.OwnerClan.IsRebelClan &&
                x.Settlement.Party.MapEvent == null); //Sly : I've left in the possibility for the player's current settlement to have the chaos rebellion as I think it will be a nice surprise to suddenly be kicked out while waiting in the settlement and a bunch of chaos parties to spawn and rush out after them.

                if (eligibleSettlements.Any())
                {
                    var settlement = eligibleSettlements.MinBy(x => x.Loyalty);
                    StartChaosUprisingEvent(settlement.Settlement);
                    _hasTriggered = true;
                    _lastUprisingTime = CampaignTime.Now.ToDays;
                }
            }
            else
            {
                _hasTriggered = false;
            }
        }

        private void StartChaosUprisingEvent(Settlement settlement)
        {
            CreateChaosPartyAndClan(settlement);
            ApplyChaosConsequencesToSettlement(settlement);
            settlement.Town.FoodStocks = settlement.Town.FoodStocksUpperLimit();
            settlement.Militia = 300f;
            TORCampaignEvents.Instance.OnChaosUprisingStarted(settlement);
        }

        private void ApplyChaosConsequencesToSettlement(Settlement settlement)
        {
            settlement.Town.GarrisonParty.MemberRoster.Clear();
            settlement.Militia = 0f;
            if (settlement.MilitiaPartyComponent != null)
            {
                DestroyPartyAction.Apply(null, settlement.MilitiaPartyComponent.MobileParty);
            }
            settlement.Town.GarrisonParty.MemberRoster.AddToCounts(settlement.OwnerClan.Culture.BasicTroop, 100, false, 0, 0, true, -1);
            settlement.Town.GarrisonParty.MemberRoster.AddToCounts((settlement.OwnerClan.Culture.BasicTroop.UpgradeTargets.Length != 0) ? settlement.OwnerClan.Culture.BasicTroop.UpgradeTargets.GetRandomElement() : settlement.OwnerClan.Culture.BasicTroop, 150, false, 0, 0, true, -1);
            settlement.Town.Loyalty = 100f;
            settlement.Town.InRebelliousState = false;
        }

        private void CreateChaosPartyAndClan(Settlement settlement)
        {
            CultureObject chaosCulture = MBObjectManager.Instance.GetObject<CultureObject>("chaos_culture");
            if (chaosCulture == null) return;

            Clan clan = Clan.All.FirstOrDefault(x => x.StringId == "chaos_clan_1");
            if (clan == null) return;
            clan.IsNoble = true;
            clan.AddRenown(MBRandom.RandomInt(200, 300), false);

            if (clan.AliveLords.WhereQ(x => x.IsAlive).Count() < _maxNumberOfChaosLords)
            {
                MBReadOnlyList<CharacterObject> heroTemplates = chaosCulture.LordTemplates;
                for (int i = 0; i < _numberOfChaosLordsToSpawnPerUprising; i++)
                {
                    if (clan.AliveLords.WhereQ(x => x.IsAlive).Count() + 1 < _maxNumberOfChaosLords)
                    {
                        CreateChaosHero(heroTemplates.GetRandomElement(), settlement, chaosCulture, clan);
                    }
                }
            }

            foreach (var comp in clan.WarPartyComponents)
            {
                var mobileParty = comp.MobileParty;
                if (mobileParty != null &&
                    mobileParty.Army == null &&
                    mobileParty.AttachedTo == null &&
                    mobileParty.CurrentSettlement == null &&
                    mobileParty.BesiegedSettlement == null &&
                    comp.Party.MapEvent == null &&
                    mobileParty.LeaderHero != null &&
                    comp.Party.IsValid &&
                    mobileParty.IsActive)
                {
                    var relocationPosition = NavigationHelper.FindReachablePointAroundPosition(
                        settlement.GatePosition,
                        mobileParty.NavigationCapability,
                        8f,
                        1f);

                    mobileParty.Position = relocationPosition;
                    comp.Party.SetVisualAsDirty();
                }
            }

            ChangeOwnerOfSettlementAction.ApplyByRebellion(clan.Leader, settlement);

            var chosenGovernor = clan.AliveLords
                .WhereQ(x => x.IsAlive && x.GovernorOf == null && x != clan.Leader)
                .GetRandomElementInefficiently();

            if (chosenGovernor != null)
            {
                var chosenGovernorParty = chosenGovernor.PartyBelongedTo;

                ChangeGovernorAction.Apply(settlement.Town, chosenGovernor);

                bool governorSuccessfullyLeftOldParty = chosenGovernorParty != null &&
                                                        chosenGovernor.PartyBelongedTo != chosenGovernorParty;

                bool oldGovernorPartyShouldBeDisbanded = governorSuccessfullyLeftOldParty &&
                                                         chosenGovernorParty.IsActive &&
                                                         chosenGovernorParty.MapEvent == null;

                if (oldGovernorPartyShouldBeDisbanded)
                {
                    DestroyPartyAction.ApplyForDisbanding(chosenGovernorParty, settlement);
                }
            }

            clan.Leader.ChangeHeroGold(100000);

            var chaosKingdom = Kingdom.All.FirstOrDefault(x => x.Name == clan.Name);

            if (chaosKingdom == null)
            {
                Campaign.Current.KingdomManager.CreateKingdom(clan.Name, clan.InformalName, chaosCulture, clan);
            }
            else if (chaosKingdom.IsEliminated)
            {
                chaosKingdom.ReactivateKingdom();
                ChangeKingdomAction.ApplyByJoinToKingdom(clan, chaosKingdom);
                chaosKingdom.RulingClan = clan;
            }
            EnforceWarWithChaos(null);
        }

        private void CreateChaosHero(CharacterObject characterObject, Settlement settlement, CultureObject culture, Clan clan)
        {
            Hero hero = HeroCreator.CreateSpecialHero(characterObject, settlement, null, null, MBRandom.RandomInt(25, 40));
            //hero.HeroDeveloper.InitializeHeroDeveloper(false, characterObject); // Sly : CreateSpecialHero already does this - commented out to see if any issue occurs, but can be removed if someone comes across this in a couple months and no rebellions have had issues
            hero.Clan = clan;
            var party = MobilePartyHelper.SpawnLordParty(hero, settlement);
            party.MemberRoster.AddToCounts(culture.BasicTroop, 50);
        }

        private void EnforceWarWithChaos(CampaignGameStarter starter)
        {
            List<Clan> chaosClans = Clan.NonBanditFactions.Where(x => x.Culture.StringId == "chaos_culture").ToList();
            List<Clan> nonChaosClans = Clan.NonBanditFactions.Where(x => x.Culture.StringId != "chaos_culture").ToList();
            List<Kingdom> chaosKingdoms = Kingdom.All.Where(x => x.Culture.StringId == "chaos_culture").ToList();
            List<Kingdom> nonChaosKingdoms = Kingdom.All.Where(x => x.Culture.StringId != "chaos_culture").ToList();

            //Set chaos kingdoms eternal enemy of all non-chaos kingdoms
            foreach (var nonChaosKingdom in nonChaosKingdoms)
            {
                foreach (var chaosKingdom in chaosKingdoms)
                {
                    if (!chaosKingdom.IsAtWarWith(nonChaosKingdom))
                    {
                        FactionManager.DeclareWar(chaosKingdom, nonChaosKingdom);
                    }
                }
            }
            //Set roaming minor faction named chaos clans enemy of all non-chaos kingdoms
            foreach (var nonChaosKingdom in nonChaosKingdoms)
            {
                foreach (var chaosClan in chaosClans)
                {
                    if (!chaosClan.IsAtWarWith(nonChaosKingdom))
                    {
                        FactionManager.DeclareWar(chaosClan, nonChaosKingdom);
                    }
                }
            }
            //Set roaming minor faction named chaos clans enemy of all non-chaos clans
            foreach (var nonChaosClan in nonChaosClans)
            {
                foreach (var chaosClan in chaosClans)
                {
                    if (!chaosClan.IsAtWarWith(nonChaosClan))
                    {
                        FactionManager.DeclareWar(chaosClan, nonChaosClan);
                    }
                }
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_lastUprisingTime", ref _lastUprisingTime);
        }
    }
}