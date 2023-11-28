using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TOR_Core.CampaignMechanics.Invasions;

namespace TOR_Core.Models
{
    public class TOREncounterModel : DefaultEncounterModel
    {
        public override Hero GetLeaderOfSiegeEvent(SiegeEvent siegeEvent, BattleSideEnum side)
        {
            if(side == BattleSideEnum.Attacker)
            {
                var list = siegeEvent.GetSiegeEventSide(side).GetInvolvedPartiesForEventType(MapEvent.BattleTypes.Siege).Select(x => x.MobileParty).ToList();
                if(list.Count > 0 && list[0].PartyComponent is IInvasionParty)
                {
                    return list[0].LeaderHero;
                }
            }
            
            return base.GetLeaderOfSiegeEvent(siegeEvent, side);
        }
    }
}
