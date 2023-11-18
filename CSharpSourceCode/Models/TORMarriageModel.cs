using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORMarriageModel : DefaultMarriageModel
    {
        public override bool IsCoupleSuitableForMarriage(Hero firstHero, Hero secondHero)
        {
            var baseDecision = base.IsCoupleSuitableForMarriage(firstHero, secondHero);

            var finalDecision = baseDecision && 
                GeneralTorMarriageRuleSet(firstHero, secondHero);

            return finalDecision;
        }

        public override bool IsSuitableForMarriage(Hero maidenOrSuitor)
        {
            var baseDecision = base.IsSuitableForMarriage(maidenOrSuitor);

            var finalDecision = baseDecision && 
                GeneralTorMarriageRuleSet(CharacterObject.PlayerCharacter.HeroObject, maidenOrSuitor) &&
                HeroSpecificTorMarriageRuleSet(maidenOrSuitor);

            return finalDecision;
        }

        protected bool GeneralTorMarriageRuleSet(Hero firstHero, Hero secondHero)
        {
            // Culture must be compatible for marriage
            if (!firstHero.Culture.IsSuitableForMarriage(secondHero.Culture))
                return false;

            return true;
        }

        protected bool HeroSpecificTorMarriageRuleSet(Hero maidenOrSuitor)
        {
            // No new rules yet
            return true;
        }
    }
}