using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Career
{
    public class CareerMissionLogic :MissionLogic
    {

        private CareerCampaignBase _careerCampaignBase;
        private bool offline;
        public override void EarlyStart()
        {
            base.EarlyStart();
            if (Game.Current.GameType is Campaign)
            {
                _careerCampaignBase = Campaign.Current.GetCampaignBehavior<CareerCampaignBase>();
            }
            else
                offline = true;
        }

        public override void OnAgentCreated(Agent agent)
        {
            if(offline) return;
            if (agent.Character==Campaign.Current.MainParty.LeaderHero.CharacterObject)
            {
                ModifyHealth(agent);
            }
        }

        private void ModifyHealth(Agent agent)
        {
    
            var ratio =agent.Health /agent.HealthLimit ;
            agent.HealthLimit+= _careerCampaignBase.GetMaximumHealthPoints();
            agent.Health = ratio*agent.HealthLimit;
        }

        private void ModifyAmmo(Agent agent)
        {
            int careerAmmo = _careerCampaignBase.GetExtraAmmoPoints();
            for (int i = 0; i < (int)EquipmentIndex.NumAllWeaponSlots; i++)
            {
                if(agent.Equipment[i].IsEmpty) continue;
                    
                MissionWeapon weapon = agent.Equipment[i];
                if (weapon.CurrentUsageItem.IsAmmo)
                {
                    if(weapon.CurrentUsageItem.WeaponClass == WeaponClass.Cartridge)
                        if (agent.GetOriginMobileParty().HasPerk(TORPerks.GunPowder.AmmoWagons))
                        {
                            careerAmmo = (int)(careerAmmo * 1.5);
                        }
                    weapon.AddExtraModifiedMaxValue((short)careerAmmo);
                    agent.SetWeaponAmountInSlot((EquipmentIndex) i ,weapon.ModifiedMaxHitPoints,false);
                }
            }
        }


        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (offline) return;
            if (agent.Character == Campaign.Current.MainParty.LeaderHero.CharacterObject)
            {
                ModifyAmmo(agent);
            }
        }
        
        

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            if (offline) return;
            if (affectedAgent.Character==Campaign.Current.MainParty.LeaderHero.CharacterObject)
            {
                TORCommon.Say(""+affectedAgent.Health);
            }
        }
    }
}