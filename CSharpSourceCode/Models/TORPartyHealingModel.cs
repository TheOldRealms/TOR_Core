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
            if (character.HasAttribute("Survivor"))
            {
                return 1;
            }

            var result = base.GetSurvivalChance(party, character, damageType, canDamageKillEvenIfBlunt, enemyParty);

            if (result < 0.5f && party != null && party.LeaderHero != null && party.LeaderHero.GetPerkValue(TORPerks.Faith.Revival)) result = TORPerks.Faith.Revival.PrimaryBonus; //Sly : perk description does not match functionality

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
                if (choices.Contains("MasterOfDeadPassive4"))
                {
                    var choice = TORCareerChoices.GetChoice("MasterOfDeadPassive4");
                    if (choice != null)
                        return result + choice.GetPassiveValue();
                }
                if (choices.Contains("CodexMortificaPassive4"))
                {
                    var choice = TORCareerChoices.GetChoice("CodexMortificaPassive4");
                    if (choice != null)
                        return result + choice.GetPassiveValue();
                }

                if (choices.Contains("WellspringOfDharPassive2"))
                {
                    var choice = TORCareerChoices.GetChoice("WellspringOfDharPassive2");
                    if (choice != null)
                        return result + choice.GetPassiveValue();
                }
            }


            return 0;
        }

        public override ExplainedNumber GetDailyHealingForRegulars(PartyBase party, bool isPrisoners, bool includeDescriptions = false)
        {
            if (party?.MobileParty == null || !party.MobileParty.IsLordParty)
            {
                return base.GetDailyHealingForRegulars(party, isPrisoners, includeDescriptions);
            }

            if (party.MobileParty.IsAffectedByCurse() && party.MobileParty.CurrentSettlement == null && party.MobileParty.BesiegedSettlement == null)
            {
                return new ExplainedNumber(0, true, GameTexts.FindText("tor_customSettlement_generic_inCursedRegion"));
            }

            var result = base.GetDailyHealingForRegulars(party, isPrisoners, includeDescriptions);


            if (party.MobileParty != MobileParty.MainParty) return result;


            if (party.MobileParty.HasBlessing("cult_of_sigmar"))
            {
                result.AddFactor(0.2f, GameTexts.FindText("tor_religion_blessing_name", "cult_of_sigmar"));
            }

            AddCareerPassivesForTroopRegeneration(party.MobileParty, ref result);

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

            if (!party.MobileParty.IsMainParty) return result;


            if (party.MobileParty.HasBlessing("cult_of_shallya")) result.AddFactor(0.2f, GameTexts.FindText("tor_religion_blessing_name", "cult_of_shallya"));

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
                            result.AddFactor(ForestHarmonyHelper.HealthRegDebuffUnBound, GameTexts.FindText("tor_forest_harmony_level", ForestHarmonyLevel.Unbound.ToString()));
                            break;
                        case ForestHarmonyLevel.Bound:
                            result.AddFactor(ForestHarmonyHelper.HealthRegDebuffBound, GameTexts.FindText("tor_forest_harmony_level", ForestHarmonyLevel.Bound.ToString()));
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