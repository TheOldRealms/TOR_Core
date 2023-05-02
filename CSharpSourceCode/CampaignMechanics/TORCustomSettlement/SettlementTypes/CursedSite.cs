using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Settlements;
using TOR_Core.CampaignMechanics.Religion;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.SettlementTypes
{
    public class CursedSite : ISettlementType
    {
        private Settlement _settlement;
        public bool IsRaidingPartySpawner => false;
        public ReligionObject Religion { get; private set; }

        public bool IsActive { get; set; } = true;

        public void OnInit(Settlement settlement, ReligionObject religion = null)
        {
            _settlement = settlement;
            Religion = religion;
        }

        public void SpawnNewParty() { }
    }
}
