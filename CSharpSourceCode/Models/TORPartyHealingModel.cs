using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORPartyHealingModel : DefaultPartyHealingModel
    {
        public override float GetSurvivalChance(PartyBase party, CharacterObject character, DamageTypes damageType, PartyBase enemyParty = null)
        {
            return character.IsUndead() ? 0f : base.GetSurvivalChance(party, character, damageType, enemyParty);
        }

        public override ExplainedNumber GetDailyHealingForRegulars(MobileParty party, bool includeDescriptions = false)
        {
            var result = base.GetDailyHealingForRegulars(party, includeDescriptions);
            if (party.HasBlessing("cult_of_sigmar")) result.AddFactor(0.2f, new TextObject("Blessing of Sigmar"));
            return result;
        }

        public override ExplainedNumber GetDailyHealingHpForHeroes(MobileParty party, bool includeDescriptions = false)
        {
            var result = base.GetDailyHealingHpForHeroes(party, includeDescriptions);
            if (party.HasBlessing("cult_of_sigmar")) result.AddFactor(0.2f, new TextObject("Blessing of Sigmar"));
            return result;
        }
    }
}
