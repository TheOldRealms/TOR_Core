using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.AI.CommonAIFunctions;

namespace TOR_Core.BattleMechanics.Artillery
{
    public abstract class BaseFieldSiegeWeapon : RangedSiegeWeapon
    {
        public bool PreferHighAngle = false;
        public abstract float ProjectileVelocity { get; }
        private BattleSideEnum _side;
        public override BattleSideEnum Side => _side;
        public void SetSide(BattleSideEnum side) => _side = side;
        public Target Target { get; protected set; }
        public Team Team { get; set; }
        public void SetTarget(Target target) => Target = target;
        public void ClearTarget() => Target = null;
        public bool IsTargetInRange(Vec3 position)
        {
            var startPos = ProjectileEntityCurrentGlobalPosition;
            var diff = position - startPos;
            var maxrange = Ballistics.GetMaximumRange(ShootingSpeed, diff.z);
            diff.z = 0;
            return diff.Length < maxrange;
        }

        public bool IsSafeToFire()
        {
            float distanceA, distanceE;
            Agent agent;
            using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
            {
                agent = Mission.Current.RayCastForClosestAgent(MissleStartingPositionForSimulation, MissleStartingPositionForSimulation + ShootingDirection.NormalizedCopy() * 60, out distanceA, -1, 0.05f);
                Mission.Current.Scene.RayCastForClosestEntityOrTerrainMT(MissleStartingPositionForSimulation, MissleStartingPositionForSimulation + ShootingDirection.NormalizedCopy() * 25, out distanceE, out GameEntity _, 0.05f);
            }
            return !(distanceA < 50 && agent != null && !agent.IsEnemyOf(PilotAgent) || distanceE < 15);
        }

        public float GetEstimatedCurrentFlightTime()
        {
            //return 0;
            if (Target == null) return 0;
            var diff = Target.SelectedWorldPosition - MissleStartingPositionForSimulation;
            return Ballistics.GetTimeOfProjectileFlight(ShootingSpeed, currentReleaseAngle, diff.Length);
        }

        public override float GetTargetReleaseAngle(Vec3 target)
        {
            float angle = GetTargetReleaseAngle(target, out _);
            if (angle == float.NaN) angle = base.GetTargetReleaseAngle(target);
            return angle;
        }

        public float GetTargetReleaseAngle(Vec3 target, out Vec3 launchVec)
        {
            Vec3 low = Vec3.Zero;
            Vec3 high = Vec3.Zero;
            launchVec = Vec3.Zero;
            float angle = 0;
            int numSolutions = Ballistics.GetLaunchVectorForProjectileToHitTarget(MissleStartingPositionForSimulation, ShootingSpeed, target, out low, out high);
            if (numSolutions <= 0) return float.NaN;

            if (numSolutions == 2)
            {
                if (PreferHighAngle) launchVec = high;
                else launchVec = low;
            }
            else
            {
                if (low != Vec3.Zero) launchVec = low;
                else launchVec = high;
            }

            Vec3 forward = launchVec.NormalizedCopy();
            forward.z = 0;
            Vec3 dir = launchVec.NormalizedCopy();
            Vec3 diff = dir - forward;
            float zDiff = diff.z;
            angle = Vec3.AngleBetweenTwoVectors(forward, dir);
            if (zDiff < 0) angle = -angle;
            return angle;
        }

        public new Vec3 GetEstimatedTargetMovementVector(Vec3 targetCurrentPosition, Vec3 targetVelocity)
        {
            if (targetVelocity == Vec3.Zero) return Vec3.Zero;
            return targetVelocity * GetEstimatedCurrentFlightTime();
        }

        public override bool IsDisabledForBattleSideAI(BattleSideEnum sideEnum)
        {
            return sideEnum != Side;
        }

        protected void ForceAmmoPointUsage()
        {
            if (State == WeaponState.LoadingAmmo && !LoadAmmoStandingPoint.HasUser && !LoadAmmoStandingPoint.HasAIMovingTo)
            {
                foreach (var sp in AmmoPickUpStandingPoints)
                {
                    if (sp.IsDeactivated) sp.SetIsDeactivatedSynched(false);
                }
            }
            else
            {
                foreach (var sp in AmmoPickUpStandingPoints)
                {
                    if (!sp.IsDeactivated) sp.SetIsDeactivatedSynched(true);
                }
            }
        }
    }
}
