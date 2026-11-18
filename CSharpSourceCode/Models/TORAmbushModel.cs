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

namespace TOR_Core.Models
{
    /// <summary>Campaign-map ambush math: entry, strike, detection, cooldown, and AI propensity.</summary>
    public class TORAmbushModel : GameModel
    {
        // Chance clamps.
        public virtual float BaseAmbushChance => 0.5f;
        public virtual float MinChance => 0.1f;
        public virtual float MaxChance => 0.95f;

        // Factor weights.
        public virtual float ScoutingPerPointBonus => 0.002f;
        public virtual float ScoutingBonusCap => 0.2f;
        public virtual float StealthDurationPerHour => 0.05f;
        public virtual float StealthDurationCap => 0.15f;

        public virtual float ConcealingTerrainBonus => 0.15f;
        public virtual float OpenTerrainPenalty => -0.10f;

        public virtual float SmallAttackerBonus => 0.2f;
        public virtual float SlightlySmallerAttackerBonus => 0.1f;
        public virtual float OverwhelmingAttackerPenalty => -0.1f;

        /// <summary>Multiplicative proximity factor applied after additive chance. 1.0 at point-blank, this value at the strike-range edge.</summary>
        public virtual float EdgeProximityMultiplier => 0.5f;

        public virtual float NightTimeBonus => 0.1f;

        // Entry chance (slipping into stealth stance).
        public virtual float BaseEntryChance => 0.5f;
        public virtual float MinEntryChance => 0.05f;
        public virtual float MaxEntryChance => 0.98f;
        public virtual float EntryHostilePenaltyPerParty => 0.12f;
        public virtual float EntryHostilePenaltyCap => 0.4f;
        /// <summary>Added penalty per point of observer Scouting, summed across observers in range.</summary>
        public virtual float EntryHostileScoutingPenaltyPerPoint => 0.0005f;
        /// <summary>Entry-chance bonus per point of the ambusher-leader's Roguery.</summary>
        public virtual float EntryRogueryPerPointBonus => 0.001f;
        public virtual float EntryRogueryBonusCap => 0.15f;

        /// <summary>Weight on the party-stealth-score delta vs the neutral baseline.</summary>
        public virtual float EntryPartyStealthScoreWeight => 0.3f;

        /// <summary>Neutral per-troop stealth score; scores above this add to entry chance, below subtract.</summary>
        public virtual float PartyStealthScoreBaseline => 0.5f;

        // Roguery XP for stealth upkeep and clean sweeps.

        public virtual int RogueryXpBasePerHiddenTick => 5;
        public virtual int RogueryXpPerHostileInRange => 1;
        public virtual int RogueryXpPerHostileCap => 5;
        public virtual int RogueryXpScoutingDivisor => 50;
        public virtual int RogueryXpTotalVictoryBase => 50;
        public virtual float RogueryXpTotalVictoryPerMember => 1f;

        // Detection model: multiplicative per-tick roll fired on QuarterHourlyTickEvent.
        public virtual float BaseDetectionChancePerTick => 0.05f;
        public virtual float MinDetectionChance => 0f;
        public virtual float MaxDetectionChance => 0.45f;
        public virtual float DetectionScanRadius => 10f;

        /// <summary>Map-unit radius around the main party within which a repelled
        /// ambusher of an ally stays revealed on the player's map.</summary>
        public virtual float AlliedRevealRadius => 12f;

        /// <summary>Quarter-hour ticks that the eye widget extrapolates the per-tick detection roll over.</summary>
        public virtual int DetectionDisplayHorizonTicks => 4;

        // Distance multiplier: lerp from adjacent to far across scan radius.
        public virtual float AdjacentDistanceMultiplier => 2.5f;
        public virtual float FarDistanceMultiplier => 0.2f;

        // Per-troop spotting score integration weights.
        public virtual float SpottingScoreDetectionWeight => 0.2f;
        public virtual float SpottingScoreSightingWeight => 0.1f;
        public virtual float SpottingScoreSizeDampingReference => 100f;

        public virtual float ConcealingTerrainMultiplier => 0.5f;
        public virtual float OpenTerrainMultiplier => 1.5f;

        public virtual float NightDetectionMultiplier => 0.7f;

        public virtual float StealthDurationDetectionPerHour => 0.1f;
        public virtual float StealthDurationDetectionCap => 0.5f;

