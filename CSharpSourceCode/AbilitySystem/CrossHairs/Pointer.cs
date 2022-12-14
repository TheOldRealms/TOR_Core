using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.AbilitySystem.Crosshairs
{
    public class Pointer : AbilityCrosshair
    {
        public Pointer(AbilityTemplate template) : base(template)
        {
            _crosshair = GameEntity.Instantiate(Mission.Current.Scene, "circular_targeting_rune", false);
            _crosshair.EntityFlags |= EntityFlags.NotAffectedBySeason;
            MatrixFrame frame = _crosshair.GetFrame();
            frame.Scale(new Vec3(template.TargetCapturingRadius, template.TargetCapturingRadius, 1, -1));
            _crosshair.SetFrame(ref frame);
            InitializeColors();
            AddLight();
            IsVisible = false;
        }

        public override void Tick()
        {
            UpdatePosition();
            Rotate();
            ChangeColor();
        }

        private void UpdatePosition()
        {
            if (_caster != null)
            {
                if (_missionScreen.GetProjectedMousePositionOnGround(out _position, out _normal, BodyFlags.CommonFocusRayCastExcludeFlags, true))
                {
                    _currentDistance = _caster.Position.Distance(_position);
                    if (_currentDistance > _template.MaxDistance)
                    {
                        _position = _caster.LookFrame.Advance(_template.MaxDistance).origin;
                        _position.z = _mission.Scene.GetGroundHeightAtPosition(Position);
                    }
                    Position = _position;
                    Mat3 _rotation = Mat3.CreateMat3WithForward(in _normal);
                    _rotation.RotateAboutSide(-90f.ToRadians());
                    Rotation = _rotation;
                }
                else
                {
                    _position = _caster.LookFrame.Advance(_template.MaxDistance).origin;
                    _position.z = _mission.Scene.GetGroundHeightAtPosition(Position);
                    Position = _position; 
                }
            }
        }

        private float _currentDistance;
       
        private Vec3 _position;
        
        private Vec3 _normal;
    }
}
