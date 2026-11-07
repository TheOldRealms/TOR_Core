using System.Collections.Generic;
using NLog;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Team-level tactic that scores scener-authored
    /// <see cref="AmbushSpot"/> positions for each ranged formation on
    /// the ambusher team and keeps the formation committed to its
    /// best-scoring point. Hysteresis is encoded in the score itself
    /// (a settled-and-firing formation gets a flat
    /// <c>AmbushTacticCommitmentBonus</c> added to the score of its
    /// current anchor), so position switches only happen when an
    /// alternative is meaningfully better, not when it scored
    /// fractionally higher one tick.
    ///
    /// Scope: AI-side only in effect, but registered on the ambusher
    /// team regardless of ownership. Player formations on direct
    /// control have <c>Formation.IsAIControlled == false</c>, so their
    /// FormationAI doesn't tick and the behavior this tactic assigns
    /// never drives their MovementOrder. When the player delegates
    /// command (F6 etc.), those formations start ticking FormationAI
    /// and pick up the ambush behavior naturally.
    ///
    /// Melee formations under this tactic are left to whatever
    /// behaviors were added by prior tactics (BehaviorCharge etc.);
    /// they're not re-weighted here. The fiction "archers hold the
    /// hill while melee charges the road" reads correctly without us
    /// touching the melee layer.
    /// </summary>
    public class AmbushTacticHoldPositions : TacticComponent
    {
        // Internal AI scoring knobs. Kept on the tactic class (not on
        // TORMissionAmbushModel) because they govern how twitchy the
        // tactic looks, not any user-facing game balance. A mod that
        // genuinely wants different behavior can subclass this tactic
        // and register the subclass instead of the base.

        /// <summary>Score added to an advantage point when the formation
        /// is already there, settled, and firing. Drives hysteresis;
        /// higher values keep archers committed longer. Roughly the
        /// point-score equivalent of vanilla's 1.5x tactic-selection
        /// bias.</summary>
        public const float CommitmentBonus = 50f;

        /// <summary>Metres the formation's cached average position may
        /// sit from the anchor and still count as "settled" for the
        /// commitment bonus.</summary>
        public const float SettleRadius = 15f;

        /// <summary>Peak of the optimal-distance scoring term in metres
        /// from the anchor to the enemy median. Ambush fiction is
        /// close-strike (10-40m window), not a vanilla field-battle
        /// fire-line (60-100m). Much shorter than this adds no penalty
        /// (point-blank ambush is the whole point). Much farther
        /// zeroes the bonus.</summary>
        public const float OptimalDistance = 30f;

        /// <summary>Hard-floor distance under which the anchor is
        /// genuinely useless (enemy literally inside the point).
        /// Separate from the smooth scoring curve. Points inside this
        /// radius get a large negative penalty regardless of other
        /// scoring. Keep low; anything 5-10m away from an archer is
        /// still ambush-shootable.</summary>
        public const float MeleeFloorDistance = 5f;

        /// <summary>Minimum score an advantage point must exceed for the
        /// tactic to commit a formation to it. Below this the formation
        /// abandons to vanilla behaviors (BehaviorCharge etc.).</summary>
        public const float MinViableScore = 30f;

        /// <summary>Ratio of agents (0..1) that must have a ranged
        /// weapon with remaining ammo for the formation to count as
        /// firing-capable. Below this the commitment bonus drops out.</summary>
        public const float ArmedRatioThreshold = 0.6f;

        /// <summary>Seconds a formation must have dwelt at its current
        /// anchor before the commitment bonus applies. Stops a
        /// just-arrived formation from being treated as "settled".</summary>
        public const float MinDwellSeconds = 5f;

        private struct AnchorEntry
        {
            public AmbushSpot Spot;
            public float DwellStartTime;
        }

        private readonly Dictionary<Formation, AnchorEntry> _anchors
            = new Dictionary<Formation, AnchorEntry>();

        private AmbushMissionController _controller;
        private AmbushTacticalAnchorLogic _anchorLogic;
        private bool _seededFromLayout;

        public AmbushTacticHoldPositions(Team team) : base(team) { }

        private void EnsureCached()
        {
            if (_controller == null)
                _controller = Mission.Current?.GetMissionBehavior<AmbushMissionController>();
            if (_anchorLogic == null)
                _anchorLogic = Mission.Current?.GetMissionBehavior<AmbushTacticalAnchorLogic>();
        }

        protected override float GetTacticWeight()
        {
            EnsureCached();
            if (_controller == null) return 0f;
            if (Team != Mission.Current?.AttackerTeam) return 0f;

            // Baseline high enough to reliably beat TacticFullScaleAttack
            // (~30 for a mixed field team) in an ambush context. We want
            // the ambush tactic to own the team as long as ranged
            // formations can hold their advantage points.
            float weight = 30f;
            int rangedCount = 0;
            bool anyInMelee = false;
            bool anyWithAmmo = false;
            foreach (var f in FormationsIncludingEmpty)
            {
                if (f.CountOfUnits == 0) continue;
                if (!IsRangedClass(f.PhysicalClass)) continue;
                rangedCount++;
                var enemy = f.CachedClosestEnemyFormation;
                if (enemy != null && enemy.Formation.CachedMedianPosition.AsVec2
                        .DistanceSquared(f.CachedAveragePosition) < 20f * 20f)
                    anyInMelee = true;
                if (HasAnyArmedAgent(f)) anyWithAmmo = true;
            }
            if (rangedCount >= 1) weight += 3f;
            if (_controller.SceneSpots != null && _controller.SceneSpots.Count > 0) weight += 2f;
            if (anyInMelee) weight -= 6f;
            if (rangedCount > 0 && !anyWithAmmo) weight -= 4f;
            return MathF.Max(0f, weight);
        }

        public override void TickOccasionally()
        {
            EnsureCached();
            if (_controller == null || _anchorLogic == null)
            {
                base.TickOccasionally();
                return;
            }
            if (_controller.SceneSpots == null || _controller.SceneSpots.Count == 0)
            {
                base.TickOccasionally();
                return;
            }

            // On first tick, seed _anchors from the layout's default
            // assignments so the formation that custom-spawn placed at
            // spot X already has an anchor at spot X with the
            // commitment bonus active. Without this, the tactic
            // re-evaluates from scratch on tick 1 and may move the
            // formation off the authored spot because the raw score
            // of a different spot is marginally higher.
            if (!_seededFromLayout)
            {
                _seededFromLayout = true;
                SeedAnchorsFromLayout();
            }

            float now = Mission.Current.CurrentTime;
            var claimed = new HashSet<int>();
            foreach (var f in FormationsIncludingEmpty)
            {
                if (f == null || f.CountOfUnits == 0) continue;
                if (IsRangedClass(f.PhysicalClass))
                    EvaluateFormation(f, now, claimed);
                else
                    ConfigureMeleeFormation(f);
            }
            if (!_loggedCurrent)
            {
                _loggedCurrent = true;
                var log = NLog.LogManager.GetCurrentClassLogger();
                log.Info("[AmbushTactic] current on AttackerTeam; formations="
                    + FormationsIncludingEmpty.Count);
                foreach (var f in FormationsIncludingEmpty)
                {
                    if (f == null || f.CountOfUnits == 0) continue;
                    var active = f.AI.ActiveBehavior;
                    log.Info("[AmbushTactic]   " + f.PhysicalClass
                        + " aiControlled=" + f.IsAIControlled
                        + " units=" + f.CountOfUnits
                        + " active=" + (active?.GetType().Name ?? "null"));
                }
            }
            base.TickOccasionally();
        }
        private bool _loggedCurrent;

        /// <summary>
        /// When our tactic is <c>CurrentTactic</c>, vanilla tactics stop
        /// ticking so their <c>SetBehaviorWeight</c> calls on
        /// infantry/cavalry formations never fire. Those formations still
        /// have all the pre-seeded behaviors in their AI set (TeamAIGeneral
        /// adds them at team setup) but their weights are frozen at
        /// whatever the previous tactic left them. For ambush fiction
        /// "archers hold the hill, melee engages the road" we baseline
        /// melee/cavalry to a standard charge. Mirrors
        /// <c>TacticFullScaleAttack.Attack</c>'s pattern without the
        /// phase-split (ambush doesn't use pre-engagement marching).
        /// Idempotent: safe to call every tick.
        /// </summary>
        private void ConfigureMeleeFormation(Formation formation)
        {
            formation.AI.ResetBehaviorWeights();
            TacticComponent.SetDefaultBehaviorWeights(formation);
            if (formation.PhysicalClass == FormationClass.Cavalry
                || formation.PhysicalClass == FormationClass.LightCavalry
                || formation.PhysicalClass == FormationClass.HeavyCavalry)
            {
                formation.AI.SetBehaviorWeight<BehaviorFlank>(1f);
                formation.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
            }
            else
            {
                // Infantry (any weight class). Charge from cover, no
                // pre-engagement advance since ambush is point-blank
                // by design.
                formation.AI.SetBehaviorWeight<BehaviorCharge>(1f);
                formation.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
            }
        }

        private void EvaluateFormation(Formation formation, float now, HashSet<int> claimed)
        {
            int bestIdx = -1;
            float bestScore = float.NegativeInfinity;
            AnchorEntry currentEntry;
            bool hasCurrent = _anchors.TryGetValue(formation, out currentEntry);

            for (int i = 0; i < _controller.SceneSpots.Count; i++)
            {
                if (claimed.Contains(i)) continue;
                var spot = _controller.SceneSpots[i];
                bool isCurrent = hasCurrent && spot.Position.NearlyEquals(currentEntry.Spot.Position, 0.01f);
                float score = ScorePoint(spot, formation,
                                          isCurrent ? currentEntry.DwellStartTime : -1f,
                                          now);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestIdx = i;
                }
            }

            if (bestIdx < 0 || bestScore < MinViableScore)
            {
                // No viable position. Drop anchor so FormationAI falls
                // back to vanilla behaviors (Charge / Skirmish / etc.)
                // which are already in the formation's AI set.
                if (hasCurrent)
                {
                    _anchorLogic.UnregisterAnchor(formation);
                    _anchors.Remove(formation);
                    NLog.LogManager.GetCurrentClassLogger().Info(
                        "[AmbushTactic] " + formation.PhysicalClass
                        + " abandoned anchor (bestScore=" + bestScore.ToString("F1") + ")");
                }
                return;
            }

            var picked = _controller.SceneSpots[bestIdx];
            claimed.Add(bestIdx);

            bool samePoint = hasCurrent && picked.Position.NearlyEquals(currentEntry.Spot.Position, 0.01f);
            if (samePoint) return;

            // New assignment (either first time, or picker-driven switch).
            RebindFormation(formation, picked, now);
        }

        private void RebindFormation(Formation formation, AmbushSpot picked, float now)
        {
            var scene = Mission.Current.Scene;
            if (scene == null) return;
            var parent = new WorldPosition(scene, picked.Position);
            var children = new List<WorldPosition>();
            if (picked.TacticalChildren != null)
                foreach (var c in picked.TacticalChildren)
                    children.Add(new WorldPosition(scene, c));

            // Reuse the existing behavior instance if the formation
            // already has one in its AI set (FormationAI has no public
            // Remove). Otherwise construct + AddAiBehavior once.
            var existing = formation.AI.GetBehavior<AmbushBehaviorDefendTacticalPoint>();
            AmbushBehaviorDefendTacticalPoint behavior;
            if (existing != null)
            {
                existing.Rebind(parent, children, picked.ActivationRadius, picked.ActivationIgnoreHeight);
                behavior = existing;
            }
            else
            {
                behavior = new AmbushBehaviorDefendTacticalPoint(
                    formation, parent, children,
                    picked.ActivationRadius, picked.ActivationIgnoreHeight);
                formation.AI.AddAiBehavior(behavior);
            }
            _anchorLogic.RebindAnchor(formation, behavior);
            _anchors[formation] = new AnchorEntry { Spot = picked, DwellStartTime = now };

            NLog.LogManager.GetCurrentClassLogger().Info(
                "[AmbushTactic] " + formation.PhysicalClass + " -> spot "
                + picked.GroupTag + " (tacticalChildren=" + children.Count + ")");
        }

        private float ScorePoint(AmbushSpot point, Formation formation,
                                  float dwellStartTime, float now)
        {
            var enemy = formation.CachedClosestEnemyFormation;
            if (enemy == null) return 0f;
            var enemyMedian = enemy.Formation.CachedMedianPosition;
            var enemyMedianVec2 = enemy.Formation.CachedMedianPosition.AsVec2;

            float score = 0f;
            float elevation = point.Position.Z - enemyMedian.GetGroundVec3().Z;
            // Elevation bonus matters a lot for ambush fiction (archers
            // on high ground). 4 per metre lets a 13m hill contribute
            // ~52 points, enough to override an equally-distanced
            // flat street spot.
            score += 4f * MathF.Max(0f, elevation);

            float distance = point.Position.AsVec2.Distance(enemyMedianVec2);
            // Symmetric linear falloff from OptimalDistance. Max bonus
            // 50 at exactly OptimalDistance (30m), decays by 2 per metre
            // either side. Negative scores fine; they stack against
            // elevation+preferred-class.
            score += 50f - 2f * MathF.Abs(distance - OptimalDistance);
            if (distance < MeleeFloorDistance) score -= 100f;

            // Flank: angle between enemy facing and the vector from
            // enemy median to the anchor. 60°+ counts as flanking.
            var toAnchor = (point.Position.AsVec2 - enemyMedianVec2).Normalized();
            float facingDot = enemy.Formation.Direction.DotProduct(toAnchor);
            if (facingDot < 0.5f) score += 30f;

            if (point.PreferredClass >= 0
                && point.PreferredClass == (int)formation.PhysicalClass)
                score += 30f;

            if (dwellStartTime >= 0f
                && (now - dwellStartTime) >= MinDwellSeconds
                && IsSettledAndFiring(formation, point))
                score += CommitmentBonus;

            return score;
        }

        private bool IsSettledAndFiring(Formation formation, AmbushSpot anchor)
        {
            var avg = formation.CachedAveragePosition;
            if (avg.Distance(anchor.Position.AsVec2) > SettleRadius)
                return false;
            float armedRatio = ArmedRatio(formation);
            if (armedRatio < ArmedRatioThreshold) return false;
            var enemy = formation.CachedClosestEnemyFormation;
            if (enemy == null) return false;
            float d = enemy.Formation.CachedMedianPosition.AsVec2.Distance(anchor.Position.AsVec2);
            return d >= 20f && d <= 150f;
        }

        private static bool HasAnyArmedAgent(Formation formation)
        {
            bool found = false;
            formation.ApplyActionOnEachUnit(a =>
            {
                if (found || a == null || !a.IsActive()) return;
                if (a.HasRangedWeapon(checkHasAmmo: true)) found = true;
            });
            return found;
        }

        private static float ArmedRatio(Formation formation)
        {
            int total = 0, armed = 0;
            formation.ApplyActionOnEachUnit(a =>
            {
                if (a == null || !a.IsActive()) return;
                total++;
                if (a.HasRangedWeapon(checkHasAmmo: true)) armed++;
            });
            return total == 0 ? 0f : (float)armed / total;
        }

        private static bool IsRangedClass(FormationClass cls)
        {
            return cls == FormationClass.Ranged
                || cls == FormationClass.Skirmisher
                || cls == FormationClass.HorseArcher;
        }

        /// <summary>
        /// Walk the ambush layout's default assignments, match each
        /// chunk to its live vanilla <see cref="Formation"/>, and
        /// pre-register the pairing in <see cref="_anchors"/> with
        /// dwell-start backdated so the commitment bonus applies
        /// immediately. Without this seed, the tactic's first
        /// evaluation runs score-only (no commitment) and may move
        /// a formation off its custom-spawn spot just because a
        /// different spot scored marginally higher.
        /// </summary>
        private void SeedAnchorsFromLayout()
        {
            if (_controller.Layout?.Formations == null) return;
            float backdated = Mission.Current.CurrentTime - MinDwellSeconds - 1f;
            // VanillaFormation on the chunk is only populated by the
            // Phase D picker's Recompute; in Phase F it's always null.
            // Resolve by matching the chunk's FormationClass to a live
            // formation on this team instead.
            foreach (var chunk in _controller.Layout.Formations)
            {
                if (!chunk.AssignedSpot.HasValue) continue;
                if (!IsRangedClass(chunk.Class)) continue;
                Formation live = null;
                foreach (var f in FormationsIncludingEmpty)
                {
                    if (f == null || f.CountOfUnits == 0) continue;
                    if (f.PhysicalClass == chunk.Class) { live = f; break; }
                }
                if (live == null) continue;
                _anchors[live] = new AnchorEntry
                {
                    Spot = chunk.AssignedSpot.Value,
                    DwellStartTime = backdated,
                };
                NLog.LogManager.GetCurrentClassLogger().Info(
                    "[AmbushTactic] seed " + live.PhysicalClass
                    + " <- layout spot " + chunk.AssignedSpot.Value.GroupTag);
            }
        }

        protected override void OnCancel()
        {
            base.OnCancel();
            // TeamAI swapped away from this tactic. Leave anchors in
            // place; the per-agent scripted positions on
            // AmbushTacticalAnchorLogic persist until a new tactic
            // explicitly repoints or the mission ends. Prevents
            // archers from falling off the hill mid-swap.
        }
    }
}
