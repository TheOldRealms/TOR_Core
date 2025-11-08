using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;
using TOR_Core.Items.InventoryUseScripts;

namespace TOR_Core.Items.WeaponHitScripts
{
    public interface IWeaponHitScript
    {
        void OnHit(Agent attackingAgent, Agent attackedAgent,  Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData);
    }

    public class BaseWeaponHitScript : IWeaponHitScript
    {
      
        protected string[] _arguments;

        protected BaseWeaponHitScript(string[] arguments)
        {
            _arguments = arguments;
        }

        protected BaseWeaponHitScript()
        {
            
        }

        public virtual void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData)
        {
            
        }
    }
}
