using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics.Chaos
{
    public class ChaosCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, Start);
        }

        private void Start(CampaignGameStarter obj)
        {
            List<Clan> chaosClans = Clan.All.Where(x => x.Culture.StringId == "chaos_culture").ToList();
            foreach (var kingdom in Kingdom.All)
            {
                foreach (var clan in chaosClans)
                {
                    if (!clan.IsAtWarWith(kingdom))
                    {
                        FactionManager.DeclareWar(clan, kingdom, true);
                    }
                }
            }
            foreach (var faction in Clan.NonBanditFactions.Where(x => x.Culture.StringId != "chaos_culture"))
            {
                foreach (var clan in chaosClans)
                {
                    if (!clan.IsAtWarWith(faction))
                    {
                        FactionManager.DeclareWar(clan, faction, true);
                    }
                }
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            //throw new NotImplementedException();
        }
    }
}
