using Helpers;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.TORCustomSettlement.Component;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Religion
{
    public class ReligionCampaignBehavior : CampaignBehaviorBase, IDisposable
    {
        // Initial relation adjustment weights for religion compatibility
        private const int SameReligionBonusMin = 25;
        private const int SameReligionBonusMax = 50;
        private const int CompatiblePantheonBonusMax = 25;
        private const int HostilePantheonMalusMax = 75;

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, AfterNewGameStart);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionStart);
            CampaignEvents.OnItemsDiscardedByPlayerEvent.AddNonSerializedListener(this, OnItemsDiscarded);
            CampaignEvents.HourlyTickSettlementEvent.AddNonSerializedListener(this, SettlementHourlyReligionTick);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, MapEventEnded);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, PlayerBattleEnded);
            TORCampaignEvents.Instance.DevotionLevelChanged += OnDevotionLevelChanged;
            TORCampaignEvents.Instance.HeroExtendedInfoCreated += OnHeroExtendedInfoCreated;
        }

        private void PlayerBattleEnded(MapEvent mapEvent)
        {
            if (mapEvent.IsPlayerMapEvent && mapEvent.PlayerSide == mapEvent.WinningSide && Hero.MainHero.PartyBelongedTo.HasBlessing("cult_of_anath_raema"))
            {
                var roster = PlayerEncounter.Current.RosterToReceiveLootItems;
                if (roster != null && roster.Count > 0)
                {
                    var randomIndex = MBRandom.RandomInt(0, roster.Count - 1);

                    var item = roster[randomIndex].EquipmentElement;

                    if (!item.IsEmpty)
                    {
                        roster.AddToCounts(item, 7);
                    }
                }
            }
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
            if (party?.Party?.LeaderHero is not Hero hero) return; //LordParty, Caravan, and CustomParty components can pass

            var mobileParty = party.Party.MobileParty; //they're already a party leader so no false positives for companions
            if (mobileParty == null || !mobileParty.IsLordParty) return; //remove caravans lead by heroes and custom parties for quests

            if (hero.GetPerkValue(TORPerks.Faith.Spirit))
            {
                int xp = 0;
                var killedtroops = party.Troops.Where(x => x.IsKilled);
                foreach (var troop in killedtroops)
                {
                    if (troop.Troop.Tier >= 6)
                    {
                        xp = (int)(1.333f * Math.Pow(troop.Troop.Level + 4, 2)); //approximates the xp needed to upgrade from a troop 1 tier lower
                    }
                }
                if (xp > 0)
                {
                    MobilePartyHelper.PartyAddSharedXp(mobileParty, xp);
                }
            }
        }

        private void SettlementHourlyReligionTick(Settlement settlement)
        {
            if (!settlement.IsTown) return;
            if (settlement.NumberOfLordPartiesAt < 1) return;
            
            foreach (var mobileParty in settlement.Parties.WhereQ(x => x.IsLordParty && x.LeaderHero != null && x.IsActive && !x.IsDisbanding))
            {
                foreach (var hero in  mobileParty.GetMemberHeroes())
                {
                    if (hero.GetPerkValue(TORPerks.Faith.Imperturbable))
                    {
                        hero.AddSkillXp(TORSkills.Faith, TORPerks.Faith.Imperturbable.PrimaryBonus / 24);
                    }
                }
            }
        }

        private void OnItemsDiscarded(ItemRoster itemRoster)
        {
            if (Settlement.CurrentSettlement?.SettlementComponent is ShrineComponent &&
                Hero.MainHero.GetPerkValue(TORPerks.Faith.Offering) &&
                itemRoster.Count > 0)
            {//ItemRoster.OnRosterUpdated uses ItemObject.Value rather than EquipmentElement.ItemValue which causes the calculation to ignore item modifiers affecting price
                Hero.MainHero.AddSkillXp(TORSkills.Faith, Math.Max(1, itemRoster.TotalValue / 100));
            }
        }

        /// <remarks>
        /// Only heroes from templates will pass through here.
        /// </remarks>
        public void OnHeroExtendedInfoCreated(object sender, HeroExtendedInfoCreatedEventArgs heroArgs)
        {
            var hero = heroArgs.Hero;
            if (hero.IsSpecial || hero.IsWanderer || hero.IsLord) DetermineReligionForHero(hero);
        }

        private void OnDevotionLevelChanged(object sender, DevotionLevelChangedEventArgs e)
        {
            if ((int)e.NewDevotionLevel > (int)e.OldDevotionLevel && e.Hero == Hero.MainHero)
            {
                var devotionLevelText = GameTexts.FindText("tor_religion_devotionlevel", e.NewDevotionLevel.ToString());
                var religionNameText = GameTexts.FindText("tor_religion_name_of_god", e.Religion.StringId);
                MBTextManager.SetTextVariable("TOR_DEVOTION_LEVEL", devotionLevelText);
                MBTextManager.SetTextVariable("TOR_RELIGION", religionNameText);
                MBTextManager.SetTextVariable("PLAYER.NAME", Hero.MainHero.Name);
                MBInformationManager.AddQuickInformation(GameTexts.FindText("tor_religion_change_notification_frame"));
            }
        }

        /// <remarks>
        /// Indexed to occur after the event in ExtendedInfoManager which creates heroInfo entries for unique heroes - Hero.Deserialize dispatches no OnHeroCreated event so this must be performed manually after it's known that the hero's extended info would exist.
        /// A small subset of heroes will end up with 2 religious influence additions - the templated heroes generated for mercenary clans and the chaos clan will trigger OnHeroExtendedInfoCreated which will set a culture-based religion, before passing through here for the 2nd.
        /// </remarks>
        private void AfterNewGameStart(CampaignGameStarter starter, int index)
        {
            if (index == CampaignEvents.OnNewGameCreatedPartialFollowUpEventMaxIndex - 1)
            {
                foreach (var religion in ReligionObject.All)
                {
                    foreach (string id in religion.InitialClans)
                    {
                        var clan = Clan.FindFirst(x => x.StringId == id);
                        if (clan != null)
                        {
                            foreach (var hero in clan.Heroes)
                            {
                                hero.AddReligiousInfluence(religion, MBRandom.RandomInt(50, 90), false);//higher than the value in DetermineReligionForHero so any religion coming from the xml is guaranteed to be the dominant religion for any heroes that have 2+ influences
                            }
                        }
                    }
                }
                SetIntialReligionBasedRelationDriftForAi();
            }
        }

        private void OnSessionStart(CampaignGameStarter starter)
        {
            //ensure mutual entries for hostile religions
            foreach (var religion in ReligionObject.All)
            {
                foreach (var religion2 in religion.HostileReligions)
                {
                    if (!religion2.HostileReligions.Contains(religion)) religion2.HostileReligions.Add(religion);
                }
            }
            //add descendants of religious units if xml only has base troop
            foreach (var religion in ReligionObject.All)
            {
                foreach (var troop in religion.ReligiousTroops.ToList())
                {
                    AddReligiousUnitToReligionRecursive(religion, troop);
                }
            }
        }

        private void AddReligiousUnitToReligionRecursive(ReligionObject religion, CharacterObject troop)
        {
            if (!religion.ReligiousTroops.Contains(troop)) religion.ReligiousTroops.Add(troop);
            if (troop.UpgradeTargets.Count() > 0)
            {
                foreach (var target in troop.UpgradeTargets) AddReligiousUnitToReligionRecursive(religion, target);
            }
        }

        private void DetermineReligionForHero(Hero hero)
        {
            ReligionObject religion = null;

            if (hero.IsWanderer)
            {
                var heroReligion = hero.GetReligionFromAttribute();
                if (heroReligion != null)
                {
                    hero.AddReligiousInfluence(heroReligion, 30, false);
                    return;
                }
                else
                {
                    if (hero.IsPriest())
                    {
                        TORCommon.Log("ReligionCampaignBehavior : null religion found for " + hero.Template.StringId, LogLevel.Warn);
                    }
                }
            }

            //Nobles : follow father, then clanleader, then culture
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
                hero.AddReligiousInfluence(religion, 30, false);//low enough that player creation options or followed religion from xml will surpass this influence
            }
        }

        public override void SyncData(IDataStore dataStore) { }

        public void Dispose()
        {
            TORCampaignEvents.Instance.DevotionLevelChanged -= OnDevotionLevelChanged;
            TORCampaignEvents.Instance.HeroExtendedInfoCreated -= OnHeroExtendedInfoCreated;
        }

        private void SetIntialReligionBasedRelationDriftForAi()
        {
            var heroList = new List<Hero>();
            foreach (var hero in Campaign.Current.AliveHeroes)
            {
                if (hero.IsNotable) continue;
                if (hero.Clan != null && hero != Hero.MainHero) //Sly : this occurs before character creation so the player has the default wood elf(battanian) culture and ends up with Taal - I wanted to have the player receive a religion and have their relations adjusted, but to do so will require putting something in the CharacterCreationOver event or later
                {
                    if (hero.GetDominantReligion() == null) DetermineReligionForHero(hero); //only the dominant religion is used for npcs so skip over anything that already has one
                    heroList.Add(hero);
                }
            }

            int i = 0;
            foreach (var hero in heroList)
            {
                foreach (var otherHero in heroList.Where((hero, index) => index > i)) //each hero checks only the heroes after them as their relations were already set with heroes before them on the prior hero's iteration
                {
                    ChangeRelationBasedOnReligion(hero, otherHero);
                }
                i++;
            }
        }

        private void ChangeRelationBasedOnReligion(Hero hero, Hero otherHero)
        {
            var heroDomReligion = hero.GetDominantReligion();
            if (hero.GetDominantReligion() == null) return;

            var otherHeroDomReligion = otherHero.GetDominantReligion();
            if (otherHero.GetDominantReligion() == null) return;

            var currentRelation = hero.GetRelation(otherHero);

            if (heroDomReligion == otherHeroDomReligion)
            {
                var bonus = MBRandom.RandomInt(SameReligionBonusMin, SameReligionBonusMax);
                hero.SetPersonalRelation(otherHero, currentRelation + bonus);
                return;
            }

            // Use Pantheon compatibility for relation adjustments
            float compatibility = ReligionObjectHelper.GetPantheonCompatibility(heroDomReligion.Pantheon, otherHeroDomReligion.Pantheon);
            if (compatibility > 0)
            {
                var bonus = MBRandom.RandomInt(0, (int)(CompatiblePantheonBonusMax * compatibility));
                hero.SetPersonalRelation(otherHero, currentRelation + bonus);
            }
            else if (compatibility < 0)
            {
                var malus = MBRandom.RandomInt(0, (int)(HostilePantheonMalusMax * -compatibility));
                hero.SetPersonalRelation(otherHero, currentRelation - malus);
            }
        }
    }
}