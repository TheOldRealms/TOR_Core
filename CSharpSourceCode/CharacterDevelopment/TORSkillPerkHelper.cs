using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.CharacterDevelopment
{
    /// <summary>
    /// Parallel to <c>CareerHelper.ApplyBasicCareerPassives</c>, but for
    /// TOR skill-tree perks (Roguery, future skills). Lets model code
    /// fold TOR perk effects into a stat computation via a single
    /// <see cref="ApplyPerkPassives"/> call keyed on
    /// <see cref="SkillEffectType"/>, instead of a per-perk inline check
    /// at every stat site.
    ///
    /// Perks are registered once (session launch) via
    /// <see cref="Register"/> with their effect type + optional attack
    /// mask + isPrimary flag. Model code then calls
    /// <see cref="ApplyPerkPassives"/> at the same site it currently
    /// calls <c>CareerHelper.ApplyBasicCareerPassives</c>.
    ///
    /// Semantics follow vanilla's own perk-apply shape:
    ///  - <c>AddFactor</c> for multiplicative bonuses (asFactor = true)
    ///  - <c>Add</c> for additive bonuses (asFactor = false)
    ///  - <c>AttackTypeMask</c> filter mirrors vanilla's weapon-type gating.
    /// </summary>
    public static class TORSkillPerkHelper
    {
        private struct PerkEffectMapping
        {
            public PerkObject Perk;
            public SkillEffectType EffectType;
            public bool IsPrimary;
            public AttackTypeMask AttackMask;
        }

        private static readonly List<PerkEffectMapping> _mappings = new List<PerkEffectMapping>();

        /// <summary>Register a TOR perk against the effect type and attack
        /// mask it modifies. Call once at session launch after perks are
        /// constructed. Multiple registrations per perk are allowed when
        /// primary and secondary target different effect types or masks.</summary>
        public static void Register(PerkObject perk, SkillEffectType effectType, bool isPrimary,
                                     AttackTypeMask attackMask = AttackTypeMask.All)
        {
            if (perk == null) return;
            _mappings.Add(new PerkEffectMapping
            {
                Perk = perk,
                EffectType = effectType,
                IsPrimary = isPrimary,
                AttackMask = attackMask,
            });
        }

        /// <summary>Fold all registered TOR perks of
        /// <paramref name="type"/> that the hero holds into
        /// <paramref name="number"/>. Mask filter matches
        /// <c>CareerHelper.ApplyBasicCareerPassives</c>'s semantics:
        /// a perk's bonus applies only if its mask intersects the
        /// call-site's mask. Use <paramref name="asFactor"/> true for
        /// multiplicative effects (percent scaling), false for flat
        /// adds.</summary>
        public static void ApplyPerkPassives(Hero hero, ref ExplainedNumber number,
            SkillEffectType type, AttackTypeMask mask = AttackTypeMask.All, bool asFactor = false)
        {
            if (hero == null) return;
            foreach (var m in _mappings)
            {
                if (m.EffectType != type) continue;
                if ((mask & m.AttackMask) == 0) continue;
                if (!hero.GetPerkValue(m.Perk)) continue;
                float bonus = m.IsPrimary ? m.Perk.PrimaryBonus : m.Perk.SecondaryBonus;
                if (bonus == 0f) continue;
                if (asFactor)
                    number.AddFactor(bonus, m.Perk.Name);
                else
                    number.Add(bonus, m.Perk.Name);
            }
        }

        /// <summary>One-shot registration of all TOR skill-tree perks
        /// against their effect-type mappings. Called from
        /// <c>SubModule</c> after <c>TORPerks</c> init.</summary>
        public static void RegisterAllPerks()
        {
            _mappings.Clear();

            // Backstabber primary: +10 damage on your thrust attacks.
            Register(TORPerks.Roguery.Backstabber, SkillEffectType.Damage, isPrimary: true, AttackTypeMask.Melee);
            // Backstabber secondary: +4 damage on thrust attacks for troops in formation.
            Register(TORPerks.Roguery.Backstabber, SkillEffectType.TroopDamage, isPrimary: false, AttackTypeMask.Melee);
        }
    }
}
