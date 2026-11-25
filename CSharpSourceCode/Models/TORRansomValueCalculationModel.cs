using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.Models
{
    /// <summary>Ransom model override: Negotiator perk adds a bonus to hero-prisoner ransom when the seller is the party leader.</summary>
    public class TORRansomValueCalculationModel : DefaultRansomValueCalculationModel
    {
        /// <summary>Hero prisoner ransom with Negotiator perk bonus applied for party-leader sellers.</summary>
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
