using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TOR_Core.CampaignMechanics.Ambush;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.Models.Ambush
{
    /// <summary>
    /// Campaign-map ambush math: entry, strike, detection, cooldown, and AI propensity.
    /// </summary>
    public class TORAmbushModel : GameModel
    {

        // Tunable knobs. Overridable via subclass for balance mods.
        public virtual float BaseAmbushChance => 0.5f;
        public virtual float MinChance => 0.1f;
        public virtual float MaxChance => 0.95f;
        public virtual float ScoutingPerPointBonus => 0.002f;
        public virtual float ScoutingBonusCap => 0.2f;
        public virtual float StealthDurationPerHour => 0.05f;
        public virtual float StealthDurationCap => 0.15f;
        public virtual float ConcealingTerrainBonus => 0.15f;
        public virtual float OpenTerrainPenalty => -0.10f;
        public virtual float SmallAttackerBonus => 0.2f;
        public virtual float SlightlySmallerAttackerBonus => 0.1f;
        public virtual float OverwhelmingAttackerPenalty => -0.1f;
        public virtual float EdgeProximityMultiplier => 0.5f;
        public virtual float NightTimeBonus => 0.1f;
        public virtual float BaseEntryChance => 0.5f;
        public virtual float MinEntryChance => 0.05f;
        public virtual float MaxEntryChance => 0.98f;
        public virtual float EntryHostilePenaltyPerParty => 0.12f;
        public virtual float EntryHostilePenaltyCap => 0.4f;
        public virtual float EntryHostileScoutingPenaltyPerPoint => 0.0005f;
        public virtual float EntryRogueryPerPointBonus => 0.001f;
        public virtual float EntryRogueryBonusCap => 0.15f;
        public virtual float EntryPartyStealthScoreWeight => 0.3f;
        public virtual float PartyStealthScoreBaseline => 0.5f;
        public virtual int RogueryXpBasePerHiddenTick => 5;
        public virtual int RogueryXpPerHostileInRange => 1;
        public virtual int RogueryXpPerHostileCap => 5;
        public virtual int RogueryXpScoutingDivisor => 50;
        public virtual int RogueryXpTotalVictoryBase => 50;
        public virtual float RogueryXpTotalVictoryPerMember => 1f;
        public virtual float BaseDetectionChancePerTick => 0.05f;
        public virtual float MinDetectionChance => 0f;
        public virtual float MaxDetectionChance => 0.45f;
        public virtual float DetectionScanRadius => 10f;
        public virtual float AlliedRevealRadius => 12f;
        public virtual int DetectionDisplayHorizonTicks => 4;
        public virtual float AdjacentDistanceMultiplier => 2.5f;
        public virtual float FarDistanceMultiplier => 0.2f;
        public virtual float SpottingScoreDetectionWeight => 0.2f;
        public virtual float SpottingScoreSightingWeight => 0.1f;
        public virtual float SpottingScoreSizeDampingReference => 100f;
        public virtual float ConcealingTerrainMultiplier => 0.5f;
        public virtual float OpenTerrainMultiplier => 1.5f;
        public virtual float NightDetectionMultiplier => 0.7f;
        public virtual float StealthDurationDetectionPerHour => 0.1f;
        public virtual float StealthDurationDetectionCap => 0.5f;
        public virtual float SettlementProximityRadius => 8f;
        public virtual float HostileSettlementEntryPenalty => 0.2f;
        public virtual float NeutralSettlementEntryPenalty => 0.08f;
        public virtual float HostileSettlementDetectionMultiplier => 1.6f;
        public virtual float NeutralSettlementDetectionMultiplier => 1.1f;
        public virtual float SkillPairLowerWeight => 0.5f;
        public virtual float ContestScoutingWeight => 1.0f;
        public virtual float ContestNormalizer => 300f;
        public virtual float BaseStrikeRange => 2.6f;
        public virtual float StrikeRangeScoutingPerPoint => 0.0273f;
        public virtual float MaxStrikeRange => 10.5f;
        public virtual float AmbushPressBaseThreshold => 1.0f;
        public virtual float AmbushedFledVanishMoralePenalty => 5f;
        public virtual float AmbushedFledDoubleFleeExtraMoralePenalty => 20f;
        public virtual float AmbushedFledWoundShare => 0.3f;
        public virtual float AmbushedDoubleFleeRemoveShare => 0.3f;
        public virtual float BasePreparationMinutes => 15f;
        public virtual int SmallPartyThreshold => 25;
        public virtual float PerTroopMinutes => 0.1f;
        public virtual float MinimumConcealmentFactor => 0.3f;
        public virtual float AiPrepPursuitRejectionRadius => 25f;
        public virtual float PlayerDetectionRevealHours => 2f;
        public virtual float PreparationScoutPerPointReduction => 0.002f;
        public virtual float MinPreparationScoutMultiplier => 0.3f;
        public virtual float AmbushGuardingHours => 3f;
        public virtual float InstantAmbushWindowHours => 2f;
        public virtual int BluffVisibleThreshold => 5;
        public virtual float DefaultCulturePropensity => 0f;
        public virtual float AiDecisionTickFraction => 0.1f;
        public virtual float AmbushSkirmishStrikePowerFraction => 0.5f;
        public virtual float AmbushSkirmishVictimCasualtyFraction => 0.4f;
        public virtual float AmbushSkirmishAmbusherCasualtyFraction => 0.1f;
        public virtual float AmbushFollowupThreshold => 1.5f;
        public virtual float AiMinCommitChance => 0.35f;
        public virtual float AiOpportunityScanRadius => 25f;
        public virtual float AiMinEntryViabilityChance => 0.3f;
        public virtual float FriendlyStealthClusterRadius => 15f;
        public virtual float FriendlyStealthClusterPenaltyPerParty => 0.1f;
        public virtual float FriendlyStealthClusterPenaltyCap => 0.4f;
        public virtual float DynamicStealthRogueryPerPoint => 0.001f;
        public virtual float DynamicStealthRogueryCap => 0.3f;
        public virtual float TierCriticalMargin => 0.25f;
        public virtual float TierTerribleMargin => 0.25f;
        public virtual float HomelandScanRadius => 25f;
        public virtual float HomelandForeignMultiplier => 0.5f;
        public virtual float HomelandOwnMultiplier => 1.5f;
        public virtual float TraitStepWeight => 0.1f;
        public virtual float TraitMultiplierMin => 0.4f;
        public virtual float TraitMultiplierMax => 1.6f;

        /// <summary>
        /// Which parties count as observers for detection rolls. Hostile parties
        /// always detect; neutral caravans also detect because they are
        /// mobile civilian traffic that notices and talks.
        /// </summary>
        public virtual bool CanDetect(MobileParty observer, MobileParty target)
        {
            if (observer == null || target == null || observer == target) return false;
            if (observer.MapFaction == null || target.MapFaction == null) return false;
            if (FactionManager.IsAtWarAgainstFaction(observer.MapFaction, target.MapFaction))
                return true;
            return observer.IsCaravan;
        }

        /// <summary>
        /// Single-observer detection roll. Returns true and fills reason on a hit.
        /// </summary>
        public virtual bool TryDetect(MobileParty observer, MobileParty target,
                                       CampaignTime stealthEnteredAt, out string reason)
        {
            reason = null;
            if (!CanDetect(observer, target)) return false;

            float distance = observer.Position.ToVec2().Distance(target.Position.ToVec2());
            if (distance > DetectionScanRadius) return false;

            float chance = CalculateDetectionChancePerTick(observer, target, stealthEnteredAt);
            if (MBRandom.RandomFloat >= chance) return false;

            reason = BuildDetectionReason(observer, target, chance);
            return true;
        }

        /// <summary>
        /// Per-tick detection chance for a single observer, clamped.
        /// </summary>
        public virtual float CalculateDetectionChancePerTick(MobileParty observer, MobileParty target, CampaignTime stealthEnteredAt)
        {
            float chance = BaseDetectionChancePerTick
                * DistanceDetectionMultiplier(observer, target)
                * ScoutingDetectionMultiplier(observer)
                * TerrainDetectionMultiplier(target)
                * NightDetectionMultiplierValue()
                * StealthDurationDetectionMultiplier(stealthEnteredAt)
                * SettlementDetectionMultiplier(target);
            return MBMath.ClampFloat(chance, MinDetectionChance, MaxDetectionChance);
        }

        /// <summary>
        /// Combined per-tick detection across all nearby observers using independent-Bernoulli composition.
        /// </summary>
        public virtual float CalculateAggregatedDetectionChancePerTick(
            MobileParty target, IReadOnlyList<MobileParty> observers, CampaignTime stealthEnteredAt)
        {
            if (target == null || observers == null || observers.Count == 0) return 0f;
            float survival = 1f;
            foreach (var observer in observers)
            {
                float p = CalculateDetectionChancePerTick(observer, target, stealthEnteredAt);
                survival *= (1f - p);
            }
            return MBMath.ClampFloat(1f - survival, 0f, 1f);
        }

        /// <summary>
        /// Aggregated per-tick detection extrapolated over <see cref="DetectionDisplayHorizonTicks"/>.
        /// </summary>
        public virtual float CalculateAggregatedDetectionChanceOverHorizon(
            MobileParty target, IReadOnlyList<MobileParty> observers, CampaignTime stealthEnteredAt)
        {
            float pTick = CalculateAggregatedDetectionChancePerTick(target, observers, stealthEnteredAt);
            int horizon = DetectionDisplayHorizonTicks;
            if (horizon <= 1 || pTick <= 0f) return pTick;
            return MBMath.ClampFloat(1f - MathF.Pow(1f - pTick, horizon), 0f, 1f);
        }

        /// <summary>
        /// Settlement-proximity multiplier on per-tick detection chance. 1.0 when no settlement is in range.
        /// </summary>
        protected virtual float SettlementDetectionMultiplier(MobileParty target)
        {
            if (target?.MapFaction == null) return 1f;
            switch (GetNearbySettlementKind(target))
            {
                case SettlementProximityKind.Hostile: return HostileSettlementDetectionMultiplier;
                case SettlementProximityKind.Neutral: return NeutralSettlementDetectionMultiplier;
                default: return 1f;
            }
        }

        /// <summary>
        /// Settlement-proximity penalty on the entry-chance roll. 0 when no settlement is in range.
        /// </summary>
        protected virtual float SettlementEntryPenalty(MobileParty self)
        {
            if (self?.MapFaction == null) return 0f;
            float penalty;
            switch (GetNearbySettlementKind(self))
            {
                case SettlementProximityKind.Hostile: penalty = HostileSettlementEntryPenalty; break;
                case SettlementProximityKind.Neutral: penalty = NeutralSettlementEntryPenalty; break;
                default: return 0f;
            }
            // Shakedown perk: reduce village-vicinity penalty.
            if (self.HasPerk(TORPerks.Roguery.Shakedown)
                && IsNearestInRangeAVillage(self))
            {
                penalty *= 1f + TORPerks.Roguery.Shakedown.SecondaryBonus;
            }
            return penalty;
        }

        /// <summary>
        /// True when the closest in-range settlement is a village.
        /// </summary>
        protected virtual bool IsNearestInRangeAVillage(MobileParty self)
        {
            var nearest = TORCommon.FindNearestSettlement(self, SettlementProximityRadius, _ => true);
            return nearest != null && nearest.IsVillage;
        }

        /// <summary>
        /// Classify any settlement inside the proximity radius. Hostile wins over neutral when both are present.
        /// </summary>
        public virtual SettlementProximityKind GetNearbySettlementKind(MobileParty party)
        {
            if (party?.MapFaction == null) return SettlementProximityKind.None;
            if (TORCommon.FindNearestSettlement(party, SettlementProximityRadius,
                    s => s.MapFaction != null &&
                         FactionManager.IsAtWarAgainstFaction(s.MapFaction, party.MapFaction)) != null)
                return SettlementProximityKind.Hostile;

            if (TORCommon.FindNearestSettlement(party, SettlementProximityRadius,
                    s => s.MapFaction != null &&
                         s.MapFaction != party.MapFaction &&
                         !FactionManager.IsAtWarAgainstFaction(s.MapFaction, party.MapFaction)) != null)
                return SettlementProximityKind.Neutral;

            return SettlementProximityKind.None;
        }

        protected virtual float DistanceDetectionMultiplier(MobileParty observer, MobileParty target)
        {
            float d = observer.Position.ToVec2().Distance(target.Position.ToVec2());
            if (d <= 0.1f) return AdjacentDistanceMultiplier;
            if (d >= DetectionScanRadius) return FarDistanceMultiplier;
            return MBMath.Lerp(AdjacentDistanceMultiplier, FarDistanceMultiplier, d / DetectionScanRadius);
        }

        protected virtual float ScoutingDetectionMultiplier(MobileParty observer)
        {
            int s = GetDefenderScouting(observer);
            float scoutingBonus = TORSkillEffects.AmbushSpottingBonus.GetSkillEffectValue(s);
            float rosterBonus = AmbushTroopSpottingScore.GetBasePartyScore(observer, SpottingScoreSizeDampingReference)
                                * SpottingScoreDetectionWeight;
            return 1f + scoutingBonus + rosterBonus;
        }

        protected virtual float TerrainDetectionMultiplier(MobileParty target)
        {
            var face = target.CurrentNavigationFace;
            if (!face.IsValid()) return 1f;
            var terrain = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(face);
            switch (terrain)
            {
                case TerrainType.Forest:
                case TerrainType.Mountain:
                case TerrainType.Canyon:
                case TerrainType.Swamp:
                    return ConcealingTerrainMultiplier;
                case TerrainType.Plain:
                case TerrainType.Desert:
                case TerrainType.Steppe:
                case TerrainType.Dune:
                    return OpenTerrainMultiplier;
                default:
                    return 1f;
            }
        }

        protected virtual float NightDetectionMultiplierValue()
        {
            if (Campaign.Current == null) return 1f;
            return Campaign.Current.IsNight ? NightDetectionMultiplier : 1f;
        }

        protected virtual float StealthDurationDetectionMultiplier(CampaignTime enteredAt)
        {
            if (enteredAt == CampaignTime.Zero) return 1f;
            float hours = (float)(CampaignTime.Now - enteredAt).ToHours;
            return 1f + MBMath.ClampFloat(hours * StealthDurationDetectionPerHour, 0f, StealthDurationDetectionCap);
        }

        protected virtual string BuildDetectionReason(MobileParty observer, MobileParty target, float chance)
        {
            string who = observer.LeaderHero?.Name?.ToString() ?? observer.Name?.ToString() ?? "a hostile party";
            return "You've been spotted by " + who + ".";
        }

        // Skill contest (party-level).

        /// <summary>
        /// Weights for the ambush success contest. Roguery dominates on the ambusher side.
        /// </summary>
        public virtual float ContestRogueryWeight  => 1.5f;
        public virtual float ContestTacticsWeight  => 0.7f;

        /// <summary>
        /// Combine two heroes' skill values; higher full-weight, lower contributes <see cref="SkillPairLowerWeight"/>.
        /// </summary>
        public virtual int CombineSkillPair(int a, int b)
        {
            int high = a > b ? a : b;
            int low = a > b ? b : a;
            return high + (int)(low * SkillPairLowerWeight);
        }

        /// <summary>
        /// Effective Roguery for the ambusher side (leader + Scout-role hero).
        /// </summary>
        public virtual int GetAmbusherRoguery(MobileParty party)
        {
            if (party == null) return 0;
            int leader = party.LeaderHero?.GetSkillValue(DefaultSkills.Roguery) ?? 0;
            int scout = party.EffectiveScout?.GetSkillValue(DefaultSkills.Roguery) ?? 0;
            return CombineSkillPair(leader, scout);
        }

        /// <summary>
        /// Effective Scouting for the ambusher side; drives strike range.
        /// </summary>
        public virtual int GetAmbusherScouting(MobileParty party)
        {
            if (party == null) return 0;
            int leader = party.LeaderHero?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
            int scout = party.EffectiveScout?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
            return CombineSkillPair(leader, scout);
        }

        /// <summary>
        /// Effective Scouting for the defender side; drives detection.
        /// </summary>
        public virtual int GetDefenderScouting(MobileParty party)
        {
            if (party == null) return 0;
            int leader = party.LeaderHero?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
            int scout = party.EffectiveScout?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
            return CombineSkillPair(leader, scout);
        }

        /// <summary>
        /// Leader-only Tactics skill for defender terrain reading.
        /// </summary>
        public virtual int GetDefenderTactics(MobileParty party)
            => party?.LeaderHero?.GetSkillValue(DefaultSkills.Tactics) ?? 0;

        /// <summary>
        /// Dynamic strike range scaled by ambusher Scouting.
        /// </summary>
        public virtual float CalculateStrikeRange(MobileParty ambusher)
        {
            int scouting = GetAmbusherScouting(ambusher);
            float range = BaseStrikeRange + scouting * StrikeRangeScoutingPerPoint;
            if (range > MaxStrikeRange) range = MaxStrikeRange;
            return range;
        }

        /// <summary>
        /// Ambush success chance for an (ambusher, target) pair. Combines skill contest, terrain, force ratio, duration, night, and proximity.
        /// </summary>
        public virtual float CalculateAmbushSuccessChance(MobileParty attacker, MobileParty target, CampaignTime stealthEnteredAt)
        {
            if (attacker == null || target == null) return MinChance;

            float chance = BaseAmbushChance;
            chance += SkillContestBonus(attacker, target);
            chance += TerrainBonus(attacker);
            chance += ForceRatioBonus(attacker, target);
            chance += StealthDurationBonus(stealthEnteredAt);
            chance += NightTimeBonusValue();
            // Proximity is multiplicative, applied last.
            chance *= ProximityMultiplier(attacker, target);
            return MBMath.ClampFloat(chance, MinChance, MaxChance);
        }

        /// <summary>
        /// Skill contest: ambusher Roguery vs defender Scouting + Tactics, normalized and clamped.
        /// </summary>
        protected virtual float SkillContestBonus(MobileParty attacker, MobileParty target)
        {
            float attackerScore = GetAmbusherRoguery(attacker) * ContestRogueryWeight;
            float defenderScore = GetDefenderScouting(target) * ContestScoutingWeight
                                + GetDefenderTactics(target)  * ContestTacticsWeight;
            float delta = (attackerScore - defenderScore) / ContestNormalizer;
            return MBMath.ClampFloat(delta, -ScoutingBonusCap, ScoutingBonusCap);
        }

        protected virtual float ProximityMultiplier(MobileParty attacker, MobileParty target)
        {
            float strikeRange = CalculateStrikeRange(attacker);
            if (strikeRange <= 0f) return 1f;
            float distance = attacker.Position.ToVec2().Distance(target.Position.ToVec2());
            // Linear fade from 1.0 to EdgeProximityMultiplier across strike range.
            float t = MBMath.ClampFloat(distance / strikeRange, 0f, 1f);
            return MBMath.Lerp(1f, EdgeProximityMultiplier, t);
        }

        protected virtual float NightTimeBonusValue()
        {
            if (Campaign.Current == null) return 0f;
            return Campaign.Current.IsNight ? NightTimeBonus : 0f;
        }

        protected virtual float TerrainBonus(MobileParty attacker)
        {
            var face = attacker.CurrentNavigationFace;
            if (!face.IsValid()) return 0f;

            var terrain = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(face);
            switch (terrain)
            {
                case TerrainType.Forest:
                case TerrainType.Mountain:
                case TerrainType.Canyon:
                case TerrainType.Swamp:
                    return ConcealingTerrainBonus;
                case TerrainType.Plain:
                case TerrainType.Desert:
                case TerrainType.Steppe:
                case TerrainType.Dune:
                    return OpenTerrainPenalty;
                default:
                    return 0f;
            }
        }

        protected virtual float ForceRatioBonus(MobileParty attacker, MobileParty target)
        {
            int a = attacker.MemberRoster.TotalManCount;
            int t = target.MemberRoster.TotalManCount;
            if (t == 0) return 0f;

            float ratio = (float)a / t;
            if (ratio < 0.5f) return SmallAttackerBonus;
            if (ratio < 1.0f) return SlightlySmallerAttackerBonus;
            if (ratio < 2.0f) return 0f;
            return OverwhelmingAttackerPenalty;
        }

        protected virtual float StealthDurationBonus(CampaignTime enteredAt)
        {
            if (enteredAt == CampaignTime.Zero) return 0f;
            float hours = (float)(CampaignTime.Now - enteredAt).ToHours;
            return MBMath.ClampFloat(hours * StealthDurationPerHour, 0f, StealthDurationCap);
        }

        /// <summary>
        /// Chance a party successfully slips into stealth stance. Factors Roguery, terrain, night, observers, settlements.
        /// </summary>
        public virtual float CalculateEntryChance(MobileParty self,
            IReadOnlyList<MobileParty> observers)
        {
            if (self == null) return MinEntryChance;

            // Instant-ambush window: stealth entry guaranteed.
            var ai = Campaign.Current?.GetCampaignBehavior<CampaignMechanics.Ambush.AiAmbushBehavior>();
            var latest = ai?.GetMostRecentRecordInvolving(self, InstantAmbushWindowHours);
            if (IsInstantAmbushAllowed(latest)) return 1f;

            float chance = BaseEntryChance
                + EntryRogueryBonus(self)
                + EntryPartyStealthScoreBonus(self)
                + EntryTerrainBonus(self)
                + EntryNightBonus()
                - EntryHostilePenalty(observers)
                - SettlementEntryPenalty(self);
            return MBMath.ClampFloat(chance, MinEntryChance, MaxEntryChance);
        }

        /// <summary>
        /// Pair-combined Roguery contribution to entry chance, capped.
        /// </summary>
        protected virtual float EntryRogueryBonus(MobileParty self)
        {
            int r = GetAmbusherRoguery(self);
            return MBMath.ClampFloat(r * EntryRogueryPerPointBonus, 0f, EntryRogueryBonusCap);
        }

        /// <summary>
        /// Party-roster stealth suitability contribution; can be negative for heavy rosters.
        /// </summary>
        protected virtual float EntryPartyStealthScoreBonus(MobileParty self)
        {
            float score = AmbushTroopStealthScore.GetBasePartyScore(self)
                        + GetDynamicStealthBonus(self);
            return (score - PartyStealthScoreBaseline) * EntryPartyStealthScoreWeight;
        }

        protected virtual float EntryTerrainBonus(MobileParty self)
        {
            var face = self.CurrentNavigationFace;
            if (!face.IsValid()) return 0f;
            var terrain = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(face);
            switch (terrain)
            {
                case TerrainType.Forest:
                case TerrainType.Mountain:
                case TerrainType.Canyon:
                case TerrainType.Swamp:
                    return ConcealingTerrainBonus;
                case TerrainType.Plain:
                case TerrainType.Desert:
                case TerrainType.Steppe:
                case TerrainType.Dune:
                    return OpenTerrainPenalty;
                default:
                    return 0f;
            }
        }

        protected virtual float EntryNightBonus()
        {
            if (Campaign.Current == null) return 0f;
            return Campaign.Current.IsNight ? NightTimeBonus : 0f;
        }

        protected virtual float EntryHostilePenalty(IReadOnlyList<MobileParty> observers)
        {
            if (observers == null || observers.Count == 0) return 0f;
            int totalScouting = 0;
            foreach (var o in observers)
                totalScouting += o?.LeaderHero?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
            float raw = observers.Count * EntryHostilePenaltyPerParty
                        + totalScouting * EntryHostileScoutingPenaltyPerPoint;
            return MBMath.ClampFloat(raw, 0f, EntryHostilePenaltyCap);
        }

        // Ambusher-presses-after-flee follow-up.

        /// <summary>
        /// True if the AI ambusher presses the attack on the campaign map after the victim fled.
        /// </summary>
        public virtual bool AmbusherPressesAttack(MobileParty ambusher, MobileParty player)
        {
            if (ambusher == null || player == null) return false;
            float ambusherStrength = ambusher.Party?.EstimatedStrength ?? 0f;
            float playerStrength = player.Party?.EstimatedStrength ?? 0f;
            if (playerStrength <= 0f) return true;
            if (ambusherStrength <= 0f) return false;
            float strengthRatio = ambusherStrength / playerStrength;
            float threshold = AmbushPressBaseThreshold
                            / System.Math.Max(HomelandMultiplier(ambusher), 0.1f)
                            / System.Math.Max(TraitAmbushMultiplier(ambusher.LeaderHero), 0.1f);
            return strengthRatio >= threshold;
        }

        // Preparation time.

        /// <summary>
        /// Game-minutes needed to set up an ambush given party size and stealth score.
        /// </summary>
        public virtual float CalculatePreparationMinutes(MobileParty party)
        {
            if (party == null) return BasePreparationMinutes;
            int size = party.MemberRoster?.TotalHealthyCount ?? 0;

            int excess = System.Math.Max(0, size - SmallPartyThreshold);
            float sizeMinutes = excess * PerTroopMinutes;

            float stealthScore = AmbushTroopStealthScore.GetBasePartyScore(party);
            float concealmentFactor = System.Math.Max(MinimumConcealmentFactor,
                                                      2f - stealthScore);

            float scoutMultiplier = GetPreparationScoutMultiplier(party);
            return (BasePreparationMinutes + sizeMinutes) * concealmentFactor * scoutMultiplier;
        }

        /// <summary>
        /// Roguery-driven prep multiplier. Returns 1.0 for leaderless / zero-skill parties.
        /// </summary>
        public virtual float GetPreparationScoutMultiplier(MobileParty party)
        {
            int rog = GetAmbusherRoguery(party);
            if (rog <= 0) return 1f;
            float mult = 1f - rog * PreparationScoutPerPointReduction;
            if (mult < MinPreparationScoutMultiplier) mult = MinPreparationScoutMultiplier;
            return mult;
        }

        // Record cooldown.

        /// <summary>
        /// Game-hours an <see cref="AmbushRecord"/> keeps a party on cooldown; role-sensitive.
        /// </summary>
        public virtual float GetAmbushCooldownHours(AmbushRecord record)
        {
            // Silent successful strike: zero cooldown, stealth re-arms.
            if (record != null && IsSilentSuccessfulStrike(record))
            {
                return 0f;
            }

            if (record != null
                && record.Victim == MobileParty.MainParty
                && !record.AmbusherRepelled)
            {
                return AmbushGuardingHours;
            }

            float hours = AmbushCampaignBehavior.CooldownHours;
            if (record != null && record.WasLoud)
            {
                var missionModel = Campaign.Current?.Models?.GetMissionAmbushModel();
                if (missionModel != null) hours += missionModel.LoudnessPenaltyHours;
            }
            // Reprisal perk shortens post-failure cooldown via a negative PrimaryBonus.
            if (record != null && record.Ambusher != null
                && record.Ambusher.HasPerk(TORPerks.Roguery.Reprisal))
            {
                hours *= 1f + TORPerks.Roguery.Reprisal.PrimaryBonus;
            }
            return hours;
        }

        /// <summary>
        /// True when the record represents a landed strike (ambusher held the field or scored partial kills).
        /// </summary>
        public virtual bool IsSuccessfulStrike(AmbushRecord record)
        {
            if (record == null) return false;
            switch (record.Resolution)
            {
                case AmbushResolution.AmbusherTotalVictory:
                case AmbushResolution.AmbusherPartialVictory:
                case AmbushResolution.VictimDefeated:
                case AmbushResolution.VictimFled:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// True when the strike landed silently (no loud signature, no witnesses).
        /// </summary>
        public virtual bool IsSilentSuccessfulStrike(AmbushRecord record)
            => IsSuccessfulStrike(record) && !record.WasLoud;

        /// <summary>
        /// True when the latest record makes the player eligible for an instant re-ambush.
        /// </summary>
        public virtual bool IsInstantAmbushAllowed(AmbushRecord record)
            => record != null
               && record.Resolution == AmbushResolution.AmbusherTotalVictory
               && !record.WasLoud
               && !record.AmbusherLeaderDown
               && (CampaignTime.Now - record.At).ToHours < InstantAmbushWindowHours;

        /// <summary>
        /// True when the scoreboard should render a row's counts as "?" instead of real numbers.
        /// </summary>
        public virtual bool ShouldMaskScoreboardRow(bool isRowOnPlayerSide)
        {
            if (isRowOnPlayerSide) return false;
            var mission = Mission.Current;
            if (mission?.PlayerTeam == null) return false;
            // Bluff mask only applies when the player is the ambushed defender.
            if (mission.PlayerTeam.Side != BattleSideEnum.Defender) return false;
            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (behavior == null || !behavior.IsCurrentMissionBluffed) return false;
            // Lift the mask once enemy visible count drops to threshold.
            var enemySide = mission.PlayerTeam.Side.GetOppositeSide();
            int visible = 0;
            foreach (var t in mission.Teams)
            {
                if (t == null || t.Side != enemySide) continue;
                foreach (var a in t.ActiveAgents)
                {
                    if (a != null && a.IsHuman && a.IsActive()) visible++;
                }
            }
            return visible > BluffVisibleThreshold;
        }

        // AI-vs-AI skirmish.

        /// <summary>
        /// Cluster penalty to subtract from entry chance, clamped.
        /// </summary>
        public virtual float FriendlyStealthClusterPenalty(int nearbyStealthedFriendlies)
        {
            if (nearbyStealthedFriendlies <= 0) return 0f;
            float raw = nearbyStealthedFriendlies * FriendlyStealthClusterPenaltyPerParty;
            return MBMath.ClampFloat(raw, 0f, FriendlyStealthClusterPenaltyCap);
        }

        /// <summary>
        /// Runtime stealth-score delta for a party, additive on top of the precomputed base score.
        /// </summary>
        public virtual float GetDynamicStealthBonus(MobileParty party)
        {
            if (party == null) return 0f;
            // Combined leader + Scout Roguery as an additive stealth bonus, capped.
            int rog = GetAmbusherRoguery(party);
            if (rog <= 0) return 0f;
            float bonus = rog * DynamicStealthRogueryPerPoint;
            if (bonus > DynamicStealthRogueryCap) bonus = DynamicStealthRogueryCap;
            return bonus;
        }

        /// <summary>
        /// AI-only base-tier (Normal success) skirmish casualties for the
        /// (ambusher, victim) pair. Player-involved ambushes resolve in the
        /// ambush mission; this abstract roll runs only when both sides are AI.
        /// </summary>
        public virtual (int ambusherWounds, int victimWounds) CalculateAiSkirmishCasualties(
            MobileParty ambusher, MobileParty victim)
            => CalculateAiSkirmishCasualties(ambusher, victim, AmbushOutcomeTier.NormalSuccess);

        /// <summary>
        /// AI-only tier-scaled casualty computation, clamped against available
        /// healthy count on each side. Only called by the AI-vs-AI skirmish
        /// resolver; the player path goes through the ambush mission instead.
        /// </summary>
        public virtual (int ambusherWounds, int victimWounds) CalculateAiSkirmishCasualties(
            MobileParty ambusher, MobileParty victim, AmbushOutcomeTier tier)
        {
            if (ambusher?.MemberRoster == null || victim?.MemberRoster == null)
                return (0, 0);
            int ambusherHealthy = ambusher.MemberRoster.TotalHealthyCount;
            int victimHealthy = victim.MemberRoster.TotalHealthyCount;
            int strikePower = (int)(ambusherHealthy * AmbushSkirmishStrikePowerFraction);
            int baseVictimWounds = (int)(strikePower * AmbushSkirmishVictimCasualtyFraction);
            int baseAmbusherWounds = (int)(ambusherHealthy * AmbushSkirmishAmbusherCasualtyFraction);

            (float ambusherMult, float victimMult) = tier switch
            {
                AmbushOutcomeTier.CriticalSuccess => (0.5f, 1.5f),
                AmbushOutcomeTier.NormalSuccess   => (1.0f, 1.0f),
                AmbushOutcomeTier.MinorFail       => (0.5f, 0.5f),
                AmbushOutcomeTier.TerribleFail    => (1.5f, 0.0f),
                _                                 => (1.0f, 1.0f),
            };

            int victimWounds = System.Math.Min(victimHealthy, (int)(baseVictimWounds * victimMult));
            int ambusherWounds = System.Math.Min(ambusherHealthy, (int)(baseAmbusherWounds * ambusherMult));
            return (ambusherWounds, victimWounds);
        }

        /// <summary>
        /// Pick an outcome tier from a success chance and roll. AI-vs-AI only.
        /// </summary>
        public virtual AmbushOutcomeTier RollOutcomeTier(float chance, float roll)
        {
            float margin = chance - roll;
            if (margin > TierCriticalMargin) return AmbushOutcomeTier.CriticalSuccess;
            if (margin >= 0f) return AmbushOutcomeTier.NormalSuccess;
            if (margin > -TierTerribleMargin) return AmbushOutcomeTier.MinorFail;
            return AmbushOutcomeTier.TerribleFail;
        }

        /// <summary>
        /// True when the ambusher's post-skirmish healthy count clears the follow-up threshold.
        /// </summary>
        public virtual bool ShouldPressAttackAfterSkirmish(MobileParty ambusher, MobileParty victim)
        {
            if (ambusher?.MemberRoster == null || victim?.MemberRoster == null) return false;
            int ambusherHealthy = ambusher.MemberRoster.TotalHealthyCount;
            int victimHealthy = victim.MemberRoster.TotalHealthyCount;
            if (victimHealthy <= 0) return true;
            return ambusherHealthy >= victimHealthy * AmbushFollowupThreshold;
        }

        /// <summary>
        /// Ambush propensity for a given AI party, composed of base value plus homeland and trait multipliers.
        /// </summary>
        public virtual float GetAmbushPropensity(MobileParty party)
        {
            if (party == null) return 0f;
            float basePropensity = GetBaseAmbushPropensity(party);
            if (basePropensity <= 0f) return 0f;
            if (party.IsBandit) return basePropensity;
            return basePropensity
                * HomelandMultiplier(party)
                * TraitAmbushMultiplier(party.LeaderHero);
        }

        /// <summary>
        /// Base propensity before multipliers. Hero override beats culture value.
        /// </summary>
        protected virtual float GetBaseAmbushPropensity(MobileParty party)
        {
            var hero = party.LeaderHero;
            if (hero != null)
            {
                float heroOverride = AmbushPropensityRegistry.GetHeroPropensity(hero);
                if (heroOverride >= 0f) return heroOverride;
            }
            var culture = party.MapFaction?.Culture ?? hero?.Culture;
            if (culture == null) return DefaultCulturePropensity;
            return AmbushPropensityRegistry.GetCulturePropensity(culture, DefaultCulturePropensity);
        }

        // Homeland awareness.

        /// <summary>
        /// Lerp from foreign to own multiplier based on homeland density.
        /// </summary>
        protected virtual float HomelandMultiplier(MobileParty party)
        {
            float density = party.GetHomelandDensity(HomelandScanRadius);
            return MBMath.Lerp(HomelandForeignMultiplier, HomelandOwnMultiplier, density);
        }

        // Personality traits.

        /// <summary>
        /// Combine Calculating (bonus), Honor (penalty), Valor (penalty) traits into a multiplier.
        /// </summary>
        protected virtual float TraitAmbushMultiplier(Hero leader)
        {
            if (leader == null) return 1f;
            int calc = leader.GetTraitLevel(DefaultTraits.Calculating);
            int hon  = leader.GetTraitLevel(DefaultTraits.Honor);
            int val  = leader.GetTraitLevel(DefaultTraits.Valor);
            float swing = (calc - hon - val) * TraitStepWeight;
            return MBMath.ClampFloat(1f + swing, TraitMultiplierMin, TraitMultiplierMax);
        }
    }
}
