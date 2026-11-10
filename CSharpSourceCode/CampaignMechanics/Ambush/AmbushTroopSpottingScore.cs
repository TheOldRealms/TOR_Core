using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Per-troop spotting suitability. Mirror of
    /// <see cref="AmbushTroopStealthScore"/> on the detection side —
    /// where StealthScore answers "how well does this troop hide?",
    /// SpottingScore answers "how well does this troop see?"
    ///
    /// Scores are precomputed once per session into <see cref="_baseCache"/>
    /// (same lifecycle hook as the stealth score) and read via O(1)
    /// dict lookups. The party-level score is a healthy-weighted average
    /// with size damping so a specialist patrol doesn't lose its
    /// advantage when the leader also drags a baggage train.
    ///
    /// Consumed by <see cref="Models.TORAmbushModel.ScoutingDetectionMultiplier"/>
    /// (primary signal) and <see cref="Models.TORMapVisibilityModel.GetPartySpottingRange"/>
    /// (smaller weight — fog-of-war is adjacent to the ambush mechanic).
    /// See campaign-stealth-design.md §18.
    /// </summary>
    public static class AmbushTroopSpottingScore
    {
        // Baseline neutral troop.
        private const float Baseline = 0.0f;

        // Race factors — less variance than StealthScore's race table;
        // spotting is less race-coded than hiding.
        private const float WoodElfSpottingBonus = 0.1f;   // sylvan sight (Eonir inherit via WOOD_ELF attr)
        private const float DwarfSpottingBonus = 0.05f;    // stone sense, gloomy-terrain focus

        // Role modifiers.
        private const float RangedBonus = 0.1f;            // long-range targeting trains the eye
        private const float HorseArcherBonus = 0.1f;       // mobility + vantage
        private const float KnightlyPenalty = -0.3f;       // heavy plate caps peripheral vision

        // Weight penalty — smooth cost on top of the knightly flag.
        private const float WeightPenaltyPerKg = 0.004f;
        private const float WeightPenaltyCap = 0.2f;

        // MasterScout attribute floors the troop at specialist territory
        // regardless of other penalties. Mirrors MasterAmbusher on the
        // stealth side.
        private const float MasterScoutFloor = 1.0f;

        private const float ScoreFloor = -0.5f;
        private const float ScoreCeiling = 1.5f;

        private static readonly Dictionary<string, float> _baseCache = new Dictionary<string, float>();

        /// <summary>Called from <see cref="AiAmbushBehavior.OnSessionLaunched"/>
        /// alongside <see cref="AmbushTroopStealthScore.PrecomputeAll"/>.
        /// The base is static per troop StringId — equipment / race /
        /// attributes don't shift mid-session.</summary>
        public static void PrecomputeAll()
        {
            _baseCache.Clear();
            foreach (var c in MBObjectManager.Instance.GetObjectTypeList<CharacterObject>())
            {
                if (c == null || string.IsNullOrEmpty(c.StringId)) continue;
                _baseCache[c.StringId] = ComputeBase(c);
            }
        }

        public static float GetBaseTroopScore(CharacterObject c)
        {
            if (c == null) return Baseline;
            if (_baseCache.TryGetValue(c.StringId, out var cached)) return cached;
            float v = ComputeBase(c);
            _baseCache[c.StringId] = v;
            return v;
        }

        /// <summary>Party-level score with size damping. Formula:
        /// <c>avg(troop scores) * min(1, SizeDampingReference / totalHealthy)</c>.
        /// Keeps small specialist patrols at their full average while
        /// damping a mega-army's roster-wide bonus — a banner-of-banners
        /// with Master Scouts in it still gets *some* credit but not
        /// dominant.</summary>
        public static float GetBasePartyScore(MobileParty party, float sizeDampingReference)
        {
            if (party?.MemberRoster == null) return Baseline;
            float weightedSum = 0f;
            int totalHealthy = 0;
            for (int i = 0; i < party.MemberRoster.Count; i++)
            {
                var elem = party.MemberRoster.GetElementCopyAtIndex(i);
                if (elem.Character == null) continue;
                int healthy = elem.Number - elem.WoundedNumber;
                if (healthy <= 0) continue;
                weightedSum += GetBaseTroopScore(elem.Character) * healthy;
                totalHealthy += healthy;
            }
            if (totalHealthy == 0) return Baseline;
            float avg = weightedSum / totalHealthy;
            float damp = sizeDampingReference / totalHealthy;
            if (damp > 1f) damp = 1f;
            return avg * damp;
        }

        private static float ComputeBase(CharacterObject c)
        {
            float score = Baseline;
            score += RaceFactor(c);
            score += FormationFactor(c);
            if (IsKnightlyTier(c)) score += KnightlyPenalty;
            score -= WeightPenalty(c);
            // MasterScout floor — applied AFTER penalties so a specialist
            // troop still accumulates deltas up to the floor if their
            // base race / class was already higher.
            if (c.HasAttribute(CharacterAttributes.MASTER_SCOUT)
                && score < MasterScoutFloor)
            {
                score = MasterScoutFloor;
            }
            if (score < ScoreFloor) score = ScoreFloor;
            if (score > ScoreCeiling) score = ScoreCeiling;
            return score;
        }

        private static bool IsKnightlyTier(CharacterObject c)
        {
            return c.HasAttribute(CharacterAttributes.EMPIRE_KNIGHT)
                || c.HasAttribute(CharacterAttributes.BRETONNIAN_KNIGHT);
        }

        private static float RaceFactor(CharacterObject c)
        {
            // Race goes through the FaceGen-based IsElf / IsDwarf helpers
            // rather than attributes. Eonir inherit from the "elf" race so
            // both Wood Elves and Eonir fall under IsElf.
            if (c.IsElf()) return WoodElfSpottingBonus;
            if (c.IsDwarf()) return DwarfSpottingBonus;
            return 0f;
        }

        private static float FormationFactor(CharacterObject c)
        {
            switch (c.DefaultFormationClass)
            {
                case FormationClass.HorseArcher: return HorseArcherBonus;
                case FormationClass.Ranged: return RangedBonus;
                default: return 0f;
            }
        }

        private static float WeightPenalty(CharacterObject c)
        {
            var equipment = c.FirstBattleEquipment;
            if (equipment == null) return 0f;
            float totalWeight = 0f;
            for (int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)
            {
                var slot = equipment[i];
                if (slot.IsEmpty) continue;
                var item = slot.Item;
                if (item == null) continue;
                totalWeight += item.Weight;
            }
            float raw = totalWeight * WeightPenaltyPerKg;
            return raw > WeightPenaltyCap ? WeightPenaltyCap : raw;
        }
    }
}
