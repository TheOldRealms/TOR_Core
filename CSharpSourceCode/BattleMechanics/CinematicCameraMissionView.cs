using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace TOR_Core.BattleMechanics
{
    [DefaultView]
    public class CinematicCameraMissionView : MissionView
    {
        private Camera _camera;
        private bool _hasInitialized = false;
        private bool _isCameraActive = false;
        private float _cameraSpeed = 5f;
        private MissionMainAgentController _agentController;
        protected IInputContext Input => MissionScreen.InputManager;

        public CinematicCameraMissionView()
        {
            _hasInitialized = false;
        }

        public override void AfterStart()
        {
            _agentController = Mission.GetMissionBehavior<MissionMainAgentController>();
        }

        public override bool UpdateOverridenCamera(float dt)
        {
            if(_hasInitialized && _isCameraActive)
            {
                Vec2 rotation = new Vec2(Input.GetMouseMoveX(), Input.GetMouseMoveY());
                if (MathF.Abs(rotation.x) < 0.2f)
                {
                    rotation.x = 0f;
                }
                if (MathF.Abs(rotation.y) < 0.2f)
                {
                    rotation.y = 0f;
                }
                
                MatrixFrame cameraFrame = _camera.Frame;
                cameraFrame.rotation.RotateAboutSide(-rotation.y.ToRadians() * dt * _cameraSpeed);
                cameraFrame.rotation.RotateAboutForward(-rotation.x.ToRadians() * dt * _cameraSpeed);

                float forwardMovement = 0f;
                float sideMovement = 0f;
                /* Gamekeys for movement
                 * 0 - Forward
                 * 1 - Backward
                 * 2 - Left
                 * 3 - Right
                 */
                if (Input.IsGameKeyDown(0)) forwardMovement -= 1f;
                if (Input.IsGameKeyDown(1)) forwardMovement += 1f;
                if (Input.IsGameKeyDown(2)) sideMovement -= 1f;
                if (Input.IsGameKeyDown(3)) sideMovement += 1f;

                cameraFrame.Elevate(forwardMovement * dt * _cameraSpeed);
                cameraFrame.Strafe(sideMovement * dt * _cameraSpeed);

                // Camera coordinate system: u = forward, f = elevation/up, s = side/right
                Vec3 worldUp = new Vec3(0f, 0f, 1f);
                Vec3 cameraForward = cameraFrame.rotation.u;
                cameraForward.Normalize();

                // Calculate corrected side vector perpendicular to world up and camera forward
                Vec3 correctedSide = Vec3.CrossProduct(worldUp, cameraForward);

                // Handle edge case: looking straight up or down
                if (correctedSide.LengthSquared < 0.01f)
                {
                    correctedSide = cameraFrame.rotation.s;
                }
                else
                {
                    correctedSide.Normalize();
                }

                // Calculate corrected elevation vector perpendicular to forward and side
                Vec3 correctedElevation = Vec3.CrossProduct(cameraForward, correctedSide);
                correctedElevation.Normalize();

                // Rebuild rotation matrix with corrected orientation
                cameraFrame.rotation.u = cameraForward;
                cameraFrame.rotation.s = correctedSide;
                cameraFrame.rotation.f = correctedElevation;

                _camera.Frame = cameraFrame;
            }

            return false;
        }

        public override void OnMissionScreenTick(float dt)
        {
            if(!_hasInitialized && _camera == null && MissionScreen.CombatCamera != null && MissionScreen.MissionStartedRendering())
            {
                _camera = Camera.CreateCamera();
                _camera.FillParametersFrom(MissionScreen.CombatCamera);
                _hasInitialized = true;
            }

            if (Input.IsKeyReleased(InputKey.NumpadPlus))
            {
                _camera.Frame = MissionScreen.CombatCamera.Frame;
                MissionScreen.CustomCamera = _camera;
                _agentController.IsDisabled = true;
                _agentController.Disable();
                _isCameraActive = true;
            }

            if (Input.IsKeyReleased(InputKey.NumpadMinus))
            {
                MissionScreen.CustomCamera = null;
                _agentController.IsDisabled = false;
                _agentController.Enable();
                _isCameraActive = false;
            }

            UpdateOverridenCamera(dt);
        }
    }
}
