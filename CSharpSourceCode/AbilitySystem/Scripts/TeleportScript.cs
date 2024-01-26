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
        
        protected override void OnInit()
        {
            targetPosition = GameEntity.GlobalPosition; //works for what ever reason better
            _cameraView = Mission.Current.GetMissionBehavior<MissionCameraFadeView>();
        }

        protected override void OnAfterTick(float dt)
        {
            if (CasterAgent == null)
            {
                Stop();
                return;
            }
            if (CasterAgent.GetHero().HasCareerChoice("EnvoyOfTheLadyKeystone"))
            {
                var troops = Mission.Current.GetNearbyAllyAgents(CasterAgent.Position.AsVec2, 5f, CasterAgent.Team, new MBList<Agent>());
                troops.Remove(CasterAgent); // so the player is not affected

                foreach (var troop in troops) troop.TeleportToPosition(Mission.Current.GetRandomPositionAroundPoint(targetPosition, 1, 3));
            }

            _cameraView.BeginFadeOutAndIn(0.1f, 0.1f, 0.5f);

            CasterAgent.TeleportToPosition(targetPosition);
            
            Stop();
        }
    }
}