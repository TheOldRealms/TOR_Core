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
        public const string WitchHunterLodgeLocationId = "tor_witchhunter_lodge";

        /// <summary>Settlements where <c>tor_wizardhall</c> is a real POI (has a scene override in <c>tor_settlements.xml</c>).</summary>
        private static readonly HashSet<string> _wizardHallSettlements = new()
        {
            "town_RL1",  // Altdorf
            "town_WI1",  // Nuln
            "town_ML1",  // Middenheim
        };

        /// <summary>
        /// Settlements where <c>tor_witchhunter_lodge</c> is a real POI. These are the Empire
        /// towns TOR already associates with the witch hunter chapterhouse scene via house_2.
        /// </summary>
        private static readonly HashSet<string> _witchHunterLodgeSettlements = new()
        {
            "town_ST1",  // Wurtbad
            "town_ST2",  // Leicheberg
            "town_WI4",  // Meissen
            "town_RL4",  // Bögenhafen
            "town_OM1",  // Bechafen
            "town_OM2",  // Eisental
            "town_OL3",  // Ferlangen
        };

        public static bool IsWizardHallSettlement(Settlement settlement) =>
            settlement != null && _wizardHallSettlements.Contains(settlement.StringId);

        public static bool IsWitchHunterLodgeSettlement(Settlement settlement) =>
            settlement != null && _witchHunterLodgeSettlements.Contains(settlement.StringId);

        /// <summary>
        /// Returns the Location StringId where in-town trainer behaviors should place their
        /// trainer hero in this settlement. <c>tor_wizardhall</c> for Imperial college
        /// towns; <c>house_1</c> fallback for everything else (vanilla pre-POI pattern).
        ///
        /// <para>Note: Witch Hunter Lodge towns do not override this — the lodge is a scene
        /// without an NPC trainer, so no trainer-spawn behavior targets it.</para>
        /// </summary>
        public static string GetTrainerLocationId(Settlement settlement)
        {
            if (IsWizardHallSettlement(settlement)) return WizardHallLocationId;
            return "house_1";
        }
    }
}
