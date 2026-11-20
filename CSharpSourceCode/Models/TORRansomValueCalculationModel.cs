using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.Models
{
    /// <summary>
    /// Replaces the stripped vanilla <c>DefaultPerks.Roguery.RansomBroker</c>
    /// hook that would otherwise fire inside
    /// <see cref="DefaultRansomValueCalculationModel.PrisonerRansomValue"/>
    /// for hero prisoners sold by a party leader. TOR's
    /// <see cref="TORPerks.Roguery.Negotiator"/> primary ("25% better deals
    /// when ransoming heroes through brokers") takes over the same slot.
    /// Non-hero ransom (vanilla's stripped <c>Manhunter</c> secondary) is
    /// not covered — TOR's Negotiator secondary targets a different
    /// mechanic (hero prisoner escape chance).
    /// </summary>
    public class TORRansomValueCalculationModel : DefaultRansomValueCalculationModel
    {
        public override int PrisonerRansomValue(CharacterObject prisoner, Hero sellerHero = null)
        {
            int baseValue = base.PrisonerRansomValue(prisoner, sellerHero);
            if (prisoner == null || !prisoner.IsHero) return baseValue;
            if (sellerHero == null || !sellerHero.IsPartyLeader) return baseValue;
            if (!sellerHero.GetPerkValue(TORPerks.Roguery.Negotiator)) return baseValue;

            return (int)(baseValue * (1f + TORPerks.Roguery.Negotiator.PrimaryBonus));
        }
    }
}
