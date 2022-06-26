using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace TOR_Core.Models
{
    public class TORPrisonerRecruitmentCalculationModel: DefaultPrisonerRecruitmentCalculationModel
    {
        public override bool IsPrisonerRecruitable(PartyBase party, CharacterObject character, out int conformityNeeded)
        {
            if (party.Culture != character.Culture)
            {
                conformityNeeded = 0;
                return false;
            }
            return base.IsPrisonerRecruitable(party, character, out conformityNeeded);
        }
    }
}
