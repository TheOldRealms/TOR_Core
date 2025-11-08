using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement
{
    public abstract class TORBaseSettlementComponent : SettlementComponent
    {
        private string _religionId;
        public Clan OwnerClan { get; set; }
        public ReligionObject Religion { get; protected set; }
        public bool IsActive { get; set; }

        public override void OnInit()
        {
            base.OnInit();
            Religion = ReligionObject.All.FirstOrDefault(x => x.StringId == _religionId);
        }

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

            if (node.Attributes["religion"] != null)
            {
                _religionId = node.Attributes["religion"].Value;
            }
        }

        protected override void OnInventoryUpdated(ItemRosterElement item, int count) { }
    }

    public abstract class BaseRaiderSpawnerComponent : TORBaseSettlementComponent
    {
        public virtual int BattlePartySize => 250;
        public int RaidingPartyCount => MobileParty.All.Where(x => x.IsRaidingParty() && x.HomeSettlement == Settlement).Count();
        public abstract string BattleSceneName { get; }
        public bool IsBattleUnderway { get; set; }
        public abstract List<string> RewardItemIds { get; }
        public abstract void SpawnNewParty(out MobileParty party, Settlement initialTarget);
    }
}
