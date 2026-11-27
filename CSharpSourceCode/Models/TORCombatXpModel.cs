using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.DualWield;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using static TOR_Core.Utilities.TORSpellBlowHelper;

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

        /// <summary>
        /// Fraction of vanilla weapon-skill XP (OneHanded etc.) kept when the
        /// main hero lands a hit while dual-wielding. The remaining share is
        /// the price of routing most of the XP into Berserk via
        /// <see cref="GrantBerserkXpForHit"/>.
        /// </summary>
        private const float DualWieldMainHandXpShare = 0.25f;

        public override ExplainedNumber GetXpFromHit(CharacterObject attackerTroop, CharacterObject captain, CharacterObject attackedTroop, PartyBase party, int damage, bool isFatal, MissionTypeEnum missionType)
        {
            // Spell hits get their own XP via TORAbilityModel - skip vanilla weapon XP
            if (IsProcessingSpellHit)
                return new ExplainedNumber(0f);

            var xpAmount = base.GetXpFromHit(attackerTroop, captain, attackedTroop, party, damage, isFatal, missionType);

            // Main hero landed this while dual-wielding: scale vanilla XP to 25%
            // because Berserk is already getting the full damage-scaled XP in
            // addition via DualWieldMissionBehavior.OnAgentHit -> GrantBerserkXpForHit.
            if (attackerTroop != null
                && Hero.MainHero != null
                && attackerTroop == Hero.MainHero.CharacterObject
                && Agent.Main != null
                && DualWieldStateManager.IsDualWielding(Agent.Main))
            {
                xpAmount.AddFactor(DualWieldMainHandXpShare - 1f, TORSkills.Berserk?.Name);
            }

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
        /// Grant Berserk XP to the main hero on top of the vanilla weapon-skill
        /// XP when they land a hit while dual-wielding. Called from
        /// <c>DualWieldMissionBehavior.OnAgentHit</c>, which is the reliable
        /// firing point for every hit including Yotthani's <c>LeftBladeStrike</c>
        /// offhand path (that one bypasses <c>MissionLogic.OnScoreHit</c> via
        /// a direct <c>Mission.MeleeHitCallback</c> invocation). Formula lives
        /// here so Berserk tuning sits alongside the other combat-XP math.
        /// </summary>
        public void GrantBerserkXpForHit(Agent affector, Agent affected, Blow blow)
        {
            if (affector == null || !affector.IsMainAgent || Hero.MainHero == null) return;
            if (!DualWieldStateManager.IsDualWielding(affector)) return;
            if (TORSkills.Berserk == null) return;
            if (blow.IsMissile) return;

            // Scale by inflicted damage (parity with the Roguery grant in
            // CareerPerkMissionBehavior); lethal blows add a flat bonus.
            int xp = Math.Max(1, (int)(blow.InflictedDamage * 0.5f));
            if (affected != null && affected.Health < 1f) xp += 20;

            Hero.MainHero.AddSkillXp(TORSkills.Berserk, xp);
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
			    CombatXpModel.MissionTypeEnum.PracticeFight => 0.125f,//native is 0.0625f, recalculated to give 50% xp after adjustment
			    CombatXpModel.MissionTypeEnum.Tournament => 0.4f,//native is 0.33f, recalculated to give 5/6th xp after adjustment
			    CombatXpModel.MissionTypeEnum.SimulationBattle => 0.9f,
			    CombatXpModel.MissionTypeEnum.Battle => 1f,
                _ => 1f,
            };

            return adjustedMultiplier/oldMultiplier;
        }
    }
}