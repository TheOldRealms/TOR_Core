using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TOREquipmentSelectionModel : DefaultEquipmentSelectionModel
    {
        public override MBList<MBEquipmentRoster> GetEquipmentRostersForCompanion(
            Hero hero,
            bool isCivilian)
        {
            var roster = new MBEquipmentRoster();
            roster.Initialize();

            var equipment = new Equipment(isCivilian ? Equipment.EquipmentType.Civilian : Equipment.EquipmentType.Battle);
            equipment.FillFrom(isCivilian ? hero.CivilianEquipment : hero.BattleEquipment);

            roster.AddEquipment(equipment);

            var list = new MBList<MBEquipmentRoster>
            {
                roster
            };

            return list;
        }
    }
}