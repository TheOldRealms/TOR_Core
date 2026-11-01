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

        // Target proximity: at zero distance = full bonus, scaling to 0 at the stealth radius edge.
        public virtual float MaxProximityBonus => 0.1f;
        public virtual float ProximityRadius => AmbushCampaignBehavior.StealthRadius;

        // Day/night: night favours ambushers.
        public virtual float NightTimeBonus => 0.1f;

        // ---- Entry chance ----
        //
        // Chance of successfully slipping into stealth stance on the hotkey
        // press. Separate from CalculateAmbushSuccessChance (chance of a
        // successful strike). Entry has a high floor so clean cover is near-
        // automatic; drops sharply with nearby hostiles so mid-chase stealth
        // is nearly impossible but never fully 0.
        public virtual float BaseEntryChance => 0.9f;
        public virtual float MinEntryChance => 0.5f;
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
        public virtual float FarDistanceMultiplier => 0.4f;

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

        public virtual float CalculateAmbushSuccessChance(MobileParty attacker, MobileParty target, CampaignTime stealthEnteredAt)
        {
            if (attacker == null || target == null) return MinChance;

            float chance = BaseAmbushChance;
            chance += ScoutingDeltaBonus(attacker, target);
            chance += TerrainBonus(attacker);
            chance += ForceRatioBonus(attacker, target);
            chance += StealthDurationBonus(stealthEnteredAt);
            chance += TargetProximityBonus(attacker, target);
            chance += NightTimeBonusValue();
            return MBMath.ClampFloat(chance, MinChance, MaxChance);
        }

        protected virtual float TargetProximityBonus(MobileParty attacker, MobileParty target)
        {
            if (ProximityRadius <= 0f) return 0f;
            float distance = attacker.Position.ToVec2().Distance(target.Position.ToVec2());
            if (distance >= ProximityRadius) return 0f;
            // Linear fade from full bonus at distance 0 to zero at ProximityRadius.
            float t = 1f - (distance / ProximityRadius);
            return MaxProximityBonus * t;
        }

        protected virtual float NightTimeBonusValue()
        {
            if (Campaign.Current == null) return 0f;
            return Campaign.Current.IsNight ? NightTimeBonus : 0f;
        }

        protected virtual float ScoutingDeltaBonus(MobileParty attacker, MobileParty target)
        {
            int a = attacker.LeaderHero?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
            int t = target.LeaderHero?.GetSkillValue(DefaultSkills.Scouting) ?? 0;
            return MBMath.ClampFloat((a - t) * ScoutingPerPointBonus, -ScoutingBonusCap, ScoutingBonusCap);
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
            // Roguery bonus temporarily disabled — most vanilla NPC lords
            // have Roguery = 0, which was zero-ing the entry chance for
            // AI parties and killing all AI ambushes in testing. Terrain
            // / night / observer penalty still apply; re-enable when
            // we've either confirmed higher Roguery values on key AI
            // heroes or combined it with Scouting as a hybrid stealth stat.
            float chance = BaseEntryChance
                + EntryTerrainBonus(self)
                + EntryNightBonus()
                - EntryHostilePenalty(observers);
            return MBMath.ClampFloat(chance, MinEntryChance, MaxEntryChance);
        }

        protected virtual float EntryRogueryBonus(MobileParty self)
        {
            int r = self.LeaderHero?.GetSkillValue(DefaultSkills.Roguery) ?? 0;
            return MBMath.ClampFloat(r * EntryRogueryPerPointBonus, 0f, EntryRogueryBonusCap);
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

        // ---- Phase F: AI-side propensity ----

        /// <summary>Per-tick chance that an eligible AI lord/bandit party even
        /// considers trying for an ambush. Baseline for a culture with no
        /// override registered; keeps the system quiet until propensity data
        /// is tuned per culture / hero.</summary>
        public virtual float DefaultCulturePropensity => 0f;

        /// <summary>How often an eligible AI party rolls for ambush entry on
        /// the decision tick. Multiplied against the full
        /// <see cref="CalculateEntryChance"/> result + the propensity to get
        /// the final roll chance. Scales the per-quarter-hour frequency
        /// across the whole AI population.</summary>
        public virtual float AiDecisionTickFraction => 1f;

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

        /// <summary>Returns (ambusherWounds, victimWounds) for a headless
        /// skirmish. The strike power is the ambusher's healthy count ×
        /// <see cref="AmbushSkirmishStrikePowerFraction"/>; victim wounds
        /// are clamped against the victim's available healthy count.
        /// </summary>
        public virtual (int ambusherWounds, int victimWounds) CalculateSkirmishCasualties(
            MobileParty ambusher, MobileParty victim)
        {
            if (ambusher?.MemberRoster == null || victim?.MemberRoster == null)
                return (0, 0);
            int ambusherHealthy = ambusher.MemberRoster.TotalHealthyCount;
            int victimHealthy = victim.MemberRoster.TotalHealthyCount;
            int strikePower = (int)(ambusherHealthy * AmbushSkirmishStrikePowerFraction);
            int victimWounds = System.Math.Min(victimHealthy,
                (int)(strikePower * AmbushSkirmishVictimCasualtyFraction));
            int ambusherWounds = System.Math.Min(ambusherHealthy,
                (int)(ambusherHealthy * AmbushSkirmishAmbusherCasualtyFraction));
            return (ambusherWounds, victimWounds);
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

        /// <summary>Returns the ambush propensity (0..1) for a given AI
        /// party, defaulting to the party culture's baseline when there's
        /// no hero override. Hero overrides take precedence — e.g. a
        /// chivalric-culture hero known for banditry can still have a high
        /// propensity (<see cref="AmbushPropensityRegistry"/>).</summary>
        public virtual float GetAmbushPropensity(MobileParty party)
        {
            if (party == null) return 0f;
            var hero = party.LeaderHero;
            if (hero != null)
            {
                float heroOverride = AmbushPropensityRegistry.GetHeroPropensity(hero);
                if (heroOverride >= 0f) return heroOverride;
            }
            var culture = party.MapFaction?.Culture ?? hero?.Culture;
            if (culture == null) return DefaultCulturePropensity;

            // Greenskin kingdom-culture exception: the registry entry is
            // the goblin-led value. Orcs are too thick to pull off an
            // ambush, so a non-goblin leader zeroes propensity. GOBLIN_BANDIT
            // ("looters") skips this gate — by definition goblin.
            if (culture.StringId == Cultures.GREENSKIN)
            {
                if (hero == null || !hero.IsGoblin()) return 0f;
            }

            return AmbushPropensityRegistry.GetCulturePropensity(culture, DefaultCulturePropensity);
        }
    }
}
