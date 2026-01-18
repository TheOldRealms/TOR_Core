using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORTradeAgreementModel : DefaultTradeAgreementModel
    {
        // Scoring constants
        private const float ReligionCompatibilityWeight = 15f;
        private const float PantheonCompatibilityWeight = 20f;
        private const float CultureCompatibilityWeight = 15f;
        private const float SameReligionBonus = 25f;
        private const float SameCultureBonus = 15f;
        private const float EonirTradeBonus = 10f;

        // Lore restriction texts
        private static readonly TextObject _chaosCannotTradeText = new TextObject("{=TOR_Trade_Chaos}The forces of Chaos do not engage in trade.");
        private static readonly TextObject _greenskinCannotTradeText = new TextObject("{=TOR_Trade_Greenskin}Greenskins do not understand the concept of trade.");

        // Scoring explanation texts
        private static readonly TextObject _sameCultureText = new TextObject("{=TOR_Trade_SameCulture}Same Culture");
        private static readonly TextObject _cultureCompatibilityText = new TextObject("{=TOR_Trade_CultureCompat}Cultural Compatibility");
        private static readonly TextObject _sameReligionText = new TextObject("{=TOR_Trade_SameReligion}Same Religion");
        private static readonly TextObject _pantheonCompatibilityText = new TextObject("{=TOR_Trade_PantheonCompat}Pantheon Compatibility");
        private static readonly TextObject _religionCompatibilityText = new TextObject("{=TOR_Trade_ReligionCompat}Religious Compatibility");
        private static readonly TextObject _eonirTradeText = new TextObject("{=TOR_Trade_Eonir}Eonir Trade Affinity");

        public override int GetMaximumTradeAgreementCount(Kingdom kingdom) => 3;

        public override bool CanMakeTradeAgreement(
            Kingdom kingdom,
            Kingdom other,
            bool checkOtherSideTradeSupport,
            out TextObject reason,
            bool includeReason = false)
        {
            reason = includeReason ? TextObject.GetEmpty() : null;

            // Check lore restrictions first - chaos check . maybe later more
            if (!CanKingdomTrade(kingdom, out reason, includeReason))
                return false;

            if (!CanKingdomTrade(other, out reason, includeReason))
                return false;

            // Call base game validation (war status, max agreements, etc.)
            return base.CanMakeTradeAgreement(kingdom, other, checkOtherSideTradeSupport, out reason, includeReason);
        }

        public override float GetScoreOfStartingTradeAgreement(
            Kingdom kingdom,
            Kingdom targetKingdom,
            Clan clan,
            out TextObject explanation,
            bool includeExplanation = false)
        {
            // Get base game score
            float baseScore = base.GetScoreOfStartingTradeAgreement(
                kingdom, targetKingdom, clan, out explanation, includeExplanation);

            // If base game already rejected (score 0), don't bother with our modifiers
            if (baseScore <= 0)
                return 0f;

            var score = new ExplainedNumber(baseScore, includeExplanation);

            // Add modifiers
            AddCultureModifier(ref score, kingdom, targetKingdom);
            AddReligionModifier(ref score, kingdom, targetKingdom);
            AddFactionModifier(ref score, kingdom);

            // Clamp to valid range
            return MBMath.ClampFloat(score.ResultNumber, 0f, 100f);
        }

        /// <summary>
        /// Checks if a kingdom is allowed to engage in trade based on lore.
        /// </summary>
        private bool CanKingdomTrade(Kingdom kingdom, out TextObject reason, bool includeReason)
        {
            reason = includeReason ? TextObject.GetEmpty() : null;

            var pantheon = GetKingdomPantheon(kingdom);

            switch (pantheon)
            {
                case Pantheon.Chaos:
                    reason = _chaosCannotTradeText;
                    return false;
            }

            return true;
        }
        

        /// <summary>
        /// Adds culture-based modifiers to the trade score.
        /// </summary>
        private void AddCultureModifier(ref ExplainedNumber score, Kingdom kingdom, Kingdom targetKingdom)
        {
            var culture1 = kingdom.Culture?.StringId;
            var culture2 = targetKingdom.Culture?.StringId;

            if (string.IsNullOrEmpty(culture1) || string.IsNullOrEmpty(culture2))
                return;

            if (culture1 == culture2)
            {
                score.Add(SameCultureBonus, _sameCultureText);
                return;
            }

            float cultureCompat = ReligionObjectHelper.CalculateCultureCompatibility(culture1, culture2);
            if (cultureCompat != 0f)
            {
                score.Add(cultureCompat * CultureCompatibilityWeight, _cultureCompatibilityText);
            }
        }

        /// <summary>
        /// Adds religion and pantheon modifiers to the trade score.
        /// </summary>
        private void AddReligionModifier(ref ExplainedNumber score, Kingdom kingdom, Kingdom targetKingdom)
        {
            var religion1 = kingdom.Leader?.GetDominantReligion();
            var religion2 = targetKingdom.Leader?.GetDominantReligion();

            if (religion1 == null || religion2 == null)
                return;

            if (religion1 == religion2)
            {
                score.Add(SameReligionBonus, _sameReligionText);
                return;
            }

            float pantheonCompat = ReligionObjectHelper.GetPantheonCompatibility(religion1.Pantheon, religion2.Pantheon);
            if (pantheonCompat != 0f)
            {
                score.Add(pantheonCompat * PantheonCompatibilityWeight, _pantheonCompatibilityText);
            }

            float religionCompat = ReligionObjectHelper.CalculateReligionCompatibility(religion1, religion2);
            if (religionCompat != 0f)
            {
                score.Add(religionCompat * ReligionCompatibilityWeight, _religionCompatibilityText);
            }
        }

        /// <summary>
        /// Adds faction-specific modifiers to the trade score.
        /// </summary>
        private void AddFactionModifier(ref ExplainedNumber score, Kingdom kingdom)
        {
            if (kingdom.Culture?.StringId == TORConstants.Cultures.EONIR)
            {
                score.Add(EonirTradeBonus, _eonirTradeText);
            }
        }
        
        private Pantheon GetKingdomPantheon(Kingdom kingdom)
        {
            // First try to get from leader's religion
            var leaderReligion = kingdom.Leader?.GetDominantReligion();
            if (leaderReligion != null)
                return leaderReligion.Pantheon;

            // Fallback to culture-based pantheon
            return ReligionObjectHelper.GetPantheon(kingdom.Culture?.StringId);
        }
    }
}