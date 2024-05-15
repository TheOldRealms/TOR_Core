using TaleWorlds.Library;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class CareerAbilityMissleScript: CareerAbilityScript
    {
        protected override bool ShouldMove()
        {
            return true;
        }
        
        protected override MatrixFrame GetNextGlobalFrame(MatrixFrame oldFrame, float dt)
        {
            return oldFrame.Advance(Ability.Template.BaseMovementSpeed * dt);
        }
    }
}