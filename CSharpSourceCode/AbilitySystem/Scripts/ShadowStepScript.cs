using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.SFX;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class ShadowStepScript : CareerAbilityScript
    {
        private readonly InputKey[] _axisKeys = new InputKey[4];
        private readonly GameKeyContext _keyContext = HotKeyManager.GetCategory("Generic");
        private const float _minimalDistance = 2;
        private float _speed = 10f;
        private float _effectTickInterval;
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
            _effectTickInterval = ability.Template.TickInterval;
        }

        private void InstantiateFlightPrefab(MatrixFrame frame)
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

        protected override void OnBeforeTick(float dt)
        {
            if (!HasTickedOnce)
            {
                CasterAgent.Disappear();
                CasterAgent.ToggleInvulnerable();
                if (CasterAgent.IsPlayerControlled) DisbindKeyBindings();
                var frame = CasterAgent.Frame.Elevate(3f);
                Agent.Main.TeleportToPosition(frame.origin);
                GameEntity.SetGlobalFrameMT(frame);
                InstantiateFlightPrefab(frame);
            }
            else
            {
                if (CasterAgent != null && CasterAgent.Health > 0)
                {
                    if (Input.IsKeyDown(InputKey.W) || Input.IsKeyPressed(InputKey.W))
                    {
                        if (_playerFlyableObjectScript.IsReady() && GetDistance() > _minimalDistance)
                        {
                            Fly(dt);
                        }
                        else
                        {
                            Fly(-dt * 0.5f);
                        }
                    }
                }
            }
        }

        private float GetDistance()
        {
            float num = 3;
            var pos2 = GameEntity.GetGlobalFrame().origin;
            var pos = GameEntity.GetGlobalFrame().Elevate(-_minimalDistance).origin;

            using(new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
            {
                if (Mission.Current.Scene.RayCastForClosestEntityOrTerrainMT(pos2, pos, out float distance))
                {
                    num = distance;
                }
            }

            return num;
        }

        private void Fly(float dt)
        {
            if (_playerFlyableObjectScript.GameEntity == null) return;

            var frame = _playerFlyableObjectScript.GameEntity.GetGlobalFrame();
            frame.rotation = Mission.Current.GetCameraFrame().rotation;
            frame.Elevate(-_speed * dt);

            _playerFlyableObjectScript.Advance(frame);
            GameEntity.SetGlobalFrameMT(frame);
        }

        protected override void OnBeforeRemoved(int removeReason)
        {
            RestoreKeyBindings();
            CasterAgent.Appear();
            CasterAgent.ToggleInvulnerable();
            _playerFlyableObjectScript.DeactivateFlying();
            _playerFlyableObjectScript.GameEntity.Remove(0);
            Mission.CameraAddedDistance = 0;
        }
    }
}