        /// <summary>Map-unit radius for the two-tier nearby-settlement check.</summary>
        public virtual float SettlementProximityRadius => 8f;

        /// <summary>Entry-chance penalty when a hostile-faction settlement is in range.</summary>
        public virtual float HostileSettlementEntryPenalty => 0.2f;

        /// <summary>Entry-chance penalty for a neutral-faction settlement in range.</summary>
        public virtual float NeutralSettlementEntryPenalty => 0.08f;

        /// <summary>Per-tick detection multiplier when a hostile settlement is near the stealthed party.</summary>
        public virtual float HostileSettlementDetectionMultiplier => 1.6f;

        /// <summary>Per-tick detection multiplier for a neutral settlement in range.</summary>
        public virtual float NeutralSettlementDetectionMultiplier => 1.1f;

        /// <summary>Which parties count as observers for detection rolls. Default is hostile-only.</summary>
        public virtual bool CanDetect(MobileParty observer, MobileParty target)
        {
            if (observer == null || target == null || observer == target) return false;
            if (observer.MapFaction == null || target.MapFaction == null) return false;
            return FactionManager.IsAtWarAgainstFaction(observer.MapFaction, target.MapFaction);
        }

        /// <summary>Single-observer detection roll. Returns true and fills reason on a hit.</summary>
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

        /// <summary>Per-tick detection chance for a single observer, clamped.</summary>
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

        /// <summary>Combined per-tick detection across all nearby observers using independent-Bernoulli composition.</summary>
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

        /// <summary>Aggregated per-tick detection extrapolated over <see cref="DetectionDisplayHorizonTicks"/>.</summary>
        public virtual float CalculateAggregatedDetectionChanceOverHorizon(
            MobileParty target, IReadOnlyList<MobileParty> observers, CampaignTime stealthEnteredAt)
        {
            float pTick = CalculateAggregatedDetectionChancePerTick(target, observers, stealthEnteredAt);
            int horizon = DetectionDisplayHorizonTicks;
            if (horizon <= 1 || pTick <= 0f) return pTick;
            return MBMath.ClampFloat(1f - MathF.Pow(1f - pTick, horizon), 0f, 1f);
        }

        /// <summary>Settlement-proximity multiplier on per-tick detection chance. 1.0 when no settlement is in range.</summary>
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

        /// <summary>Settlement-proximity penalty on the entry-chance roll. 0 when no settlement is in range.</summary>
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

        /// <summary>True when the closest in-range settlement is a village.</summary>
        protected virtual bool IsNearestInRangeAVillage(MobileParty self)
        {
            var nearest = TORCommon.FindNearestSettlement(self, SettlementProximityRadius, _ => true);
            return nearest != null && nearest.IsVillage;
        }

        /// <summary>Classify any settlement inside the proximity radius. Hostile wins over neutral when both are present.</summary>
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

        /// <summary>Lower-hero weight when combining leader + Scout-role hero into an effective party skill.</summary>
        public virtual float SkillPairLowerWeight => 0.5f;

        /// <summary>Weights for the ambush success contest. Roguery dominates on the ambusher side.</summary>
        public virtual float ContestRogueryWeight  => 1.5f;
        public virtual float ContestScoutingWeight => 1.0f;
        public virtual float ContestTacticsWeight  => 0.7f;

        /// <summary>Divisor applied to the raw skill-delta before it folds into the chance.</summary>
        public virtual float ContestNormalizer => 300f;

        /// <summary>Base ambusher strike range. Scales up with Scouting.</summary>
        public virtual float BaseStrikeRange => 2.6f;
        public virtual float StrikeRangeScoutingPerPoint => 0.0273f;
        public virtual float MaxStrikeRange => 10.5f;

        /// <summary>Combine two heroes' skill values; higher full-weight, lower contributes <see cref="SkillPairLowerWeight"/>.</summary>
        public virtual int CombineSkillPair(int a, int b)
        {
            int high = a > b ? a : b;
            int low = a > b ? b : a;
            return high + (int)(low * SkillPairLowerWeight);
        }

        /// <summary>Effective Roguery for the ambusher side (leader + Scout-role hero).</summary>
        public virtual int GetAmbusherRoguery(MobileParty party)
        {
            if (party == null) return 0;
            int leader = party.LeaderHero?.GetSkillValue(DefaultSkills.Roguery) ?? 0;
            int scout = party.EffectiveScout?.GetSkillValue(DefaultSkills.Roguery) ?? 0;
            return CombineSkillPair(leader, scout);
        }

