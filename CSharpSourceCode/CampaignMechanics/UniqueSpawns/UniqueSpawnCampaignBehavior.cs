using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.UniqueSpawns
{
    public enum UniqueSpawnState
    {
        Inactive,
        Active,
        RetreatingToHome,
        RetreatedToHome,
        DefeatedCooldown
    }

    public class UniqueSpawnCampaignBehavior : CampaignBehaviorBase
    {
        public const int WarPlanOwnVillagePatrol = 0;
        public const int WarPlanEnemyDeepPatrol = 1;
        public const int WarPlanEnemyVillageRaid = 2;
        public const int WarPlanHomeSiegePatrol = 3;

        public const float UniqueSpawnHealingFactor = 9f;

        public override void RegisterEvents()
        {
            CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, AiHourlyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void AiHourlyTick(MobileParty party, PartyThinkParams thinkParams)
        {
            if (!party.IsUniqueSpawn())
            {
                return;
            }

            BlockTownAndSiegePlans(thinkParams);
        }

        private void BlockTownAndSiegePlans(PartyThinkParams thinkParams)
        {
            const float blockedPlanScore = -10000f;

            foreach (var behaviorScore in thinkParams.AIBehaviorScores.ToList())
            {
                if (!IsTownOrSiegePlan(behaviorScore.Item1.AiBehavior))
                {
                    continue;
                }

                // decision is blocked before its tasked
                thinkParams.SetBehaviorScore(behaviorScore.Item1, blockedPlanScore);
            }
        }

        private bool IsTownOrSiegePlan(AiBehavior aiBehavior)
        {
            return aiBehavior == AiBehavior.GoToSettlement ||
                   aiBehavior == AiBehavior.AssaultSettlement ||
                   aiBehavior == AiBehavior.BesiegeSettlement ||
                   aiBehavior == AiBehavior.DefendSettlement;
        }

        public static void AddOrUpdateBehaviorScore(PartyThinkParams thinkParams, AIBehaviorData behavior, float score)
        {
            // preferences, not orders. native can still override these decisions for immediate actions. unlike retreating to oak of ages
            if (thinkParams.TryGetBehaviorScore(behavior, out var existingScore))
            {
                thinkParams.SetBehaviorScore(behavior, existingScore > score ? existingScore : score);
                return;
            }

            thinkParams.AddBehaviorScore(new ValueTuple<AIBehaviorData, float>(behavior, score));
        }

        public static int PickWarPlanMode(
            float HomeFactionStrength,
            int lootableVillageCount,
            int weakStrengthThreshold,
            int manyLootableVillagesThreshold,
            int weakOwnVillagePatrolWeight,
            int weakEnemyDeepPatrolWeight,
            int weakEnemyRaidWeight,
            int strongManyVillagesOwnVillagePatrolWeight,
            int strongManyVillagesEnemyDeepPatrolWeight,
            int strongManyVillagesEnemyRaidWeight,
            int strongFewVillagesOwnVillagePatrolWeight,
            int strongFewVillagesEnemyDeepPatrolWeight,
            int strongFewVillagesEnemyRaidWeight)
        {
            if (HomeFactionStrength < weakStrengthThreshold)
            {
                // if home faction is weakened unique spawns will prioritize protecting their own fiefs
                return PickWeightedWarPlan(
                    weakOwnVillagePatrolWeight,
                    weakEnemyDeepPatrolWeight,
                    weakEnemyRaidWeight);
            }

            if (lootableVillageCount >= manyLootableVillagesThreshold)
            {
                // enough villages exist to harm the enemy by raiding
                return PickWeightedWarPlan(
                    strongManyVillagesOwnVillagePatrolWeight,
                    strongManyVillagesEnemyDeepPatrolWeight,
                    strongManyVillagesEnemyRaidWeight);
            }

            // if home faction is doing well and not enough villages exist to raid, prioritize enemy lords
            return PickWeightedWarPlan(
                strongFewVillagesOwnVillagePatrolWeight,
                strongFewVillagesEnemyDeepPatrolWeight,
                strongFewVillagesEnemyRaidWeight);
        }

        public static int PickWeightedWarPlan(
            int ownVillagePatrolWeight,
            int enemyDeepPatrolWeight,
            int enemyRaidWeight)
        {
            var totalWeight = ownVillagePatrolWeight + enemyDeepPatrolWeight + enemyRaidWeight;
            var roll = MBRandom.RandomInt(totalWeight);

            if (roll < ownVillagePatrolWeight)
            {
                return WarPlanOwnVillagePatrol;
            }

            roll -= ownVillagePatrolWeight;

            if (roll < enemyDeepPatrolWeight)
            {
                return WarPlanEnemyDeepPatrol;
            }

            return WarPlanEnemyVillageRaid;
        }

        public static void RegisterWarPressureForHomeFaction(
            MapEvent mapEvent,
            IFaction HomeFaction,
            Dictionary<string, float> warPressureByFaction)
        {
            if (warPressureByFaction == null)
            {
                return;
            }

            var attackerSide = mapEvent.GetMapEventSide(BattleSideEnum.Attacker);
            var defenderSide = mapEvent.GetMapEventSide(BattleSideEnum.Defender);

            var affiliatedOnAttackers = SideHasFaction(attackerSide, HomeFaction);
            var affiliatedOnDefenders = SideHasFaction(defenderSide, HomeFaction);

            if (affiliatedOnAttackers == affiliatedOnDefenders)
            {
                return;
            }

            var affiliatedSide = affiliatedOnAttackers ? attackerSide : defenderSide;
            var enemySide = affiliatedOnAttackers ? defenderSide : attackerSide;

            var affiliatedLossScore = GetSideLossPressure(affiliatedSide);
            if (affiliatedLossScore <= 0f)
            {
                return;
            }

            var enemyFactions = enemySide.Parties
                .Select(mapEventParty => mapEventParty.Party?.MapFaction)
                .Where(faction => faction != null && faction != HomeFaction && HomeFaction.IsAtWarWith(faction))
                .GroupBy(faction => faction.StringId)
                .Select(group => group.First())
                .ToList();

            if (enemyFactions.Count == 0)
            {
                return;
            }

            var pressureShare = affiliatedLossScore / enemyFactions.Count;

            foreach (var enemyFaction in enemyFactions)
            {
                if (!warPressureByFaction.ContainsKey(enemyFaction.StringId))
                {
                    warPressureByFaction[enemyFaction.StringId] = 0f;
                }
                warPressureByFaction[enemyFaction.StringId] += pressureShare;
            }
        }

        public static void DecayWarPressure(
            Dictionary<string, float> warPressureByFaction,
            float dailyPressureDecay,
            float forgottenPressureThreshold)
        {
            if (warPressureByFaction == null)
            {
                return;
            }

            foreach (var factionId in warPressureByFaction.Keys.ToList())
            {
                warPressureByFaction[factionId] *= dailyPressureDecay;

                if (warPressureByFaction[factionId] < forgottenPressureThreshold)
                {
                    warPressureByFaction.Remove(factionId);
                }
            }
        }

        public static IFaction PickFactionThatHurtHomeFactionMost(
            IEnumerable<IFaction> validWarFactions,
            Dictionary<string, float> warPressureByFaction,
            Func<IFaction, float> fallbackDistanceScore)
        {
            var warFactions = validWarFactions.ToList();
            if (warFactions.Count == 0)
            {
                return null;
            }

            var pressureTarget = warFactions
                .OrderByDescending(faction => warPressureByFaction != null && warPressureByFaction.TryGetValue(faction.StringId, out var pressure) ? pressure : 0f)
                .FirstOrDefault();

            if (pressureTarget != null &&
                warPressureByFaction != null &&
                warPressureByFaction.TryGetValue(pressureTarget.StringId, out var pressureScore) &&
                pressureScore > 0f)
            {
                return pressureTarget;
            }

            return warFactions
                .OrderBy(fallbackDistanceScore)
                .FirstOrDefault();
        }

        public static IEnumerable<Settlement> AffiliatedBorderVillagesFacing(
            IFaction HomeFaction,
            IFaction targetFaction,
            Func<Settlement, float> distanceToAffiliatedBorder)
        {
            var enemySettlements = EnemyDeepPatrolSettlements(targetFaction, distanceToAffiliatedBorder).ToList();

            return Settlement.All
                .Where(settlement => settlement.IsVillage && settlement.MapFaction == HomeFaction)
                .OrderBy(settlement => enemySettlements
                    .Select(enemySettlement => settlement.Position.DistanceSquared(enemySettlement.Position))
                    .DefaultIfEmpty(float.MaxValue)
                    .Min());
        }

        public static IEnumerable<Settlement> EnemyDeepPatrolSettlements(
            IFaction targetFaction,
            Func<Settlement, float> distanceToAffiliatedBorder)
        {
            return Settlement.All
                .Where(settlement => settlement.MapFaction == targetFaction && (settlement.IsVillage || settlement.IsTown || settlement.IsCastle))
                .OrderBy(distanceToAffiliatedBorder);
        }
        public static IEnumerable<Settlement> EnemyLootableVillages(
            IFaction targetFaction,
            Func<Settlement, bool> canRaidVillage,
            Func<Settlement, float> distanceToAffiliatedBorder)
        {
            return Settlement.All
                .Where(settlement => settlement.MapFaction == targetFaction && canRaidVillage(settlement))
                .OrderBy(distanceToAffiliatedBorder);
        }
        public static Settlement BesiegedOwnedOriginalHomeFief(
            IFaction ownerFaction,
            Func<Settlement, bool> isOriginalHomeFief,
            Func<Settlement, float> distanceToAffiliatedBorder)
        {
            // will only help original home fiefs
            return Settlement.All
                .Where(settlement => IsBesiegedOwnedOriginalHomeFief(settlement, ownerFaction, isOriginalHomeFief))
                .OrderBy(distanceToAffiliatedBorder)
                .FirstOrDefault();
        }

        public static bool IsBesiegedOwnedOriginalHomeFief(
            Settlement settlement,
            IFaction ownerFaction,
            Func<Settlement, bool> isOriginalHomeFief)
        {
            return settlement != null &&
                   (settlement.IsTown || settlement.IsCastle) &&
                   settlement.MapFaction == ownerFaction &&
                   isOriginalHomeFief(settlement) &&
                   settlement.SiegeEvent != null;
        }

        public static bool IsOwnedOriginalHomeFief(
            Settlement settlement,
            IFaction ownerFaction,
            Func<Settlement, bool> isOriginalHomeFief)
        {
            return settlement != null &&
                   (settlement.IsTown || settlement.IsCastle) &&
                   settlement.MapFaction == ownerFaction &&
                   isOriginalHomeFief(settlement);
        }

        public static Settlement ClosestSettlementToAffiliatedBorder(
            IEnumerable<Settlement> settlements,
            Func<Settlement, float> distanceToAffiliatedBorder)
        {
            return settlements
                .OrderBy(distanceToAffiliatedBorder)
                .FirstOrDefault();
        }

        public static float DistanceToHomeFactionBorder(
            IFaction HomeFaction,
            Settlement fallbackAnchorSettlement,
            Settlement targetSettlement)
        {
            return Settlement.All
                .Where(settlement => settlement.IsVillage && settlement.MapFaction == HomeFaction)
                .Select(settlement => settlement.Position.DistanceSquared(targetSettlement.Position))
                .DefaultIfEmpty(fallbackAnchorSettlement.Position.DistanceSquared(targetSettlement.Position))
                .Min();
        }

        public static Settlement CurrentWarTarget(
            string primarySettlementId,
            string secondarySettlementId,
            int targetSwapIndex)
        {
            var primaryTarget = string.IsNullOrWhiteSpace(primarySettlementId)
                ? null
                : Settlement.Find(primarySettlementId);

            var secondaryTarget = string.IsNullOrWhiteSpace(secondarySettlementId)
                ? null
                : Settlement.Find(secondarySettlementId);

            if (primaryTarget == null)
            {
                return secondaryTarget;
            }

            if (secondaryTarget == null)
            {
                return primaryTarget;
            }

            return targetSwapIndex % 2 == 0
                ? primaryTarget
                : secondaryTarget;
        }

        public static IEnumerable<IFaction> WarMirrorTargets(Clan uniqueSpawnClan, Kingdom affiliatedKingdom)
        {
            foreach (var kingdom in Kingdom.All.Where(kingdom => kingdom != affiliatedKingdom && !kingdom.IsEliminated))
            {
                yield return kingdom;
            }

            foreach (var clan in Clan.NonBanditFactions.Where(clan =>
                         clan != uniqueSpawnClan &&
                         clan.MapFaction == clan &&
                         clan.Kingdom != affiliatedKingdom &&
                         !clan.IsEliminated))
            {
                yield return clan;
            }
        }

        private static bool SideHasFaction(MapEventSide side, IFaction faction)
        {
            return side.Parties.Any(mapEventParty => mapEventParty.Party?.MapFaction == faction);
        }

        private static float GetSideLossPressure(MapEventSide side)
        {
            var totalLossPressure = 0f;

            foreach (var mapEventParty in side.Parties)
            {
                totalLossPressure += mapEventParty.DiedInBattle.TotalManCount * 2f;
                totalLossPressure += mapEventParty.WoundedInBattle.TotalManCount;
            }

            return totalLossPressure;
        }
    }
}