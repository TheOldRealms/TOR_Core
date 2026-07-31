using TaleWorlds.Library;
using static TaleWorlds.Engine.GameEntityPhysicsExtensions;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class BombardmentScript : AbilityScript
    {
        private bool _impulseGiven;

        protected override void OnAfterTick(float dt)
        {
            if (!_impulseGiven && Ability.Template.TriggerType == TriggerType.OnCollision)
            {
                _impulseGiven = true;
                GameEntity.ApplyLocalImpulseToDynamicBody(GameEntity.CenterOfMass, new Vec3(0, 0, -100));
            }
        }

        protected override void HandleCollision(Vec3 position, Vec3 normal)
        {
            normal.RotateAboutX(90f.ToRadians());
            base.HandleCollision(position, normal);
        }
    }
}