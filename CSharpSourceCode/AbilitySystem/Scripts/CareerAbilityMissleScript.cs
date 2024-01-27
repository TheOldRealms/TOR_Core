using TaleWorlds.Library;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class CareerAbilityMissleScript: CareerAbilityScript
    {
        private float movementSpeed;
        protected override bool ShouldMove()
        {
            //return base.ShouldMove();
            return true;
            
        }
        
        protected override MatrixFrame GetNextGlobalFrame(MatrixFrame oldFrame, float dt)
        {
            return oldFrame.Advance(this.Ability.Template.BaseMovementSpeed * dt);
        }
    }
}