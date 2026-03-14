using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class DoomSeekingScript : CareerAbilityScript
    {
        public override void Initialize(Ability ability, ref GameEntity entity)
        {
            base.Initialize(ability, ref entity);
        }

        protected override void OnBeforeRemoved(int removeReason)
        {
            // Don't reset CareerMissionVariables[0] - Doom Seeker stacks persist throughout the battle
        }
    }
}
