using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class TeleportScript : CareerAbilityScript
    {
        private MissionCameraFadeView _cameraView;
        private Vec3 targetPosition;
        private bool teleported;
        
        protected override void OnInit()
        {
            targetPosition = GameEntity.GlobalPosition; //works for what ever reason better
            _cameraView = Mission.Current.GetMissionBehavior<MissionCameraFadeView>();
        }

        protected override void OnTick(float dt)
        {
            if (teleported) return;
            base.OnTick(dt);

            TORCommon.Say(GameEntity.GlobalPosition.ToWorldPosition().GetGroundVec3().ToString());
            if (_casterAgent == null) return;

            if (_casterAgent.GetHero().HasCareerChoice("EnvoyOfTheLadyKeystone"))
            {
                var troops = Mission.Current.GetNearbyAllyAgents(_casterAgent.Position.AsVec2, 5f, _casterAgent.Team, new MBList<Agent>());
                troops.Remove(_casterAgent); // so the player is not affected

                foreach (var troop in troops) troop.TeleportToPosition(Mission.Current.GetRandomPositionAroundPoint(targetPosition, 1, 3));
            }

            _cameraView.BeginFadeOutAndIn(0.1f, 0.1f, 0.5f);

            _casterAgent.TeleportToPosition(targetPosition);


            TriggerEffects(targetPosition, Vec3.Up);
            teleported = true;

            IsFading = true;
        }
    }
}