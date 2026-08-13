using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation.Persuasion;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Library;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.Models
{
    public class TORPersuasionModel : DefaultPersuasionModel
    {
        public override void GetChances(PersuasionOptionArgs optionArgs, out float successChance, out float critSuccessChance, out float critFailChance, out float failChance, float difficultyMultiplier)
        {
            float _successChance;
            float _critSuccessChance;
            float _critFailChance;
            float _failChance;
            ExplainedNumber bonusChance = new ExplainedNumber(1f, false, null);
            base.GetChances(optionArgs, out _successChance, out _critSuccessChance, out _critFailChance, out _failChance, difficultyMultiplier);
            if (CharacterObject.PlayerCharacter.GetPerkValue(TORPerks.Spellcraft.Improvision))
            {
                PerkHelper.AddPerkBonusForCharacter(TORPerks.Spellcraft.Improvision, CharacterObject.PlayerCharacter, false, ref bonusChance);
            }
            successChance = MathF.Clamp(_successChance * bonusChance.ResultNumber, 0f, 1f);
            critFailChance = _critFailChance;
            critSuccessChance = _critSuccessChance;
            if (successChance > 0)
            {
                critSuccessChance = successChance;
                successChance = 0;
            }
            failChance = 1f - critSuccessChance - successChance - critFailChance;
        }
    }
}