using System.Linq;
using SandBox.Objects.Usables;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics.SFX
{
    public class PlayerFlyableObjectScript : ScriptComponentBehavior
    {
        private StandingPoint sitingPoint;
        private bool seated;
        private bool init;
        private bool animationSkipped;
        private bool animationSkipped2;
        
        public bool IsReady()
        {
            return init && seated;
        }

        protected override void OnRemoved(int removeReason)
        {
            if (seated)
            {
                DeactivateFlying();
            }

            base.OnRemoved(removeReason);
        }

        protected override void OnInit()
        {
            base.OnInit();
            this.SetScriptComponentToTick(this.GetTickRequirement());
            var chair = GameEntity.GetScriptComponents<Chair>().FirstOrDefault();
            sitingPoint = chair.StandingPoints[0];
        }

        public override TickRequirement GetTickRequirement() => base.GetTickRequirement() | TickRequirement.Tick;


        protected override void OnTick(float dt)
        {
            if (!init) return;
            base.OnTick(dt);

            if (Agent.Main.CurrentlyUsedGameObject == sitingPoint)
            {
                var sitAnim = ActionIndexCache.Create("act_sit_1");
                Agent.Main.SetActionChannel(0, sitAnim, true); //the sitting for what ever reason affects not just the visuals but also connects the agent to the chair. I did not had more time to test this more deliberate, but it works fine and not without it.
                seated = true;
            }

            if (!seated) return;
            if (seated && !animationSkipped2 && Agent.Main.GetCurrentAction(0).Name.StartsWith("act_stand_up"))
            {
                var sitAnim = ActionIndexCache.Create("act_none");
                Agent.Main.SetActionChannel(0, sitAnim, true);
                animationSkipped2 = true;
            }

            if (seated && !animationSkipped && Agent.Main.GetCurrentAction(0).Name.StartsWith("act_sit_down"))
            {
                var sitAnim = ActionIndexCache.Create("act_sit_1");
                Agent.Main.SetActionChannel(0, sitAnim, true);
                animationSkipped = true;
            }

            Agent.Main.TeleportToPosition(GameEntity.GlobalPosition);
        }

        public void Advance(MatrixFrame frame)
        {
            GameEntity.SetGlobalFrameMT(frame);
            GameEntity.GetChild(0).SetGlobalFrameMT(frame);
        }
        
        public void ActivateFlying()
        {
            var chair = GameEntity.GetScriptComponents<Chair>().FirstOrDefault();
            var point = chair.StandingPoints[0];

            chair.Activate();
            if (!init)
            {
                point.SetUserForClient(Agent.Main);
                Agent.Main.TeleportToPosition(GameEntity.GlobalPosition + Vec3.Up);

                Agent.Main.UseGameObject(point);
                init = true;
            }
        }
        
        public void DeactivateFlying()
        {
            var agent = Agent.Main;
            if (agent != null)
            {
                agent.Appear();
                agent.SetActionChannel(0, ActionIndexCache.Create("act_none"), true);
                agent.StopUsingGameObject();
            }

            sitingPoint.SetDisabledAndMakeInvisible();
            seated = false;
        }
    }
}