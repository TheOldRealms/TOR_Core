using TaleWorlds.MountAndBlade;

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

        public virtual void OnHit(Agent attackingAgent, Agent attackedAgent, Blow blow, MissionWeapon missionWeapon, AttackCollisionData collisionData)
        {

        }
    }
}