namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    /// <summary>
    /// POI capability: hosts a spell-trainer hero. The POI framework creates
    /// one hero per settlement using <see cref="SpellTrainerTemplateId"/>, places
    /// them at the POI's Location, and routes dialog triggers.
    /// </summary>
    public interface ISpellTrainerHost
    {
        /// <summary>Template id used to create / find this POI's trainer hero.</summary>
        string SpellTrainerTemplateId { get; }

        /// <summary>
        /// Can the player's party engage this trainer in dialog?
        /// Each POI owns the "main hero OR party-companion matches this culture"
        /// decision — avoids a central switch-on-template-id in the helper.
        ///
        /// Called when the player opens a conversation with the trainer hero.
        /// Distinct from <see cref="SettlementPOI.CheckAccess"/>, which gates
        /// walking into the POI location at all.
        /// </summary>
        bool IsPartyEligibleForTrainer();
    }
}
