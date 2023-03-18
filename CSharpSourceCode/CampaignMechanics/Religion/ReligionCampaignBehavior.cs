using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.Religion
{
    public class ReligionCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, AfterNewGameStart);
            CampaignEvents.HeroCreated.AddNonSerializedListener(this, OnHeroCreated);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionStart);
        }

        private void OnHeroCreated(Hero hero, bool arg2)
        {
            if (!hero.HasAnyReligion()) AddReligionToHero(hero);
        }

        private void AfterNewGameStart(CampaignGameStarter starter, int index)
        {
            foreach(var hero in Hero.AllAliveHeroes)
            {
                if (!hero.HasAnyReligion()) AddReligionToHero(hero);
            }
        }

        private void AddReligionToHero(Hero hero)
        {
            //TODO, actually make this based on some personality trait, or xml stuff
            hero.AddReligiousInfluence(ReligionObject.All.GetRandomElementInefficiently(), MBRandom.RandomInt(1, 10));
        }

        private void OnSessionStart(CampaignGameStarter starter)
        {
            //MBObjectManager.Instance.LoadXML("Religions", false);
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
