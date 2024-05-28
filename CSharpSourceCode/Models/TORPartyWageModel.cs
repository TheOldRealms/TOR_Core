using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORPartyWageModel : DefaultPartyWageModel
    {

        public override int GetCharacterWage(CharacterObject character)
        {
            if (character.IsUndead()) return 0;
            var value = 0;
            value = GetWageForTier(character.Tier);

            if (character.Culture.StringId == TORConstants.Cultures.BRETONNIA && character.IsKnightUnit())
            {
                value *= 2;
            }

            return value;
        }

        private static int GetWageForTier(int tier)
        {
            switch (tier)
            {
                case 0:
                    return  1;
                case 1:
                    return 2;
                case 2:
                    return 3;
                case 3:
                    return  5;
                case 4:
                    return 8;
                case 5:
                    return 12;
                case 6:
                    return 17;
                case 7:
                    return 23;
                case 8:
                    return 30;
                default:
                    return 40;
            }
        }

        public override ExplainedNumber GetTotalWage(MobileParty mobileParty, bool includeDescriptions)
        {
            var value = base.GetTotalWage(mobileParty, includeDescriptions);

            if (mobileParty.IsMainParty)
            {
                for (int index = 0; index < mobileParty.MemberRoster.Count; ++index)
                {
                    TroopRosterElement elementCopyAtIndex = mobileParty.MemberRoster.GetElementCopyAtIndex(index);
                    if (mobileParty.LeaderHero.HasAnyCareer())
                    {
                        var careerFactors = new ExplainedNumber(0, true); 
                        careerFactors = AddCareerSpecificWagePerks(careerFactors, mobileParty.LeaderHero, elementCopyAtIndex);
                        foreach (var line in careerFactors.GetLines())
                        {
                            value.Add(line.number, new TextObject(line.name));
                        }
                    }

                    if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.BRETONNIA && elementCopyAtIndex.Character.IsKnightUnit())
                    {
                        var level = mobileParty.LeaderHero.GetChivalryLevel();
                        var factor = 0f;
                        switch (level)
                        {
                            case ChivalryLevel.Unknightly:
                                factor=0.75f;
                                break;
                            case ChivalryLevel.Uninspiring:
                                factor=0.5f;
                                break;
                            case ChivalryLevel.Sincere:
                                factor=0.25f;
                                break;
                            case ChivalryLevel.Noteworthy:
                                factor=0.1f;
                                break;
                            case ChivalryLevel.PureHearted:
                                break;
                            case ChivalryLevel.Honourable:
                                factor=-0.1f;
                                break;
                            case ChivalryLevel.Chivalrous:
                                factor=-0.2f;
                                break;
                        }
                        value.AddFactor(factor,new TextObject(level.ToString()));
                    }


                }
            }
            return value;
        }

        public override int GetTroopRecruitmentCost(CharacterObject troop, Hero buyerHero, bool withoutItemCost = false)
        {
            var value = base.GetTroopRecruitmentCost(troop, buyerHero, withoutItemCost);

            if (troop.Level <= 41)
            {
                return value;
            }
            // if we ever decide to add more tiers to the Unit tree, we need to differ like in the base model. 
            var troopRecruitmentCost = 2500;
            //vanilla copy paste.
            bool specialFlag = troop.Occupation == Occupation.Mercenary || troop.Occupation == Occupation.Gangster || troop.Occupation == Occupation.CaravanGuard;


            if (specialFlag)
                troopRecruitmentCost = MathF.Round((float)troopRecruitmentCost * 2f);
            if (buyerHero != null)
            {
                var explainedNumber = new ExplainedNumber(1f);
                if (troop.Tier >= 2 && buyerHero.GetPerkValue(DefaultPerks.Throwing.HeadHunter))
                    explainedNumber.AddFactor(DefaultPerks.Throwing.HeadHunter.SecondaryBonus);
                if (troop.IsInfantry)
                {
                    if (buyerHero.GetPerkValue(DefaultPerks.OneHanded.ChinkInTheArmor))
                        explainedNumber.AddFactor(DefaultPerks.OneHanded.ChinkInTheArmor.SecondaryBonus);
                    if (buyerHero.GetPerkValue(DefaultPerks.TwoHanded.ShowOfStrength))
                        explainedNumber.AddFactor(DefaultPerks.TwoHanded.ShowOfStrength.SecondaryBonus);
                    if (buyerHero.GetPerkValue(DefaultPerks.Polearm.HardyFrontline))
                        explainedNumber.AddFactor(DefaultPerks.Polearm.HardyFrontline.SecondaryBonus);
                    if (buyerHero.Culture.HasFeat(DefaultCulturalFeats.SturgianRecruitUpgradeFeat))
                        explainedNumber.AddFactor(DefaultCulturalFeats.SturgianRecruitUpgradeFeat.EffectBonus, GameTexts.FindText("str_culture"));
                }
                else if (troop.IsRanged)
                {
                    if (buyerHero.GetPerkValue(DefaultPerks.Bow.RenownedArcher))
                        explainedNumber.AddFactor(DefaultPerks.Bow.RenownedArcher.SecondaryBonus);
                    if (buyerHero.GetPerkValue(DefaultPerks.Crossbow.Piercer))
                        explainedNumber.AddFactor(DefaultPerks.Crossbow.Piercer.SecondaryBonus);
                }
                if (troop.IsMounted && buyerHero.Culture.HasFeat(DefaultCulturalFeats.KhuzaitRecruitUpgradeFeat))
                    explainedNumber.AddFactor(DefaultCulturalFeats.KhuzaitRecruitUpgradeFeat.EffectBonus, GameTexts.FindText("str_culture"));
                if (buyerHero.IsPartyLeader && buyerHero.GetPerkValue(DefaultPerks.Steward.Frugal))
                    explainedNumber.AddFactor(DefaultPerks.Steward.Frugal.SecondaryBonus);
                if (specialFlag)
                {
                    if (buyerHero.GetPerkValue(DefaultPerks.Trade.SwordForBarter))
                        explainedNumber.AddFactor(DefaultPerks.Trade.SwordForBarter.PrimaryBonus);
                    if (buyerHero.GetPerkValue(DefaultPerks.Charm.SlickNegotiator))
                        explainedNumber.AddFactor(DefaultPerks.Charm.SlickNegotiator.PrimaryBonus);
                }
                
                troopRecruitmentCost = MathF.Max(1, MathF.Round((float)troopRecruitmentCost * explainedNumber.ResultNumber));
            }
            return troopRecruitmentCost;
        }


        private ExplainedNumber AddCareerSpecificWagePerks(ExplainedNumber resultValue, Hero hero, TroopRosterElement unit)
        {
            if (hero != Hero.MainHero || !Hero.MainHero.HasAnyCareer()) return resultValue;
            var choices = hero.GetAllCareerChoices();
            foreach (var choiceID in choices)
            {
                var choice = TORCareerChoices.GetChoice(choiceID);
                if (choice == null)
                    continue;
                if (choice.Passive==null) continue;
                if (choice.Passive.PassiveEffectType != PassiveEffectType.TroopWages) continue;
                
                if (!choice.Passive.IsValidCharacterObject(unit.Character))
                {
                    continue;
                }

                var value = CareerHelper.CalculateTroopWageCareerPerkEffect(unit, choice, out var textObject);
                resultValue.Add(value, textObject);
            }
                
            return resultValue;
        }
    }
}
