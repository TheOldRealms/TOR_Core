using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Phase F prototype — per-troop stealth suitability. Values are in
    /// roughly [0, 1] where 1 is "perfect ambusher" (goblin skirmisher)
    /// and 0 is "could not possibly hide" (ogre, lancer knight).
    ///
    /// Split into two layers:
    /// - **Base** (this class): fixed-per-troop values — race, mount,
    ///   knightly tier, equipment weight, MasterAmbusher attribute.
    ///   Precomputed at session launch into <see cref="_baseCache"/> and
    ///   never recomputed; the whole roster's base score is a sequence
    ///   of O(1) dict lookups.
    /// - **Dynamic bonus** (lives on <c>TORAmbushModel.GetDynamicStealth-
    ///   Bonus</c>, called by whoever needs the full score): per-party
    ///   runtime deltas — leader perks, buffs, culture-level dynamic
    ///   effects. Zero in the base implementation; override points for
    ///   future perk hooks.
    ///
    /// Final party stealth value = <see cref="GetBasePartyScore"/> + the
    /// dynamic bonus. Keeping them separate avoids re-walking the roster
    /// each tick for the heavy per-troop factors.
    /// </summary>
    public static class AmbushTroopStealthScore
    {
        // Baseline for a neutral human foot infantry troop.
        private const float Baseline = 0.5f;

        // Race-based factors.
        private const float GoblinBodyBonus = 0.3f;    // small, sneaky
        private const float OrcBodyPenalty = -0.2f;    // bulky, loud
        private const float MonstrousPenalty = -0.5f;  // trolls / giants — practically impossible
        private const float UndeadPenalty = -0.1f;     // skeletons rattle; vampires override at hero level
        private const float TreeSpiritBonus = 0.2f;    // woodland camouflage

        // Equipment / role factors.
        private const float MountedPenalty = -0.3f;    // horses are loud
        private const float KnightlyPenalty = -0.2f;   // heavy plate on top of mount
        // Equipment weight: tuned so a bare-chested goblin (~5kg) is
        // ~neutral and a fully-plated knight (~40kg) adds ~−0.15. Scales
        // linearly with sum of equipped weights.
        private const float WeightPenaltyPerKg = 0.004f;
        private const float WeightPenaltyCap = 0.2f;

        // MasterAmbusher floor — specialist units can't drop below this
        // regardless of mount / size / weight penalties.
        private const float MasterAmbusherFloor = 0.6f;

        private static readonly Dictionary<string, float> _baseCache = new Dictionary<string, float>();

        /// <summary>Precomputes the base stealth score for every registered
        /// CharacterObject in the ObjectManager and caches it. Call once
        /// per session (fires from <see cref="AiAmbushBehavior.OnSessionLaunched"/>
        /// so the dict is populated before any decision tick runs). The
        /// base is static per troop StringId — equipment / race / mount /
        /// attributes don't shift mid-session. Dynamic deltas (perks /
        /// buffs) live on <c>TORAmbushModel.GetDynamicStealthBonus</c>.
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

        /// <summary>Fast O(1) read from the precomputed base. Falls back
        /// to on-the-fly compute if the cache hasn't been primed (e.g.
        /// in tests). Returns the baseline for null input.</summary>
        public static float GetBaseTroopScore(CharacterObject c)
        {
            if (c == null) return Baseline;
            if (_baseCache.TryGetValue(c.StringId, out var cached)) return cached;
            // Shouldn't normally hit — session launch populates everyone.
            // Lazy fallback keeps callers robust against pre-session use.
            float v = ComputeBase(c);
            _baseCache[c.StringId] = v;
            return v;
        }

        /// <summary>Party-level base score: healthy-weighted average of
        /// per-troop base scores. Pair with
        /// <c>TORAmbushModel.GetDynamicStealthBonus(party)</c> for the
        /// full stealth value. Baseline when the party has no healthy
        /// troops.</summary>
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

        private static float ComputeBase(CharacterObject c)
        {
            float score = Baseline;
            score += RaceFactor(c);
            if (c.IsMounted) score += MountedPenalty;
            if (c.HasAttribute(CharacterAttributes.KNIGHTLY)) score += KnightlyPenalty;
            score -= WeightPenalty(c);
            // MasterAmbusher: floor at a specialist-worthy value, don't
            // let mount / plate / weight pull it below. Deliberately
            // AFTER the penalties so you still get the delta if the base
            // race was already higher than the floor.
            if (c.HasAttribute(CharacterAttributes.MASTER_AMBUSHER)
                && score < MasterAmbusherFloor)
            {
                score = MasterAmbusherFloor;
            }
            if (score < 0f) score = 0f;
            if (score > 1f) score = 1f;
            return score;
        }

        private static float RaceFactor(CharacterObject c)
        {
            // Checks in priority order — most-specific first.
            if (c.HasAttribute(CharacterAttributes.MONSTROUS)) return MonstrousPenalty;
            if (c.HasAttribute(CharacterAttributes.TREE_SPIRIT)) return TreeSpiritBonus;
            if (c.HasAttribute(CharacterAttributes.UNDEAD)) return UndeadPenalty;
            if (c.IsGoblin()) return GoblinBodyBonus;
            if (c.IsOrc()) return OrcBodyPenalty;
            return 0f;  // neutral humanoid (empire, bretonnia, aserai, etc.)
        }

        private static float WeightPenalty(CharacterObject c)
        {
            // Sum weights of items in the first battle equipment set.
            // Vanilla heroes / troops carry at most one battle equipment
            // reference — good enough for a prototype. If future TOR work
            // adds alternate loadouts, average or max across the set.
            var equipment = c.FirstBattleEquipment;
            if (equipment == null) return 0f;
            float totalWeight = 0f;
            // 12 standard equipment slots (weapons 0-3, armor pieces,
            // horse, horse harness). EquipmentIndex.NumEquipmentSetSlots
            // is the loop bound; iterate numerically to stay enum-agnostic.
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
