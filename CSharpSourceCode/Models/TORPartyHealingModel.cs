using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORPartyHealingModel : DefaultPartyHealingModel
    {
        public override float GetSurvivalChance(PartyBase party, CharacterObject character, DamageTypes damageType, PartyBase enemyParty = null)
        {
            var result = base.GetSurvivalChance(party, character, damageType, enemyParty);
          
            if (result < 0.5f && party != null && party.LeaderHero != null && party.LeaderHero.GetPerkValue(TORPerks.Faith.Revival)) result = TORPerks.Faith.Revival.PrimaryBonus;
            if (character.IsUndead()) result = 0;
            return result;
        }

        public override ExplainedNumber GetDailyHealingForRegulars(MobileParty party, bool includeDescriptions = false)
        {
            var result = base.GetDailyHealingForRegulars(party, includeDescriptions);
            
            if (party == MobileParty.MainParty) AddCareerPassivesForTroopRegeneration(party, ref result);


            if (party.HasBlessing("cult_of_sigmar")) result.AddFactor(0.2f, GameTexts.FindText("tor_religion_blessing_name", "cult_of_sigmar"));
            if (party.IsAffectedByCurse())
            {
                result = new ExplainedNumber(0, true, new TextObject("{=!}Inside a cursed region"));
            }
            return result;
        }

        public override ExplainedNumber GetDailyHealingHpForHeroes(MobileParty party, bool includeDescriptions = false)
        {
            var result = base.GetDailyHealingHpForHeroes(party, includeDescriptions);
            if (party == MobileParty.MainParty) AddCareerPassivesForHeroRegeneration(party, ref result);
            if (party.HasBlessing("cult_of_sigmar")) result.AddFactor(0.2f, GameTexts.FindText("tor_religion_blessing_name", "cult_of_sigmar"));
            if (party.IsAffectedByCurse())
            {
                result = new ExplainedNumber(0, true, new TextObject("{=!}Inside a cursed region"));
            }
            return result;
        }



        private void AddCareerPassivesForTroopRegeneration(MobileParty party, ref ExplainedNumber explainedNumber)
        {
            if (party.LeaderHero.HasAnyCareer())
            {
                CareerHelper.ApplyCareerPassives(party.LeaderHero, ref explainedNumber, PassiveEffectType.TroopRegeneration);
            }
        }
        
        private void AddCareerPassivesForHeroRegeneration(MobileParty party, ref ExplainedNumber explainedNumber)
        {
            if (party.LeaderHero.HasAnyCareer())
            {
                CareerHelper.ApplyCareerPassives(party.LeaderHero, ref explainedNumber, PassiveEffectType.HealthRegeneration);
            }
        }
    }
}
