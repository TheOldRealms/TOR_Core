using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>Per-troop spotting suitability. Mirror of AmbushTroopStealthScore on the detection side.</summary>
    public static class AmbushTroopSpottingScore
    {
        private const float Baseline = 0.0f;

        // Race factors.
        private const float WoodElfSpottingBonus = 0.1f;
        private const float DwarfSpottingBonus = 0.05f;

        // Role modifiers.
        private const float RangedBonus = 0.1f;
        private const float HorseArcherBonus = 0.1f;
        private const float KnightlyPenalty = -0.3f;

        // Smooth weight penalty on top of the knightly flag.
        private const float WeightPenaltyPerKg = 0.004f;
        private const float WeightPenaltyCap = 0.2f;

        // MasterScout floors the troop at specialist territory.
        private const float MasterScoutFloor = 1.0f;

        private const float ScoreFloor = -0.5f;
        private const float ScoreCeiling = 1.5f;

        private static readonly Dictionary<string, float> _baseCache = new Dictionary<string, float>();

        /// <summary>Precompute base scores for every CharacterObject. Called once per session.</summary>
        public static void PrecomputeAll()
        {
            _baseCache.Clear();
            foreach (var c in MBObjectManager.Instance.GetObjectTypeList<CharacterObject>())
            {
                if (c == null || string.IsNullOrEmpty(c.StringId)) continue;
                _baseCache[c.StringId] = ComputeBase(c);
            }
        }

        /// <summary>Base spotting score for a single troop.</summary>
        public static float GetBaseTroopScore(CharacterObject c)
        {
            if (c == null) return Baseline;
            if (_baseCache.TryGetValue(c.StringId, out var cached)) return cached;
            float v = ComputeBase(c);
            _baseCache[c.StringId] = v;
            return v;
        }

        /// <summary>Healthy-weighted party score with size damping.</summary>
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
            // MasterScout floor applies after penalties. Heroes excluded; it's a troop-template marker.
            if (!c.IsHero
                && c.HasAttribute(CharacterAttributes.MASTER_SCOUT)
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
            // Race via FaceGen-based IsElf / IsDwarf; both Wood Elves and Eonir fall under IsElf.
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
