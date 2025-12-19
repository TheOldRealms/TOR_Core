using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORCombatXpModel : DefaultCombatXpModel
    {
        public override SkillObject GetSkillForWeapon(WeaponComponentData weapon, bool isSiegeWeaponHit)
        {
            SkillObject result = DefaultSkills.Athletics;
            var baseResult = base.GetSkillForWeapon(weapon, isSiegeWeaponHit);
            if (baseResult != null) result = baseResult;
            return result;
        }

        public override ExplainedNumber GetXpFromHit(CharacterObject attackerTroop, CharacterObject captain, CharacterObject attackedTroop, PartyBase party, int damage, bool isFatal, MissionTypeEnum missionType)
        {
            var xpAmount = base.GetXpFromHit(attackerTroop, captain, attackedTroop, party, damage, isFatal, missionType);

            var previousFactor = xpAmount.SumOfFactors;
            var inverseMultiplier = AdjustXpMultiplierToBattle(missionType);
            xpAmount.AddFactor((previousFactor + 1) * (inverseMultiplier - 1));//the inverse can't be applied directly because there may already be existing factors on the explained number


            if (missionType != MissionTypeEnum.Battle) return xpAmount;

            if (party == null || (party != PartyBase.MainParty && !MobileParty.MainParty.LeaderHero.HasAnyCareer())) return xpAmount;

            var choices = MobileParty.MainParty.LeaderHero.GetAllCareerChoices();

            if (isFatal && attackerTroop.Tier > 3 && party.MobileParty == MobileParty.MainParty && choices.Contains("PeerlessWarriorPassive3"))
            {
                var choice = TORCareerChoices.GetChoice("PeerlessWarriorPassive3");
                if (choice != null)
                {
                    xpAmount.AddFactor(choice.GetPassiveValue());
                }
            }

            if (isFatal && party.MobileParty == MobileParty.MainParty && MobileParty.MainParty.HasBlessing("cult_of_ulric"))
            {
                xpAmount.AddFactor(0.2f);
            }

            // Myrmidia blessing - XP bonus for player only
            if ( attackedTroop.IsHero && party.MobileParty == MobileParty.MainParty && MobileParty.MainParty.HasBlessing("cult_of_myrmidia"))
            {
                xpAmount.AddFactor(0.10f);
            }

            // Myrmidia Seal 2 - XP bonus for sealed unit
            if (party?.MobileParty != null && attackerTroop != null)
            {
                var info = ExtendedInfoManager.Instance.GetPartyInfoFor(party.MobileParty.StringId);
                if (info?.TroopAttributes.TryGetValue(attackerTroop.StringId, out var attrs) == true)
                {
                    if (attrs.Contains("MyrmidiaSeal2"))
                    {
                        xpAmount.AddFactor(0.10f);
                    }
                }
            }

            return xpAmount;
        }

        /// <summary>
        /// Used to reverse the xp reductions based on mission type when awarding combat xp.
        /// </summary>
        public float AdjustXpMultiplierToBattle(CombatXpModel.MissionTypeEnum missionType)
        {
            float adjustedMultiplier = 1;
            var oldMultiplier = missionType switch
            {
                CombatXpModel.MissionTypeEnum.NoXp => 1f,//avoid division by 0 error; results in no change to the multiplier, but in any case the xp granted would still be 0 because (baseXp * 0) was already performed and this adjusted multiplier would then be applied to 0.
			    CombatXpModel.MissionTypeEnum.PracticeFight => 0.0625f,
			    CombatXpModel.MissionTypeEnum.Tournament => 0.33f,
			    CombatXpModel.MissionTypeEnum.SimulationBattle => 0.9f,
			    CombatXpModel.MissionTypeEnum.Battle => 1f,
                _ => 1f,
            };

            return adjustedMultiplier/oldMultiplier;
        }
    }
}