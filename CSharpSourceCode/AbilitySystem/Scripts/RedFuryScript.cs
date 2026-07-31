using TaleWorlds.Engine;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class RedFuryScript : CareerAbilityScript
    {
        public override void Initialize(Ability ability, ref GameEntity entity)
        {
            base.Initialize(ability, ref entity);
        }

        protected override void OnBeforeRemoved(int removeReason)
        {
        }
    }
}