using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
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
            TORCampaignEvents.Instance.DevotionLevelChanged += OnDevotionLevelChanged;
        }

        private void OnHeroCreated(Hero hero, bool arg2)
        {
            if(hero.IsLord) DetermineReligionForHero(hero);
        }

        private void OnDevotionLevelChanged(object sender, DevotionLevelChangedEventArgs e)
        {
            if((int)e.NewDevotionLevel > (int)e.OldDevotionLevel)
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
        }

        private void DetermineReligionForHero(Hero hero)
        {
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
