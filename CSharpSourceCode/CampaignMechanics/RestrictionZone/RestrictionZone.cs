using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.RestrictionZone
{
    public class RestrictionZone
    {
        public TextObject Name { get; init; }
        public int NavMeshFaceId { get; init; }
        public Func<MobileParty, RestrictionZone, bool> CanPartyEnter { get; init; }
        public List<Settlement> Settlements { get; } = [];
        public List<IFaction> Factions { get; } = [];

        public void UpdateSettlementsAndFactions()
        {
            Settlements.Clear();
            Factions.Clear();
            foreach (var settlement in Settlement.All)
            {
                if(settlement.CurrentNavigationFace.FaceGroupIndex == NavMeshFaceId)
                {
                    if(!Settlements.Contains(settlement) && settlement != null && (settlement.IsTown || settlement.IsCastle)) Settlements.Add(settlement);
                    if (!Factions.Contains(settlement.MapFaction) && settlement.MapFaction != null && settlement.MapFaction.IsKingdomFaction) Factions.Add(settlement.MapFaction);
                }
            }
        }

        public static bool CanPartyEnterCommonConditions(MobileParty party, RestrictionZone zone)
        {
            var common = party.IsVillager ||
                    party.IsBandit ||
                    party.IsRaidingParty() ||
                    party.IsInvasionParty() ||
                    party.MapFaction == Clan.PlayerClan ||
                    party.IsCurrentlyUsedByAQuest;
            if (common) return true;
            if (zone.Factions.Contains(party.MapFaction)) return true;
            foreach (var faction in zone.Factions)
            {
                if (faction.IsAtWarWith(party.MapFaction)) return true;
            }
            return false;
        }
    }
}
