using Helpers;
using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Religion
{
    public class ReligionCampaignBehavior : CampaignBehaviorBase, IDisposable
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, AfterNewGameStart);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionStart);
            CampaignEvents.HeroCreated.AddNonSerializedListener(this, OnHeroCreated);
            CampaignEvents.OnItemsDiscardedByPlayerEvent.AddNonSerializedListener(this, OnItemsDiscarded);
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, HourlyPartyTick);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, MapEventEnded);
            TORCampaignEvents.Instance.DevotionLevelChanged += OnDevotionLevelChanged;
        }

        private void MapEventEnded(MapEvent mapEvent)
        {
            var attackerParties = mapEvent.PartiesOnSide(BattleSideEnum.Attacker);
            var defenderParties = mapEvent.PartiesOnSide(BattleSideEnum.Defender);
            attackerParties.ForEach(x => DistributeXpForKilledUnits(x));
            defenderParties.ForEach(x => DistributeXpForKilledUnits(x));
        }

        private void DistributeXpForKilledUnits(MapEventParty party)
        {
            if(party != null && party.Party != null && party.Party.MobileParty != null && party.Party.MobileParty.IsLordParty)
            {
                var mobileParty = party.Party.MobileParty;
                if(mobileParty.LeaderHero != null && mobileParty.LeaderHero.GetPerkValue(TORPerks.Faith.Spirit))
                {
                    int xp = 0;
                    var killedtroops = party.Troops.Where(x => x.IsKilled);
                    foreach (var troop in killedtroops)
                    {
                        if (troop.Troop.Tier >= 6)
                        {
                            xp = (int)(1.333f * Math.Pow(troop.Troop.Level + 4, 2));
                        }
                    }
                    if (xp > 0)
                    {
                        MobilePartyHelper.PartyAddSharedXp(mobileParty, xp);
                    }
                }
            }
        }

        private void HourlyPartyTick(MobileParty party)
        {
            if(party.IsLordParty && party.IsActive && !party.IsDisbanding && party.CurrentSettlement != null && 
                party.CurrentSettlement.IsTown && party.LeaderHero.GetPerkValue(TORPerks.Faith.Imperturbable))
            {
                party.LeaderHero.AddSkillXp(TORSkills.Faith, TORPerks.Faith.Imperturbable.PrimaryBonus / 24);
            }
        }

        private void OnItemsDiscarded(ItemRoster itemRoster)
        {
            if(Settlement.CurrentSettlement != null && 
                Settlement.CurrentSettlement.SettlementComponent is ShrineComponent && 
                Hero.MainHero.GetPerkValue(TORPerks.Faith.Offering) &&
                itemRoster.Count > 0)
            {
                GainRenownAction.Apply(Hero.MainHero, Math.Max(1, itemRoster.TotalValue / 1000));
            }
        }

        private void OnHeroCreated(Hero hero, bool arg2)
        {
            if(hero.IsLord) DetermineReligionForHero(hero);
        }

        private void OnDevotionLevelChanged(object sender, DevotionLevelChangedEventArgs e)
        {
            if((int)e.NewDevotionLevel > (int)e.OldDevotionLevel && e.Hero == Hero.MainHero)
            {
                MBInformationManager.AddQuickInformation(new TextObject(e.Hero.Name.ToString() + " is now a " + e.NewDevotionLevel.ToString() + " of the " + e.Religion.Name));
            }
        }

        private void AfterNewGameStart(CampaignGameStarter starter, int index)
        {
            if(index == CampaignEvents.OnNewGameCreatedPartialFollowUpEventMaxIndex - 1)
            {
                foreach (var religion in ReligionObject.All)
                {
                    foreach (string id in religion.InitialClans)
                    {
                        var clan = Clan.FindFirst(x => x.StringId == id);
                        if (clan != null)
                        {
                            foreach(var hero in clan.Heroes)
                            {
                                if (!hero.HasAnyReligion()) hero.AddReligiousInfluence(religion, MBRandom.RandomInt(30, 90), false);
                            }
                        }
                    }
                }
            }
        }

        private void OnSessionStart(CampaignGameStarter starter)
        {
            foreach(var hero in Hero.AllAliveHeroes)
            {
                if (hero.IsLord && !hero.HasAnyReligion()) DetermineReligionForHero(hero);
            }
            //ensure mutual entries for hostile religions
            foreach(var religion in ReligionObject.All)
            {
                foreach(var religion2 in religion.HostileReligions)
                {
                    if (!religion2.HostileReligions.Contains(religion)) religion2.HostileReligions.Add(religion);
                }
            }
            //add descendants of religious units if xml only has base troop
            foreach(var religion in ReligionObject.All)
            {
                foreach(var troop in religion.ReligiousTroops.ToList())
                {
                    AddReligiousUnitToReligionRecursive(religion, troop);
                }
            }
        }

        private void AddReligiousUnitToReligionRecursive(ReligionObject religion, CharacterObject troop)
        {
            if (!religion.ReligiousTroops.Contains(troop)) religion.ReligiousTroops.Add(troop);
            if(troop.UpgradeTargets.Count() > 0)
            {
                foreach (var target in troop.UpgradeTargets) AddReligiousUnitToReligionRecursive(religion, target);
            }
        }

        private void DetermineReligionForHero(Hero hero)
        {
            if (hero == Hero.MainHero) return;
            ReligionObject religion = null;
            //follow fater, then clanleader, then culture
            if (hero.Father != null && hero.Father.HasAnyReligion())
            {
                religion = hero.Father.GetDominantReligion();
            }
            else if (hero.Clan != null && hero.Clan.Leader != null && hero.Clan.Leader.HasAnyReligion())
            {
                religion = hero.Clan.Leader.GetDominantReligion();
            }
            else if (hero.Culture != null && ReligionObject.All.Any(x => x.Culture == hero.Culture))
            {
                religion = ReligionObject.All.FirstOrDefault(x => x.Culture == hero.Culture);
            }
            if (religion != null)
            {
                hero.AddReligiousInfluence(religion, MBRandom.RandomInt(30, 90), false);
            }
        }

        public override void SyncData(IDataStore dataStore) { }

        public void Dispose()
        {
            TORCampaignEvents.Instance.DevotionLevelChanged -= OnDevotionLevelChanged;
        }
    }
}