        /// <summary>Effective Scouting for the ambusher side; drives strike range.</summary>
        public virtual int GetAmbusherScouting(MobileParty party)
        {
            if (party == null) return 0;
            int leader = party.LeaderHero?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
            int scout = party.EffectiveScout?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
            return CombineSkillPair(leader, scout);
        }

        /// <summary>Effective Scouting for the defender side; drives detection.</summary>
        public virtual int GetDefenderScouting(MobileParty party)
        {
            if (party == null) return 0;
            int leader = party.LeaderHero?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
            int scout = party.EffectiveScout?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
            return CombineSkillPair(leader, scout);
        }

        /// <summary>Leader-only Tactics skill for defender terrain reading.</summary>
        public virtual int GetDefenderTactics(MobileParty party)
            => party?.LeaderHero?.GetSkillValue(DefaultSkills.Tactics) ?? 0;

        /// <summary>Dynamic strike range scaled by ambusher Scouting.</summary>
        public virtual float CalculateStrikeRange(MobileParty ambusher)
        {
            int scouting = GetAmbusherScouting(ambusher);
            float range = BaseStrikeRange + scouting * StrikeRangeScoutingPerPoint;
            if (range > MaxStrikeRange) range = MaxStrikeRange;
            return range;
        }

        /// <summary>Ambush success chance for an (ambusher, target) pair. Combines skill contest, terrain, force ratio, duration, night, and proximity.</summary>
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

        /// <summary>Skill contest: ambusher Roguery vs defender Scouting + Tactics, normalized and clamped.</summary>
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

        /// <summary>Chance a party successfully slips into stealth stance. Factors Roguery, terrain, night, observers, settlements.</summary>
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

        /// <summary>Pair-combined Roguery contribution to entry chance, capped.</summary>
        protected virtual float EntryRogueryBonus(MobileParty self)
        {
            int r = GetAmbusherRoguery(self);
            return MBMath.ClampFloat(r * EntryRogueryPerPointBonus, 0f, EntryRogueryBonusCap);
        }

        /// <summary>Party-roster stealth suitability contribution; can be negative for heavy rosters.</summary>
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

        /// <summary>Base strength-ratio threshold for the AI ambusher pressing after the victim flees.</summary>
        public virtual float AmbushPressBaseThreshold => 1.0f;

        /// <summary>True if the AI ambusher presses the attack on the campaign map after the victim fled.</summary>
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

        /// <summary>Flat morale penalty applied to the main party when the ambusher vanishes after an AmbushedFled outcome.</summary>
        public virtual float AmbushedFledVanishMoralePenalty => 5f;

        /// <summary>Extra morale penalty on top of vanilla's defeat-morale when the player retreats twice in one ambush.</summary>
        public virtual float AmbushedFledDoubleFleeExtraMoralePenalty => 20f;

        /// <summary>Fraction of each committed troop type shifted from healthy to wounded on first flee.</summary>
        public virtual float AmbushedFledWoundShare => 0.3f;

        /// <summary>Fraction of each committed troop type removed from the roster on double-flee.</summary>
        public virtual float AmbushedDoubleFleeRemoveShare => 0.3f;

        // Preparation time.

        /// <summary>Minimum prep window for an ambush, in game-minutes.</summary>
        public virtual float BasePreparationMinutes => 15f;

        /// <summary>Party sizes at or below this get the base floor only; excess troops stack <see cref="PerTroopMinutes"/>.</summary>
        public virtual int SmallPartyThreshold => 25;

        /// <summary>Added game-minutes per troop above <see cref="SmallPartyThreshold"/>.</summary>
        public virtual float PerTroopMinutes => 0.1f;

        /// <summary>Floor on the concealment multiplier (2 - stealthScore).</summary>
        public virtual float MinimumConcealmentFactor => 0.3f;

        /// <summary>AI only: scan radius for pursuers that might intercept before setup completes.</summary>
        public virtual float AiPrepPursuitRejectionRadius => 25f;

        /// <summary>Hours a player-detected stealthed AI party stays visible on the map regardless of fog distance.</summary>
        public virtual float PlayerDetectionRevealHours => 2f;

        /// <summary>Game-minutes needed to set up an ambush given party size and stealth score.</summary>
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

