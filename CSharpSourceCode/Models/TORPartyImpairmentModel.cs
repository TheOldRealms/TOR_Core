using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TOR_Core.CampaignMechanics.Ambush;

namespace TOR_Core.Models
{
    /// <summary>
    /// Shields parties that just landed a successful ambush from the
    /// post-battle disorganize vanilla's
    /// <c>DisorganizedStateCampaignBehavior.OnMapEventEnd</c> would
    /// otherwise apply. Everything else defers to the vanilla default.
    ///
    /// Vanilla's behavior iterates the MapEvent's involved parties and
    /// calls <c>SetDisorganized(true)</c> gated on this model's
    /// <c>CanGetDisorganized</c>. Returning false here is the single
    /// clean lever — subclass + <c>AddModel</c> rather than Harmony-
    /// patching, matching the rule in
    /// <see href="feedback_bannerlord_model_override_vs_harmony"/>.
    /// </summary>
    public class TORPartyImpairmentModel : DefaultPartyImpairmentModel
    {
        // Covers the mission → menu → MapEvent-closed cascade for both
        // Extracted (player may linger on the escape menu) and
        // TotalVictory. Game-time doesn't advance during missions, so
        // minutes here are real post-mission wallclock rather than
        // elapsed combat.
        private const float RecentAmbusherWindowMinutes = 5f;

        public override bool CanGetDisorganized(PartyBase partyBase)
        {
            if (partyBase?.MobileParty != null)
            {
                var ai = Campaign.Current?.GetCampaignBehavior<AiAmbushBehavior>();
                if (ai != null && ai.IsRecentSuccessfulAmbusher(partyBase.MobileParty, RecentAmbusherWindowMinutes))
                    return false;
            }
            return base.CanGetDisorganized(partyBase);
        }
    }
}
