using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.UniqueSpawns;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORPartyWageModel : DefaultPartyWageModel
    {
        private static readonly Dictionary<CharacterObject, int> _wageCache = [];
        private const double TOTAL_WAGE_CACHE_TTL_SECONDS = 0.3;
        private static readonly long TotalWageCacheTtlTicks =
            (long)(Stopwatch.Frequency * TOTAL_WAGE_CACHE_TTL_SECONDS);

        private static readonly ConditionalWeakTable<MobileParty, TotalWageCacheEntry> _totalWageCache = new();

        private sealed class TotalWageCacheEntry
        {
            public long Timestamp;
            public int TotalManCount;
            public string LeaderHeroStringId;
            public bool IncludeDescriptions;
            public ExplainedNumber Value;
        }


        private int CalculateCharacterWageCache(CharacterObject character)
        {
            if (!_wageCache.ContainsKey(character))
            {
                int wage = 0;
                if(!character.IsUndead() && !character.IsTreeSpirit())
                {
                    wage = GetWageForTier(character.Tier);
                    if (character.Culture.StringId == TORConstants.Cultures.BRETONNIA && character.IsKnightUnit())
                    {
                        wage *= 2;
                    }

                    if (character.Culture.StringId == TORConstants.Cultures.EONIR && character.IsEliteTroop())
                    {
                        wage *= 2;
                    }
                }
                _wageCache[character] = wage;
                return wage;
            }
            return _wageCache[character];
        }

        public override int GetCharacterWage(CharacterObject character)
        {
            if (_wageCache.TryGetValue(character, out int wage))
            {
                return wage;
            }
            else
            {
                return CalculateCharacterWageCache(character);
            }
        }

        private static int GetWageForTier(int tier)
        {
            switch (tier)
            {
                case 0:
                    return 1;
                case 1:
                    return 2;
                case 2:
                    return 3;
                case 3:
                    return 5;
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

        public override ExplainedNumber GetTotalWage(MobileParty mobileParty, TroopRoster troopRoster, bool includeDescriptions = false)
        {
            TotalWageCacheEntry cacheEntry = null;
            long nowTimestamp = 0;
            string leaderHeroStringId = null;
            int totalManCount = 0;

            if (mobileParty != null)
            {
                nowTimestamp = Stopwatch.GetTimestamp();
                leaderHeroStringId = mobileParty.LeaderHero?.StringId ?? string.Empty;
                totalManCount = troopRoster.TotalManCount;

                if (_totalWageCache.TryGetValue(mobileParty, out cacheEntry) &&
                    cacheEntry.IncludeDescriptions == includeDescriptions &&
                    cacheEntry.TotalManCount == totalManCount &&
                    cacheEntry.LeaderHeroStringId == leaderHeroStringId &&
                    (nowTimestamp - cacheEntry.Timestamp) <= TotalWageCacheTtlTicks)
                {
                    return cacheEntry.Value;
                }

                if (cacheEntry == null)
                {
                    cacheEntry = _totalWageCache.GetValue(mobileParty, _ => new TotalWageCacheEntry());
                }

            }

            var value = base.GetTotalWage(mobileParty, troopRoster, includeDescriptions);
            value.LimitMin(0f); //no getting paid after all bonuses are applied
            if (mobileParty == null)
            {
                return value;
            }

            if (mobileParty.GetUniqueSpawnComponent() != null)
            {
                var uniqueSpawnWage = new ExplainedNumber(0, includeDescriptions);

                foreach (var troop in troopRoster.GetTroopRoster())
                {
                    if (troop.Character == null || troop.Character.IsHero)
                    {
                        continue;
                    }

                    uniqueSpawnWage.Add(troop.Number * UniqueSpawnCampaignBehavior.UniqueSpawnTroopWage);
                }

                return uniqueSpawnWage;
            }
            void StoreCacheIfPossible()
            {
                if (mobileParty == null || cacheEntry == null)
                    return;

                if (nowTimestamp == 0)
                    nowTimestamp = Stopwatch.GetTimestamp();

                cacheEntry.Timestamp = nowTimestamp;
                cacheEntry.TotalManCount = totalManCount;
                cacheEntry.LeaderHeroStringId = leaderHeroStringId ?? string.Empty;
                cacheEntry.IncludeDescriptions = includeDescriptions;
                cacheEntry.Value = value;
            }

            if (!mobileParty.IsMainParty)
            {
                StoreCacheIfPossible();
                return value;
            }

            var leaderHero = mobileParty.LeaderHero;
            if (leaderHero != Hero.MainHero) //Sly : when player is prisoner
            {
                StoreCacheIfPossible();
                return value;
            }


            bool hasCareer = leaderHero.HasAnyCareer();
            var leaderCulture = leaderHero.Culture;
            var isKnightOldWorld = hasCareer && leaderHero.HasCareer(TORCareers.KnightOldWorld);
            var partyAttributes = isKnightOldWorld
                ? ExtendedInfoManager.Instance.GetPartyInfoFor(mobileParty.StringId)
                : null;
            for (int index = 0; index < mobileParty.MemberRoster.Count; ++index)
            {
                TroopRosterElement elementCopyAtIndex = mobileParty.MemberRoster.GetElementCopyAtIndex(index);
                var character = elementCopyAtIndex.Character;
                if (character.IsHero && character.HeroObject == Hero.MainHero) continue;//player has no wage, but leaving this open to effects that can reduce companion wages

                float troopwage = elementCopyAtIndex.Character.TroopWage * elementCopyAtIndex.Number;

                if (hasCareer)
                {
                    var careerFactors = new ExplainedNumber(0, includeDescriptions);
                    careerFactors = AddCareerSpecificWagePerks(careerFactors, leaderHero, elementCopyAtIndex);

                    if (includeDescriptions)
                    {
                        foreach (var line in careerFactors.GetLines())
                        {
                            value.Add(line.number, new TextObject(line.name));
                        }
                    }
                    else
                    {
                        value.Add(careerFactors.ResultNumber);
                    }

                    if (partyAttributes != null &&
                        partyAttributes.TroopAttributes.TryGetValue(elementCopyAtIndex.Character.StringId, out var attributes) &&
                        attributes != null)
                    {
                        for (var attributeIndex = 0; attributeIndex < attributes.Count; attributeIndex++)
                        {
                            if (attributes[attributeIndex] == "SecularSeal2")
                            {
                                value.Add(-0.25f * troopwage, includeDescriptions ? new TextObject("Secular Seal") : null);
                            }
                        }
                    }
                }

                if (leaderCulture.StringId == TORConstants.Cultures.DAWI)
                {
                    if (elementCopyAtIndex.Character.HasAttribute("DwarfGun"))
                    {
                        if (leaderHero.HasAttribute("DwarfEngineersIII"))
                        {
                            value.Add(-0.25f * troopwage, includeDescriptions ? new TextObject("Engineers Guild") : null);
                        }
                        else if (leaderHero.HasAttribute("DwarfEngineersII"))
                        {
                            value.Add(-0.15f * troopwage, includeDescriptions ? new TextObject("Engineers Guild") : null);
                        }
                    }
                }


                if (leaderCulture.StringId == TORConstants.Cultures.BRETONNIA && elementCopyAtIndex.Character.IsKnightUnit())
                {
                    var level = leaderHero.GetChivalryLevel();
                    var factor = 0f;
                    switch (level)
                    {
                        case ChivalryLevel.Unknightly:
                            factor = 0.75f;
                            break;
                        case ChivalryLevel.Uninspiring:
                            factor = 0.5f;
                            break;
                        case ChivalryLevel.Sincere:
                            factor = 0.25f;
                            break;
                        case ChivalryLevel.Noteworthy:
                            factor = 0.1f;
                            break;
                        case ChivalryLevel.PureHearted:
                            break;
                        case ChivalryLevel.Honourable:
                            factor = -0.1f;
                            break;
                        case ChivalryLevel.Chivalrous:
                            factor = -0.2f;
                            break;
                    }
                    value.Add((troopwage * factor), includeDescriptions ? ChivalryHelper.GetChivalryRankText(level) : null);
                }

                if (leaderCulture.StringId == TORConstants.Cultures.ASRAI)
                {
                    if (leaderHero.HasAttribute("WEOrionSymbol"))
                    {
                        if (elementCopyAtIndex.Character.IsElf() && elementCopyAtIndex.Character.Culture.StringId == TORConstants.Cultures.ASRAI)
                        {
                            value.Add(-0.5f * troopwage, ForestHarmonyHelper.TreeSymbolText("WEOrionSymbol"));
                        }
                    }

                    if (leaderHero.HasAttribute("WEArielSymbol"))
                    {
                        value.Add(0.5f * troopwage, ForestHarmonyHelper.TreeSymbolText("WEArielSymbol"));
                    }

                    if (leaderHero.HasAttribute("WEWandererSymbol"))
                    {
                        value.Add(0.5f * troopwage, ForestHarmonyHelper.TreeSymbolText("WEWandererSymbol"));
                    }

                    if (leaderHero.HasAttribute("WETreekinSymbol") && !elementCopyAtIndex.Character.IsTreeSpirit())
                    {
                        value.Add(0.25f * troopwage, ForestHarmonyHelper.TreeSymbolText("WETreekinSymbol"));
                    }

                    if (leaderHero.HasAttribute("WEKithbandSymbol"))
                    {
                        value.Add(0.15f * troopwage, ForestHarmonyHelper.TreeSymbolText("WEKithbandSymbol"));
                    }
                }
            }

            StoreCacheIfPossible();
            return value;
        }

        //Sly : with the buyer hero argument it's possible to apply bonuses or penalties to specific clans or heroes
        public override ExplainedNumber GetTroopRecruitmentCost(CharacterObject troop, Hero buyerHero, bool withoutItemCost = false)
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

            if (specialFlag) troopRecruitmentCost = MathF.Round(troopRecruitmentCost * 2f);

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
                    if (buyerHero.Culture.HasFeat(DefaultCulturalFeats.KhuzaitRecruitUpgradeFeat))
                        explainedNumber.AddFactor(DefaultCulturalFeats.KhuzaitRecruitUpgradeFeat.EffectBonus, GameTexts.FindText("str_culture"));
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

                troopRecruitmentCost = MathF.Max(1, MathF.Round(troopRecruitmentCost * explainedNumber.ResultNumber));
            }
            return new ExplainedNumber(troopRecruitmentCost);
        }


        private ExplainedNumber AddCareerSpecificWagePerks(ExplainedNumber resultValue, Hero hero, TroopRosterElement unit)
        {
            if (hero != Hero.MainHero || !Hero.MainHero.HasAnyCareer()) return resultValue;
            var choices = hero.GetAllCareerChoices();
            foreach (var choiceID in choices)
            {
                var choice = TORCareerChoices.GetChoice(choiceID);
                if (choice?.Passive?.PassiveEffectType != PassiveEffectType.TroopWages) continue;

                if (!choice.Passive.IsValidCharacterObject(unit.Character))
                {
                    continue;
                }

                var value = CareerHelper.CalculateTroopWageCareerPerkEffect(unit, choice, out var textObject);
                resultValue.Add(value, textObject);
            }

            return resultValue;
        }

        public static void ClearCharacterWageCache()
        {
            _wageCache.Clear();
        }
    }
}