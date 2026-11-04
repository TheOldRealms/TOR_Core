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
    /// Player mode keeps the sidebar legible (max 2 formations per class);
    /// AI mode spreads to fill matching spots with distribution randomness
    /// so repeat ambushes feel different.
    /// </summary>
    public enum LayoutMode
    {
        Player,
        AI
    }

    /// <summary>
    /// A scener-authored spawn position on the ambush scene. Each entity
    /// tagged <c>attacker_ambush_spawnpoint</c> carries one additional group
    /// tag (e.g. <c>attacker_ambush_hill</c>, <c>attacker_ambush_street</c>)
    /// that the picker UI uses for column headers and the chip label uses
    /// for localized display.
    /// </summary>
    public struct AmbushSpot
    {
        public string GroupTag;
        public Vec3 Position;
        public float Yaw;
        /// <summary>Per-spot cap from the scene-attached AmbushSpawnPointScript. Zero = unlimited.</summary>
        public int MaxFormations;
        /// <summary>Per-spot width budget from the scene-attached AmbushSpawnPointScript. Zero = unlimited.</summary>
        public float MaxWidth;
    }

    /// <summary>
    /// One player-side deployable troop group (auto-chunked roster subset).
    /// Becomes a vanilla <see cref="TaleWorlds.MountAndBlade.Formation"/> at
    /// mission start.
    /// </summary>
    public class AmbushFormation
    {
        public FormationClass Class;
        public List<CharacterObject> Troops;
        public int Count => Troops?.Count ?? 0;
        public AmbushSpot? AssignedSpot;
        /// <summary>
        /// The vanilla <see cref="Formation"/> this chunk is bound to.
        /// Set lazily the first time an <c>OrderOfBattleFormationItemVM</c>
        /// panel for a vanilla Formation runs through Recompute.
        /// </summary>
        public TaleWorlds.MountAndBlade.Formation VanillaFormation;
    }

    public class AmbushLayoutResult
    {
        public List<AmbushFormation> Formations = new List<AmbushFormation>();
        public Dictionary<AmbushFormation, AmbushSpot> DefaultAssignments
            = new Dictionary<AmbushFormation, AmbushSpot>();
    }

    /// <summary>
    /// Pure data model. Produces the formation list + default spot bindings
    /// from a roster plus the scene's spot entities. Caller feeds the output
    /// into <see cref="AmbushMissionController"/> at mission open and the
    /// deployment UI for the sidebar + picker.
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
                var formations = mode == LayoutMode.Player
                    ? ChunkForPlayer(cls, troops, model)
                    : ChunkForAi(cls, troops, spots.Count, model);
                AssignDefaultSpots(formations, spots, result);
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

        private static List<AmbushFormation> ChunkForAi(FormationClass cls,
            List<CharacterObject> troops, int spotCount,
            TORMissionAmbushModel model)
        {
            var list = new List<AmbushFormation>();
            int n = troops.Count;
            int maxByFloor = n / System.Math.Max(1, model.MinAiFormationSize);
            int targetCount = MBMath.ClampInt(spotCount, 1, System.Math.Max(1, maxByFloor));
            if (targetCount <= 1)
            {
                list.Add(new AmbushFormation { Class = cls, Troops = troops });
                return list;
            }
            var pattern = RollDistributionPattern(model);
            var weights = BuildWeights(pattern, targetCount);
            SplitByWeights(troops, weights, cls, list);
            if (ContainsUndersized(list, model.MinAiFormationSize))
            {
                list.Clear();
                SplitEven(troops, targetCount, cls, list);
                if (ContainsUndersized(list, model.MinAiFormationSize))
                {
                    list.Clear();
                    list.Add(new AmbushFormation { Class = cls, Troops = troops });
                }
            }
            return list;
        }

        private static bool ContainsUndersized(List<AmbushFormation> list, int min)
        {
            foreach (var f in list) if (f.Count < min) return true;
            return false;
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

        /// <summary>
        /// Default spot assignment: first-unused spot in scene iteration order,
        /// no class or bucket filtering. The player can reassign via the UI
        /// after deployment opens.
        /// </summary>
        private static void AssignDefaultSpots(List<AmbushFormation> formations,
            IList<AmbushSpot> spots, AmbushLayoutResult result)
        {
            var usedPositions = new HashSet<Vec3>(
                result.DefaultAssignments.Values.Select(s => s.Position));
            foreach (var f in formations)
            {
                AmbushSpot? picked = null;
                foreach (var s in spots)
                {
                    if (!usedPositions.Contains(s.Position))
                    {
                        picked = s;
                        break;
                    }
                }
                if (!picked.HasValue) continue;
                f.AssignedSpot = picked.Value;
                result.DefaultAssignments[f] = picked.Value;
                usedPositions.Add(picked.Value.Position);
            }
        }
    }
}