        /// <summary>Per-point-of-Roguery speedup on the ambusher's preparation window.</summary>
        public virtual float PreparationScoutPerPointReduction => 0.002f;

        /// <summary>Floor on the preparation Scout multiplier.</summary>
        public virtual float MinPreparationScoutMultiplier => 0.3f;

        /// <summary>Roguery-driven prep multiplier. Returns 1.0 for leaderless / zero-skill parties.</summary>
        public virtual float GetPreparationScoutMultiplier(MobileParty party)
        {
            int rog = GetAmbusherRoguery(party);
            if (rog <= 0) return 1f;
            float mult = 1f - rog * PreparationScoutPerPointReduction;
            if (mult < MinPreparationScoutMultiplier) mult = MinPreparationScoutMultiplier;
            return mult;
        }

        // Record cooldown.

        /// <summary>Protection window after the player gets ambushed, blocking AI re-targeting of MainParty.</summary>
        public virtual float AmbushGuardingHours => 3f;

        /// <summary>Game-hours an <see cref="AmbushRecord"/> keeps a party on cooldown; role-sensitive.</summary>
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

        /// <summary>True when the record represents a landed strike (ambusher held the field or scored partial kills).</summary>
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

        /// <summary>True when the strike landed silently (no loud signature, no witnesses).</summary>
        public virtual bool IsSilentSuccessfulStrike(AmbushRecord record)
            => IsSuccessfulStrike(record) && !record.WasLoud;

        /// <summary>Game-hours the hooded instant-ambush nudge stays lit after a silent clean sweep.</summary>
        public virtual float InstantAmbushWindowHours => 2f;

        /// <summary>True when the latest record makes the player eligible for an instant re-ambush.</summary>
        public virtual bool IsInstantAmbushAllowed(AmbushRecord record)
            => record != null
               && record.Resolution == AmbushResolution.AmbusherTotalVictory
               && !record.WasLoud
               && !record.AmbusherLeaderDown
               && (CampaignTime.Now - record.At).ToHours < InstantAmbushWindowHours;

        /// <summary>Visible troop count threshold below which real enemy numbers show instead of the bluff mask.</summary>
        public virtual int BluffVisibleThreshold => 5;

        /// <summary>True when the scoreboard should render a row's counts as "?" instead of real numbers.</summary>
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

        // AI-side propensity.

        /// <summary>Per-tick base propensity for any culture without a registered override.</summary>
        public virtual float DefaultCulturePropensity => 0f;

        /// <summary>Multiplicative scalar on the per-tick propensity roll; primary volume dial.</summary>
        public virtual float AiDecisionTickFraction => 0.1f;

        // AI-vs-AI skirmish.

        /// <summary>Fraction of the ambusher roster forming the strike force in a headless AI-vs-AI ambush.</summary>
        public virtual float AmbushSkirmishStrikePowerFraction => 0.5f;

        /// <summary>Fraction of strike-force power that lands as wounds on the victim.</summary>
        public virtual float AmbushSkirmishVictimCasualtyFraction => 0.4f;

        /// <summary>Fraction of the strike force itself that gets wounded in the exchange.</summary>
        public virtual float AmbushSkirmishAmbusherCasualtyFraction => 0.1f;

        /// <summary>Strength ratio above which the ambusher presses into a full field battle instead of melting away.</summary>
        public virtual float AmbushFollowupThreshold => 1.5f;

        /// <summary>Minimum predicted success chance an AI ambusher requires before committing to a strike.</summary>
        public virtual float AiMinCommitChance => 0.35f;

        /// <summary>Radius an AI party scans for potential ambush targets before deciding to enter stealth.</summary>
        public virtual float AiOpportunityScanRadius => 25f;

        /// <summary>Minimum predicted success against the best visible candidate before trying to enter stealth.</summary>
        public virtual float AiMinEntryViabilityChance => 0.3f;

        /// <summary>Radius in which already-stealthed friendlies count as a cluster.</summary>
        public virtual float FriendlyStealthClusterRadius => 15f;

        /// <summary>Entry-chance penalty per nearby already-stealthed friendly party.</summary>
        public virtual float FriendlyStealthClusterPenaltyPerParty => 0.1f;
        public virtual float FriendlyStealthClusterPenaltyCap => 0.4f;

