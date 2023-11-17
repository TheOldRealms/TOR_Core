using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;

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

            //return firstHero.IsFemale != secondHero.IsFemale && 
            //    !firstHero.IsFactionLeader && !secondHero.IsFactionLeader &&
            //    firstHero.IsLord && secondHero.IsLord;
        }

        public override bool IsSuitableForMarriage(Hero maidenOrSuitor)
        {
            var baseDecision = base.IsSuitableForMarriage(maidenOrSuitor);

            var finalDecision = baseDecision && 
                GeneralTorMarriageRuleSet(CharacterObject.PlayerCharacter.HeroObject, maidenOrSuitor) &&
                HeroSpecificTorMarriageRuleSet(maidenOrSuitor);

            return finalDecision;

            //return CharacterObject.PlayerCharacter.IsFemale != maidenOrSuitor.IsFemale &&
            //    CharacterObject.PlayerCharacter.HeroObject.MapFaction.Id == maidenOrSuitor.MapFaction.Id &&
            //    !CharacterObject.PlayerCharacter.HeroObject.IsFactionLeader && !maidenOrSuitor.IsFactionLeader &&
            //    maidenOrSuitor.IsLord;
        }

        protected bool GeneralTorMarriageRuleSet(Hero firstHero, Hero secondHero)
        {
            // New Rules
            return true;
        }

        protected bool HeroSpecificTorMarriageRuleSet(Hero maidenOrSuitor)
        {
            // New Rules
            return true;
        }
    }
}