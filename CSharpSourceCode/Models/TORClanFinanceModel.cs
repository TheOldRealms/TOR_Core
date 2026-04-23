using Ink.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.LinQuick;
using TOR_Core.CampaignMechanics.ServeAsAHireling;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORClanFinanceModel : DefaultClanFinanceModel
    {
        private static readonly string _cheatGoldAdjustmentName = "AI Gold Adjustment";
        private const double GOLD_CHANGE_CACHE_TTL_SECONDS = 0.5;
        private static readonly long GoldChangeCacheTtlTicks =
            (long)(Stopwatch.Frequency * GOLD_CHANGE_CACHE_TTL_SECONDS);

        private sealed class ClanGoldChangeCacheEntry
        {
            public long Timestamp;
            public bool ApplyWithdrawals;
            public bool IncludeDetails;
            public ExplainedNumber Value;
        }
        private static readonly Dictionary<string, ClanGoldChangeCacheEntry> _goldChangeCache = new();

        public override ExplainedNumber CalculateClanGoldChange(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false)
        {
            long nowTimestamp = 0;

            var canUseCache = clan != null && !includeDescriptions && !includeDetails;

            if (canUseCache)
            {
                nowTimestamp = Stopwatch.GetTimestamp();
                var clanKey = clan.StringId;

                if (_goldChangeCache.TryGetValue(clanKey, out var cacheEntry) &&
                    cacheEntry.ApplyWithdrawals == applyWithdrawals &&
                    cacheEntry.IncludeDetails == includeDetails &&
                    (nowTimestamp - cacheEntry.Timestamp) <= GoldChangeCacheTtlTicks)
                {
                    return cacheEntry.Value;
                }
            }


            var num = base.CalculateClanGoldChange(clan, includeDescriptions, applyWithdrawals, includeDetails);
            AddCareerPerkBenefits(clan, ref num);

            if (num.ResultNumber < 0 && clan.Kingdom != null && clan != Clan.PlayerClan && !clan.IsMinorFaction && clan.Gold < 200000)
            {
                var cheat = num.GetLines().Where(x => x.name == _cheatGoldAdjustmentName);
                if (cheat != null && cheat.Count() < 1)
                {
                    AdjustIncomeForAI(ref num);
                }
            }

            if (Hero.MainHero.Clan == clan)
            {
                if (Hero.MainHero.IsEnlisted())
                {
                    ServeAsAHirelingHelpers.AddHirelingWage(Hero.MainHero, ref num);
                }

            }
            if (canUseCache)
            {
                if (nowTimestamp == 0)
                    nowTimestamp = Stopwatch.GetTimestamp();

                _goldChangeCache[clan.StringId] = new ClanGoldChangeCacheEntry
                {
                    Timestamp = nowTimestamp,
                    ApplyWithdrawals = applyWithdrawals,
                    IncludeDetails = includeDetails,
                    Value = num
                };
            }
            return num;
        }

        public override ExplainedNumber CalculateClanIncome(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false)
        {
            var income = base.CalculateClanIncome(clan, includeDescriptions, applyWithdrawals, includeDetails);
            AddCareerPerkBenefits(clan, ref income);

            var num = CalculateClanGoldChange(clan, includeDescriptions, applyWithdrawals);
            var cheat = num.GetLines().Where(x => x.name == _cheatGoldAdjustmentName);
            if (cheat != null && cheat.Count() > 0)
            {
                income.Add(cheat.FirstOrDefault().number, new TextObject(_cheatGoldAdjustmentName));
            }

            if (Hero.MainHero.Clan == clan)
            {
                if (Hero.MainHero.IsEnlisted())
                {
                    ServeAsAHirelingHelpers.AddHirelingWage(Hero.MainHero, ref income);
                }

                if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.GREENSKIN)
                {
                    var playerSettlements = clan.Fiefs.WhereQ(x => x.Settlement.Owner == Hero.MainHero && x.Settlement.IsGreenskinCamp());
                    var teefBagGold = 0;
                    foreach (var fief in playerSettlements)
                    {
                        var teefBagElement = fief.Settlement.Stash.FirstOrDefaultQ(x => x.EquipmentElement.Item?.StringId == "tor_gs_teef_bag");
                        if (teefBagElement.EquipmentElement.Item != null)
                        {
                            teefBagGold += teefBagElement.Amount * 10;
                        }
                    }
                    if (teefBagGold > 0)
                    {
                        income.Add(teefBagGold, new TextObject("Teef Bags"));
                    }
                }
            }

            return income;
        }

        private void AddCareerPerkBenefits(Clan clan, ref ExplainedNumber income)
        {
            if (clan != Clan.PlayerClan) return;

            var playerHero = Hero.MainHero;

            if (playerHero.HasAnyCareer())
            {
                var choices = playerHero.GetAllCareerChoices();
                if (choices.Contains("MercenaryLordPassive3"))
                {
                    var choice = TORCareerChoices.GetChoice("MercenaryLordPassive3");
                    int mercenaryAward = MathF.Ceiling(clan.Influence * (1f / Campaign.Current.Models.ClanFinanceModel.RevenueSmoothenFraction())) * clan.MercenaryAwardMultiplier; //stolen vanilla calculation, it got too messy to redirect the income.
                    var skillFactor = ((float)playerHero.GetSkillValue(DefaultSkills.Trade)) / 300f;
                    var bonus = mercenaryAward * skillFactor;
                    income.Add((int)bonus, choice.BelongsToGroup.Name);
                }
            }
        }

        private void AdjustIncomeForAI(ref ExplainedNumber num)
        {
            num.Add(Math.Abs(num.ResultNumber) + TORConfig.AIGoldAdjustmentAmount, new TextObject(_cheatGoldAdjustmentName));
        }
    }
}