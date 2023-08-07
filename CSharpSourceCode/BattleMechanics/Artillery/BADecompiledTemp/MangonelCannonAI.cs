using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;


namespace TOR_Core.BattleMechanics.Artillery.BA
{
    internal sealed class MangonelCannonAI : RangedSiegeWeaponAi
    {
        private SoundEvent _fireSound;
        private bool _isParticlesAllowed = true;

        public MangonelCannonAI(MangonelCannon mangonel)
            : base((RangedSiegeWeapon) mangonel)
        {
        }

        protected override void OnTick(
            Agent agentToCompareTo,
            Formation formationToCompareTo,
            Team potentialUsersTeam,
            float dt)
        {
            base.OnTick(agentToCompareTo, formationToCompareTo, potentialUsersTeam, dt);
            RangedSiegeWeapon usableMachine = this.UsableMachine as RangedSiegeWeapon;
            if (usableMachine.PilotAgent != null)
                usableMachine.PilotAgent.Health = 10000f;
            if (usableMachine.State == RangedSiegeWeapon.WeaponState.Reloading)
                this._isParticlesAllowed = true;
            if (usableMachine.State != RangedSiegeWeapon.WeaponState.Shooting || !this._isParticlesAllowed)
                return;
            MatrixFrame frame = usableMachine.PilotAgent.Frame;
            frame.Advance(8f);
            frame.Elevate(6f);
            this.PlayFireProjectileEffects(usableMachine.PilotAgent.Position, usableMachine.PilotAgent.Frame);
            this.PlayFireProjectileEffects(usableMachine.PilotAgent.Position, frame);
            this._isParticlesAllowed = false;
        }

        private void PlayFireProjectileEffects(Vec3 projectilePos, MatrixFrame projFrame)
        {
            Mission.Current.AddParticleSystemBurstByName("cla_explosion", projFrame, false);
            int eventIdFromString = SoundEvent.GetEventIdFromString("cla_cannon_1");
            if (this._fireSound == null || !this._fireSound.IsValid)
                this._fireSound = SoundEvent.CreateEvent(eventIdFromString, Mission.Current.Scene);
            this._fireSound.SetPosition(projectilePos);
            this._fireSound.Play();
        }
    }
}
