using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;

namespace TOR_Core.Models
{
    public class TOREquipmentSelectionModel : DefaultEquipmentSelectionModel
    {
        /// <summary>
        /// Override to preserve companion's existing equipment when they become a lord.
        /// Returns the companion's current equipment instead of generating new equipment.
        /// </summary>
        public override Equipment GetEquipmentForCompanionWhenTurningToLord(
            Hero companionHero,
            Equipment.EquipmentType equipmentType)
        {
            var equipment = new Equipment(equipmentType);
            equipment.FillFrom(equipmentType == Equipment.EquipmentType.Civilian
                ? companionHero.CivilianEquipment
                : companionHero.BattleEquipment);
            return equipment;
        }
    }
}