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
            var id = this._ability.StringID;
            var arrowItem = MBObjectManager.Instance.GetObject<ItemObject>(id);
            
            var projectile = new MissionWeapon(arrowItem, null, null);
            _id = agent.Index;
            var speed = this._ability.Template.BaseMovementSpeed;
            RemoveProjectile();
            
            Mission.Current.AddCustomMissile(agent, projectile,
                agent.GetEyeGlobalPosition(),agent.LookDirection,
                orientation: Mat3.CreateMat3WithForward(agent.LookDirection),
                speed,speed,false,null,_id);
            
           init = true;
           _lifeTime = 0;
        }

        protected override void OnTick(float dt)
        {
            if(!init)
                return;
            
            var frame = GameEntity.GetGlobalFrame();
            var missile = Mission.Current.Missiles.FirstOrDefault(x => x.Index == _id);
            if (missile == null)
            {
                Stop();
                OnRemoved(0);
            }
            if(_ability==null)
                return;
            
            UpdatePosition(frame,dt);
            UpdateSound(GameEntity.GlobalPosition);
            _lifeTime += dt;
            if (_lifeTime>_ability.Template.Duration)
            {
                RemoveProjectile();
            }
        }

        protected override void UpdatePosition(MatrixFrame frame, float dt)
        {
            var missile = Mission.Current.Missiles.FirstOrDefault(x => x.Index == _id);
            if (missile == null)
            {
                Stop();
                OnRemoved(0);
                return;
            }
            frame.origin= missile.GetPosition();
            GameEntity.SetGlobalFrame(frame);
        }
        
        protected override void OnRemoved(int removeReason)
        {
            init = false;
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
            
        }
    }
}