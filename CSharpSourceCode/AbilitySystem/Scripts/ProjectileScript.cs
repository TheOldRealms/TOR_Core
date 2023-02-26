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
        private int _id;
        private float _lifeTime;
        private bool init;

        public override void SetAgent(Agent agent)
        {
            base.SetAgent(agent);
            var arrowItem = MBObjectManager.Instance.GetObject<ItemObject>(this._ability.StringID);
            
            var projectile = new MissionWeapon(arrowItem, null, null);
            _id = agent.Index+10000+1337;
            var speed = this._ability.Template.BaseMovementSpeed;
            RemoveProjectile();
            Mission.Current.AddCustomMissile(agent, projectile,
                agent.GetEyeGlobalPosition(),agent.LookDirection,
                orientation: Mat3.CreateMat3WithForward(agent.LookDirection),
                speed,speed,false,null,_id);

           //_lifeTime = _ability.Template.Duration;
           init = true;
           _hasCollided = false;
           _lifeTime = 0;
        }

        protected override void OnTick(float dt)
        {
            base.OnTick(dt);
            
            if(!init)
                return;
            
            var missile = Mission.Current.Missiles.FirstOrDefault(x => x.Index == _id);
            if (missile != null)
            {
                //RemoveProjectile();
                Stop();
            }

            
            //if(_hasCollided) return;
            TORCommon.Say(_lifeTime.ToString());
            _lifeTime += dt;
            if (_lifeTime>_ability.Template.Duration)
            {
                RemoveProjectile();
            }


        }


        protected override void OnRemoved(int removeReason)
        {
            RemoveProjectile();
            base.OnRemoved(removeReason);
        }

        private void RemoveProjectile()
        {
            var missile = Mission.Current.Missiles.FirstOrDefault(x => x.Index == _id);
            if (missile != null)
            {
                Mission.Current.RemoveMissileAsClient(_id);
                missile.Entity.Remove(0);
                TORCommon.Say("killed");
            }

            if (init) return;
            _hasCollided = true;

        }
    }
}