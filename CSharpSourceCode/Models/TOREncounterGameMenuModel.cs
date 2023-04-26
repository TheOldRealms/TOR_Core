using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.CampaignMechanics.TORCustomSettlement.SettlementTypes;

namespace TOR_Core.Models
{
    public class TOREncounterGameMenuModel : DefaultEncounterGameMenuModel
    {
        public override string GetEncounterMenu(PartyBase attackerParty, PartyBase defenderParty, out bool startBattle, out bool joinBattle)
        {
            var settlement = GetEncounteredPartyBase(attackerParty, defenderParty).Settlement;
            if (settlement != null && settlement.SettlementComponent is TORCustomSettlementComponent)
            {
                startBattle = false;
                joinBattle = false;
                var type = ((TORCustomSettlementComponent)settlement.SettlementComponent).SettlementType;
                if (type is Shrine) return "shrine_menu";
                else if (type is ChaosPortal) return "chaosportal_menu";
                else if (type is HerdStone) return "herdstone_menu";
                else return string.Empty;
            }
            else return base.GetEncounterMenu(attackerParty, defenderParty, out startBattle, out joinBattle);
        }
    }
}
