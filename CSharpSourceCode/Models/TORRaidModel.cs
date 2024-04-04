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

                    float daytime = CampaignTime.Hours(Campaign.CurrentTime).CurrentHourInDay;
                    var isNight = daytime > 18 || daytime < 6;
                    if (choices.Contains("NightRiderPassive3")&& isNight)
                    {
                        var choice = TORCareerChoices.GetChoice("NightRiderPassive3");
                        if (choice != null)
                        {
                            explainedNumber.AddFactor(choice.GetPassiveValue());
                        }
                    }
                }
            }
            
            if (attackerSide.IsMainPartyAmongParties())
            {
                if (MobileParty.MainParty.LeaderHero.HasAnyCareer())
                {
                    var choices = MobileParty.MainParty.LeaderHero.GetAllCareerChoices();
                    
                    if (choices.Contains("RobberKnightPassive4"))
                    {
                        var choice = TORCareerChoices.GetChoice("RobberKnightPassive4");
                        if (choice != null)
                        {
                            explainedNumber.AddFactor(-choice.GetPassiveValue());
                        }
                        
                        Hero.MainHero.AddCustomResource("DarkEnergy",explainedNumber.ResultNumber*250);
                    }
                }
            }

            return explainedNumber.ResultNumber;
        }
    }
}