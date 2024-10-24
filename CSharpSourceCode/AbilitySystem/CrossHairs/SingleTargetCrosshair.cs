using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.AbilitySystem.Crosshairs
{
    public class SingleTargetCrosshair(AbilityTemplate template) : MissileCrosshair(template)
    {
        public override void Tick()
        {
            FindTarget();
        }

        public override void Show()
        {
            base.Show();
            _cachedTarget = null;
        }

        public override void Hide()
        {
            base.Hide();
            UnlockTarget();
        }

        private void FindTarget()
        {
            Vec3 sourcePoint = Vec3.Zero;
            Vec3 targetPoint = Vec3.Zero;
            _missionScreen.ScreenPointToWorldRay(Input.MousePositionRanged, out sourcePoint, out targetPoint);
            if (!_mission.CameraIsFirstPerson)
            {
                _missionScreen.GetProjectedMousePositionOnGround(out targetPoint, out _, BodyFlags.CommonFocusRayCastExcludeFlags, true);
            }
            float collisionDistance;
            Agent newTarget;
            using(new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
            {
                newTarget = _mission.RayCastForClosestAgent(sourcePoint, targetPoint, out collisionDistance);
            }
            if (newTarget == null)
            {
                UnlockTarget();
                return;
            }
            if (newTarget.IsMount && newTarget.RiderAgent != null)
            {
                newTarget = newTarget.RiderAgent;
            }
            var targetType = _template.AbilityTargetType;
            bool isTargetMatching = collisionDistance <= _template.MaxDistance &&
                                    ((targetType == AbilityTargetType.SingleEnemy|| targetType ==  AbilityTargetType.EnemiesInAOE) && newTarget.IsEnemyOf(_caster)) ||          // the target filter can be single, but the effect for multiple
                                    ((targetType == AbilityTargetType.SingleAlly || targetType ==  AbilityTargetType.AlliesInAOE) && !newTarget.IsEnemyOf(_caster));
            if (isTargetMatching)
            {
                if (newTarget != _cachedTarget)
                {
                    UnlockTarget();
                }

                LockTarget(newTarget);
                Position = newTarget.Position;
            }
            else
            {
                UnlockTarget();
                Position = new Vec3();
            }
        }

        private void LockTarget(Agent newTarget)
        {
            _cachedTarget = newTarget;
            if (newTarget.IsEnemyOf(_caster))
            {
                _cachedTarget.AgentVisuals.SetContourColor(enemyColor);
            }
            else
            {
                _cachedTarget.AgentVisuals.SetContourColor(friendColor);
            }
            _isTargetLocked = true;
        }

        public void UnlockTarget()
        {
            if (Mission.Current.CurrentState != Mission.State.Over)
            {
                if (_cachedTarget!=null&& !_cachedTarget.IsFadingOut())
                {
                    _cachedTarget.AgentVisuals?.SetContourColor(colorLess);
                }
                
            }
            
            _isTargetLocked = false;
        }


        public Agent CachedTarget
        {
            get => _cachedTarget;
        }

        public bool IsTargetLocked
        {
            get => _isTargetLocked;
        }

        private Agent _cachedTarget;

        private bool _isTargetLocked;
    }
}
