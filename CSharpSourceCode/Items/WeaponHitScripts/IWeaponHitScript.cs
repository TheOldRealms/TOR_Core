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
        void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData);
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

        protected static void ApplyWeaponTraitDamage(Agent target, int damage, TaleWorlds.Library.Vec3 impactPosition, Agent source = null, bool originatesFromAbility = false)
        {
            if (damage <= 0)
            {
                return;
            }

            // defer secondary damage outside the current hit callback to avoid changing agent state while the original blow is being resolved
            var abilityLogic = Mission.Current?.GetMissionBehavior<TOR_Core.AbilitySystem.AbilityManagerMissionLogic>();
            if (abilityLogic != null)
            {
                abilityLogic.QueueOnHitSecondaryDamage(target, damage, impactPosition, source, originatesFromAbility);
                return;
            }

            TOR_Core.Extensions.AgentExtensions.ApplyDamage(target, damage, impactPosition, source, doBlow: true, hasShockWave: false, originatesFromAbility: originatesFromAbility);
        }

        public virtual void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData)
        {

        }
    }
}