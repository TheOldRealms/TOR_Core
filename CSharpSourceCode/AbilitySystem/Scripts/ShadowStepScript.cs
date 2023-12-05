using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using TOR_Core.BattleMechanics.SFX;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class ShadowStepScript : CareerAbilityScript
    {
        private readonly InputKey[] _axisKeys = new InputKey[4];
        private readonly GameKeyContext _keyContext = HotKeyManager.GetCategory("Generic");
        private const float minimalDistance =2;
        private float _speed = 10f;
        private float currentTick;
        private float effectTickInterval;
        private Camera _Flycamera;


        private PlayerFlyableObjectScript _playerFlyableObjectScript;

        public override void Initialize(Ability ability)
        {
            base.Initialize(ability);
            SaveKeyBindings();
            _speed = 10;
            if (Agent.Main.GetHero().GetAllCareerChoices().Contains("FeralKeystone"))
            {
                _speed *= 1.2f;
            }

            Mission.CameraAddedDistance = 200;
            effectTickInterval = ability.Template.TickInterval;
        }

        public override void SetAgent(Agent agent)
        {
            base.SetAgent(agent);
            agent.Disappear();
            agent.ToggleInvulnerable();
            TriggerEffects(agent.Position, agent.Position.NormalizedCopy());
            if (agent.IsPlayerControlled) DisbindKeyBindings();
            var frame = _casterAgent.Frame.Elevate(3f);
            Agent.Main.TeleportToPosition(frame.origin);
            GameEntity.SetGlobalFrame(frame);
            InstantiateFlightPrefab(frame);

        }

        private void  InstantiateFlightPrefab(MatrixFrame frame)
        {
            var chair = GameEntity.Instantiate(Mission.Current.Scene, "FlyableObject", frame);
            chair.BodyFlag = BodyFlags.Barrier3D;
            chair.BodyFlag |= BodyFlags.DontCollideWithCamera;
            chair.BodyFlag |= BodyFlags.CommonCollisionExcludeFlagsForAgent;
            chair.EntityVisibilityFlags |= EntityVisibilityFlags.VisibleOnlyForEnvmap;
            
            _playerFlyableObjectScript = chair.GetFirstScriptOfType<PlayerFlyableObjectScript>();
            _playerFlyableObjectScript.ActivateFlying();
        }

        private void SaveKeyBindings()
        {
            for (var i = 0; i < 4; i++) _axisKeys[i] = _keyContext.GetGameKey(i).KeyboardKey.InputKey;
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
            for (var i = 0; i < 4; i++) _keyContext.GetGameKey(i).KeyboardKey.ChangeKey(InputKey.Invalid);
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
             if (Input.IsKeyDown(InputKey.W) || Input.IsKeyPressed(InputKey.W))
             {
                 if (_playerFlyableObjectScript.IsReady()&& GetDistance()>minimalDistance)
                 {
                     Fly(dt);
                 }
                 else
                 {
                     Fly(-dt*0.5f);
                 }
             }
            }
            
            if (_timeSinceLastTick >= effectTickInterval)
            {
                TriggerEffects(_casterAgent.Position, -_casterAgent.Position.NormalizedCopy());
                _timeSinceLastTick = 0f;
            }

            
        }

        private float GetDistance()
        {
            float num;
            var pos2= GameEntity.GetGlobalFrame().origin;
            var pos = GameEntity.GetGlobalFrame().Elevate(-minimalDistance).origin;
            if (Mission.Current.Scene.RayCastForClosestEntityOrTerrain(pos2, pos, out num))
            {
                TORCommon.Say(num+"");
                return num;
            }
            else
            {
                return 3;
            } 
            
        }

        private void Fly(float dt)
        {
            if(_playerFlyableObjectScript.GameEntity==null) return;

            var frame = _playerFlyableObjectScript.GameEntity.GetGlobalFrame();
            frame.rotation = Mission.Current.GetCameraFrame().rotation;
            frame.Elevate(-_speed * dt);
            
            _playerFlyableObjectScript.Advance(frame);
            GameEntity.SetGlobalFrame(frame);

        }

        public override void Stop()
        {
            base.Stop();
            RestoreKeyBindings();
            _casterAgent.Appear();
            _casterAgent.ToggleInvulnerable();
            _playerFlyableObjectScript.DeactivateFlying();
            _playerFlyableObjectScript.GameEntity.Remove(0);

            Mission.CameraAddedDistance = 0;
            TriggerEffects(_casterAgent.Position, -_casterAgent.Position.NormalizedCopy());
        }

        protected override void OnRemoved(int removeReason)
        {
            RestoreKeyBindings();
            base.OnRemoved(removeReason);
        }
    }
}