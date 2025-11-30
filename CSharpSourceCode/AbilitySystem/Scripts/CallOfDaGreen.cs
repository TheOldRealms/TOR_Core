using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem.Scripts
{
    /// <summary>
    /// Call uf da Green - Orc Shaman Career Ability
    ///
    /// Simplified implementation using the WindsDeathLink attribute system.
    /// CareerPerkMissionBehavior handles all WoM generation from combat events
    /// via the winds_death_link status effect applied to nearby Greenskins.
    ///
    /// Keystone Effects:
    /// - BonesAnFirepitzKeystone: CA is charged at battle start (handled elsewhere)
    /// - VisionsUvDaOrcayneKeystone: Gaze uv Mork is free and ready after CA ends
    /// - BrutalCunninKeystone: 10% extra physical resistance (via status effect mutation)
    /// - CunninBrutalityKeystone: 15% damage bonus for Greenskins (via status effect mutation)
    /// - GiftzFromDaGreatGreenKeystone: WoM multiplier scaling (via status effect mutation)
    /// - GorkAnMorkAreWatchinKeystone: WoM multiplier scaling (via status effect mutation)
    /// - PowerUvDaWaaaghKeystone: 50% physical resistance (via status effect mutation)
    /// </summary>
    public class CallOfDaGreen : CareerAbilityScript
    {
        private const string GAZE_UV_MORK_ID = "GazeUvMork";
        private const string WINDS_LINK_EFFECT = "callofdagreen_windslink";
        private const string WINDS_DEATH_LINK_EFFECT = "callofdagreen_windsdeathlink";

        private bool _initialized;

        protected override void OnAfterTick(float dt)
        {
            base.OnAfterTick(dt);

            // Apply status effects once on first tick
            if (!_initialized)
            {
                _initialized = true;
                ApplyLinkAttributeToNearbyGreenskins();
            }
        }

        /// <summary>
        /// Applies WindsLink and WindsDeathLink status effects to nearby friendly Greenskins.
        /// This is done ONCE when the ability activates, not every tick.
        /// CareerPerkMissionBehavior will then track combat events from these agents.
        /// </summary>
        private void ApplyLinkAttributeToNearbyGreenskins()
        {
            if (CasterAgent == null || !CasterAgent.IsActive()) return;

            float radius = Ability.Template.Radius;
            float duration = Ability.Template.Duration;

            // Get all agents within ability radius
            MBList<Agent> nearbyAgents = new MBList<Agent>();
            Mission.Current.GetNearbyAgents(CasterAgent.Position.AsVec2, radius, nearbyAgents);

            // Apply winds_death_link to friendly Greenskins (excluding the caster)
            foreach (var agent in nearbyAgents)
            {
                if (!agent.IsActive()) continue;
                if (agent == CasterAgent) continue;  // Exclude the player
                if (!agent.BelongsToMainParty()) continue;

                var character = agent.Character as CharacterObject;
                if (character == null) continue;

                // Check if Orc or Goblin
                if (character.IsOrc() || character.IsGoblin())
                {
                    // Apply both WindsLink (for kill bonus) and WindsDeathLink (for death penalty)
                    agent.ApplyStatusEffect(WINDS_LINK_EFFECT, CasterAgent, duration, false);
                    agent.ApplyStatusEffect(WINDS_DEATH_LINK_EFFECT, CasterAgent, duration, false);
                }
            }
        }

        /// <summary>
        /// Called when the ability is about to be removed (duration expired or cancelled).
        /// Handles VisionsUvDaOrcayneKeystone: Makes Gaze uv Mork free and ready.
        /// </summary>
        protected override void OnBeforeRemoved(int removeReason)
        {
            base.OnBeforeRemoved(removeReason);

            // VisionsUvDaOrcayneKeystone: Gaze uv Mork is free and ready after CA
            if (Hero.MainHero.HasCareerChoice("VisionsUvDaOrcayneKeystone"))
            {
                MakeGazeUvMorkFreeAndReady();
            }

            _initialized = false;
        }

        /// <summary>
        /// Makes Gaze uv Mork spell free (next cast costs 0 WoM) and resets its cooldown.
        /// Called when VisionsUvDaOrcayneKeystone is active.
        /// </summary>
        private void MakeGazeUvMorkFreeAndReady()
        {
            if (Agent.Main == null) return;

            var component = Agent.Main.GetComponent<AbilityComponent>();
            if (component == null) return;

            var abilities = component.KnownAbilitySystem;
            var gazeUvMork = abilities.FirstOrDefaultQ(a => a.StringID == GAZE_UV_MORK_ID);

            if (gazeUvMork != null)
            {
                gazeUvMork.SetCoolDown(0);
            }
        }
    }
}
