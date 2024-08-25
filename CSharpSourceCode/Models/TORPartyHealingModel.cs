using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    /**
     * ONLY applies for campaign map related events!
     */
    public class TORPartyHealingModel : DefaultPartyHealingModel
    {
        public override float GetSurvivalChance(PartyBase party, CharacterObject character, DamageTypes damageType, bool canDamageKillEvenIfBlunt, PartyBase enemyParty = null)
        {
            var result = base.GetSurvivalChance(party, character, damageType, canDamageKillEvenIfBlunt, enemyParty);
            
            if (result < 0.5f && party != null && party.LeaderHero != null && party.LeaderHero.GetPerkValue(TORPerks.Faith.Revival)) result = TORPerks.Faith.Revival.PrimaryBonus;
            if (!character.IsUndead()) 
                return result;   
            //undead "survival chance"
            if (character.IsHero)
            {
                return result;
            }
            if (character.Tier < 4)
            { 
                return 0;
            }

            if (party != null && party.LeaderHero != null && party.LeaderHero == Hero.MainHero && party.LeaderHero.HasAnyCareer())
            {
                var choices = party.LeaderHero.GetAllCareerChoices();
                if(choices.Contains("MasterOfDeadPassive4"))
                {
                    var choice = TORCareerChoices.GetChoice("MasterOfDeadPassive4");
                    if (choice != null)
                        return result + choice.GetPassiveValue();
                }
                if(choices.Contains( "CodexMortificaPassive4"))
                {
                    var choice = TORCareerChoices.GetChoice("CodexMortificaPassive4");
                    if (choice != null)
                        return result + choice.GetPassiveValue();
                }
                
                if(choices.Contains( "WellspringOfDharPassive2"))
                {
                    var choice = TORCareerChoices.GetChoice("WellspringOfDharPassive2");
                    if (choice != null)
                        return result + choice.GetPassiveValue();
                }
            }

            
            return 0;
        }

        public override ExplainedNumber GetDailyHealingForRegulars(MobileParty party, bool includeDescriptions = false)
        {
            var result = base.GetDailyHealingForRegulars(party, includeDescriptions);
            
            if (party == MobileParty.MainParty) AddCareerPassivesForTroopRegeneration(party, ref result);


            if (party.HasBlessing("cult_of_sigmar")) result.AddFactor(0.2f, GameTexts.FindText("tor_religion_blessing_name", "cult_of_sigmar"));
            if (party.IsAffectedByCurse() && party.CurrentSettlement == null && party.BesiegedSettlement == null)
            {
                result = new ExplainedNumber(0, true, new TextObject("{=!}Inside a cursed region"));
            }
            
            
            if (Hero.MainHero.HasAttribute("WEWardancerSymbol"))
            {
                result.AddFactor(-0.25f);
            }
            
            return result;
        }

        public override ExplainedNumber GetDailyHealingHpForHeroes(MobileParty party, bool includeDescriptions = false)
        {
            var result = base.GetDailyHealingHpForHeroes(party, includeDescriptions);
            if (party == MobileParty.MainParty) AddCareerPassivesForHeroRegeneration(party, ref result);
            if (party.HasBlessing("cult_of_shallya")) result.AddFactor(0.2f, GameTexts.FindText("tor_religion_blessing_name", "cult_of_shallya"));
            if (party.IsAffectedByCurse())
            {
                result = new ExplainedNumber(0, true, new TextObject("{=!}Inside a cursed region"));
            }

            if (party != MobileParty.MainParty && party.LeaderHero != null && party.LeaderHero.IsVampire())
            {
                result.AddFactor(0.2f);
            }
            
            if (party.IsMainParty && party.LeaderHero!=null &&  party.LeaderHero.Culture.StringId == TORConstants.Cultures.ASRAI)
            {
                if (!Hero.MainHero.HasAttribute("WEWandererSymbol"))
                {
                    var level = Hero.MainHero.GetForestHarmonyLevel();
                    switch (level)
                    {
                        case ForestHarmonyLevel.Harmony: break;
                        case ForestHarmonyLevel.Unbound:
                            result.AddFactor(ForestHarmonyHelper.HealthRegDebuffUnBound, new TextObject(ForestHarmonyLevel.Unbound.ToString()));
                            break;
                        case ForestHarmonyLevel.Bound:
                            result.AddFactor(ForestHarmonyHelper.HealthRegDebuffBound,new TextObject(ForestHarmonyLevel.Bound.ToString()));
                            break;
                    }
                }

                if (Hero.MainHero.HasAttribute("WEWardancerSymbol"))
                {
                    result.AddFactor(0.25f);
                }
            }
            
            return result;
        }

        private void AddCareerPassivesForTroopRegeneration(MobileParty party, ref ExplainedNumber explainedNumber)
        {
            if (party.LeaderHero.HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(party.LeaderHero, ref explainedNumber, PassiveEffectType.TroopRegeneration, false);
            }
        }
        
        private void AddCareerPassivesForHeroRegeneration(MobileParty party, ref ExplainedNumber explainedNumber)
        {
            if (party.LeaderHero.HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(party.LeaderHero, ref explainedNumber, PassiveEffectType.HealthRegeneration, false);
            }
        }
    }
}
