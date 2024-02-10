using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class ProjectileScript : AbilityScript
    {
        private int _missileId;

        protected override bool ShouldMove() => true;

        protected override void OnBeforeTick(float dt)
        {
            if (HasTickedOnce) CheckMissile();
            else if (CasterAgent != null) FireMissile();
        }

        private void FireMissile()
        {
            var arrowItem = MBObjectManager.Instance.GetObject<ItemObject>(Ability.StringID);

            _missileId = CasterAgent.Index;

            if (Mission.Current.Missiles.FirstOrDefault(x => x.Index == _missileId) != null)
            {
                RemoveProjectile();
            }

            var speed = Ability.Template.BaseMovementSpeed;
            var projectile = new MissionWeapon(arrowItem, null, null);

            Mission.Current.AddCustomMissile(CasterAgent, projectile,
                CasterAgent.GetEyeGlobalPosition(), CasterAgent.LookDirection,
                orientation: Mat3.CreateMat3WithForward(CasterAgent.LookDirection),
                speed, speed, false, null, _missileId);
        }

        private void CheckMissile()
        {
            var missile = Mission.Current.Missiles.FirstOrDefault(x => x.Index == _missileId);
            if (missile == null) Stop();
        }

        protected override MatrixFrame GetNextGlobalFrame(MatrixFrame oldFrame, float dt)
        {
            var missile = Mission.Current.Missiles.FirstOrDefault(x => x.Index == _missileId);
            if (missile != null)
            {
                MatrixFrame newFrame = oldFrame;
                newFrame.origin = missile.GetPosition();
                return newFrame;
            }
            return MatrixFrame.Identity;
        }

        protected override void OnBeforeRemoved(int removeReason)
        {
            RemoveProjectile();
        }

        private void RemoveProjectile()
        {
            var missile = Mission.Current.Missiles.FirstOrDefault(x => x.Index == _missileId);
            if (missile != null)
            {
                Mission.Current.RemoveMissileAsClient(_missileId);
                missile.Entity.Remove(0);
            }
        }
    }
}