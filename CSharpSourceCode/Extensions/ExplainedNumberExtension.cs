using TaleWorlds.CampaignSystem;
using TOR_Core.CharacterDevelopment.CareerSystem;

namespace TOR_Core.Extensions
{
    public static class ExplainedNumberExtension
    {




        public static void ApplyPassiveCareerPerk(ref  ExplainedNumber explainedNumber, CareerChoiceObject choice, bool ShowDescription=false)
        {
            if(choice==null) return;
            if(choice.Passive==null) return;

            if (choice.Passive.InterpretAsPercentage)
            {
                if(ShowDescription)
                    explainedNumber.AddFactor(choice.GetPassiveValue(), choice.BelongsToGroup.Name);
                else
                {
                    explainedNumber.AddFactor(choice.GetPassiveValue());
                }
            }
            else
            {
                if(ShowDescription)
                    explainedNumber.Add(choice.GetPassiveValue(),choice.BelongsToGroup.Name);
                else
                {
                    explainedNumber.Add(choice.GetPassiveValue());
                }
            }
        }
        
    }
}