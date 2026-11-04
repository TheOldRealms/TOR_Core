using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.Models;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Role tag encoded in the scene entity (ambush_attacker_infantry etc).
    /// </summary>
    public enum SpotClass
    {
        Infantry,
        Archer,
        Cavalry,
        HorseArcher
    }

    /// <summary>
    /// Reassignment bucket: a formation can be moved only within its bucket.
    /// Foot covers Infantry + Archer, Mounted covers Cavalry + HorseArcher.
    /// </summary>
    public enum AmbushBucket
    {
        Foot,
        Mounted
    }

    /// <summary>
    /// Player mode keeps the sidebar legible (max 2 formations per class);
    /// AI mode spreads to fill matching spots with distribution randomness
    /// so repeat ambushes feel different.
    /// </summary>
    public enum LayoutMode
    {
        Player,
        AI
    }

    public static class SpotClassExtensions
    {
        public static AmbushBucket Bucket(this SpotClass c)
            => c == SpotClass.Infantry || c == SpotClass.Archer
                ? AmbushBucket.Foot
                : AmbushBucket.Mounted;

        public static SpotClass FromFormationClass(FormationClass f)
        {
            switch (f)
            {
                case FormationClass.Infantry:        return SpotClass.Infantry;
                case FormationClass.Ranged:          return SpotClass.Archer;
                case FormationClass.Cavalry:         return SpotClass.Cavalry;
                case FormationClass.HorseArcher:     return SpotClass.HorseArcher;
                default:                             return SpotClass.Infantry;
            }
        }
    }

    /// <summary>
    /// A scener-authored spawn position on the ambush scene. One struct per
    /// entity tagged <c>ambush_attacker_*</c>.
    /// </summary>
    public struct AmbushSpot
    {
        public string Tag;
        public SpotClass Class;
        public AmbushBucket Bucket;
        public Vec3 Position;
        public float Yaw;
    }

    /// <summary>
    /// One player-side deployable troop group (auto-chunked roster subset).
    /// Becomes a vanilla <see cref="Formation"/> at mission start. Up to
    /// 8 per mission (2 per class × 4 classes in player mode; capped by
    /// matching spot count in AI mode).
    /// </summary>
    public class AmbushFormation
    {
        public FormationClass Class;
        public List<CharacterObject> Troops;
        public int Count => Troops?.Count ?? 0;
        public AmbushSpot? AssignedSpot;
    }

    /// <summary>
    /// Result of <see cref="AmbushFormationLayout.Compute"/>: the auto-chunked
    /// formations plus a default spot assignment per formation. The UI binds
    /// the formations to sidebar panels; player reassignments via the
    /// click-rectangle dropdown update <see cref="AmbushFormation.AssignedSpot"/>
    /// in place.
    /// </summary>
    public class AmbushLayoutResult
    {
        public List<AmbushFormation> Formations = new List<AmbushFormation>();
        public Dictionary<AmbushFormation, AmbushSpot> DefaultAssignments
            = new Dictionary<AmbushFormation, AmbushSpot>();
    }

    /// <summary>
    /// Pure data model. No Mission access, no UI. Computes the formation
    /// list + default spot bindings from a roster plus the scene's spot
    /// entities. Caller feeds the output into <see cref="AmbushMissionController"/>
    /// at mission open and <see cref="AmbushDeploymentVM"/> for the sidebar.
    /// </summary>
    public static class AmbushFormationLayout
    {
        public static AmbushLayoutResult Compute(TroopRoster ambusher,
                                                  IList<AmbushSpot> spots,
                                                  LayoutMode mode,
                                                  TORMissionAmbushModel model = null)
        {
            model = model ?? new TORMissionAmbushModel();
            var result = new AmbushLayoutResult();
            if (ambusher == null || ambusher.Count == 0 || spots == null)
                return result;

            var byClass = GroupTroopsByClass(ambusher);
            foreach (var kv in byClass)
            {
                var cls = kv.Key;
                var troops = kv.Value;
                if (troops.Count == 0) continue;
                var matchingSpots = spots
                    .Where(s => s.Class == SpotClassExtensions.FromFormationClass(cls))
                    .ToList();
                var formations = mode == LayoutMode.Player
                    ? ChunkForPlayer(cls, troops, model)
                    : ChunkForAi(cls, troops, matchingSpots.Count, model);
                AssignDefaultSpots(formations, matchingSpots, spots, result);
                result.Formations.AddRange(formations);
            }
            return result;
        }

        private static Dictionary<FormationClass, List<CharacterObject>>
            GroupTroopsByClass(TroopRoster roster)
        {
            var dict = new Dictionary<FormationClass, List<CharacterObject>>();
            for (int i = 0; i < roster.Count; i++)
            {
                var elem = roster.GetElementCopyAtIndex(i);
                var chr = elem.Character;
                if (chr == null) continue;
                var cls = chr.GetFormationClass();
                if (!dict.ContainsKey(cls)) dict[cls] = new List<CharacterObject>();
                for (int k = 0; k < elem.Number; k++) dict[cls].Add(chr);
            }
            return dict;
        }

        // ---- Player chunking ----

        private static List<AmbushFormation> ChunkForPlayer(FormationClass cls,
            List<CharacterObject> troops, TORMissionAmbushModel model)
        {
            var list = new List<AmbushFormation>();
            if (troops.Count <= model.PlayerChunkThreshold)
            {
                list.Add(new AmbushFormation { Class = cls, Troops = troops });
                return list;
            }
            int chunks = System.Math.Min(model.PlayerMaxChunksPerClass, 2);
            SplitEven(troops, chunks, cls, list);
            return list;
        }

        // ---- AI chunking with distribution randomness ----

        private static List<AmbushFormation> ChunkForAi(FormationClass cls,
            List<CharacterObject> troops, int matchingSpotCount,
            TORMissionAmbushModel model)
        {
            var list = new List<AmbushFormation>();
            int n = troops.Count;
            int maxByFloor = n / System.Math.Max(1, model.MinAiFormationSize);
            int targetCount = MBMath.ClampInt(matchingSpotCount, 1, System.Math.Max(1, maxByFloor));
            if (targetCount <= 1)
            {
                list.Add(new AmbushFormation { Class = cls, Troops = troops });
                return list;
            }
            var pattern = RollDistributionPattern(model);
            var weights = BuildWeights(pattern, targetCount);
            SplitByWeights(troops, weights, cls, list);
            if (list.Any(f => f.Count < model.MinAiFormationSize))
            {
                list.Clear();
                SplitEven(troops, targetCount, cls, list);
                if (list.Any(f => f.Count < model.MinAiFormationSize))
                {
                    list.Clear();
                    list.Add(new AmbushFormation { Class = cls, Troops = troops });
                }
            }
            return list;
        }

        private enum DistPattern { Concentrated, Even, Asymmetric }

        private static DistPattern RollDistributionPattern(TORMissionAmbushModel m)
        {
            float wC = m.AiDistributionConcentratedWeight;
            float wE = m.AiDistributionEvenWeight;
            float wA = m.AiDistributionAsymmetricWeight;
            float total = wC + wE + wA;
            if (total <= 0f) return DistPattern.Even;
            float roll = MBRandom.RandomFloat * total;
            if (roll < wC) return DistPattern.Concentrated;
            if (roll < wC + wE) return DistPattern.Even;
            return DistPattern.Asymmetric;
        }

        private static float[] BuildWeights(DistPattern p, int count)
        {
            var weights = new float[count];
            switch (p)
            {
                case DistPattern.Concentrated:
                    weights[MBRandom.RandomInt(count)] = 1f;
                    return weights;
                case DistPattern.Even:
                    for (int i = 0; i < count; i++) weights[i] = 1f / count;
                    return weights;
                case DistPattern.Asymmetric:
                    float sum = 0f;
                    for (int i = 0; i < count; i++)
                    {
                        weights[i] = 0.15f + MBRandom.RandomFloat;
                        sum += weights[i];
                    }
                    for (int i = 0; i < count; i++) weights[i] /= sum;
                    Shuffle(weights);
                    return weights;
            }
            return weights;
        }

        private static void Shuffle(float[] a)
        {
            for (int i = a.Length - 1; i > 0; i--)
            {
                int j = MBRandom.RandomInt(i + 1);
                var tmp = a[i]; a[i] = a[j]; a[j] = tmp;
            }
        }

        private static void SplitByWeights(List<CharacterObject> troops,
            float[] weights, FormationClass cls, List<AmbushFormation> output)
        {
            int n = troops.Count;
            int placed = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                int take = i == weights.Length - 1
                    ? n - placed
                    : (int)System.Math.Round(n * weights[i]);
                if (take <= 0 && i < weights.Length - 1) continue;
                var slice = troops.GetRange(placed, take);
                output.Add(new AmbushFormation { Class = cls, Troops = slice });
                placed += take;
            }
        }

        private static void SplitEven(List<CharacterObject> troops, int chunks,
            FormationClass cls, List<AmbushFormation> output)
        {
            int n = troops.Count;
            int baseSize = n / chunks;
            int remainder = n - baseSize * chunks;
            int placed = 0;
            for (int i = 0; i < chunks; i++)
            {
                int size = baseSize + (i < remainder ? 1 : 0);
                var slice = troops.GetRange(placed, size);
                output.Add(new AmbushFormation { Class = cls, Troops = slice });
                placed += size;
            }
        }

        // ---- Default spot assignment ----

        private static void AssignDefaultSpots(List<AmbushFormation> formations,
            List<AmbushSpot> matchingClassSpots, IList<AmbushSpot> allSpots,
            AmbushLayoutResult result)
        {
            var usedPositions = new HashSet<Vec3>(result.DefaultAssignments.Values.Select(s => s.Position));
            foreach (var f in formations)
            {
                var spot = PickFirstUnused(matchingClassSpots, usedPositions);
                if (!spot.HasValue)
                {
                    var bucket = SpotClassExtensions.FromFormationClass(f.Class).Bucket();
                    var bucketSpots = allSpots.Where(s => s.Bucket == bucket).ToList();
                    spot = PickFirstUnused(bucketSpots, usedPositions);
                }
                if (!spot.HasValue) continue;
                f.AssignedSpot = spot.Value;
                result.DefaultAssignments[f] = spot.Value;
                usedPositions.Add(spot.Value.Position);
            }
        }

        private static AmbushSpot? PickFirstUnused(IList<AmbushSpot> candidates,
            HashSet<Vec3> used)
        {
            foreach (var s in candidates)
                if (!used.Contains(s.Position)) return s;
            return null;
        }
    }
}
