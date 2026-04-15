using Helpers;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public sealed class TORSettlementProsperityModel : DefaultSettlementProsperityModel
    {
        // vanilla values
        private const float VANILLA_TIER_1_HEARTH = 300f;
        private const float VANILLA_TIER_2_HEARTH = 600f;

        private const float VANILLA_BASE_CHANGE_BELOW_TIER_1 = 4f;
        private const float VANILLA_BASE_CHANGE_BELOW_TIER_2 = 1.2f;
        private const float VANILLA_BASE_CHANGE_AT_OR_ABOVE_TIER_2 = 0.2f;

        private const float VANILLA_LOOTED_PENALTY = -1f;

        // TODO
        private const float VANILLA_GRAZING_RIGHTS_PENALTY = -0.25f;

        private static readonly TextObject RaidedText = new TextObject("{=RVas572P}Raided");
        private static readonly TextObject CultureScalingText = new TextObject("{=!}Culture Hearth Scaling");

        // culture multipliers
        private readonly struct HearthScalingSettings
        {
            public readonly float TierMultiplier;
            public readonly float ValueMultiplier;

            public HearthScalingSettings(float tierMultiplier, float valueMultiplier)
            {
                TierMultiplier = tierMultiplier;
                ValueMultiplier = valueMultiplier;
            }
        }

        private static readonly Dictionary<string, HearthScalingSettings> ScalingByCultureId = new()
        {
            // x2: empire + bretonnia
            [TORConstants.Cultures.EMPIRE] = new HearthScalingSettings(tierMultiplier: 2f, valueMultiplier: 2f),
            [TORConstants.Cultures.BRETONNIA] = new HearthScalingSettings(tierMultiplier: 2f, valueMultiplier: 2f),

            // x3: dwarfs + elves
            [TORConstants.Cultures.DAWI] = new HearthScalingSettings(tierMultiplier: 3f, valueMultiplier: 3f),
            [TORConstants.Cultures.ASRAI] = new HearthScalingSettings(tierMultiplier: 3f, valueMultiplier: 3f),
            [TORConstants.Cultures.EONIR] = new HearthScalingSettings(tierMultiplier: 3f, valueMultiplier: 3f),
        };

        public override ExplainedNumber CalculateHearthChange(Village village, bool includeDescriptions = false)
        {
            var result = new ExplainedNumber(includeDescriptions: includeDescriptions);

            var ownerCultureId = village.Settlement.OwnerClan.Culture.StringId;
            if (!ScalingByCultureId.TryGetValue(ownerCultureId, out var scaling))
            {
                return base.CalculateHearthChange(village, includeDescriptions);
            }

            var tier1 = VANILLA_TIER_1_HEARTH * scaling.TierMultiplier;
            var tier2 = VANILLA_TIER_2_HEARTH * scaling.TierMultiplier;

            if (village.VillageState == Village.VillageStates.Normal)
            {
                var baseChange =
                    (village.Hearth < tier1) ? VANILLA_BASE_CHANGE_BELOW_TIER_1 :
                    (village.Hearth < tier2) ? VANILLA_BASE_CHANGE_BELOW_TIER_2 :
                    VANILLA_BASE_CHANGE_AT_OR_ABOVE_TIER_2;

                result = new ExplainedNumber(baseChange, includeDescriptions);
            }

            if (village.VillageState == Village.VillageStates.Looted)
            {
                result.Add(VANILLA_LOOTED_PENALTY, RaidedText);
            }

            if (village.Bound != null && village.VillageState == Village.VillageStates.Normal)
            {
                PerkHelper.AddPerkBonusForTown(DefaultPerks.Medicine.BushDoctor, village.Bound.Town, ref result);
                PerkHelper.AddPerkBonusForTown(DefaultPerks.Athletics.Energetic, village.Bound.Town, ref result);
                PerkHelper.AddPerkBonusForTown(DefaultPerks.Steward.AidCorps, village.Bound.Town, ref result);

                if (village.Bound.IsFortification)
                {
                    village.Bound.Town.AddEffectOfBuildings(BuildingEffectEnum.VillageHeartsPerDay, ref result);
                }
            }

            Campaign.Current.Models.IssueModel.GetIssueEffectsOfSettlement(
                DefaultIssueEffects.VillageHearth,
                village.Settlement,
                ref result);

            if (scaling.ValueMultiplier != 1f)
            {
                result.AddFactor(scaling.ValueMultiplier - 1f, CultureScalingText);
            }

            // TODO
            if (village.Settlement.OwnerClan?.Kingdom != null &&
                village.Settlement.OwnerClan.Kingdom.ActivePolicies.Contains(DefaultPolicies.GrazingRights))
            {
                result.Add(VANILLA_GRAZING_RIGHTS_PENALTY, ((PropertyObject)DefaultPolicies.GrazingRights).Name);
            }

            return result;
        }
    }
}