using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.SFX;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class ShadowStepScript : CareerAbilityScript
    {
        private readonly InputKey[] _axisKeys = new InputKey[4];
        private readonly GameKeyContext _keyContext = HotKeyManager.GetCategory("Generic");
        private const float _minimalDistance = 2;
        private float _speed = 10f;
        private float _effectTickInterval;
        private float _previousCameraAddedDistance = 0;
        private PlayerFlyableObjectScript _playerFlyableObjectScript;

        public override void Initialize(Ability ability, ref GameEntity entity)
        {
            base.Initialize(ability, ref entity);
            SaveKeyBindings();
            _speed = 10;
            if (Agent.Main.GetHero().GetAllCareerChoices().Contains("FeralKeystone"))
            {
                _speed *= 1.2f;
            }

            _previousCameraAddedDistance = Mission.CameraAddedDistance;
            Mission.CameraAddedDistance = 200; //max possible value is 2.4
            _effectTickInterval = ability.Template.TickInterval;
        }

        private void InstantiateFlightPrefab(MatrixFrame frame)
        {
            var chair = TaleWorlds.Engine.GameEntity.Instantiate(Mission.Current.Scene, "FlyableObject", frame);
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
            //_axisKeys[0] is the move forward button; it is left unbound so the player can use mistform regardless of what keybinds they use when playing
            for (var i = 1; i < 4; i++) _keyContext.GetGameKey(i).KeyboardKey.ChangeKey(InputKey.Invalid);
        }

        protected override void OnBeforeTick(float dt)
        {
            if (CasterAgent == null || CasterAgent.State != AgentState.Active || CasterAgent.Health <= 0)
            {
                if (_playerFlyableObjectScript.IsReady())
                {
                    //eventually triggers removal of the script and the chair game entity is removed with OnBeforeRemoved
                    _playerFlyableObjectScript.DeactivateFlying();
                }
                return;
            }
            if (!HasTickedOnce)
            {
                CasterAgent.Disappear();
                CasterAgent.ToggleInvulnerable();
                if (CasterAgent.IsPlayerControlled) DisbindKeyBindings();
                var frame = CasterAgent.Frame.Elevate(3f);
                Agent.Main.TeleportToPosition(frame.origin);
                GameEntity.SetGlobalFrame(frame);
                InstantiateFlightPrefab(frame);
            }
            else
            {
                if (Input.IsKeyDown(_axisKeys[0]))
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

        private float GetDistance()
        {
            float num = 3;
            var pos2 = GameEntity.GetGlobalFrame().origin;
            var pos = GameEntity.GetGlobalFrame().Elevate(-_minimalDistance).origin;

            using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
            {
                if (Mission.Current.Scene.RayCastForClosestEntityOrTerrain(pos2, pos, out float distance))
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
            GameEntity.SetGlobalFrame(frame);
        }

        protected override void OnBeforeRemoved(int removeReason)
        {
            RestoreKeyBindings();
            if (CasterAgent.State == AgentState.Unconscious || CasterAgent.State == AgentState.Killed) //unsure if this can throw a NRE if the player dies during mistform
            {
                //var lookDirection = CasterAgent.LookDirection;
                CasterAgent.AgentVisuals.GetEntity().ActivateRagdoll();
                //continuous memory lock errors when trying to touch the player's corpse entity after it's dead regardless of where in the script process I try to touch it; ragdolling works, adding (extra?) physics isn't an issue, but specifically an impulse throws the error. Doesn't matter if the impulse is applied before or after ragdoll. Don't really want to hide the original entity and create a duplicate to work with, but it looks like I need to to prevent a possible issue of momentum being lost when transitioning out of mist form to dead body
                //entity.ApplyLocalImpulseToDynamicBody(entity.CenterOfMass, new Vec3(lookDirection.x, lookDirection.y, lookDirection.z) * 50);
            }
            CasterAgent.Appear();
            CasterAgent.ToggleInvulnerable();
            _playerFlyableObjectScript.DeactivateFlying();
            _playerFlyableObjectScript.GameEntity.Remove(0);
            Mission.CameraAddedDistance = _previousCameraAddedDistance;
        }
    }
}