using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.AbilitySystem.Crosshairs
{
    public class WindCrosshair : AbilityCrosshair
    {
        public WindCrosshair(AbilityTemplate template) : base(template)
        {
            _crosshair = GameEntity.CreateEmpty(_mission.Scene, false);
            GameEntity decal = GameEntity.Instantiate(_mission.Scene, "linear_targeting_rune", false);

            MatrixFrame frame = decal.GetFrame();
            frame.Scale(new Vec3(template.Radius, template.Radius, 1, -1));
            frame.Advance(-0.8f);
            frame.Strafe(0.025f);
            decal.SetFrame(ref frame);
            _crosshair.AddChild(decal);
            _crosshair.EntityFlags |= EntityFlags.NotAffectedBySeason;
            InitializeColors();
            AddLight((Int32)template.Radius * 8);
            IsVisible = false;
        }

        public override void Tick()
        {
            UpdateFrame();
            ChangeColor();
        }

        private void UpdateFrame()
        {
            if (_caster != null)
            {
                _missionScreen.GetProjectedMousePositionOnGround(out _position, out _normal, BodyFlags.CommonFocusRayCastExcludeFlags, true);
                _currentHeight = _mission.Scene.GetGroundHeightAtPosition(Position);
                _currentDistance = _caster.Position.Distance(_position);
                _frame = _caster.LookFrame;
                _frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();

                if (_currentDistance < _template.MinDistance)
                {
                    _position = _frame.Advance(_template.MinDistance).origin;
                    _position.z = _currentHeight;
                }
                else if (_currentDistance > _template.MaxDistance)
                {
                    _position = _caster.LookFrame.Advance(_template.MaxDistance).origin;
                    _position.z = _currentHeight;
                }

                _frame.origin = _position;
                var _rotation = Mat3.CreateMat3WithForward(in _normal);
                _frame.rotation.u = _rotation.f;
                _frame.rotation.RotateAboutSide(5f.ToRadians());
                _frame.rotation.Orthonormalize();
                _crosshair.SetGlobalFrame(_frame);
            }
        }

        private float _currentHeight;

        private float _currentDistance;

        private Vec3 _position;

        private Vec3 _normal;

        private MatrixFrame _frame;
    }
}
