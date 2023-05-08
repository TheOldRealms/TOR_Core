using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;
using TOR_Core.Extensions;

namespace TOR_Core.BaseGameDebug
{
    public class BaseGameDebugCampaignBehavior : CampaignBehaviorBase
    {
        private Dictionary<string, int> _heroRaceMap = new Dictionary<string, int>();

        public override void RegisterEvents()
        {
            CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, OnSave);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionStart);
            //CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
        }

        private void HourlyTick()
        {
            foreach(var hideout in Hideout.All)
            {
                var parties = hideout.Settlement.Parties.Where(x => x.IsBandit && x.IsActive && !x.IsDisbanding && !x.IsRaidingParty() && x.CurrentSettlement != null).ToList();
                var num = parties.Count() - Campaign.Current.Models.BanditDensityModel.NumberOfMaximumBanditPartiesInEachHideout;
                if(num > 0)
                {
                    for(var i = num; i >= 0; i--)
                    {
                        LeaveSettlementAction.ApplyForParty(parties[i]);
                        parties[i].Aggressiveness = 1f - 0.2f * MBRandom.RandomFloat;
                        SetPartyAiAction.GetActionForPatrollingAroundSettlement(parties[i], hideout.Settlement);
                    }
                }
            }
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
}
