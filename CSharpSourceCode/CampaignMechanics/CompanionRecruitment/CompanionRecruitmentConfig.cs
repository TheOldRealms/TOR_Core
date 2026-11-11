using System;
using System.Collections.Generic;

namespace TOR_Core.CampaignMechanics.CompanionRecruitment
{
    /// <summary>
    /// One recruitable "slot" at a host NPC. A slot holds at most one current
    /// offer picked randomly from <see cref="CandidateTemplateIds"/>; recruiting
    /// clears it and a replacement rolls after <see cref="RefreshDays"/>. A
    /// non-recruited offer that has been sitting unused for <see cref="RefreshDays"/>
    /// is also re-rolled, so the pool stays visibly dynamic over long campaigns.
    ///
    /// <para>Pattern mirrors how town/tavern stocks rotate — one dialog line per
    /// config, randomised content.</para>
    /// </summary>
    public readonly struct CompanionRecruitmentConfig
    {
        /// <summary>Candidate hero templates the slot rolls from. Must contain
        /// at least one id.</summary>
        public readonly IReadOnlyList<string> CandidateTemplateIds;

        /// <summary>Hero template id of the NPC offering this slot. Used to
        /// scope the dialog line to the right partner at hubs shared by
        /// multiple NPCs.</summary>
        public readonly string HostTemplateId;

        /// <summary>Unique suffix appended to all dialog-line ids + used as the
        /// save key for this slot's state. Convention: short PascalCase,
        /// e.g. <c>WitchHunterChapterhouseHunter</c>.</summary>
        public readonly string DialogSuffix;

        /// <summary>Dialog state the "ask for a recruit" player line attaches
        /// to. Must be a state already registered by the host before the
        /// behavior's session-launched discovery pass fires.</summary>
        public readonly string HubState;

        /// <summary>State to route back to after the offer or refill
        /// reply. Usually the same as <see cref="HubState"/>.</summary>
        public readonly string ReintroState;

        /// <summary>Days before the slot refreshes. Applied symmetrically:
        /// an empty slot waits this long after the last recruit to repopulate,
        /// and a lingering unrecruited offer re-rolls after the same interval.
        /// Default 60.</summary>
        public readonly int RefreshDays;

        /// <summary>Optional attribute ids on <c>Hero.MainHero</c> required to
        /// see the player line (e.g. <c>INQUISITION_KNOWN</c>). Empty = no
        /// gate.</summary>
        public readonly IReadOnlyList<string> GateAttributes;

        /// <summary>Gold cost per recruit. Zero = free.</summary>
        public readonly int GoldCost;

        /// <summary>Hero-main custom-resource cost per recruit (uses the
        /// player's culture-specific custom resource via
        /// <c>HeroExtensions.GetCultureSpecificCustomResource</c>). Zero = free.</summary>
        public readonly int CustomResourceCost;

        public CompanionRecruitmentConfig(
            IReadOnlyList<string> candidateTemplateIds,
            string hostTemplateId,
            string dialogSuffix,
            string hubState,
            string reintroState,
            int refreshDays = 60,
            IReadOnlyList<string> gateAttributes = null,
            int goldCost = 0,
            int customResourceCost = 0)
        {
            CandidateTemplateIds = candidateTemplateIds ?? Array.Empty<string>();
            HostTemplateId       = hostTemplateId;
            DialogSuffix         = dialogSuffix;
            HubState             = hubState;
            ReintroState         = reintroState;
            RefreshDays          = refreshDays;
            GateAttributes       = gateAttributes ?? Array.Empty<string>();
            GoldCost             = goldCost;
            CustomResourceCost   = customResourceCost;
        }
    }
}
