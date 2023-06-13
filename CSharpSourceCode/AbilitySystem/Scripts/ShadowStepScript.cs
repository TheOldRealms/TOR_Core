using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class ShadowStepScript : CareerAbilityScript
    {

        private float effectTickIntervall;
        private float currentTick;

        public override void Initialize(Ability ability)
        {
            base.Initialize(ability);
            var sphere = GameEntity.Instantiate(Mission.Current.Scene, "flying_sphere", true);
            sphere.BodyFlag = BodyFlags.Barrier3D;
            sphere.BodyFlag |= BodyFlags.DontCollideWithCamera;
            sphere.EntityVisibilityFlags |= EntityVisibilityFlags.VisibleOnlyForEnvmap;
            GameEntity.AddChild(sphere);
            SaveKeyBindings();
            _speed = 10;
            if (Agent.Main.GetHero().GetAllCareerChoices().Contains("NewBloodKeystone"))
            {
                _speed *= 1.2f;
            }

            effectTickIntervall = ability.Template.TickInterval;
        }

        public override void SetAgent(Agent agent)
        {
            base.SetAgent(agent);
            agent.Disappear();
            agent.ToggleInvulnerable();
            TriggerEffects(agent.Position,agent.Position.NormalizedCopy());
            if(agent.IsPlayerControlled) DisbindKeyBindings();
            var frame = _casterAgent.Frame.Elevate(1f);
            GameEntity.SetGlobalFrame(frame);
        }

        private void SaveKeyBindings()
        {
            for (int i = 0; i < 4; i++)
            {
                _axisKeys[i] = _keyContext.GetGameKey(i).KeyboardKey.InputKey;
            }
        }

        private void RestoreKeyBindings()
        {
            _keyContext.GetGameKey(0).KeyboardKey.ChangeKey(_axisKeys[0]);
            _keyContext.GetGameKey(1).KeyboardKey.ChangeKey(_axisKeys[1]);
            _keyContext.GetGameKey(2).KeyboardKey.ChangeKey(_axisKeys[2]);
            _keyContext.GetGameKey(3).KeyboardKey.ChangeKey(_axisKeys[3]);
        }

        private void DisbindKeyBindings()
        {
            for (int i = 0; i < 4; i++)
            {
                _keyContext.GetGameKey(i).KeyboardKey.ChangeKey(InputKey.Invalid);
            }
        }

        protected override void OnTick(float dt)
        {
            if (_ability == null) return;
            if (IsFading) return;
            _timeSinceLastTick += dt;
            UpdateLifeTime(dt);
            
            

            if (_casterAgent != null && _casterAgent.Health > 0)
            {
                UpdateSound(_casterAgent.Position);
                var dist = GetDistance();
                if (Input.IsKeyDown(InputKey.W) || Input.IsKeyPressed(InputKey.W))
                {
                    if (dist > 3 || dist.Equals(float.NaN))
                    {
                        Fly(dt);
                    }
                }
            }
            

            MBList<Agent> agents = new MBList<Agent>();
            agents = Mission.Current.GetNearbyAgents(_casterAgent.Position.AsVec2, 2, agents);
            foreach (Agent agent in agents)
            {
                if (agent != _casterAgent && MathF.Abs(_casterAgent.Position.Z - agent.Position.Z) < 1)
                {
                    Vec3 pos = agent.Position - _casterAgent.Position;
                    pos.Normalize();
                    pos.z = 0;
                    agent.TeleportToPosition(agent.Position + pos);
                }
            }

            currentTick += dt;
            if (dt >= effectTickIntervall)
            {
                TriggerEffects(_casterAgent.Position,-_casterAgent.Position.NormalizedCopy());
                currentTick = 0f;
            }
            
        }

        private float GetDistance()
        {
            float num;
            Vec3 pos = _casterAgent.LookFrame.Advance(3).origin;
            Mission.Current.Scene.RayCastForClosestEntityOrTerrain(_casterAgent.Position + new Vec3(0f, 0f, _casterAgent.GetEyeGlobalHeight(), -1f), pos, out num);
            return num;
        }
        
        private void Fly(float dt)
        {
            MatrixFrame frame = GameEntity.GetGlobalFrame();
            frame.rotation = _casterAgent.LookRotation;
            frame.Advance(_speed * dt);
            GameEntity.SetGlobalFrame(frame);
        }

        public override void Stop()
        {
            base.Stop();
            RestoreKeyBindings();
            _casterAgent.Appear();
            _casterAgent.ToggleInvulnerable();
        }

        protected override void OnRemoved(int removeReason)
        {
            RestoreKeyBindings();
            base.OnRemoved(removeReason);
        }

        private float _speed = 10f;
        private InputKey[] _axisKeys = new InputKey[4];
        private GameKeyContext _keyContext = HotKeyManager.GetCategory("Generic");
    }
}