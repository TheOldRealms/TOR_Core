using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.TORCustomSettlement.SettlementTypes;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement
{
    public class TORCustomSettlementComponent : SettlementComponent
    {
        private Clan _ownerClan;
        public Clan OwnerClan => _ownerClan;
        private ISettlementType _settlementType;
        public ISettlementType SettlementType => _settlementType;
        public int RaidingPartyCount => MobileParty.All.Where(x => x.IsRaidingParty() && x.HomeSettlement == Settlement).Count();
        public void SetClan(Clan clan) => _ownerClan = clan;
        protected override void OnInventoryUpdated(ItemRosterElement item, int count) { }

        public override void OnInit() => _settlementType.SetSettlement(Settlement);

        protected override void AfterLoad() => OnInit();

        public override void Deserialize(MBObjectManager objectManager, XmlNode node)
        {
            base.Deserialize(objectManager, node);
            if (node.Attributes["background_crop_position"] != null)
            {
                BackgroundCropPosition = float.Parse(node.Attributes["background_crop_position"].Value);
            }

            if (node.Attributes["background_mesh"] != null)
            {
                BackgroundMeshName = node.Attributes["background_mesh"].Value;
            }

            if (node.Attributes["wait_mesh"] != null)
            {
                WaitMeshName = node.Attributes["wait_mesh"].Value;
            }

            if (node.Attributes["settlement_type"] != null)
            {
                var attribute = node.Attributes["settlement_type"].Value;
                if(Enum.TryParse<SettlementType>(attribute, out var type))
                {
                    _settlementType = SettlementTypeHelper.GetSettlementType(type);
                }
            }
        }
    }
}
