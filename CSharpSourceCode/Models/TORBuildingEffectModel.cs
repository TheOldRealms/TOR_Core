using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements.Buildings;

namespace TOR_Core.Models
{
    public class TORBuildingEffectModel : DefaultBuildingEffectModel
    {
        public override ExplainedNumber GetBuildingEffect(Building building, BuildingEffectEnum effect)
        {
            //fortifications are one of the default buildings guaranteed to be in any, well, fortification (town/castle)
            //because all building bonuses are summed and shown under the "building bonus" descriptor, it doesn't matter which building is checked for besides knowing that around half of the buildings aren't present on campaign start, eg. I wanted to check for settlementOrchard/castleGarden but that's 1) not a directly accessible building type from DBT, and 2) starts at level 0 (unconstructed) and is therefore contributing nothing initially
            //I had wanted to detect the value added by those to increase its effective bonus, but i also realized that the settlement orchard doesn't apply when the town is under siege which would cause faster food depletions during sieges; i'm now wondering if I'm remembering that correctly or if all FoodProduction bonuses from buildings are ignored while sieged?
            var result = base.GetBuildingEffect(building, effect);
            if ((building.BuildingType == DefaultBuildingTypes.SettlementFortifications || building.BuildingType == DefaultBuildingTypes.CastleFortifications) && effect == BuildingEffectEnum.FoodProduction)
            {
                result.Add(30);
            }
            return result;
        }

    }
}