using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.SettlementTypes
{
    public class Shrine : ISettlementType
    {
        private ReligionObject _religion;
        private Settlement _settlement;
        private TORCustomSettlementComponent _component;
        public const int DEFAULT_BLESSING_DURATION = 72;

        public bool IsRaidingPartySpawner => false;
        public bool IsActive { get; set; } = true;
        public ReligionObject Religion => _religion;

        public void OnInit(Settlement settlement, ReligionObject religion)
        {
            _settlement = settlement;
            _component = settlement.SettlementComponent as TORCustomSettlementComponent;
            _religion = religion;
        }

        public void SpawnNewParty() { }
    }
}
