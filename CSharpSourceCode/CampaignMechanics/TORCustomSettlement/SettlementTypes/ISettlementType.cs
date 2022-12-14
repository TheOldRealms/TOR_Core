﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.SettlementTypes
{
    public interface ISettlementType
    {
        string GameMenuName { get; }
        bool IsRaidingPartySpawner { get; }
        bool IsActive { get; set; }
        bool IsBattleUnderway { get; set; }
        string RewardItemId { get; }
        void AddGameMenus(CampaignGameStarter starter);
        void SpawnNewParty();
        void SetSettlement(Settlement settlement);
    }

    public enum SettlementType
    {
        ChaosPortal,
        HerdStone
    }

    public static class SettlementTypeHelper
    {
        public static ISettlementType GetSettlementType(SettlementType type)
        {
            switch (type)
            {
                case SettlementType.ChaosPortal: return new ChaosPortal();
                default: return null;
            }
        }
    }
}
