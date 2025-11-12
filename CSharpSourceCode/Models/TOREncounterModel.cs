using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.MapEvents;
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
    }
}