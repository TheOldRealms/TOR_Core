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

        private void DailyTick()
        {
            if (CampaignTime.Now.ToDays > _lastUprisingTime + _minimumElapsedDaysBetweenUprisings && !_hasTriggered)
            {
                var mostPowerfulKingdom = Kingdom.All.WhereQ(x => (x.Culture.StringId == TORConstants.Cultures.EMPIRE || x.Culture.StringId == TORConstants.Cultures.BRETONNIA) && x.Fiefs.Count > 1).MaxBy(x => x.TotalStrength);

                var eligibleSettlements = Settlement.All.WhereQ(x=>x.OwnerClan != null && x.OwnerClan.Kingdom != null && 
                x.OwnerClan.Kingdom == mostPowerfulKingdom &&
                x.IsTown && !x.IsUnderSiege &&
                x.Party.MapEvent == null && x.Party.SiegeEvent == null &&
                !x.OwnerClan.IsRebelClan && Settlement.CurrentSettlement != x
                );

                if(eligibleSettlements.Count() > 0 )
                {
                    var settlement = eligibleSettlements.MinBy(x => x.Town.Loyalty);
                    StartChaosUprisingEvent(settlement);
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

            if (clan.Lords.WhereQ(x => x.IsAlive).Count() < _maxNumberOfChaosLords)
            {
                MBReadOnlyList<CharacterObject> heroTemplates = chaosCulture.LordTemplates;
                for(int i = 0; i < _numberOfChaosLordsToSpawnPerUprising; i++)
                {
                    if(clan.Lords.WhereQ(x => x.IsAlive).Count() + 1 < _maxNumberOfChaosLords)
                    {
                        CreateChaosHero(heroTemplates.GetRandomElement(), settlement, chaosCulture, clan);
                    }
                }
            }

            foreach(var comp in clan.WarPartyComponents)
            {
                if(comp.MobileParty != null && 
                    comp.MobileParty.Army == null && 
                    comp.Party.MapEvent == null && 
                    comp.MobileParty.LeaderHero != null &&
                    comp.Party.IsValid &&
                    comp.MobileParty.IsActive)
                {
                    comp.MobileParty.Position2D = settlement.GatePosition;
                    comp.Party.SetVisualAsDirty();
                    comp.Party.UpdateVisibilityAndInspected();
                }
            }

            ChangeOwnerOfSettlementAction.ApplyByRebellion(clan.Leader, settlement);

            var chosenGovernor = clan.Lords.WhereQ(x => x.IsAlive && x.GovernorOf == null && x != clan.Leader).GetRandomElementInefficiently();
            var chosenGovernorParty = MobileParty.All.FirstOrDefaultQ(x => x.LeaderHero == chosenGovernor);

            ChangeGovernorAction.Apply(settlement.Town, chosenGovernor);

            if (chosenGovernorParty != null) DestroyPartyAction.ApplyForDisbanding(chosenGovernorParty, settlement);

            clan.Leader.ChangeHeroGold(100000);

            var chaosKingdom = Kingdom.All.FirstOrDefault(x => x.Name == clan.Name);
            
            if(chaosKingdom == null)
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
            hero.HeroDeveloper.InitializeHeroDeveloper(false, characterObject);
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
                        FactionManager.DeclareWar(chaosKingdom, nonChaosKingdom, true);
                    }
                    var stance = chaosKingdom.GetStanceWith(nonChaosKingdom);
                    stance.IsAtConstantWar = true;
                    stance.IsAtWar = true;
                }
            }
            //Set roaming minor faction named chaos clans enemy of all non-chaos kingdoms
            foreach (var nonChaosKingdom in nonChaosKingdoms)
            {
                foreach (var chaosClan in chaosClans)
                {
                    if (!chaosClan.IsAtWarWith(nonChaosKingdom))
                    {
                        FactionManager.DeclareWar(chaosClan, nonChaosKingdom, true);
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
                        FactionManager.DeclareWar(chaosClan, nonChaosClan, true);
                    }
                }
            }
            //set all chaos minor roaming factions neutral to chaos clans and kingdoms
            foreach (var chaosClan in chaosClans)
            {
                foreach (var otherChaosClan in chaosClans)
                {
                    if (otherChaosClan == chaosClan) continue;
                    if (otherChaosClan.IsAtWarWith(chaosClan))
                    {
                        var stance = otherChaosClan.GetStanceWith(chaosClan);
                        stance.IsAtConstantWar = false;
                        stance.IsAtWar = false;
                    }
                }
                foreach (var chaosKingdom in chaosKingdoms)
                {
                    if (chaosKingdom.IsAtWarWith(chaosClan))
                    {
                        var stance = chaosKingdom.GetStanceWith(chaosClan);
                        stance.IsAtConstantWar = false;
                        stance.IsAtWar = false;
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
