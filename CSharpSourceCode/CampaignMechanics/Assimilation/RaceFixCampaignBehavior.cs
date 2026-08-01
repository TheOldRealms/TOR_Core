using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics.Assimilation
{
    public class RaceFixCampaignBehavior : CampaignBehaviorBase
    {
        private Dictionary<string, int> _heroRaceMap = new Dictionary<string, int>();

        public override void RegisterEvents()
        {
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, OnSave);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionStart);
        }

        /// <remarks>
        /// The only hero with a mismatch is the player because the race is read from the main_hero/Eren character object which has default human race. All heroes with a static race read from their xml entry don't need adjustments; the player is a special case because the default is human that gets updated during character creation.
        /// Storing a full dictionary shouldn't be necessary unless more race swap mechanics are introduced.
        /// </remarks>
        private void OnSessionStart(CampaignGameStarter obj)
        {
            if (_heroRaceMap.Count > 0)
            {
                foreach (var hero in Hero.AllAliveHeroes)
                {
                    if (_heroRaceMap.ContainsKey(hero.StringId) && _heroRaceMap[hero.StringId] != hero.CharacterObject.Race)
                    {
                        hero.CharacterObject.Race = _heroRaceMap[hero.StringId];
                    }
                }
            }
        }

        private void OnSave()
        {
            _heroRaceMap = new Dictionary<string, int>();
            foreach (var hero in Hero.AllAliveHeroes)
            {
                if (!_heroRaceMap.ContainsKey(hero.StringId))
                {
                    _heroRaceMap.Add(hero.StringId, hero.CharacterObject.Race);
                }
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_heroRaceMap", ref _heroRaceMap);
        }
    }
}