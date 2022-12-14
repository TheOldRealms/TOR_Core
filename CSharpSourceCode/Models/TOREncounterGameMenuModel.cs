using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TOR_Core.CampaignMechanics.TORCustomSettlement;

namespace TOR_Core.Models
{
    public class TOREncounterGameMenuModel : DefaultEncounterGameMenuModel
    {
        public override string GetEncounterMenu(PartyBase attackerParty, PartyBase defenderParty, out bool startBattle, out bool joinBattle)
        {
            var settlement = GetEncounteredPartyBase(attackerParty, defenderParty).Settlement;
            if (settlement != null && settlement.SettlementComponent is TORCustomSettlementComponent)
            {
                var component = settlement.SettlementComponent as TORCustomSettlementComponent;
                startBattle = false;
                joinBattle = false;
                return component.SettlementType.GameMenuName;
            }
            else return base.GetEncounterMenu(attackerParty, defenderParty, out startBattle, out joinBattle);
        }
    }
}
