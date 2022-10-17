using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace TOR_Core.BaseGameDebug
{
    public class RestoreRaceCampaignBehavior : CampaignBehaviorBase
    {
        private Dictionary<string, int> _heroRaceMap = new Dictionary<string, int>();

        public override void RegisterEvents()
        {
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, OnSave);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionStart);
        }

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
    
    public class HeroRaceMapSaveableTypeDefiner : SaveableTypeDefiner
    {
        public HeroRaceMapSaveableTypeDefiner() : base(576011) { }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(Dictionary<string, int>));
        }
    }
    
}
