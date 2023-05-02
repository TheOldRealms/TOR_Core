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
    public interface ISettlementType
    {
        bool IsRaidingPartySpawner { get; }
        bool IsActive { get; set; }
        void SpawnNewParty();
        void OnInit(Settlement settlement, ReligionObject religion = null);
    }

    public enum SettlementType
    {
        ChaosPortal,
        HerdStone,
        Shrine,
        CursedSite
    }

    public static class SettlementTypeHelper
    {
        public static ISettlementType GetSettlementType(SettlementType type)
        {
            switch (type)
            {
                case SettlementType.ChaosPortal: return new ChaosPortal();
                case SettlementType.HerdStone: return new HerdStone();
                case SettlementType.Shrine: return new Shrine();
                case SettlementType.CursedSite: return new CursedSite();
                default: return null;
            }
        }
    }
}
