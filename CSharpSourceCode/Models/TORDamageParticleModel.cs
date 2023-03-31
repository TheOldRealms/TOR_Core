using System.Linq;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORDamageParticleModel : DefaultDamageParticleModel
    {
        public override void GetMeleeAttackBloodParticles(Agent attacker, Agent victim, in Blow blow, in AttackCollisionData collisionData, out HitParticleResultData particleResultData)
        {
            if (victim.IsUndead())
            {
                particleResultData.ContinueHitParticleIndex = -1;
                particleResultData.StartHitParticleIndex = -1;
                particleResultData.EndHitParticleIndex = -1;
            }
            else
            {
                base.GetMeleeAttackBloodParticles(attacker, victim, blow, collisionData, out particleResultData);
            }
        }
        public override void GetMeleeAttackSweatParticles(Agent attacker, Agent victim, in Blow blow, in AttackCollisionData collisionData, out HitParticleResultData particleResultData)
        {
            if (victim.IsUndead())
            {
                particleResultData.ContinueHitParticleIndex = -1;
                particleResultData.StartHitParticleIndex = -1;
                particleResultData.EndHitParticleIndex = -1;
            }
            else
            {
                base.GetMeleeAttackSweatParticles(attacker, victim, blow, collisionData, out particleResultData);
            }
        }
        public override int GetMissileAttackParticle(Agent attacker, Agent victim, in Blow blow, in AttackCollisionData collisionData)
        {
            if (victim.GetComponent<StatusEffectComponent>()!=null)
            {
                if (victim.GetComponent<StatusEffectComponent>().HasTemporaryAttribute("ClearBloodBurst"))
                {
                    HideMissleIfExists(blow.WeaponRecord.AffectorWeaponSlotOrMissileIndex);
                    return -1;
                }
                   
            }
            
            if (victim.IsUndead())
            {
                return -1;
            }
            return base.GetMissileAttackParticle(attacker, victim, blow, collisionData);
        }




        private void HideMissleIfExists(int index)
        {
            var missle = Mission.Current.Missiles.FirstOrDefault(x=> x.Index==index);
            if(missle!=null)
                missle.Entity.SetAlpha(0);
        }
    }
}