        /// <summary>Cluster penalty to subtract from entry chance, clamped.</summary>
        public virtual float FriendlyStealthClusterPenalty(int nearbyStealthedFriendlies)
        {
            if (nearbyStealthedFriendlies <= 0) return 0f;
            float raw = nearbyStealthedFriendlies * FriendlyStealthClusterPenaltyPerParty;
            return MBMath.ClampFloat(raw, 0f, FriendlyStealthClusterPenaltyCap);
        }

        /// <summary>Runtime stealth-score delta for a party, additive on top of the precomputed base score.</summary>
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

        /// <summary>Per-point-of-Roguery additive to the party stealth score.</summary>
        public virtual float DynamicStealthRogueryPerPoint => 0.001f;

        /// <summary>Cap on the Roguery-driven dynamic stealth bonus.</summary>
        public virtual float DynamicStealthRogueryCap => 0.3f;

        /// <summary>Base-tier (Normal success) skirmish casualties for the (ambusher, victim) pair.</summary>
        public virtual (int ambusherWounds, int victimWounds) CalculateSkirmishCasualties(
            MobileParty ambusher, MobileParty victim)
            => CalculateSkirmishCasualties(ambusher, victim, AmbushOutcomeTier.NormalSuccess);

        /// <summary>Tier-scaled casualty computation, clamped against available healthy count on each side.</summary>
        public virtual (int ambusherWounds, int victimWounds) CalculateSkirmishCasualties(
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

        /// <summary>Band widths for the outcome-tier margin check.</summary>
        public virtual float TierCriticalMargin => 0.25f;
        public virtual float TierTerribleMargin => 0.25f;

        /// <summary>Pick an outcome tier from a success chance and roll. AI-vs-AI only.</summary>
        public virtual AmbushOutcomeTier RollOutcomeTier(float chance, float roll)
        {
            float margin = chance - roll;
            if (margin > TierCriticalMargin) return AmbushOutcomeTier.CriticalSuccess;
            if (margin >= 0f) return AmbushOutcomeTier.NormalSuccess;
            if (margin > -TierTerribleMargin) return AmbushOutcomeTier.MinorFail;
            return AmbushOutcomeTier.TerribleFail;
        }

        /// <summary>True when the ambusher's post-skirmish healthy count clears the follow-up threshold.</summary>
        public virtual bool ShouldPressAttackAfterSkirmish(MobileParty ambusher, MobileParty victim)
        {
            if (ambusher?.MemberRoster == null || victim?.MemberRoster == null) return false;
            int ambusherHealthy = ambusher.MemberRoster.TotalHealthyCount;
            int victimHealthy = victim.MemberRoster.TotalHealthyCount;
            if (victimHealthy <= 0) return true;
            return ambusherHealthy >= victimHealthy * AmbushFollowupThreshold;
        }

        /// <summary>Ambush propensity for a given AI party, composed of base value plus homeland and trait multipliers.</summary>
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

        /// <summary>Base propensity before multipliers. Hero override beats culture value.</summary>
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

        /// <summary>Scan radius around an AI party for the homeland density check.</summary>
        public virtual float HomelandScanRadius => 25f;

        /// <summary>Propensity multiplier when the ambusher's neighbourhood is 100% foreign.</summary>
        public virtual float HomelandForeignMultiplier => 0.5f;

        /// <summary>Propensity multiplier when the ambusher's neighbourhood is 100% own-culture.</summary>
        public virtual float HomelandOwnMultiplier => 1.5f;

        /// <summary>Lerp from foreign to own multiplier based on homeland density.</summary>
        protected virtual float HomelandMultiplier(MobileParty party)
        {
            float density = party.GetHomelandDensity(HomelandScanRadius);
            return MBMath.Lerp(HomelandForeignMultiplier, HomelandOwnMultiplier, density);
        }

        // Personality traits.

        /// <summary>Multiplier shift per ±1 trait step on Calculating / Honor / Valor.</summary>
        public virtual float TraitStepWeight => 0.1f;

        /// <summary>Lower bound of the trait multiplier (honorable, valorous, straightforward lord).</summary>
        public virtual float TraitMultiplierMin => 0.4f;

        /// <summary>Upper bound of the trait multiplier (scheming, dishonourable, cowardly lord).</summary>
        public virtual float TraitMultiplierMax => 1.6f;

        /// <summary>Combine Calculating (bonus), Honor (penalty), Valor (penalty) traits into a multiplier.</summary>
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
