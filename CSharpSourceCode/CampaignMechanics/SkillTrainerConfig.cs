using System;
using System.Collections.Generic;

namespace TOR_Core.CampaignMechanics
{
    /// <summary>
    /// One trainer NPC's registration. Each config = one hero template teaching
    /// one skill, plugged into one dialog hub. Hosts (POIs, bespoke dialog hubs)
    /// yield these to <see cref="SkillTrainerBehavior"/> which then registers
    /// the common training dialog lines against <see cref="HubState"/>.
    /// </summary>
    public readonly struct SkillTrainerConfig
    {
        /// <summary>Hero template id of the trainer NPC (e.g. <c>tor_bountymaster_empire_0</c>).</summary>
        public readonly string TemplateId;

        /// <summary>Suffix appended to dialog-line ids to keep them unique when
        /// multiple trainers share a hub. Convention: short PascalCase name
        /// like <c>InquisitorCrossbow</c>.</summary>
        public readonly string DialogSuffix;

        /// <summary>StringId of the skill this trainer teaches (vanilla or TOR).</summary>
        public readonly string SkillId;

        /// <summary>Dialog state the training player-line attaches to (e.g. the
        /// lodge hub). Must be a state already registered by the host before
        /// SkillTrainerBehavior's session-launched pass fires.</summary>
        public readonly string HubState;

        /// <summary>Dialog state the training sub-hub's "quit" line routes back to.
        /// Usually the same as <see cref="HubState"/>.</summary>
        public readonly string ReintroState;

        /// <summary>Companion-eligibility restrictions passed through to
        /// <c>HeroMatchesTrainingRestrictions</c>. Empty = any companion.</summary>
        public readonly IReadOnlyList<string> Restrictions;

        public SkillTrainerConfig(
            string templateId,
            string dialogSuffix,
            string skillId,
            string hubState,
            string reintroState,
            IReadOnlyList<string> restrictions = null)
        {
            TemplateId = templateId;
            DialogSuffix = dialogSuffix;
            SkillId = skillId;
            HubState = hubState;
            ReintroState = reintroState;
            Restrictions = restrictions ?? Array.Empty<string>();
        }
    }
}
