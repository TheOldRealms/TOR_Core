using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.CampaignMechanics.Ambush;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>Counts player-side loudness events (gunpowder shots and magic casts) and writes the tally to the campaign behavior.</summary>
    public class AmbushLoudnessTracker : MissionLogic
    {
        private int _loudnessCount;
        private AbilityManagerMissionLogic _abilityLogic;

        public override void AfterStart()
        {
            base.AfterStart();
            // Subscribe in AfterStart; submodule-registered behaviors aren't present in OnBehaviorInitialize.
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
            // Count only the player side (AttackerTeam in ambusher missions).
            if (shooterAgent.Team != Mission.AttackerTeam) return;

            var weapon = shooterAgent.Equipment?[weaponIndex];
            if (weapon == null || weapon.Value.IsEmpty) return;
            var wc = weapon.Value.CurrentUsageItem?.WeaponClass ?? WeaponClass.Undefined;
            if (IsGunpowderClass(wc)) _loudnessCount++;
        }

        private static bool IsGunpowderClass(WeaponClass wc)
        {
            // TOR Firearms uses Musket + Pistol as the gunpowder classes.
            return wc == WeaponClass.Musket
                || wc == WeaponClass.Pistol;
        }

        /// <summary>Count player-side Spell casts. Prayer and CareerAbility are excluded.</summary>
        private void OnAbilityCastComplete(Ability ability, Agent agent)
        {
            if (ability?.Template == null || agent == null) return;
            if (agent.Team != Mission.AttackerTeam) return;
            if (ability.Template.AbilityType != AbilityType.Spell) return;
            // TODO: filter by Shadow lore once implemented.
            _loudnessCount++;
        }
    }
}
