using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORPrisonerRecruitmentCalculationModel : DefaultPrisonerRecruitmentCalculationModel
    {
        /// <summary>
        /// Stops AI parties from recruiting cross-culture prisoners.
        /// </summary>
        /// <remarks>
        /// GetConformityChangePerHour is used to prevent conformity gain for both player and AI. This method sits as a fallback.
        /// </remarks>
        public override bool IsPrisonerRecruitable(PartyBase party, CharacterObject prisoner, out int conformityNeeded)
        {
            if (party.Culture != prisoner.Culture)
            {
                conformityNeeded = 0;
                return false;
            }

            //Undead and treespirits have 0 wage and cause division errors. Can be justified as these troops not being able to change allegiance or something.
            if (!party.MobileParty.IsMainParty)
            {
                var wageModel = Campaign.Current.Models.PartyWageModel;
                if (wageModel.GetCharacterWage(prisoner) == 0)
                {
                    conformityNeeded = 0;
                    return false;
                }
            }

            return base.IsPrisonerRecruitable(party, prisoner, out conformityNeeded);
        }

        /// <summary>
        /// Prevents cross-culture prisoner conversion.
        /// </summary>
        /// <remarks>
        /// I originally tried CalculateRecruitableNumber, but it sets the recruitable number to 0 and stops you from clicking the "recruit prisoner" button despite conformity still being generated and the UI telling the player they can recruit X number despite the button being greyed out.
        /// </remarks>
        public override ExplainedNumber GetConformityChangePerHour(PartyBase party, CharacterObject prisoner)
        {
            if (party.Culture != prisoner.Culture) return new ExplainedNumber();
            
            //Undead and treespirits have 0 wage and cause division errors during recruitment for ai parties in RecruitPrisonersCampaignBehavior. Conformity calculations cut off here to avoid wastage.
            if (!party.MobileParty.IsMainParty)
            {
                var wageModel = Campaign.Current.Models.PartyWageModel;
                if (wageModel.GetCharacterWage(prisoner) == 0) return new ExplainedNumber();
            }

            //base handles all of the modifiers for recruitment rate
            return base.GetConformityChangePerHour(party, prisoner);
        }



        public override int GetPrisonerRecruitmentMoraleEffect(
            PartyBase party,
            CharacterObject character,
            int num)
        {
            var value = 0;
            if (party.LeaderHero?.Culture.StringId == TORConstants.Cultures.GREENSKIN)
            {
                return value;
            }

            value = base.GetPrisonerRecruitmentMoraleEffect(party, character, num);

            return value;
        }

    }
}