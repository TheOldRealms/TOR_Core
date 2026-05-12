using Helpers;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.CampaignMechanics.TORCustomSettlement.Settlement;

namespace TOR_Core.Models
{
    public class TOREncounterGameMenuModel : DefaultEncounterGameMenuModel
    {
        public override string GetEncounterMenu(PartyBase attackerParty, PartyBase defenderParty, out bool startBattle, out bool joinBattle)
        {
            //Sly : can we control the encounter menu here so that an enlisted player doesn't get the native encounter menu after retreating from a battle and returning to the campaign map with a now-existant PlayerEncounter?
            var settlement = MapEventHelper.GetEncounteredPartyBase(attackerParty, defenderParty).Settlement;
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
                else if (settlement.SettlementComponent is TrollCaveComponent) return "trollcave_menu";
                else return string.Empty;
            }
            else return base.GetEncounterMenu(attackerParty, defenderParty, out startBattle, out joinBattle);
        }
    }
}