using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace TOR_Core.Utilities
{
    public class TOREquipmentHelper
    {
        
        public static void RemoveLanceFromEquipment(Agent agent, bool replaceWithSpear)
        {
            MissionEquipment equipment = agent.Equipment;

            for (int i = (int) EquipmentIndex.WeaponItemBeginSlot; i < (int) EquipmentIndex.NumAllWeaponSlots; i++)
            {
                EquipmentIndex equipmentIndex = (EquipmentIndex)i;
                MissionWeapon missionWeapon = equipment[equipmentIndex];
                if(missionWeapon.IsEmpty )continue;
                            
                if (missionWeapon.Item.StringId.Contains("lance"))
                {
                    agent.RemoveEquippedWeapon((EquipmentIndex)i);
                    if (replaceWithSpear)
                    {
                        var spear = MBObjectManager.Instance.GetObject<ItemObject>("tor_empire_weapon_spear_003");
                        var weapon = new MissionWeapon(spear,missionWeapon.ItemModifier, missionWeapon.Banner);
                        agent.EquipWeaponWithNewEntity((EquipmentIndex) i, ref weapon);
                    }
                }
            }
        }
    }
}