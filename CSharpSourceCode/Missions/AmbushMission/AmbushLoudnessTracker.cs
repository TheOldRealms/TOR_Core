using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.CampaignMechanics.Ambush;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Counts player-side loudness events during the ambush — gunpowder shots
    /// (Musket / Pistol <see cref="WeaponClass"/>) and non-silent magic casts.
    /// The tally lands on <see cref="AmbushCampaignBehavior.LastAmbushLoudness"/>
    /// at mission close; the campaign-side cooldown stamp + TotalVictory
    /// cooldown-zero gate read it to decide whether the mission was quiet
    /// enough to keep the stealth cooldown short / zero.
    ///
    /// Design: campaign-stealth-design.md § 6b.
    ///
    /// Shadow-lore exception is forward-looking: the lore isn't implemented
    /// in TOR yet, so every spell counts as loud today. Once Shadow lands,
    /// filter by its <c>BelongsToLoreID</c> in <see cref="OnAbilityCastComplete"/>.
    /// </summary>
    public class AmbushLoudnessTracker : MissionLogic
    {
        private int _loudnessCount;
        private AbilityManagerMissionLogic _abilityLogic;

        public override void AfterStart()
        {
            base.AfterStart();
            // Subscribe here, not in OnBehaviorInitialize: the mission init
            // sequence runs behavior OnBehaviorInitialize BEFORE submodules'
            // OnMissionBehaviorInitialize, and TOR's SubModule adds
            // AbilityManagerMissionLogic in that submodule pass. By AfterStart
            // (after the EarlyStart loop) all behaviors from both passes are
            // registered, so GetMissionBehavior<AbilityManagerMissionLogic>
            // actually finds the instance to subscribe to.
            _abilityLogic = Mission.GetMissionBehavior<AbilityManagerMissionLogic>();
            if (_abilityLogic != null)
                _abilityLogic.OnAbilityCastComplete += OnAbilityCastComplete;
        }

        protected override void OnEndMission()
        {
            base.OnEndMission();
            if (_abilityLogic != null)
                _abilityLogic.OnAbilityCastComplete -= OnAbilityCastComplete;
            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (behavior != null) behavior.LastAmbushLoudness = _loudnessCount;
        }

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
        /// Fired by AbilityManagerMissionLogic.OnCastComplete whenever any
        /// agent successfully completes a cast. We count the player side's
        /// Spell-type casts; Prayer and CareerAbility are deliberately
        /// excluded (not visible-magic in the "sends up a flare" sense).
        /// Shadow-lore exclusion is a future filter — not yet implemented
        /// upstream so there's nothing to check today.
        /// </summary>
        private void OnAbilityCastComplete(Ability ability, Agent agent)
        {
            if (ability?.Template == null || agent == null) return;
            if (agent.Team != Mission.AttackerTeam) return;
            if (ability.Template.AbilityType != AbilityType.Spell) return;
            // TODO: once Shadow lore lands, skip when
            // ability.Template.BelongsToLoreID equals its StringId. For now
            // every Spell is loud by design-doc default.
            _loudnessCount++;
        }
    }
}
