using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;

namespace TOR_Core.Models
{
    public class TOREncounterModel : DefaultEncounterModel
    {
        public override Hero GetLeaderOfSiegeEvent(SiegeEvent siegeEvent, BattleSideEnum side)
        {
            return base.GetLeaderOfSiegeEvent(siegeEvent, side);
        }

        // villager parties and caravans will no longer surrender if there's a lord helping them
        public override float GetSurrenderChance(MobileParty defenderParty, MobileParty attackerParty)
        {
            float baseChance = base.GetSurrenderChance(defenderParty, attackerParty);

            if (defenderParty == null || (!defenderParty.IsCaravan && !defenderParty.IsVillager))
            {
                return baseChance;
            }

            var mapEvent = defenderParty.MapEvent;
            if (mapEvent == null)
            {
                return baseChance;
            }

            foreach (var mapEventParty in mapEvent.DefenderSide.Parties)
            {
                var mobileParty = mapEventParty.Party?.MobileParty;
                if (mobileParty != null && mobileParty != defenderParty && mobileParty.IsLordParty)
                {
                    return 0f;
                }
            }

            return baseChance;
        }
    }
}