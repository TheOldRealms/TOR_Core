using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class TeleportScript : CareerAbilityScript
    {
        private Vec3 targetPosition;
        private bool teleported;


        protected override void OnInit()
        {
          //  base.OnInit();
            targetPosition = GameEntity.GlobalPosition; //works for what ever reason better
        }

        protected override void OnTick(float dt)
        {
            if(teleported) return;
            base.OnTick(dt);
            
            TORCommon.Say(GameEntity.GlobalPosition.ToWorldPosition().GetGroundVec3().ToString());
            if(_casterAgent==null) return;
            
            _casterAgent.TeleportToPosition( targetPosition);
            
            TriggerEffects(targetPosition,Vec3.Up);
            teleported = true;

            IsFading = true;

        }


    }
}