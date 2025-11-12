using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORPrisonerRecruitmentCalculationModel : DefaultPrisonerRecruitmentCalculationModel
    {
        /// <summary>
        /// Stops AI parties from recruiting cross-culture prisoners.
        /// </summary>
        /// <remarks>This only affects AI parties because it's a daily AI tick that calls the method. GetConformityChangePerHour is used to prevent the player from recruiting off-culture prisoners. It's probably possible to remove this method and use only GCCPH as that also stops the AI's prisoners from accumulating conformity, and returning 0 there will skip lots of perk checks that will be wasted if recruitment is blocked here.</remarks>
        /// <param name="party"></param>
        /// <param name="character"></param>
        /// <param name="conformityNeeded"></param>
        /// <returns></returns>
        public override bool IsPrisonerRecruitable(PartyBase party, CharacterObject character, out int conformityNeeded)
        {
            if (party.Culture != character.Culture)
            {
                conformityNeeded = 999;
                return false;
            }
            return base.IsPrisonerRecruitable(party, character, out conformityNeeded);
        }

        /// <summary>
        /// Returns 0 if a prisoner does not match the culture of the party; prevents cross-culture prisoner conversion.
        /// </summary>
        /// <remarks>I originally tried CalculateRecruitableNumber, but it sets the recruitable number to 0 and stops you from clicking the "recruit prisoner" button despite conformity still being generated and the UI telling the player they can recruit X number despite the button being greyed out.</remarks>
        /// <param name="party"></param>
        /// <param name="character"></param>
        /// <returns></returns>
        public override ExplainedNumber GetConformityChangePerHour(PartyBase party, CharacterObject character)
        {
            if (party.Culture != character.Culture) return new ExplainedNumber();
            //base handles all of the modifiers for recruitment rate
            return base.GetConformityChangePerHour(party, character);
        }



        public override int GetPrisonerRecruitmentMoraleEffect(
            PartyBase party,
            CharacterObject character,
            int num)
        {
            var value = base.GetPrisonerRecruitmentMoraleEffect(party, character, num);

            if (party.LeaderHero?.Culture.StringId == TORConstants.Cultures.GREENSKIN)
            {
                value = 0;
            }
            return value;
        }

    }
}