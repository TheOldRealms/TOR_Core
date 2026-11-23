using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.Models
{
    /// <summary>Settlement militia model with culture bonuses, Fence governor perk, and guild/attribute boosters.</summary>
    public class TORSettlementMilitiaModel : DefaultSettlementMilitiaModel
    {
        /// <summary>Daily militia change with TOR culture bonuses and Fence besieged-town governor perk layered onto the vanilla base.</summary>
        public override ExplainedNumber CalculateMilitiaChange(Settlement settlement, bool includeDescriptions = false)
        {
            var result = base.CalculateMilitiaChange(settlement, includeDescriptions);

            // Fence perk: militia bonus in besieged governed towns.
            if (settlement.IsUnderSiege && settlement.Town?.Governor != null)
            {
                TORSkillPerkHelper.ApplyPerkPassives(settlement.Town.Governor, ref result,
                    SkillEffectType.GovernorBesiegedMilitia);
            }
            if (settlement.IsCastle)
            {
                if (settlement.IsBloodKeep() && settlement.Owner.IsVampire() && !settlement.IsUnderSiege)
                {
                    if (settlement.Militia < 2000)
                    {
                        result.LimitMin(10);
                    }
                    return result;
                }

                if (settlement.IsBrassKeep())
                {
                    if (settlement.Militia < 2000)
                    {
                        result.LimitMin(10);
                    }
                }

                if (settlement.IsMelkhiorsTower())
                {
                    if (settlement.Militia < 2000)
                    {
                        result.LimitMin(10);
                    }
                }

                switch (settlement.OwnerClan.Culture.StringId)
                {
                    case TORConstants.Cultures.SYLVANIA:
                        result.Add(2f, new TextObject("Bonus"));
                        break;
                    case TORConstants.Cultures.EMPIRE:
                        result.Add(1f, new TextObject("Bonus"));
                        break;
                    default:
                        result.Add(1f, new TextObject("Bonus"));
                        break;
                }
            }
            else if (settlement.IsTown)
            {
                switch (settlement.OwnerClan.Culture.StringId)
                {
                    case TORConstants.Cultures.SYLVANIA:
                        result.Add(4f, new TextObject("Bonus"));
                        break;
                    case TORConstants.Cultures.EMPIRE:
                        result.Add(3f, new TextObject("Bonus"));
                        break;
                    default:
                        result.Add(2f, new TextObject("Bonus"));
                        break;
                }
            }
            if (settlement.OwnerClan != null && settlement.OwnerClan.Leader != null)
            {
                PerkHelper.AddPerkBonusForCharacter(TORPerks.Faith.DivineMission, settlement.OwnerClan.Leader.CharacterObject, false, ref result, BattleEnvironment.Any);
            }

            if (settlement.Culture.StringId == TORConstants.Cultures.DAWI && settlement.IsDwarfKarak())
            {
                if (Hero.MainHero.HasAttribute(CharacterAttributes.GUILD_WARRIORS_3))
                {
                    result.Add(4, new TextObject("Warriors Guild"));
                }
                else if (Hero.MainHero.HasAttribute(CharacterAttributes.GUILD_WARRIORS_2))
                {
                    result.Add(2, new TextObject("Warriors Guild"));
                }
            }
            return result;
        }
    }
}