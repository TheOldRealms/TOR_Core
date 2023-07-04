using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace TOR_Core.Utilities
{
    public class TOREquipmentHelper
    {
        public static void RemoveLanceFromEquipment(Agent agent)
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
                }
            }
        }
        
        public static void AddSpearEquipment(Agent agent)
        {
            MissionEquipment equipment = agent.Equipment;
            for (int i = (int) EquipmentIndex.WeaponItemBeginSlot; i < (int) EquipmentIndex.NumAllWeaponSlots; i++)
            {
                EquipmentIndex equipmentIndex = (EquipmentIndex)i;
                MissionWeapon wieldedWeapon = equipment[equipmentIndex];
                if(!wieldedWeapon.IsEmpty )continue;
                var item = MBObjectManager.Instance.GetObject<ItemObject>("tor_empire_weapon_halberd_001");
                var missionWeapon = new MissionWeapon(item,wieldedWeapon.ItemModifier, wieldedWeapon.Banner);
                agent.EquipWeaponWithNewEntity((EquipmentIndex) i, ref missionWeapon);
                
            }
        }
    }
}