using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TOR_Core.CampaignMechanics.Shared;

namespace TOR_Core.HarmonyPatches
{
    /// <summary>
    /// Suppresses the kingdom-influence gain vanilla grants on prisoner donation
    /// when TOR's custom donation flow is active (Eonir envoy trade, Witch Hunter
    /// Lodge prisoner turn-in). The donation screen is reused as a UI vehicle;
    /// the player's rewards are applied by the caller, so vanilla's influence
    /// bonus is a leak we don't want.
    ///
    /// Prefix returns a zeroed <see cref="ExplainedNumber"/> and skips the original
    /// method when <see cref="DonationSideEffectSuppression.IsActive"/>; otherwise
    /// vanilla behavior is unchanged.
    /// </summary>
    [HarmonyPatch(typeof(DefaultPrisonerDonationModel), "CalculateInfluenceGainAfterPrisonerDonation")]
    public static class SuppressDonationInfluencePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref ExplainedNumber __result)
        {
            if (!DonationSideEffectSuppression.IsActive) return true;
            __result = new ExplainedNumber(0f);
            return false;
        }
    }
}
