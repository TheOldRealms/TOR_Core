using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.Models;
using TOR_Core.Models.Ambush;

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
        /// <summary>
        /// Per-spot cap from the scene-attached AmbushSpawnPointScript. Zero = unlimited.
        /// </summary>
        public int MaxFormations;
        /// <summary>
        /// Per-spot width budget from the scene-attached AmbushSpawnPointScript. Zero = unlimited.
        /// </summary>
        public float MaxWidth;
        /// <summary>
        /// Preferred FormationClass (cast to int) for this spot, or -1 for
        /// no preference. Sourced from <see cref="AmbushSpawnPointScript.PreferredClass"/>.
        /// Read during default-assignment so matching classes claim preferred
        /// spots before wildcards.
        /// </summary>
        public int PreferredClass;
        /// <summary>
        /// Child <see cref="TaleWorlds.MountAndBlade.TacticalPosition"/>
        /// entities authored under the spawn-point entity. Empty when the
        /// scener didn't attach any. Used by the ranged-anchor behavior:
        /// the formation's plate still sits at <see cref="Position"/> but
        /// individual archers scripted-position onto these children so
        /// they stand on the authored high-ground / cover slots.
        /// </summary>
        public List<Vec3> TacticalChildren;
        /// <summary>Metres the formation plate can be from this spawn
        /// point before its tactical children stop being eligible for
        /// anchor assignment. Sourced from
        /// <see cref="AmbushTacticalActivationZone.Radius"/> on the
        /// spawn-point entity, or <c>30</c> if no zone script is
        /// attached.</summary>
        public float ActivationRadius;
        /// <summary>When true the activation volume is a flat XZ-plane
        /// circle. When false it's a 3D sphere (hill-top spawn-point
        /// elevation counted against street-level plate positions).
        /// Sourced from <see cref="AmbushTacticalActivationZone.IgnoreHeight"/>.</summary>
        public bool ActivationIgnoreHeight;
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
                AssignDefaultSpots(formations, spots, result, mode);
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

        /// <summary>
        /// Split player-side troops of a class into one or two chunks based on the model's chunk threshold.
        /// </summary>
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

        /// <summary>
        /// Split AI-side troops of a class into chunks bounded by spot count and the class min/max size.
        /// </summary>
        private static List<AmbushFormation> ChunkForAi(FormationClass cls,
            List<CharacterObject> troops, int spotCount,
            TORMissionAmbushModel model)
        {
            var list = new List<AmbushFormation>();
            int n = troops.Count;
            int min = model.GetMinFormationSize(cls);
            int max = model.GetMaxFormationSize(cls);

            // How many chunks we could produce without any chunk falling
            // below the class floor. The actual chunk count is capped by
            // the number of matching spots the scene offers.
            int maxChunks = n / System.Math.Max(1, min);
            int chunks = MBMath.ClampInt(spotCount, 1, System.Math.Max(1, maxChunks));

            // Single spot available, too small to split without
            // violating the floor, or at/below the floor: one formation.
            if (chunks < 2 || n <= min)
            {
                list.Add(new AmbushFormation { Class = cls, Troops = troops });
                return list;
            }

            // Probability of splitting ramps linearly from 0 at min to 1
            // at max. At or above max the split is unconditional.
            float splitChance = (float)(n - min) / System.Math.Max(1, max - min);
            if (n < max && MBRandom.RandomFloat >= splitChance)
            {
                list.Add(new AmbushFormation { Class = cls, Troops = troops });
                return list;
            }

            SplitEven(troops, chunks, cls, list);
            return list;
        }

        /// <summary>
        /// Split the troop list into the requested number of near-equal chunks, appending each to output.
        /// </summary>
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
        /// Default spot assignment. Two-pass: first claim every spot whose
        /// <see cref="AmbushSpot.PreferredClass"/> matches the formation's
        /// class, then fill remaining formations from wildcard spots.
        /// Spots authored with a preference always win against wildcards
        /// for the matching class, which is the whole point of the field.
        ///
        /// Player mode iterates spots in scene order so the layout is
        /// stable across restarts (and matches the manual picker's
        /// scene-ordered chip rows). AI mode shuffles the spot list once
        /// per pass so repeat ambushes on the same scene feel different
        /// when multiple spots match a class; the chunking randomness
        /// in <see cref="ChunkForAi"/> was the only prior source of
        /// variety and it only moved troop counts, never positions.
        /// </summary>
        private static void AssignDefaultSpots(List<AmbushFormation> formations,
            IList<AmbushSpot> spots, AmbushLayoutResult result, LayoutMode mode)
        {
            var usedPositions = new HashSet<Vec3>(
                result.DefaultAssignments.Values.Select(s => s.Position));
            IList<AmbushSpot> order = mode == LayoutMode.AI
                ? ShuffleCopy(spots)
                : spots;
            // Pass 1: match preferred-class spots.
            foreach (var f in formations)
            {
                if (f.AssignedSpot.HasValue) continue;
                AmbushSpot? picked = null;
                foreach (var s in order)
                {
                    if (s.PreferredClass != (int)f.Class) continue;
                    if (usedPositions.Contains(s.Position)) continue;
                    picked = s;
                    break;
                }
                if (!picked.HasValue) continue;
                f.AssignedSpot = picked.Value;
                result.DefaultAssignments[f] = picked.Value;
                usedPositions.Add(picked.Value.Position);
            }
            // Pass 2: fill from any remaining spot (preferred or wildcard).
            foreach (var f in formations)
            {
                if (f.AssignedSpot.HasValue) continue;
                AmbushSpot? picked = null;
                foreach (var s in order)
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

        /// <summary>
        /// Return a Fisher-Yates-shuffled copy of the source list.
        /// </summary>
        private static List<AmbushSpot> ShuffleCopy(IList<AmbushSpot> source)
        {
            var copy = new List<AmbushSpot>(source);
            for (int i = copy.Count - 1; i > 0; i--)
            {
                int j = MBRandom.RandomInt(i + 1);
                var tmp = copy[i]; copy[i] = copy[j]; copy[j] = tmp;
            }
            return copy;
        }
    }
}
