using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.Models.Ambush
{
    /// <summary>
    /// Per-troop stealth suitability roughly in [0, 1]. Precomputed per session; combine with a dynamic bonus for the full party score.
    /// </summary>
    public static class AmbushTroopStealthScore
    {
        private const float Baseline = 0.5f;

        // Race factors.
        private const float GoblinBodyBonus = 0.3f;
        private const float OrcBodyPenalty = -0.2f;
        private const float MonstrousPenalty = -0.5f;
        private const float UndeadPenalty = -0.1f;
        private const float TreeSpiritBonus = 0.2f;

        // Equipment / role factors.
        private const float MountedPenalty = -0.3f;
        private const float KnightlyPenalty = -0.2f;
        // Weight scales linearly; cap keeps a single heavy piece from dominating.
        private const float WeightPenaltyPerKg = 0.004f;
        private const float WeightPenaltyCap = 0.2f;

        // MasterAmbusher floor.
        private const float MasterAmbusherFloor = 0.6f;

        // Clamp bounds; negative scores allowed so heavy cav drags the party average down.
        private const float ScoreFloor = -0.5f;
        private const float ScoreCeiling = 1.0f;

        private static readonly Dictionary<string, float> _baseCache = new Dictionary<string, float>();

        /// <summary>
        /// Precompute base scores for every CharacterObject. Called once per session.
        /// </summary>
        public static void PrecomputeAll()
        {
            _baseCache.Clear();
            foreach (var c in MBObjectManager.Instance.GetObjectTypeList<CharacterObject>())
            {
                if (c == null || string.IsNullOrEmpty(c.StringId)) continue;
                _baseCache[c.StringId] = ComputeBase(c);
            }
        }

        /// <summary>
        /// O(1) read from the precomputed base, falls back to compute if missing.
        /// </summary>
        public static float GetBaseTroopScore(CharacterObject c)
        {
            if (c == null) return Baseline;
            if (_baseCache.TryGetValue(c.StringId, out var cached)) return cached;
            float v = ComputeBase(c);
            _baseCache[c.StringId] = v;
            return v;
        }

        /// <summary>
        /// Healthy-weighted party average of base troop scores.
        /// </summary>
        public static float GetBasePartyScore(MobileParty party)
        {
            if (party?.MemberRoster == null) return Baseline;
            float sum = 0f;
            int count = 0;
            for (int i = 0; i < party.MemberRoster.Count; i++)
            {
                var elem = party.MemberRoster.GetElementCopyAtIndex(i);
                if (elem.Character == null) continue;
                int healthy = elem.Number - elem.WoundedNumber;
                if (healthy <= 0) continue;
                sum += GetBaseTroopScore(elem.Character) * healthy;
                count += healthy;
            }
            return count > 0 ? sum / count : Baseline;
        }

        /// <summary>
        /// Build a single troop's stealth score from race, mount, knightly tier, and gear weight.
        /// </summary>
        private static float ComputeBase(CharacterObject c)
        {
            float score = Baseline;
            score += RaceFactor(c);
            if (c.IsMounted) score += MountedPenalty;
            if (IsKnightlyTier(c)) score += KnightlyPenalty;
            score -= WeightPenalty(c);
            // MasterAmbusher floor applies after penalties. Heroes excluded; it's a troop-template marker.
            if (!c.IsHero
                && c.HasAttribute(CharacterAttributes.MASTER_AMBUSHER)
                && score < MasterAmbusherFloor)
            {
                score = MasterAmbusherFloor;
            }
            if (score < ScoreFloor) score = ScoreFloor;
            if (score > ScoreCeiling) score = ScoreCeiling;
            return score;
        }

        /// <summary>
        /// True when the troop carries any of TOR's culture-specific knightly-tier attribute tags.
        /// </summary>
        private static bool IsKnightlyTier(CharacterObject c)
        {
            return c.HasAttribute(CharacterAttributes.EMPIRE_KNIGHT)
                || c.HasAttribute(CharacterAttributes.BRETONNIAN_KNIGHT);
        }

        /// <summary>
        /// Return the race-based stealth modifier for TOR body types.
        /// </summary>
        private static float RaceFactor(CharacterObject c)
        {
            // Priority order, most specific first.
            if (c.HasAttribute(CharacterAttributes.MONSTROUS)) return MonstrousPenalty;
            if (c.HasAttribute(CharacterAttributes.TREE_SPIRIT)) return TreeSpiritBonus;
            if (c.HasAttribute(CharacterAttributes.UNDEAD)) return UndeadPenalty;
            if (c.IsGoblin()) return GoblinBodyBonus;
            if (c.IsOrc()) return OrcBodyPenalty;
            return 0f;
        }

        /// <summary>
        /// Penalty proportional to total equipment weight, capped at WeightPenaltyCap.
        /// </summary>
        private static float WeightPenalty(CharacterObject c)
        {
            // Sum weights across the first battle equipment set.
            var equipment = c.FirstBattleEquipment;
            if (equipment == null) return 0f;
            float totalWeight = 0f;
            // Iterate numerically to stay enum-agnostic.
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
