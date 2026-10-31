using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.CampaignMechanics.Ambush;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Counts player-side loudness events during the ambush — gunpowder shots
    /// (handgun / pistol / musket / grenade weapons) and non-Shadow magic
    /// casts. The tally lands on <see cref="AmbushCampaignBehavior.LastAmbushLoudness"/>
    /// at mission close; the campaign-side cooldown stamp + TotalVictory
    /// cooldown-zero gate read it to decide whether the mission was quiet
    /// enough to keep the stealth cooldown short / zero.
    ///
    /// Design: campaign-stealth-design.md § 6b.
    ///
    /// Magic hook is deferred: TOR's ability system doesn't expose a clean
    /// cast event we can subscribe to without a Harmony patch; the gunpowder
    /// side carries the first-pass mechanic. See the TODO in <see cref="OnAgentShootMissile"/>.
    /// </summary>
    public class AmbushLoudnessTracker : MissionLogic
    {
        private int _loudnessCount;

        public override void OnAgentShootMissile(Agent shooterAgent, EquipmentIndex weaponIndex,
            Vec3 position, Vec3 velocity, Mat3 orientation, bool hasRigidBody,
            int forcedMissileIndex)
        {
            base.OnAgentShootMissile(shooterAgent, weaponIndex, position, velocity,
                                     orientation, hasRigidBody, forcedMissileIndex);

            if (shooterAgent == null || !shooterAgent.IsHuman) return;
            // Only count shots from the player's side (AttackerTeam in our mission).
            if (shooterAgent.Team != Mission.AttackerTeam) return;

            var weapon = shooterAgent.Equipment?[weaponIndex];
            if (weapon == null || weapon.Value.IsEmpty) return;
            var wc = weapon.Value.CurrentUsageItem?.WeaponClass ?? WeaponClass.Undefined;
            if (IsGunpowderClass(wc)) _loudnessCount++;
        }

        private static bool IsGunpowderClass(WeaponClass wc)
        {
            // The TOR Firearms system treats Musket + Pistol as the two gunpowder
            // weapon classes (see FirearmsMissionLogic). Grenades land in vanilla's
            // Thrown family but TOR flags them separately — count them too if we
            // find them. If TOR adds a dedicated gunpowder-grenade class later,
            // add it here.
            return wc == WeaponClass.Musket
                || wc == WeaponClass.Pistol;
        }

        /// <summary>
        /// TODO: non-Shadow magic cast hook. TOR's AbilityManagerMissionLogic
        /// doesn't expose a cast event we can subscribe to from here; wiring
        /// this up needs either a new event on AbilityComponent/AbilityManager
        /// or a Harmony patch. Deferred to a follow-up pass.
        /// </summary>
        private void OnMagicCastPlaceholder() { }

        protected override void OnEndMission()
        {
            base.OnEndMission();
            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (behavior != null) behavior.LastAmbushLoudness = _loudnessCount;
        }
    }
}
