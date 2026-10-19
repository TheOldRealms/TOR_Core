using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TOR_Core.CampaignMechanics.Shared;

namespace TOR_Core.Models
{
    /// <summary>
    /// Zeroes the vanilla kingdom-influence gain on prisoner donation when a TOR
    /// custom donation flow is active (Eonir envoy trade, Witch Hunter Lodge
    /// prisoner turn-in). The donate-to-town screen is being reused as a UI
    /// vehicle; the caller applies its own rewards, so vanilla's influence bonus
    /// would be a leak.
    ///
    /// <para>Lives as a subclass of <see cref="DefaultPrisonerDonationModel"/>
    /// rather than a Harmony patch — standard Bannerlord mod convention, and
    /// means a vanilla signature change surfaces as a compile error on this file
    /// rather than a runtime Harmony failure during <c>PatchAll</c>.</para>
    /// </summary>
    public class TORPrisonerDonationModel : DefaultPrisonerDonationModel
    {
        public override float CalculateInfluenceGainAfterPrisonerDonation(PartyBase donatingParty, CharacterObject donatedPrisoner, Settlement donatedSettlement)
        {
            if (DonationSideEffectSuppression.IsActive) return 0f;
            return base.CalculateInfluenceGainAfterPrisonerDonation(donatingParty, donatedPrisoner, donatedSettlement);
        }
    }
}
