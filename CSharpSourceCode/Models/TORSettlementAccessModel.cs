using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.TownPOIs;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    /// <summary>
    /// Answers <c>CanMainHeroAccessLocation</c> for TOR-specific POIs (location ids starting
    /// with <c>tor_</c>). Vanilla ids delegate to the base model unchanged.
    ///
    /// <para>Access-check conventions (full rationale in
    /// <c>Dokumente/TORTasks/town-pois-and-access-model/architecture.md</c>):</para>
    /// <list type="number">
    ///   <item>Defer to vanilla access first — never re-implement crime/disguise/relation logic.</item>
    ///   <item>One method per POI, no fat switch (switch dispatches by one line each).</item>
    ///   <item>Predicates ordered cheapest-and-broadest first:
    ///         discovery → siege → settlement eligibility → race → culture → career → …</item>
    ///   <item>One reason per denial, highest-priority wins.</item>
    ///   <item>Denial text is <see cref="TextObject"/> with variables (localizable).</item>
    ///   <item>Fail-closed on undefined TOR-prefixed POIs.</item>
    ///   <item>Siege is centralized at the top of this method, not per-POI.</item>
    ///   <item>Stateless — reads live hero / settlement / world state each call.</item>
    /// </list>
    /// </summary>
    public class TORSettlementAccessModel : DefaultSettlementAccessModel
    {
        public override bool CanMainHeroAccessLocation(Settlement settlement, string locationId,
                                                       out bool disableOption, out TextObject disabledText)
        {
            // Rule 1: non-TOR ids delegate to vanilla.
            if (locationId == null || !locationId.StartsWith("tor_"))
                return base.CanMainHeroAccessLocation(settlement, locationId, out disableOption, out disabledText);

            // Rule 7: siege is centralized for every TOR POI, not re-checked in each method.
            if (settlement != null && settlement.IsUnderSiege)
                return Access.Deny(out disableOption, out disabledText, Reasons.SiegeSealed);

            // Rule 2: dispatch one line each.
            switch (locationId)
            {
                case "tor_wizardhall":
                    return CanAccessWizardHall(settlement, out disableOption, out disabledText);

                // Rule 6: unknown tor_ id → fail-closed with generic reason.
                default:
                    return Access.Deny(out disableOption, out disabledText, Reasons.Sealed);
            }
        }

        private bool CanAccessWizardHall(Settlement s, out bool disableOption, out TextObject disabledText)
        {
            var h = Hero.MainHero;

            // Discovery hook — stubbed to true today (see IsDiscovered). Hide-eligible reason.
            if (!IsDiscovered(s, "tor_wizardhall"))
                return Access.Deny(out disableOption, out disabledText, Reasons.Undiscovered);

            // Settlement whitelist. Defensive: the menu option should only register on eligible
            // towns, so this branch is a fail-safe for callers that query the model directly.
            if (!HallLocations.IsWizardHallSettlement(s))
                return Access.Deny(out disableOption, out disabledText, Reasons.NotAHallHere);

            // Race lock — humans only.
            if (h.CharacterObject.Race != FaceGen.GetRaceOrDefault("human"))
                return Access.Deny(out disableOption, out disabledText, Reasons.WardsRejectRace);

            // Career lock — Imperial Magister only.
            if (!h.HasCareer(TORCareers.ImperialMagister))
                return Access.Deny(out disableOption, out disabledText,
                    Reasons.MustHaveCareer(TORCareers.ImperialMagister.Name));

            return Access.Allow(out disableOption, out disabledText);
        }

        /// <summary>
        /// Discovery hook. Returns <c>true</c> today — placeholder for the future discovery
        /// feature. When that lands, override to consult a POI discovery tracker keyed on
        /// <c>(settlement.StringId, locationId)</c>. Callers of
        /// <see cref="CanMainHeroAccessLocation"/> and the per-POI check methods need no changes.
        /// </summary>
        protected virtual bool IsDiscovered(Settlement settlement, string locationId) => true;
    }
}
