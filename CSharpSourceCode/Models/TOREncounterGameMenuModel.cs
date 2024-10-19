using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.Ink;

namespace TOR_Core.Models
{
    public class TOREncounterGameMenuModel : DefaultEncounterGameMenuModel
    {
        public override string GetEncounterMenu(PartyBase attackerParty, PartyBase defenderParty, out bool startBattle, out bool joinBattle)
        {
            var settlement = GetEncounteredPartyBase(attackerParty, defenderParty).Settlement;
            if (settlement != null && settlement.SettlementComponent is TORBaseSettlementComponent)
            {
                startBattle = false;
                joinBattle = false;
                if (settlement.SettlementComponent is ShrineComponent) return "shrine_menu";
                else if (settlement.SettlementComponent is OakOfAgesComponent) return "oak_of_ages_menu";
                else if (settlement.SettlementComponent is WorldRootsComponent) return "worldroots_menu";
                else if (settlement.SettlementComponent is ChaosPortalComponent) return "raidingsite_menu";
                else if (settlement.SettlementComponent is HerdStoneComponent) return "raidingsite_menu";
                else if (settlement.SettlementComponent is SlaverCampComponent) return "raidingsite_menu";
                else if (settlement.SettlementComponent is CursedSiteComponent) return "cursedsite_menu";
                else return string.Empty;
            }
            else return base.GetEncounterMenu(attackerParty, defenderParty, out startBattle, out joinBattle);
        }
    }
}
