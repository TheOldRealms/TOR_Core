using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TOR_Core.CampaignMechanics.Religion;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.SettlementTypes
{
    public class HerdStone : ISettlementType
    {
        public string GameMenuName => throw new NotImplementedException();

        public bool IsRaidingPartySpawner => true;

        public bool IsActive { get; set; } = true;
        public bool IsBattleUnderway { get; set; } = false;

        public string RewardItemId => throw new NotImplementedException();

        public void AddGameMenus(CampaignGameStarter starter)
        {
            throw new NotImplementedException();
        }

        public void OnInit(Settlement settlement, ReligionObject religion = null)
        {
            throw new NotImplementedException();
        }

        public void SpawnNewParty()
        {
            throw new NotImplementedException();
        }
    }
}
