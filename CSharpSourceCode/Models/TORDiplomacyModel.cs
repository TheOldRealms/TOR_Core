using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORDiplomacyModel : DefaultDiplomacyModel
    {
        public override float GetRelationIncreaseFactor(Hero hero1, Hero hero2, float relationChange)
        {
            var baseValue =  base.GetRelationIncreaseFactor(hero1, hero2, relationChange);
            var values = new ExplainedNumber(baseValue);

            var playerHero = hero1.IsHumanPlayerCharacter || hero2.IsHumanPlayerCharacter ? (hero1.IsHumanPlayerCharacter ? hero1 : hero2) : null;
            if (playerHero == null) return baseValue;


            if (playerHero.HasAnyCareer())
            {
                var choices = playerHero.GetAllCareerChoices();

                if (choices.Contains("CourtleyPassive1"))
                {
                    if (baseValue > 0)
                    {
                        var choice = TORCareerChoices.GetChoice("CourtleyPassive1");
                        if (choice != null)
                        {
                            var value = choice.Passive.InterpretAsPercentage ? choice.Passive.EffectMagnitude / 100 : choice.Passive.EffectMagnitude;
                            values.AddFactor(value);
                        }
                    }
                }
            }
            return values.ResultNumber;
        }
    }
}