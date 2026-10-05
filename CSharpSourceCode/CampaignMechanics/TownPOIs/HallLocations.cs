using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;

namespace TOR_Core.CampaignMechanics.TownPOIs
{
    /// <summary>
    /// Single source of truth for which settlements host which TOR hall POIs, plus the
    /// helpers that in-town trainer behaviors use to pick between a hall location id and
    /// the vanilla <c>house_1</c> fallback.
    ///
    /// <para>Consumers:</para>
    /// <list type="bullet">
    ///   <item><see cref="TOR_Core.Models.TORSettlementAccessModel"/> — reads the wizard-hall
    ///         whitelist to answer <c>CanMainHeroAccessLocation("tor_wizardhall", ...)</c>.</item>
    ///   <item><see cref="TOR_Core.CampaignMechanics.Crafting.EnchanterTownBehavior"/> and
    ///         <see cref="TOR_Core.CampaignMechanics.SpellTrainers.SpellTrainerInTownBehavior"/>
    ///         — call <see cref="GetTrainerLocationId"/> to decide where to place the
    ///         Imperial Magister trainer per settlement.</item>
    /// </list>
    ///
    /// <para>Adding new hall POIs (Runehall, Engineer Hall, …): add a new
    /// <c>_xxxHallSettlements</c> HashSet + the <c>GetTrainerLocationId</c> branch that
    /// matches it. The model and behavior changes follow the Wizard Hall pattern.</para>
    /// </summary>
    public static class HallLocations
    {
        public const string WizardHallLocationId = "tor_wizardhall";

        /// <summary>Settlements where <c>tor_wizardhall</c> is a real POI (has a scene override in <c>tor_settlements.xml</c>).</summary>
        private static readonly HashSet<string> _wizardHallSettlements = new()
        {
            "town_RL1",  // Altdorf
            "town_WI1",  // Nuln
            "town_ML1",  // Middenheim
        };

        public static bool IsWizardHallSettlement(Settlement settlement) =>
            settlement != null && _wizardHallSettlements.Contains(settlement.StringId);

        /// <summary>
        /// Returns the Location StringId where in-town trainer behaviors should place their
        /// trainer hero in this settlement. <c>tor_wizardhall</c> for Imperial college
        /// towns; <c>house_1</c> fallback for everything else (vanilla pre-POI pattern).
        /// </summary>
        public static string GetTrainerLocationId(Settlement settlement)
        {
            if (IsWizardHallSettlement(settlement)) return WizardHallLocationId;
            return "house_1";
        }
    }
}
