using Helpers;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items;
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
            if (character.IsHero || character.HasAttribute("Survivor"))
            {
                return 1f;
            }

            var survivalChance = CalculateRegularSurvivalChance_TweakedVanilla(party,character,damageType,canDamageKillEvenIfBlunt, enemyParty);


            if (party?.LeaderHero != null && party.LeaderHero.GetPerkValue(TORPerks.Faith.Revival))
            {
                var secondChance = TORPerks.Faith.Revival.PrimaryBonus;
                survivalChance = survivalChance + (1f - survivalChance) * secondChance; // chance to survive if would have died
            }

            if (!character.IsUndead())
            {
                return survivalChance;
            }
            if (character.Tier < 4)
            {
                return 0;
            }

            if (party != null && party.LeaderHero != null && party.LeaderHero == Hero.MainHero && party.LeaderHero.HasAnyCareer())
            {
                var choices = party.LeaderHero.GetAllCareerChoices();
                if (choices.Contains("MasterOfDeadPassive4"))
                {
                    var choice = TORCareerChoices.GetChoice("MasterOfDeadPassive4");
                    if (choice != null)
                        return survivalChance + choice.GetPassiveValue();
                }
                if (choices.Contains("CodexMortificaPassive4"))
                {
                    var choice = TORCareerChoices.GetChoice("CodexMortificaPassive4");
                    if (choice != null)
                        return survivalChance + choice.GetPassiveValue();
                }

                if (choices.Contains("WellspringOfDharPassive2"))
                {
                    var choice = TORCareerChoices.GetChoice("WellspringOfDharPassive2");
                    if (choice != null)
                        return survivalChance + choice.GetPassiveValue();
                }
            }
            return 0;
        }
        //from vanilla
        private const float SURVIVAL_CHANCE_PER_CHARACTER_LEVEL = 0.01f; // 1/3 vanilla
        private const float SURGEON_SURVIVAL_BONUS_MULTIPLIER = 0.5f; // 1/2 vanilla

        private static float CalculateRegularSurvivalChance_TweakedVanilla(
            PartyBase party,
            CharacterObject character,
            DamageTypes damageType,
            bool canDamageKillEvenIfBlunt,
            PartyBase enemyParty)
        {
            if (damageType == DamageTypes.Blunt && !canDamageKillEvenIfBlunt)
            {
                return 1f;
            }

            var mobileParty = party?.MobileParty;
            if (mobileParty == null)
            {
                return 0f;
            }

            var survivalChanceDenominator = new ExplainedNumber(1f);

            AddScaledSurgeonSurvivalBonus(mobileParty, ref survivalChanceDenominator);

            if (enemyParty?.MobileParty != null &&
                enemyParty.MobileParty.HasPerk(DefaultPerks.Medicine.DoctorsOath))
            {
                AddScaledSurgeonSurvivalBonus(enemyParty.MobileParty, ref survivalChanceDenominator);
                SkillLevelingManager.OnSurgeryApplied(enemyParty.MobileParty, surgerySuccess: false, character.Tier);
            }

            survivalChanceDenominator.Add(character.Level * SURVIVAL_CHANCE_PER_CHARACTER_LEVEL);

            if (party.MapEvent != null && character.Tier < 3)
            {
                PerkHelper.AddPerkBonusForParty(
                    DefaultPerks.Medicine.PhysicianOfPeople,
                    mobileParty,
                    isPrimaryBonus: false,
                    ref survivalChanceDenominator,
                    mobileParty.IsCurrentlyAtSea);
            }

            var deathChance = 1f / survivalChanceDenominator.ResultNumber;
            return 1f - MBMath.ClampFloat(deathChance, 0f, 1f);
        }


        private static void AddScaledSurgeonSurvivalBonus(MobileParty mobileParty, ref ExplainedNumber survivalChanceDenominator)
        {
            var surgeonSurvivalBonus = new ExplainedNumber(0f);
            SkillHelper.AddSkillBonusForParty(DefaultSkillEffects.SurgeonSurvivalBonus, mobileParty, ref surgeonSurvivalBonus);

            survivalChanceDenominator.Add(surgeonSurvivalBonus.ResultNumber * SURGEON_SURVIVAL_BONUS_MULTIPLIER);
        }

        public override ExplainedNumber GetDailyHealingForRegulars(PartyBase party, bool isPrisoners, bool includeDescriptions = false)
        {
            if (party?.MobileParty == null || !party.MobileParty.IsLordParty)
            {
                return base.GetDailyHealingForRegulars(party, isPrisoners, includeDescriptions);
            }

            var mobileParty = party.MobileParty;

            if (mobileParty.IsAffectedByCurse() && mobileParty.CurrentSettlement == null && mobileParty.BesiegedSettlement == null)
            {
                return new ExplainedNumber(0, true, GameTexts.FindText("tor_customSettlement_generic_inCursedRegion"));
            }

            var result = base.GetDailyHealingForRegulars(party, isPrisoners, includeDescriptions);

            if (mobileParty != MobileParty.MainParty)
                return result;

            if (mobileParty.HasBlessing("cult_of_sigmar"))
            {
                result.AddFactor(0.2f, GameTexts.FindText("tor_religion_blessing_name", "cult_of_sigmar"));
            }

            AddCareerPassivesForTroopRegeneration(mobileParty, ref result);

            if (Hero.MainHero.HasAttribute("WEWardancerSymbol"))
            {
                result.AddFactor(-0.25f, ForestHarmonyHelper.TreeSymbolText("WEWardancerSymbol"));
            }

            return result;
        }


        public override ExplainedNumber GetDailyHealingHpForHeroes(PartyBase party, bool isPrisoners, bool includeDescriptions = false)
        {
            if (party?.MobileParty == null || !party.MobileParty.IsLordParty)
            {
                return base.GetDailyHealingHpForHeroes(party, isPrisoners, includeDescriptions);
            }

            if (party.MobileParty.IsAffectedByCurse())
            {
                return new ExplainedNumber(0, true, GameTexts.FindText("tor_customSettlement_generic_inCursedRegion"));
            }

            var result = base.GetDailyHealingHpForHeroes(party, isPrisoners, includeDescriptions);

            if (party.MobileParty != MobileParty.MainParty && party.LeaderHero != null && party.LeaderHero.IsVampire())
            {
                result.AddFactor(0.2f);
            }

            if (!party.MobileParty.IsMainParty)
                return result;

            if (party.MobileParty.HasBlessing("cult_of_shallya"))
                result.AddFactor(0.2f, GameTexts.FindText("tor_religion_blessing_name", "cult_of_shallya"));

            AddCareerPassivesForHeroRegeneration(party.MobileParty, ref result);

            //requires me to add the strings for forest harmony levels
            if (party.LeaderHero?.Culture?.StringId == TORConstants.Cultures.ASRAI)
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
                            result.AddFactor(ForestHarmonyHelper.HealthRegDebuffBound, new TextObject(ForestHarmonyLevel.Bound.ToString()));
                            break;
                    }
                }

                if (Hero.MainHero.HasAttribute("WEWardancerSymbol"))
                {
                    result.AddFactor(0.25f, ForestHarmonyHelper.TreeSymbolText("WEWardancerSymbol"));
                }
            }

            return result;
        }


        private void AddCareerPassivesForTroopRegeneration(MobileParty party, ref ExplainedNumber explainedNumber)
        {
            if (party.LeaderHero.HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(party.LeaderHero, ref explainedNumber, PassiveEffectType.TroopRegeneration, false);

                if (Hero.MainHero.HasCareer(TORCareers.KnightOldWorld))
                {
                    var shallya = ReligionObject.All.FirstOrDefaultQ(x => x.StringId == "cult_of_shallya");
                    if (Hero.MainHero.GetDevotionLevelForReligion(shallya) >= DevotionLevel.Fanatic)
                    {
                        var info = ExtendedInfoManager.Instance.GetPartyInfoFor(party.StringId);

                        if (info == null) return;

                        var troopAttributes = info.TroopAttributes;

                        var bonus = 0f;
                        foreach (var troop in party.MemberRoster.GetTroopRoster())
                        {
                            if (troopAttributes.TryGetValue(troop.Character.StringId, out List<string> elementAttributes))
                            {
                                if (elementAttributes.Contains("ShallyaSeal1"))
                                {
                                    bonus += 0.05f * troop.Number;
                                }
                            }
                        }

                        explainedNumber.Add(bonus, new TextObject("Shallya Seal"));
                    }
                }
            }
        }

        private void AddCareerPassivesForHeroRegeneration(MobileParty party, ref ExplainedNumber explainedNumber)
        {
            if (party.LeaderHero.HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(party.LeaderHero, ref explainedNumber, PassiveEffectType.HealthRegeneration, false);
            }
        }

        /// <remarks>
        /// AI parties heal once per day, or 4 times on the quarterDaily tick - I don't remember the specifics. The main party heals on hourly ticks.
        /// </remarks>
        public override int GetHeroesEffectedHealingAmount(Hero hero, float healingRate)
        {
            var effectiveRate = new ExplainedNumber(base.GetHeroesEffectedHealingAmount(hero, healingRate));

            if (hero.PartyBelongedTo == MobileParty.MainParty)
            {
                var equipmentEffect = hero.GetAggregatedStatEffectFromEquipment(ItemTraitStatType.HealthRegen);
                if (equipmentEffect > 0)
                {
                    effectiveRate.AddFactor(equipmentEffect, GameTexts.FindText("tor_generic_enchantedEquipment"));//percentage values are listed on the item traits
                }
            }
            float resultNumber = effectiveRate.ResultNumber;
            if (resultNumber - (float)(int)resultNumber > MBRandom.RandomFloat)
            {
                return (int)resultNumber + 1;
            }

            return (int)resultNumber;
        }
    }
}