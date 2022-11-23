using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Career
{
    public class CareerMissionLogic :MissionLogic
    {

        private CareerCampaignBase _careerCampaignBase;
        public override void EarlyStart()
        {
            base.EarlyStart();
            _careerCampaignBase= Campaign.Current.GetCampaignBehavior<CareerCampaignBase>();
            
        }

        public override void OnAgentCreated(Agent agent)
        {
            base.OnAgentCreated(agent);
            
            if (agent.Character==Campaign.Current.MainParty.LeaderHero.CharacterObject)
            {
                var ratio =agent.Health /agent.HealthLimit ;

                agent.HealthLimit+= _careerCampaignBase.GetMaximumHealthPoints();

                agent.Health = ratio*agent.HealthLimit;

             
                
                //agent.Character
                //agent.Character.Equipment.GetAmmoCountAndIndexOfType(ItemObject.ItemTypeEnum.Musket,out currentAmmo, out index);

                //currentAmmo = _careerCampaignBase.GetExtraAmmoPoints();
                //agent.SetWeaponAmmoAsClient(0,index,(short)currentAmmo);

                

            }
        }


        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);
            if (agent.Character == Campaign.Current.MainParty.LeaderHero.CharacterObject)
            {
                int currentAmmo = 0;
                //  agent.Equipment.GetAmmoCountAndIndexOfType(ItemObject.ItemTypeEnum.Musket, out currentAmmo, out index);

              //currentAmmo = agent.Equipment.GetAmmoAmount(WeaponClass.Cartridge);
             // agent.Equipment.GetAmmoCountAndIndexOfType(ItemObject.ItemTypeEnum.Bullets,out currentAmmo, out index,0);
               // var t = agent.WieldedWeapon.AmmoWeapon.HitPoints;

               int careerAmmo = _careerCampaignBase.GetExtraAmmoPoints();
               for (int i = 0; i < (int)EquipmentIndex.NumAllWeaponSlots; i++)
                {
                    if(agent.Equipment[i].IsEmpty) continue;
                    
                    MissionWeapon weapon = agent.Equipment[i];
                    if (weapon.CurrentUsageItem.IsAmmo)
                    {
                        weapon.AddExtraModifiedMaxValue((short)careerAmmo);
                        agent.SetWeaponAmountInSlot((EquipmentIndex) i ,weapon.ModifiedMaxHitPoints,false);
                    }
                }
              
               
               
               /*
               foreach (var weapon in weapons)
               {
                   if (weapon.CurrentUsageItem.IsAmmo)
                   {
                       weapon.AddExtraModifiedMaxValue((short)careerAmmo);
                       agent.SetWeaponAmmoAsClient(amm);
                       agent.Equipment.SetHitPointsOfSlot((EquipmentIndex)weapon.CurrentUsageIndex,weapon.ModifiedMaxAmount,false);
                       //agent.SetWeaponAmountInSlot((EquipmentIndex)weapon.CurrentUsageIndex,weapon.ModifiedMaxAmount,false);
                       TORCommon.Say("ammo "+weapon.Ammo +" of " +weapon.ModifiedMaxAmount);
                   }
                   
               }*/
             
               /*
               agent.WieldedWeapon.AddExtraModifiedMaxValue((short)careerAmmo);

               var t = agent.WieldedWeapon.Ammo;

               agent.Equipment.GetMaxAmmo(WeaponClass.Cartridge);

               short s= (short) (currentAmmo+_careerCampaignBase.GetExtraAmmoPoints());
               agent.Equipment.SetAmountOfSlot(index,s,true);*/
                
              

            }
            //agent.SetWeaponAmmoAsClient(index,index,(short)currentAmmo);

        }

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            base.OnAgentHit(affectedAgent, affectorAgent, in affectorWeapon, in blow, in attackCollisionData);
            
            
            if (affectedAgent.Character==Campaign.Current.MainParty.LeaderHero.CharacterObject)
            {
                TORCommon.Say(""+affectedAgent.Health);
            }
        }
    }
}