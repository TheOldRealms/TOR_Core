using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TOR_Core.CampaignMechanics.Ambush;

namespace TOR_Core.Models
{
    /// <summary>
    /// Two ambush-shaped overrides on top of vanilla's default:
    ///
    /// 1. <b>Shield the ambusher.</b> Parties that just landed a
    ///    successful ambush should NOT get the post-battle disorganize
    ///    vanilla's <c>DisorganizedStateCampaignBehavior.OnMapEventEnd</c>
    ///    would otherwise apply. Returns false for recent strikers.
    ///
    /// 2. <b>Allow sub-10 victims to be disorganized.</b> Vanilla's
    ///    default gates disorganize on <c>TotalManCount &gt;= 10</c>.
    ///    A patrol wounded down to 6 by a clean ambush should still
    ///    read as disorganized; the ambush narrative is the point,
    ///    the member-count threshold exists for generic battles where
    ///    small bands don't warrant the state. Returns true for
    ///    recent victims regardless of count (modulo
    ///    <c>IsActive</c> / <c>IsMobile</c>).
    ///
    /// Subclass + <c>AddModel</c> rather than Harmony-patching, per
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
            var mp = partyBase?.MobileParty;
            if (mp != null)
            {
                var ai = Campaign.Current?.GetCampaignBehavior<AiAmbushBehavior>();
                if (ai != null)
                {
                    if (ai.IsRecentSuccessfulAmbusher(mp, RecentAmbusherWindowMinutes))
                        return false;

                    // Victim bypass: ignore vanilla's >=10 TotalManCount
                    // gate so a wounded-down patrol still takes the
                    // debuff. Keep the IsActive/IsMobile pre-reqs so
                    // destroyed parties can't be flagged.
                    if (ai.IsRecentAmbushVictim(mp, RecentAmbusherWindowMinutes)
                        && partyBase.IsActive && partyBase.IsMobile)
                    {
                        return true;
                    }
                }
            }
            return base.CanGetDisorganized(partyBase);
        }
    }
}
