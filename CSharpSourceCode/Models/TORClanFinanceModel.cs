using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.ServeAsAHireling;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORClanFinanceModel : DefaultClanFinanceModel
    {
        private static readonly string _cheatGoldAdjustmentName = "AI Gold Adjustment";

        public override ExplainedNumber CalculateClanGoldChange(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false)
        {
            var num = base.CalculateClanGoldChange(clan, includeDescriptions, applyWithdrawals, includeDetails);
            AddCareerPerkBenefits(clan, ref num);
            
            if(num.ResultNumber < 0 && clan.Kingdom != null && clan != Clan.PlayerClan && !clan.IsMinorFaction && num.ResultNumber < 0 && clan.Gold < 100000)
            {
                AdjustIncomeForAI(ref num);
            }
            
            if (Hero.MainHero.Clan == clan)
            {
                if (Hero.MainHero.IsEnlisted())
                {
                    ServeAsAHirelingHelpers.AddHirelingWage(Hero.MainHero, ref num);
                }
            }
            
            return num;
        }

        public override ExplainedNumber CalculateClanIncome(Clan clan, bool includeDescriptions = false, bool applyWithdrawals = false, bool includeDetails = false)
        {
            var income = base.CalculateClanIncome(clan, includeDescriptions, applyWithdrawals, includeDetails);
            AddCareerPerkBenefits(clan, ref income);
            var num = CalculateClanGoldChange(clan, includeDescriptions, applyWithdrawals);
            
            var cheat = num.GetLines().Where(x => x.name == _cheatGoldAdjustmentName);
            if(cheat != null && cheat.Count() > 0)
            {
                income.Add(cheat.FirstOrDefault().number, new TextObject(_cheatGoldAdjustmentName));
            }


            if (Hero.MainHero.Clan == clan)
            {
                if (Hero.MainHero.IsEnlisted())
                {
                    ServeAsAHirelingHelpers.AddHirelingWage(Hero.MainHero, ref income);
                }
            }
            
            return income;
        }


        private void AddCareerPerkBenefits(Clan clan, ref ExplainedNumber income)
        {
            if(clan != Clan.PlayerClan) return;

            var playerHero = Hero.MainHero;

            if (playerHero.HasAnyCareer())
            {
                var choices = playerHero.GetAllCareerChoices();
                if (choices.Contains("MercenaryLordPassive3"))
                {
                    var choice = TORCareerChoices.GetChoice("MercenaryLordPassive3");
                    int mercenaryAward = MathF.Ceiling(clan.Influence * (1f / Campaign.Current.Models.ClanFinanceModel.RevenueSmoothenFraction())) * clan.MercenaryAwardMultiplier; //stolen vanilla calculation, it got too messy to redirect the income.
                    var skillFactor = ((float)playerHero.GetSkillValue(DefaultSkills.Trade)) /300f;
                    var bonus =  mercenaryAward * skillFactor;
                    income.Add((int)bonus,choice.BelongsToGroup.Name);
                }
            }
        }

        private void AdjustIncomeForAI(ref ExplainedNumber num)
        {
            num.Add(Math.Abs(num.ResultNumber) + 200, new TextObject(_cheatGoldAdjustmentName));
        }
    }
}
