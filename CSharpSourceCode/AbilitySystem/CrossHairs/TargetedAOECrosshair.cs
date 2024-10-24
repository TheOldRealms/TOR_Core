using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.AbilitySystem.Crosshairs
{
    public class TargetedAOECrosshair : AbilityCrosshair
    {
        public TargetedAOECrosshair(AbilityTemplate template) : base(template)
        {
            _targetType = _template.AbilityTargetType;
            _crosshair = GameEntity.Instantiate(Mission.Current.Scene, "circular_targeting_rune", false);
            _crosshair.EntityFlags |= EntityFlags.NotAffectedBySeason;
            MatrixFrame frame = _crosshair.GetFrame();
            frame.Scale(new Vec3(template.TargetCapturingRadius * 2, template.TargetCapturingRadius * 2, 1, -1));
            _crosshair.SetFrame(ref frame);
            InitializeColors();
            AddLight();
            IsVisible = false;
        }

        public override void Tick()
        {
            if (_caster != null)
            {
                UpdatePosition();
                if (Targets != null)
                {
                    _previousTargets = new MBReadOnlyList<Agent>([.. Targets]);
                }
                UpdateTargets();
                UpdateAgentsGlow();
                Rotate();
                ChangeColor();
            }
        }

        public override void Hide()
        {
            base.Hide();
            ClearArrays();
        }

        private void UpdatePosition()
        {
            if (_caster != null)
            {
                if (_missionScreen.GetProjectedMousePositionOnGround(out _position, out _normal, BodyFlags.CommonFocusRayCastExcludeFlags,  true))
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

        private void UpdateTargets()
        {
            switch (_targetType)
            {
                case AbilityTargetType.AlliesInAOE:
                    {
                        Targets = _mission.GetNearbyAllyAgents(Position.AsVec2, _template.TargetCapturingRadius, _mission.PlayerTeam, Targets);
                        break;
                    }
                case AbilityTargetType.EnemiesInAOE:
                    {
                        Targets = _mission.GetNearbyEnemyAgents(Position.AsVec2, _template.TargetCapturingRadius, _mission.PlayerTeam, Targets);
                        break;
                    }
            }
        }

        private void UpdateAgentsGlow()
        {
            if (Targets != null)
            {
                foreach(var agent in Targets)
                {
                    if (agent.State == TaleWorlds.Core.AgentState.Active || agent.State == TaleWorlds.Core.AgentState.Routed)
                    {
                        switch (_targetType)
                        {
                            case AbilityTargetType.AlliesInAOE:
                                {
                                    agent.AgentVisuals.GetEntity().Root.SetContourColor(friendColor, true);
                                    break;
                                }
                            case AbilityTargetType.EnemiesInAOE:
                                {
                                    agent.AgentVisuals.GetEntity().Root.SetContourColor(enemyColor, true);
                                    break;
                                }
                        }
                    }
                }
            }
            if (_previousTargets != null)
            {
                foreach (Agent agent in _previousTargets.Except(Targets))
                    agent.AgentVisuals.GetEntity().Root.SetContourColor(colorLess, true);
            }
        }

        private void ClearArrays()
        {
            if (Targets != null)
                foreach (Agent agent in Targets)
                    agent.AgentVisuals.GetEntity().Root.SetContourColor(colorLess, true);
            if (_previousTargets != null)
                foreach (Agent agent in _previousTargets.Except(Targets))
                    agent.AgentVisuals.GetEntity().Root.SetContourColor(colorLess, true);
            _previousTargets = null;
            Targets.Clear();
        }

        public MBList<Agent> Targets  = [];

        private MBReadOnlyList<Agent> _previousTargets;

        private float _currentDistance;

        private Vec3 _position;

        private Vec3 _normal;

        private AbilityTargetType _targetType;
    }
}
