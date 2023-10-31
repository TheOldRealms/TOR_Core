using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORRaidModel : DefaultRaidModel
    {
        public override float CalculateHitDamage(MapEventSide attackerSide, float settlementHitPoints)
        {
            var explainedNumber= new ExplainedNumber(base.CalculateHitDamage(attackerSide, settlementHitPoints));
            if (attackerSide.IsMainPartyAmongParties())
            {
                if (MobileParty.MainParty.LeaderHero.HasAnyCareer())
                {
                    var choices = MobileParty.MainParty.LeaderHero.GetAllCareerChoices();

                    if (choices.Contains("NightRiderPassive3")&& Campaign.Current.IsNight)
                    {
                        var choice = TORCareerChoices.GetChoice("NightRiderPassive3");
                        if (choice != null)
                        {
                            explainedNumber.AddFactor(choice.GetPassiveValue());
                        }
                    }
                }
            }

            return explainedNumber.ResultNumber;
        }
    }
}