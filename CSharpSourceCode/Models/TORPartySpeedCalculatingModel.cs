using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORPartySpeedCalculatingModel : DefaultPartySpeedCalculatingModel
    {
        public override ExplainedNumber CalculateFinalSpeed(MobileParty mobileParty, ExplainedNumber finalSpeed)
        {
            var result = base.CalculateFinalSpeed(mobileParty, finalSpeed);
            if(mobileParty != null && mobileParty.Party.Culture.StringId == "khuzait")
            {
                result.Add(0.5f, new TextObject("Vampire bonus"));
                if (Campaign.Current.IsNight)
                {
                    result.Add(0.25f, new TextObject("Vampire nighttime bonus"));
                }
            }
            if (mobileParty.HasBlessing("cult_of_taal"))
            {
                result.AddFactor(0.2f, GameTexts.FindText("tor_religion_blessing_name", "cult_of_taal"));
            }
            return result;
        }
    }
}
