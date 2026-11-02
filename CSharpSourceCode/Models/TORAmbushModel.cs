using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.CampaignMechanics.Ambush;
using TOR_Core.Extensions;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.Models
{
    public class TORAmbushModel : GameModel
    {
        // Chance clamps — virtual so subclasses can rebalance via override.
        public virtual float BaseAmbushChance => 0.5f;
        public virtual float MinChance => 0.1f;
        public virtual float MaxChance => 0.95f;

        // Factor weights — virtual for the same reason.
        public virtual float ScoutingPerPointBonus => 0.002f;   // ±100 skill delta = ±0.2
        public virtual float ScoutingBonusCap => 0.2f;
        public virtual float StealthDurationPerHour => 0.05f;   // +0.05 per in-game hour
        public virtual float StealthDurationCap => 0.15f;       // capped at +0.15

        public virtual float ConcealingTerrainBonus => 0.15f;   // forest / mountain / canyon / swamp
        public virtual float OpenTerrainPenalty => -0.10f;      // plain / desert / steppe / dune

        public virtual float SmallAttackerBonus => 0.2f;        // attacker roster < 0.5× target
        public virtual float SlightlySmallerAttackerBonus => 0.1f;  // 0.5× ≤ ratio < 1.0×
        public virtual float OverwhelmingAttackerPenalty => -0.1f;  // attacker ≥ 2× target

        // Target proximity: applied as a MULTIPLICATIVE factor on the
        // summed additive chance, so it dominates even when the chance
        // has already saturated at MaxChance. At point-blank the factor
        // is 1.0 (no reduction); at the strike-range edge it's
        // EdgeProximityMultiplier (default 0.5 → half the chance). Half
        // range → ~0.75× via linear interpolation, so edge vs 50 % → a
        // ~24-point delta on a 95 % base chance. Pivot comes from the
        // ambusher's live CalculateStrikeRange, not a static 15u
        // constant.
        public virtual float EdgeProximityMultiplier => 0.5f;

        // Day/night: night favours ambushers.
        public virtual float NightTimeBonus => 0.1f;

        // ---- Entry chance ----
        //
        // Chance of successfully slipping into stealth stance on the hotkey
        // press. Separate from CalculateAmbushSuccessChance (chance of a
        // successful strike). Entry has a high floor so clean cover is near-
        // automatic; drops sharply with nearby hostiles so mid-chase stealth
        // is nearly impossible but never fully 0.
        public virtual float BaseEntryChance => 0.5f;
        public virtual float MinEntryChance => 0.05f;
        public virtual float MaxEntryChance => 0.98f;
        public virtual float EntryHostilePenaltyPerParty => 0.12f;
        public virtual float EntryHostilePenaltyCap => 0.4f;
        /// <summary>Added penalty per point of observer Scouting (summed
        /// across observers in range). 300 Scouting observer → +0.15
        /// detection probability, on top of the per-party flat.</summary>
        public virtual float EntryHostileScoutingPenaltyPerPoint => 0.0005f;
        /// <summary>Entry-chance bonus per point of the ambusher-leader's
        /// Roguery skill. Thematically the stealth stat; Scouting lives
        /// on the detection side only.</summary>
        public virtual float EntryRogueryPerPointBonus => 0.001f;
        public virtual float EntryRogueryBonusCap => 0.15f;

        /// <summary>Weight on the party-stealth-score delta (vs the
        /// baseline 0.5 troop score). Positive when the party is
        /// stealthier than neutral; negative when weighted down by
        /// heavy cav / monstrous / plate. Baseline is 0.5 so a
        /// goblin-skirmisher roster at 0.75 → +0.25 × weight; a
        /// knightly roster at -0.1 → -0.6 × weight.</summary>
        public virtual float EntryPartyStealthScoreWeight => 0.3f;

        /// <summary>Baseline per-troop stealth score used as the pivot
        /// for the party-stealth-score bonus. Any score above this
        /// adds; below subtracts. Matches
        /// <c>AmbushTroopStealthScore</c>'s neutral baseline.</summary>
        public virtual float PartyStealthScoreBaseline => 0.5f;

        // ---- Roguery XP (hidden-tick + perfect-ambush bonus) ----

        public virtual int RogueryXpBasePerHiddenTick => 5;
        public virtual int RogueryXpPerHostileInRange => 1;
        public virtual int RogueryXpPerHostileCap => 5;
        public virtual int RogueryXpScoutingDivisor => 50;  // +1 XP per 50 Scouting on the highest-skilled observer in range
        public virtual int RogueryXpTotalVictoryBase => 50;
        public virtual float RogueryXpTotalVictoryPerMember => 1f;

        // ---- Detection model (Phase B v2) ----
        //
        // Fires on QuarterHourlyTickEvent per nearby hostile observer while the
        // player is in stealth. Multiplicative composition (vs the additive
        // CalculateAmbushSuccessChance above) because this is a per-tick "escape
        // notice" roll called many times, not a one-shot strike outcome.
        public virtual float BaseDetectionChancePerTick => 0.05f;
        public virtual float MinDetectionChance => 0f;
        public virtual float MaxDetectionChance => 0.80f;
        public virtual float DetectionScanRadius => 10f;

        // Distance multiplier: closer = higher. Lerped from AdjacentDistanceMultiplier
        // at distance 0 to FarDistanceMultiplier at the scan-radius edge.
        public virtual float AdjacentDistanceMultiplier => 2.5f;
        public virtual float FarDistanceMultiplier => 0.2f;

        public virtual float DetectionScoutingPerPointBonus => 0.004f;   // observer scouting bias
        public virtual float DetectionScoutingBonusCap => 0.5f;

        public virtual float ConcealingTerrainMultiplier => 0.5f;        // forest / mountain / canyon / swamp
        public virtual float OpenTerrainMultiplier => 1.5f;              // plain / desert / steppe / dune

        public virtual float NightDetectionMultiplier => 0.5f;

        public virtual float StealthDurationDetectionPerHour => 0.1f;    // multiplicative +10% per hour
        public virtual float StealthDurationDetectionCap => 0.5f;        // capped at +50%

        // Filter: which parties count as "observers" that can even attempt a detection
        // roll against a stealthed target. Default is hostile parties only — a friendly
        // caravan or allied lord riding past shouldn't blow your cover. Overrides can
        // widen this (e.g., bandit parties always count) or narrow it (e.g., only parties
        // with a sufficiently high Scouting leader).
        public virtual bool CanDetect(MobileParty observer, MobileParty target)
        {
            if (observer == null || target == null || observer == target) return false;
            if (observer.MapFaction == null || target.MapFaction == null) return false;
            return FactionManager.IsAtWarAgainstFaction(observer.MapFaction, target.MapFaction);
        }

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

        public virtual float CalculateDetectionChancePerTick(MobileParty observer, MobileParty target, CampaignTime stealthEnteredAt)
        {
            float chance = BaseDetectionChancePerTick
                * DistanceDetectionMultiplier(observer, target)
                * ScoutingDetectionMultiplier(observer)
                * TerrainDetectionMultiplier(target)
                * NightDetectionMultiplierValue()
                * StealthDurationDetectionMultiplier(stealthEnteredAt);
            return MBMath.ClampFloat(chance, MinDetectionChance, MaxDetectionChance);
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
            int s = observer.LeaderHero?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
            return 1f + MBMath.ClampFloat(s * DetectionScoutingPerPointBonus, 0f, DetectionScoutingBonusCap);
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

        // ---- Skill contest (party-level) ----

        /// <summary>How much of the lower hero's skill contributes when
        /// combining the leader + Scout-role hero into an effective
        /// party skill. 0.5 = the specialist carries, but a second
        /// decent hero still helps.</summary>
        public virtual float SkillPairLowerWeight => 0.5f;

        /// <summary>Weights for the ambush success contest. Roguery
        /// dominates on the ambusher side; Scouting + Tactics share
        /// detection duty on the defender side.</summary>
        public virtual float ContestRogueryWeight  => 1.5f;
        public virtual float ContestScoutingWeight => 1.0f;
        public virtual float ContestTacticsWeight  => 0.7f;

        /// <summary>Divisor applied to the raw skill-delta before it folds
        /// into the chance. 300 = a 300-point weighted-Roguery advantage
        /// shifts the chance by ~1.0 (clamped), so a typical 50-point
        /// delta shifts ~0.17.</summary>
        public virtual float ContestNormalizer => 300f;

        /// <summary>Base strike range for the ambusher. Scouting of the
        /// ambusher's leader + Scout-role hero scales this up — a well-
        /// scouted party reaches further. Calibrated against the player's
        /// in-game perception of "mid-tier distance" at Scouting 15 ≈
        /// 0.5× that distance, Scouting 125 ≈ 1×, 250 ≈ 1.5×, cap at 300.
        /// Anchor X ≈ 6 map units — iterate from stealth-entry diagnostics.</summary>
        public virtual float BaseStrikeRange => 2.6f;
        public virtual float StrikeRangeScoutingPerPoint => 0.0273f;
        public virtual float MaxStrikeRange => 10.5f;

        /// <summary>Combine two heroes' skill values using the half-weight-
        /// lower rule: the higher score carries full weight, the lower
        /// contributes <see cref="SkillPairLowerWeight"/> of its value.
        /// A solo specialist equals max(solo, 0); two decent heroes stack
        /// without doubling.</summary>
        public virtual int CombineSkillPair(int a, int b)
        {
            int high = a > b ? a : b;
            int low = a > b ? b : a;
            return high + (int)(low * SkillPairLowerWeight);
        }

        /// <summary>Effective Roguery for the AMBUSHER side — leader + the
        /// party's <c>EffectiveScout</c> hero, combined via the pair rule.
        /// Drives the ambush success chance.</summary>
        public virtual int GetAmbusherRoguery(MobileParty party)
        {
            if (party == null) return 0;
            int leader = party.LeaderHero?.GetSkillValue(DefaultSkills.Roguery) ?? 0;
            int scout = party.EffectiveScout?.GetSkillValue(DefaultSkills.Roguery) ?? 0;
            return CombineSkillPair(leader, scout);
        }

        /// <summary>Effective Scouting for the AMBUSHER side. Drives the
        /// strike range — a well-scouted ambusher can execute a successful
        /// ambush from further away.</summary>
        public virtual int GetAmbusherScouting(MobileParty party)
        {
            if (party == null) return 0;
            int leader = party.LeaderHero?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
            int scout = party.EffectiveScout?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
            return CombineSkillPair(leader, scout);
        }

        /// <summary>Effective Scouting for the DEFENDER side. Drives
        /// ambush detection; contested against the ambusher's Roguery
        /// in the success chance formula.</summary>
        public virtual int GetDefenderScouting(MobileParty party)
        {
            if (party == null) return 0;
            int leader = party.LeaderHero?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
            int scout = party.EffectiveScout?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
            return CombineSkillPair(leader, scout);
        }

        /// <summary>Tactics is a leader-only skill — unlike Scouting, it's
        /// not tied to the Scout role. Only the leader's value counts
        /// toward reading the terrain for ambush spots.</summary>
        public virtual int GetDefenderTactics(MobileParty party)
            => party?.LeaderHero?.GetSkillValue(DefaultSkills.Tactics) ?? 0;

        /// <summary>Dynamic strike range: how far around the ambusher's
        /// position a successful ambush can be executed. Scales with the
        /// ambusher's effective Scouting (not Roguery — Scouting is "where
        /// can we usefully lie in wait").</summary>
        public virtual float CalculateStrikeRange(MobileParty ambusher)
        {
            int scouting = GetAmbusherScouting(ambusher);
            float range = BaseStrikeRange + scouting * StrikeRangeScoutingPerPoint;
            if (range > MaxStrikeRange) range = MaxStrikeRange;
            return range;
        }

        /// <summary>
        /// Ambush success chance for a given (ambusher, target) pair.
        /// Combines a baseline, the skill contest (Roguery vs Scouting
        /// + Tactics via the pair-combined effective skills), terrain,
        /// force ratio, stealth-duration freshness, proximity, and night
        /// bonus. Clamped to [MinChance, MaxChance].
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
            // Proximity is multiplicative — applied LAST so it survives
            // the saturation that was previously pinning ambush chance
            // at the 0.95 cap regardless of distance.
            chance *= ProximityMultiplier(attacker, target);
            return MBMath.ClampFloat(chance, MinChance, MaxChance);
        }

        /// <summary>Skill contest: ambusher Roguery vs defender Scouting +
        /// Tactics. Positive when the ambusher's weighted skill advantage
        /// clears the defender's awareness. Normalized by
        /// <see cref="ContestNormalizer"/>; clamp mirrors the old
        /// Scouting-delta bounds so no single factor dominates.</summary>
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
            // Linear fade from 1.0 at distance 0 to EdgeProximityMultiplier
            // at the strike-range edge. Pinned at the edge value beyond
            // the ring (same cliff whether you're just inside or just
            // outside the attacker's live strike radius).
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
        /// Chance that a party successfully slips into stealth stance.
        /// Factors: ambusher Roguery (+), terrain (+/-), night (+),
        /// observers in range (- per party AND - per observer-Scouting
        /// point, both capped). Clamp [MinEntryChance..MaxEntryChance].
        /// See campaign-stealth-design.md § 1.
        ///
        /// <paramref name="observers"/> is the live list of hostile
        /// parties within <see cref="DetectionScanRadius"/>; passing it
        /// in (rather than just the count) lets the penalty scale with
        /// the observers' actual Scouting totals, so a lone scout-master
        /// is a bigger problem than a herd of untrained levies.
        /// </summary>
        public virtual float CalculateEntryChance(MobileParty self,
            IReadOnlyList<MobileParty> observers)
        {
            if (self == null) return MinEntryChance;
            float chance = BaseEntryChance
                + EntryRogueryBonus(self)
                + EntryPartyStealthScoreBonus(self)
                + EntryTerrainBonus(self)
                + EntryNightBonus()
                - EntryHostilePenalty(observers);
            return MBMath.ClampFloat(chance, MinEntryChance, MaxEntryChance);
        }

        /// <summary>Pair-combined Roguery contribution to entry chance.
        /// Uses <see cref="GetAmbusherRoguery"/> so a party with a
        /// Roguery-specialist as either leader or dedicated Scout
        /// benefits. Cap prevents a maxed hero from single-handedly
        /// pushing entry chance to certainty.</summary>
        protected virtual float EntryRogueryBonus(MobileParty self)
        {
            int r = GetAmbusherRoguery(self);
            return MBMath.ClampFloat(r * EntryRogueryPerPointBonus, 0f, EntryRogueryBonusCap);
        }

        /// <summary>Party-roster stealth suitability contribution.
        /// Reads <see cref="AmbushTroopStealthScore.GetBasePartyScore"/>
        /// plus the dynamic bonus (perks / buffs), pivots around the
        /// neutral baseline, scales by
        /// <see cref="EntryPartyStealthScoreWeight"/>. Can be negative —
        /// a heavy-cav roster actively drags entry chance down.</summary>
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

        // ---- Phase F §6 follow-up: ambusher-presses-after-flee ----

        /// <summary>Base strength-ratio threshold for the AI ambusher
        /// deciding to press the attack after the player flees the
        /// Phase F mission (AmbushedFled). The actual threshold is
        /// scaled by <see cref="HomelandMultiplier"/> and
        /// <see cref="TraitAmbushMultiplier"/> — a Wood Elf lord in
        /// Laurelorn with a scheming hero presses at a far lower
        /// ratio than a Bretonnian honour-bound knight in foreign
        /// terrain. 1.0 means "equal strength = always press"
        /// before culture/trait modifiers are applied.</summary>
        public virtual float AmbushPressBaseThreshold => 1.0f;

        /// <summary>True if the AI ambusher presses the attack on the
        /// campaign map after the player fled the Phase F mission.
        /// Combination of raw strength ratio + culture-homeland +
        /// hero-trait multipliers — mirrors the propensity formula
        /// so cultures and heroes read consistently from the
        /// entry decision through the press decision.</summary>
        public virtual bool AmbusherPressesAttack(MobileParty ambusher, MobileParty player)
        {
            if (ambusher == null || player == null) return false;
            float ambusherStrength = ambusher.Party?.EstimatedStrength ?? 0f;
            float playerStrength = player.Party?.EstimatedStrength ?? 0f;
            if (playerStrength <= 0f) return true;    // player has no fighting force
            if (ambusherStrength <= 0f) return false; // ambusher wiped
            float strengthRatio = ambusherStrength / playerStrength;
            // Threshold shrinks for aggressive cultures / scheming heroes.
            // AmbushPressBaseThreshold / mult inverts the same multipliers
            // that boost propensity (homeland + traits).
            float threshold = AmbushPressBaseThreshold
                            / System.Math.Max(HomelandMultiplier(ambusher), 0.1f)
                            / System.Math.Max(TraitAmbushMultiplier(ambusher.LeaderHero), 0.1f);
            return strengthRatio >= threshold;
        }

        /// <summary>Flat morale penalty applied to the main party when
        /// the ambusher vanishes after a Phase F AmbushedFled outcome.
        /// Light — fiction is "being ambushed is rattling but no
        /// combat loss."</summary>
        public virtual float AmbushedFledVanishMoralePenalty => 5f;

        /// <summary>Extra morale penalty stacked on top of vanilla's
        /// defeat-morale when the player retreats from the follow-up
        /// field battle (double-flee). Pushes a mid-morale party into
        /// the desertion zone — the fiction is "running twice in one
        /// afternoon shatters the band."</summary>
        public virtual float AmbushedFledDoubleFleeExtraMoralePenalty => 20f;

        /// <summary>Fraction of each committed troop type that gets
        /// shifted from healthy → wounded when the player flees the
        /// first ambush. Applied per-character-type via the roster
        /// snapshot taken at mission start. E.g. 0.3 = 30% of each
        /// troop stack from the ambushed-into-mission commitment ends
        /// up carrying wounds.</summary>
        public virtual float AmbushedFledWoundShare => 0.3f;

        /// <summary>Fraction of each committed troop type that gets
        /// removed from the roster on double-flee (fled the follow-up
        /// field battle too). Same snapshot semantics as
        /// <see cref="AmbushedFledWoundShare"/>. These are the "left
        /// behind for good" — not wounded, gone.</summary>
        public virtual float AmbushedDoubleFleeRemoveShare => 0.3f;

        // ---- Record-cooldown ----

        /// <summary>Protection window after the player gets ambushed:
        /// hours during which the AI-ambush decision tick refuses to
        /// re-target MainParty. Distinct from the stealth-rebuild
        /// cooldown — "I was just ambushed and need to recover" is a
        /// different mechanical state than "I just ambushed and gave
        /// away my position." Shorter by default; the player should
        /// get back to normal campaign play reasonably soon, just not
        /// get stacked by back-to-back AI ambushes.
        /// See [[feedback_ambush_role_asymmetry]].</summary>
        public virtual float AmbushGuardingHours => 3f;

        /// <summary>How many game-hours an <see cref="AmbushRecord"/>
        /// keeps a party on cooldown.
        ///
        /// Role-sensitive:
        ///  - Phase F victim guard (<c>Victim == MainParty</c>, not
        ///    repelled) → <see cref="AmbushGuardingHours"/>. Protects
        ///    the player from immediate re-ambush.
        ///  - Phase D / repelled / AI-vs-AI → baseline from
        ///    <c>AmbushCampaignBehavior.CooldownHours</c>, plus the
        ///    mission model's <c>LoudnessPenaltyHours</c> for loud
        ///    ambushes (gunpowder / non-Shadow magic).</summary>
        public virtual float GetAmbushCooldownHours(AmbushRecord record)
        {
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
            return hours;
        }

        // ---- Phase F: AI-side propensity ----

        /// <summary>Per-tick chance that an eligible AI lord/bandit party even
        /// considers trying for an ambush. Baseline for a culture with no
        /// override registered; keeps the system quiet until propensity data
        /// is tuned per culture / hero.</summary>
        public virtual float DefaultCulturePropensity => 0f;

        /// <summary>Multiplicative scalar on the per-tick propensity roll.
        /// Each quarter-hour tick, every eligible party rolls
        /// <c>MBRandom.RandomFloat &lt; propensity × AiDecisionTickFraction</c>
        /// to decide whether to even try entering stealth. 1.0 means the
        /// raw propensity drives the per-tick rate; 0.1 means ten times
        /// fewer attempts per party per tick. Keep this as the primary
        /// volume dial — propensity values stay flavor-shaped, this
        /// knob controls world-wide rate.</summary>
        public virtual float AiDecisionTickFraction => 0.1f;

        // ---- Phase F Slice 2b: AI-vs-AI skirmish ----

        /// <summary>Fraction of the ambusher's roster that counts as the
        /// "strike" force in a headless AI-vs-AI ambush. The strike delivers
        /// casualties to the victim and takes some itself; the rest of the
        /// ambusher roster sits in reserve. Default 0.5 — half the party
        /// commits to the surprise salvo.</summary>
        public virtual float AmbushSkirmishStrikePowerFraction => 0.5f;

        /// <summary>Fraction of the strike force's power that lands as
        /// wounds on the victim. Default 0.4 → a 50-troop strike wounds
        /// ~20 of the victim's roster.</summary>
        public virtual float AmbushSkirmishVictimCasualtyFraction => 0.4f;

        /// <summary>Fraction of the strike force itself that gets wounded
        /// in the exchange. Default 0.1 → surprise advantage keeps the
        /// ambusher's own losses light.</summary>
        public virtual float AmbushSkirmishAmbusherCasualtyFraction => 0.1f;

        /// <summary>Strength ratio above which the ambusher presses the
        /// attack into a full field battle instead of melting away.
        /// Mirrors design doc § 6 — same gate the Phase F AmbushedFled
        /// follow-up uses on the player side.</summary>
        public virtual float AmbushFollowupThreshold => 1.5f;

        /// <summary>Minimum CalculateAmbushSuccessChance an AI ambusher
        /// requires before committing to a strike. Below this, the party
        /// stays in stealth and lets the target walk past — the
        /// "lurking is also valid" case: small forces that spotted a
        /// stronger hostile just vanish and wait for it to clear.
        /// </summary>
        public virtual float AiMinCommitChance => 0.35f;

        /// <summary>Radius an AI party scans for potential ambush targets
        /// BEFORE deciding to enter stealth. If no hostile party is in
        /// this range, the propensity / entry roll is skipped entirely —
        /// the party doesn't lie in wait for ambushes that will never
        /// happen. Wider than <see cref="AmbushCampaignBehavior.StealthRadius"/>
        /// (the actual strike range) so the party stealths slightly
        /// before the target walks into strike range, not at the moment
        /// it's already too late to react.</summary>
        public virtual float AiOpportunityScanRadius => 25f;

        /// <summary>Minimum predicted ambush success chance against the
        /// best candidate target before even considering entering
        /// stealth. Below this, the party knows a strike wouldn't work
        /// against anyone it can see, so it doesn't bother hiding.
        /// Separate from <see cref="AiMinCommitChance"/> (which is the
        /// strike-commit gate at target-detection time) — this is the
        /// "is it worth even trying" entry gate.</summary>
        public virtual float AiMinEntryViabilityChance => 0.3f;

        /// <summary>Radius in which other already-stealthed friendlies
        /// count as "cluster" — too many hiding in the same spot makes
        /// the position obvious. Shared with the planned scoring
        /// formula so the friendly-stealth multiplier reads the same
        /// local density as this penalty.</summary>
        public virtual float FriendlyStealthClusterRadius => 15f;

        /// <summary>Entry-chance penalty per nearby already-stealthed
        /// friendly party. Rationale: ten lords hiding behind the same
        /// hill compound each other's trail / profile — enemies will
        /// catch on. Capped by <see cref="FriendlyStealthClusterPenaltyCap"/>
        /// so a dense cluster can't drive entry below the minimum
        /// floor on its own.</summary>
        public virtual float FriendlyStealthClusterPenaltyPerParty => 0.1f;
        public virtual float FriendlyStealthClusterPenaltyCap => 0.4f;

        /// <summary>Returns the cluster penalty to subtract from entry
        /// chance given the number of friendlies already stealthed in
        /// range. Clamped at the cap. Zero if no cluster.</summary>
        public virtual float FriendlyStealthClusterPenalty(int nearbyStealthedFriendlies)
        {
            if (nearbyStealthedFriendlies <= 0) return 0f;
            float raw = nearbyStealthedFriendlies * FriendlyStealthClusterPenaltyPerParty;
            return MBMath.ClampFloat(raw, 0f, FriendlyStealthClusterPenaltyCap);
        }

        /// <summary>Runtime stealth-score delta for a party, on top of
        /// its precomputed <see cref="AmbushTroopStealthScore.GetBasePartyScore"/>.
        /// This is the hook for perks / buffs / culture-level dynamic
        /// effects — anything that shifts stealth value during a session
        /// (base is fixed per troop StringId). Returns 0 by default; a
        /// subclass or future design pass fills in actual perks (e.g.
        /// Roguery leader bonus, "Shadow Rites" career choice, etc.).
        /// </summary>
        public virtual float GetDynamicStealthBonus(MobileParty party) => 0f;

        /// <summary>Returns (ambusherWounds, victimWounds) for a headless
        /// skirmish. The strike power is the ambusher's healthy count ×
        /// <see cref="AmbushSkirmishStrikePowerFraction"/>; victim wounds
        /// are clamped against the victim's available healthy count.
        ///
        /// Base-tier numbers (Normal success multiplier 1.0×). Call
        /// <see cref="CalculateSkirmishCasualties(MobileParty, MobileParty, AmbushOutcomeTier)"/>
        /// to scale by tier.
        /// </summary>
        public virtual (int ambusherWounds, int victimWounds) CalculateSkirmishCasualties(
            MobileParty ambusher, MobileParty victim)
            => CalculateSkirmishCasualties(ambusher, victim, AmbushOutcomeTier.NormalSuccess);

        /// <summary>Tier-scaled casualty computation. Multipliers per
        /// tier are documented in campaign-stealth-design.md § 13.
        /// Values are rounded via (int) truncation and clamped against
        /// the available healthy count on each side.</summary>
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

        /// <summary>Band widths for the outcome-tier margin check.
        /// Critical / terrible require a decisive margin either way;
        /// in between is a normal pass / minor fail.</summary>
        public virtual float TierCriticalMargin => 0.25f;
        public virtual float TierTerribleMargin => 0.25f;

        /// <summary>
        /// Pick an outcome tier from a chance (0..1) and a roll (0..1).
        /// margin = chance - roll; the magnitude decides how decisive the
        /// outcome is. See campaign-stealth-design.md § 13 for the design.
        ///
        /// Only applies to AI-vs-AI skirmishes — player-involved cases
        /// stay binary (pass → NormalSuccess, fail → MinorFail) per § 14.
        /// </summary>
        public virtual AmbushOutcomeTier RollOutcomeTier(float chance, float roll)
        {
            float margin = chance - roll;
            if (margin > TierCriticalMargin) return AmbushOutcomeTier.CriticalSuccess;
            if (margin >= 0f) return AmbushOutcomeTier.NormalSuccess;
            if (margin > -TierTerribleMargin) return AmbushOutcomeTier.MinorFail;
            return AmbushOutcomeTier.TerribleFail;
        }

        /// <summary>True when the ambusher's post-skirmish healthy count
        /// clears the follow-up threshold against the victim's healthy
        /// count — the signal to press the attack rather than fade back
        /// into cover. First-pass uses raw healthy counts; a Slice 2c
        /// refinement can promote to tier-weighted strength.</summary>
        public virtual bool ShouldPressAttackAfterSkirmish(MobileParty ambusher, MobileParty victim)
        {
            if (ambusher?.MemberRoster == null || victim?.MemberRoster == null) return false;
            int ambusherHealthy = ambusher.MemberRoster.TotalHealthyCount;
            int victimHealthy = victim.MemberRoster.TotalHealthyCount;
            if (victimHealthy <= 0) return true;
            return ambusherHealthy >= victimHealthy * AmbushFollowupThreshold;
        }

        /// <summary>Returns the ambush propensity for a given AI party.
        /// Composed of a base value (hero override or culture baseline),
        /// a <see cref="HomelandMultiplier"/> that boosts when the party
        /// is surrounded by its own culture's settlements, and a
        /// <see cref="TraitAmbushMultiplier"/> that shifts per hero
        /// personality (Calculating / Honor / Valor). Bandits bypass
        /// the two multipliers — their fake-culture StringIds don't
        /// match any settlement and they rarely have a LeaderHero.
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

        /// <summary>Base propensity before the homeland / trait multipliers.
        /// Hero override takes precedence over culture value — e.g. a
        /// chivalric-culture hero known for banditry can still have a high
        /// base (see <see cref="AmbushPropensityRegistry"/>).</summary>
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
            // No greenskin-goblin gate here — the "orcs are bad at
            // stealth" penalty is already baked into the precomputed
            // AmbushTroopStealthScore via RaceFactor. An orc-heavy
            // greenskin party will naturally score too low to pass
            // the entry chance; the culture-level propensity is only
            // the inclination to try, not the capability.
            return AmbushPropensityRegistry.GetCulturePropensity(culture, DefaultCulturePropensity);
        }

        // ---- Homeland awareness ----

        /// <summary>Scan radius around an AI party for the homeland density
        /// check. 25u is tuned against the TOR map (median same-culture
        /// settlement spacing ~15u; at 25 the median settlement has ~4
        /// neighbours in range — enough for a stable ratio without blurring
        /// across lore regions).</summary>
        public virtual float HomelandScanRadius => 25f;

        /// <summary>Propensity multiplier when the ambusher's neighbourhood
        /// is 100 % foreign (no same-culture settlements in scan). Thematic:
        /// a lord lying in wait in enemy territory is cautious.</summary>
        public virtual float HomelandForeignMultiplier => 0.5f;

        /// <summary>Propensity multiplier when the ambusher's neighbourhood
        /// is 100 % own-culture (homeland). Wood elves in Laurelorn,
        /// greenskins in orc hills, Empire lords in Reikland.</summary>
        public virtual float HomelandOwnMultiplier => 1.5f;

        /// <summary>Linear interpolation from <see cref="HomelandForeignMultiplier"/>
        /// (ratio 0, foreign) to <see cref="HomelandOwnMultiplier"/> (ratio
        /// 1, home). Reads density via the <see cref="Extensions.MobilePartyExtensions.GetHomelandDensity"/>
        /// extension so other systems can reuse the same signal.</summary>
        protected virtual float HomelandMultiplier(MobileParty party)
        {
            float density = party.GetHomelandDensity(HomelandScanRadius);
            return MBMath.Lerp(HomelandForeignMultiplier, HomelandOwnMultiplier, density);
        }

        // ---- Personality traits ----

        /// <summary>How much a single trait step (±1 of
        /// <c>Hero.GetTraitLevel</c>) shifts the ambush multiplier. 0.1 →
        /// each ±1 trait step moves the multiplier by ±10 %. With three
        /// contributing traits (<see cref="DefaultTraits.Calculating"/>,
        /// <see cref="DefaultTraits.Honor"/>, <see cref="DefaultTraits.Valor"/>)
        /// at max ±2 each, total swing is ±0.6 → clamped to the band
        /// below.</summary>
        public virtual float TraitStepWeight => 0.1f;

        /// <summary>Lower bound of the trait multiplier — a maxed-honorable,
        /// maxed-valorous, zero-calculating lord. Thematic: a knight who
        /// disdains ambushes to the point of practical refusal.</summary>
        public virtual float TraitMultiplierMin => 0.4f;

        /// <summary>Upper bound of the trait multiplier — a scheming,
        /// dishonourable, cowardly lord. The classic "cunning assassin"
        /// archetype: ambushes whenever possible.</summary>
        public virtual float TraitMultiplierMax => 1.6f;

        /// <summary>Combine Calculating (bonus), Honor (penalty), Valor
        /// (penalty) trait levels into a propensity multiplier. Returns
        /// 1.0 when no leader (bandit tag parties etc.).</summary>
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
