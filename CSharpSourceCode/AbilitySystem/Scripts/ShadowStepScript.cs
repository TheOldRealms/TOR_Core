using System.Collections.Generic;
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
        private Dictionary<int, InputKey> _keyboardMovementKeys = [];
        private Dictionary<int, InputKey> _controllerMovementKeys = [];
        private readonly GameKeyContext _keyContext = HotKeyManager.GetCategory("Generic");
        private IInputContext InputContext => Mission.Current.InputManager;
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
            for (var i = (int)GameKeyDefinition.Down; i <= (int)GameKeyDefinition.Right; i++)
            {
                var keyboardKey = _keyContext.GetGameKey(i).KeyboardKey?.InputKey ?? InputKey.Invalid;
                _keyboardMovementKeys[i] = keyboardKey;
            }
            for (var i = (int)GameKeyDefinition.Down; i <= (int)GameKeyDefinition.Right; i++)
            {
                var controllerKey = _keyContext.GetGameKey(i).ControllerKey?.InputKey ?? InputKey.Invalid;
                _controllerMovementKeys[i] = controllerKey;
            }
        }

        private void RestoreKeyBindings()
        {
            for (var i = (int)GameKeyDefinition.Down; i <= (int)GameKeyDefinition.Right; i++)
            {
                if (_keyboardMovementKeys.TryGetValue(i, out InputKey keyboardInputKey))
                _keyContext.GetGameKey(i).KeyboardKey.ChangeKey(keyboardInputKey);
            }

            for (var i = (int)GameKeyDefinition.Down; i <= (int)GameKeyDefinition.Right; i++)
            {
                if (_controllerMovementKeys.TryGetValue(i, out InputKey controllerInputKey))
                _keyContext.GetGameKey(i).ControllerKey.ChangeKey(controllerInputKey);
            }
        }

        private void DisbindKeyBindings()
        {
            //The GameKey for Up is left bound so the player moves forward with their normal key
            for (var i = (int)GameKeyDefinition.Down; i <= (int)GameKeyDefinition.Right; i++) _keyContext.GetGameKey(i).KeyboardKey.ChangeKey(InputKey.Invalid);
            for (var i = (int)GameKeyDefinition.Down; i <= (int)GameKeyDefinition.Right; i++) _keyContext.GetGameKey(i).ControllerKey.ChangeKey(InputKey.Invalid);
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
                if (InputContext.IsGameKeyPressed((int)GameKeyDefinition.Up))
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
            //Sly : this should be accounting for the chair's volume when advancing to avoid clipping the chair or player into terrain. The ray thickness can account for the widest part of the chair, and the distance reduced to leave a minimum amount of space for the depth.
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