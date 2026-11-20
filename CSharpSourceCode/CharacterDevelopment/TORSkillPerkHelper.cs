using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using static TOR_Core.Utilities.TORConstants;

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
    /// Registration carries Career-style filter delegates so per-perk
    /// conditions (e.g. "applies only to Master Ambusher troops", "only
    /// fires when attacker is on foot") live with the perk rather than
    /// at every call site:
    ///  - <c>characterPredicate</c>: gates on the <c>targetCharacter</c>
    ///    passed to Apply. Mirrors
    ///    <c>PassiveEffect.SpecialCharacterEvaluationFunction</c>.
    ///  - <c>combatPredicate</c>: gates on attacker/victim/mask for the
    ///    combat Apply variant. Mirrors
    ///    <c>PassiveEffect.SpecialCombatInteractionFunction</c>.
    /// </summary>
    public static class TORSkillPerkHelper
    {
        private class PerkEffectConfig
        {
            public PerkObject Perk;
            public SkillEffectType EffectType;
            public bool IsPrimary;
            public AttackTypeMask AttackMask = AttackTypeMask.All;
            public Func<CharacterObject, bool> CharacterPredicate;
            public Func<Agent, Agent, AttackTypeMask, bool> CombatPredicate;
            /// <summary>Non-null set restricts this mapping to specific
            /// skill StringIds (used for <see cref="SkillEffectType.TroopSkill"/>
            /// perks that boost only particular weapon skills). Null = any
            /// skill passes.</summary>
            public HashSet<string> TargetSkillIds;
            /// <summary>Per-registration interpretation. <c>true</c> =
            /// multiplicative via <c>AddFactor</c>, <c>false</c> = flat
            /// via <c>Add</c>. Mirrors Career's <c>InterpretAsPercentage</c>
            /// pattern so a single Apply call can fold in mixed-semantic
            /// perks without the call-site picking one.</summary>
            public bool AsFactor;
        }

        private static readonly List<PerkEffectConfig> _configs = new List<PerkEffectConfig>();

        /// <summary>Register a TOR perk against the effect type + optional
        /// filters. Call once at session launch after perks are constructed.
        /// Multiple registrations per perk are allowed (primary and
        /// secondary targeting different effects, or one perk firing on
        /// multiple effect types).</summary>
        public static void Register(PerkObject perk, SkillEffectType effectType, bool isPrimary,
                                     AttackTypeMask attackMask = AttackTypeMask.All,
                                     Func<CharacterObject, bool> characterPredicate = null,
                                     Func<Agent, Agent, AttackTypeMask, bool> combatPredicate = null,
                                     HashSet<string> targetSkillIds = null,
                                     bool asFactor = false)
        {
            if (perk == null) return;
            _configs.Add(new PerkEffectConfig
            {
                Perk = perk,
                EffectType = effectType,
                IsPrimary = isPrimary,
                AttackMask = attackMask,
                CharacterPredicate = characterPredicate,
                CombatPredicate = combatPredicate,
                TargetSkillIds = targetSkillIds,
                AsFactor = asFactor,
            });
        }

        /// <summary>Fold all registered TOR perks of
        /// <paramref name="type"/> that the hero holds — and whose
        /// per-perk filters accept the current context — into
        /// <paramref name="number"/>.
        ///
        /// Pass the <paramref name="targetCharacter"/> that the effect
        /// is landing on (e.g. the troop whose speed is being computed);
        /// per-perk character predicates read this to filter by attribute
        /// or troop flag. Pass <paramref name="mask"/> for attack-type
        /// gating (vanilla's convention: perk mask must intersect
        /// call-site mask).</summary>
        public static void ApplyPerkPassives(Hero hero, ref ExplainedNumber number,
            SkillEffectType type,
            CharacterObject targetCharacter = null,
            AttackTypeMask mask = AttackTypeMask.All,
            SkillObject targetSkill = null,
            Agent attacker = null,
            Agent victim = null)
        {
            if (hero == null) return;
            foreach (var cfg in _configs)
            {
                if (cfg.EffectType != type) continue;
                if ((mask & cfg.AttackMask) == 0) continue;
                if (!hero.GetPerkValue(cfg.Perk)) continue;
                if (cfg.CharacterPredicate != null
                    && (targetCharacter == null || !cfg.CharacterPredicate(targetCharacter))) continue;
                if (cfg.TargetSkillIds != null
                    && (targetSkill == null || !cfg.TargetSkillIds.Contains(targetSkill.StringId))) continue;
                if (cfg.CombatPredicate != null
                    && (attacker == null || !cfg.CombatPredicate(attacker, victim, mask))) continue;

                float bonus = cfg.IsPrimary ? cfg.Perk.PrimaryBonus : cfg.Perk.SecondaryBonus;
                if (bonus == 0f) continue;
                if (cfg.AsFactor) number.AddFactor(bonus, cfg.Perk.Name);
                else number.Add(bonus, cfg.Perk.Name);
            }
        }

        /// <summary>One-shot registration of all TOR skill-tree perks
        /// against their effect-type mappings. Called from
        /// <c>ObjectManagerPatches</c> after <c>TORPerks</c> init.</summary>
        public static void RegisterAllPerks()
        {
            _configs.Clear();

            // Backstabber primary: "+10 damage on your thrust attacks."
            // Applies to the main-hero's own melee attacks (perk text says
            // thrust; AttackTypeMask has no Thrust bucket so we gate on Melee).
            Register(TORPerks.Roguery.Backstabber, SkillEffectType.Damage,
                isPrimary: true, attackMask: AttackTypeMask.Melee);

            // Backstabber secondary: "Troops in your formation deal +4
            // damage on thrust attacks." No MA restriction — all troops.
            Register(TORPerks.Roguery.Backstabber, SkillEffectType.TroopDamage,
                isPrimary: false, attackMask: AttackTypeMask.Melee);

            // Shadowed primary: "+10% combat speed for your Master Ambusher
            // troops in formation." MA filter via character predicate; applies
            // multiplicatively to CombatMaxSpeedMultiplier at stat-calc time.
            Register(TORPerks.Roguery.Shadowed, SkillEffectType.TroopSpeed,
                isPrimary: true,
                characterPredicate: c => c != null
                    && c.HasAttribute(CharacterAttributes.MASTER_AMBUSHER),
                asFactor: true);

            // Skirmisher primary: "+10 vigor and control for your Master
            // Ambusher troops in your formation." Vigor affects OneHanded /
            // TwoHanded / Polearm weapon skills; Control affects Bow /
            // Crossbow / Throwing. Applied as a flat +10 to each of those
            // six skills' effective value for MA troops under the captain,
            // via the TargetSkillIds set.
            Register(TORPerks.Roguery.Skirmisher, SkillEffectType.TroopSkill,
                isPrimary: true,
                characterPredicate: c => c != null
                    && c.HasAttribute(CharacterAttributes.MASTER_AMBUSHER),
                targetSkillIds: new HashSet<string>
                {
                    DefaultSkills.OneHanded.StringId,
                    DefaultSkills.TwoHanded.StringId,
                    DefaultSkills.Polearm.StringId,
                    DefaultSkills.Bow.StringId,
                    DefaultSkills.Crossbow.StringId,
                    DefaultSkills.Throwing.StringId,
                });

            // Strider primary: "+50% damage bonus from speed while fighting
            // on foot." Personal self-effect — applies to the main hero's
            // own attacks. Combat predicate gates on attacker not being
            // mounted. Interpreted as a flat +50% damage factor on on-foot
            // attacks (approximation: vanilla's speed-damage scaling isn't
            // directly reachable without touching the damage engine, so we
            // land the bonus as a generic damage multiplier instead).
            Register(TORPerks.Roguery.Strider, SkillEffectType.Damage,
                isPrimary: true,
                combatPredicate: (attacker, victim, mask) =>
                    attacker != null && !attacker.HasMount,
                asFactor: true);
        }
    }
}
