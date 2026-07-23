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

        // 1.4 replaces ruler gear through culture templates for new rulers. they will not change in tor but missing ruler equipment sets will return null upon a chaos uprising.
        public override (Equipment, Equipment) GetEquipmentsForChangingRuler(Hero newRuler, Hero oldRuler, Equipment.EquipmentType equipmentType)
        {
            var newRulerEquipment = newRuler == Hero.MainHero 
                ? null : new Equipment(equipmentType);

            if (newRulerEquipment != null)
            {
                newRulerEquipment.FillFrom(equipmentType == Equipment.EquipmentType.Civilian
                    ? newRuler.CivilianEquipment : newRuler.BattleEquipment);
            }

            var oldRulerEquipment = oldRuler == null || !oldRuler.IsActive || oldRuler == Hero.MainHero
                ? null : new Equipment(equipmentType);

            if (oldRulerEquipment != null)
            {
                oldRulerEquipment.FillFrom(equipmentType == Equipment.EquipmentType.Civilian
                    ? oldRuler.CivilianEquipment : oldRuler.BattleEquipment);
            }

            return (newRulerEquipment, oldRulerEquipment);
        }
    }
}