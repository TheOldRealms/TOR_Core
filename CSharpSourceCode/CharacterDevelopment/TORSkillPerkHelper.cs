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
    /// Registry of TOR perks keyed by <see cref="SkillEffectType"/>.
    /// Model code calls <see cref="ApplyPerkPassives"/> to fold every
    /// applicable perk the hero holds into a running number.
    /// </summary>
    public static class TORSkillPerkHelper
    {
        /// <summary>
        /// Per registration filter block for one perk.
        /// </summary>
        private class SkillEffectConfig
        {
            public PerkObject Perk;
            public SkillEffectType EffectType;
            public bool IsPrimary;
            public AttackTypeMask AttackMask = AttackTypeMask.All;
            public Func<CharacterObject, bool> CharacterPredicate;
            public Func<Agent, Agent, AttackTypeMask, bool> CombatPredicate;
            /// <summary>
            /// Non-null set restricts the mapping to specific skill ids.
            /// </summary>
            public HashSet<string> TargetSkillIds;
            /// <summary>
            /// True applies the bonus through <c>AddFactor</c>, false uses <c>Add</c>.
            /// </summary>
            public bool AsFactor;
        }

        private static readonly List<SkillEffectConfig> _configs = new List<SkillEffectConfig>();

        /// <summary>
        /// Register a perk against an effect type. Call once during setup.
        /// A perk can register more than once to cover multiple effects.
        /// </summary>
        public static void Register(PerkObject perk, SkillEffectType effectType, bool isPrimary,
                                     AttackTypeMask attackMask = AttackTypeMask.All,
                                     Func<CharacterObject, bool> characterPredicate = null,
                                     Func<Agent, Agent, AttackTypeMask, bool> combatPredicate = null,
                                     HashSet<string> targetSkillIds = null,
                                     bool asFactor = false)
        {
            if (perk == null) return;
            _configs.Add(new SkillEffectConfig
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

        /// <summary>
        /// Fold every registered perk of <paramref name="type"/> that the
        /// hero holds and that passes the registered filters into
        /// <paramref name="number"/>.
        /// </summary>
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

        /// <summary>
        /// Register every TOR skill-tree perk against its effect type.
        /// Called once at session launch after <c>TORPerks</c> is built.
        /// </summary>
        public static void RegisterAllPerks()
        {
            _configs.Clear();

            // Backstabber primary: flat thrust damage for the main hero.
            Register(TORPerks.Roguery.Backstabber, SkillEffectType.Damage,
                isPrimary: true, attackMask: AttackTypeMask.Melee);

            // Backstabber secondary: formation troops gain thrust damage.
            Register(TORPerks.Roguery.Backstabber, SkillEffectType.TroopDamage,
                isPrimary: false, attackMask: AttackTypeMask.Melee);

            // Shadowed primary: Master Ambusher troops move faster under captain.
            Register(TORPerks.Roguery.Shadowed, SkillEffectType.TroopSpeed,
                isPrimary: true,
                characterPredicate: c => c != null
                    && c.HasAttribute(CharacterAttributes.MASTER_AMBUSHER),
                asFactor: true);

            // Skirmisher primary: vigor and control for Master Ambusher troops.
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

            // Strider primary: on-foot damage bonus for the main hero.
            Register(TORPerks.Roguery.Strider, SkillEffectType.Damage,
                isPrimary: true,
                combatPredicate: (attacker, victim, mask) =>
                    attacker != null && !attacker.HasMount,
                asFactor: true);

            // Governor passives below. Each fires in a dedicated model path.

            // Fencer secondary: flat tariff revenue for governed towns.
            Register(TORPerks.Roguery.Fencer, SkillEffectType.GovernorTariff,
                isPrimary: false);

            // Fence secondary: extra militia per day while besieged.
            Register(TORPerks.Roguery.Fence, SkillEffectType.GovernorBesiegedMilitia,
                isPrimary: false);

            // Unarmed secondary: extra food per day while besieged.
            Register(TORPerks.Roguery.Unarmed, SkillEffectType.GovernorBesiegedFood,
                isPrimary: false);

            // Strider secondary: denars per day while a tournament is active.
            Register(TORPerks.Roguery.Strider, SkillEffectType.GovernorTournamentIncome,
                isPrimary: false);

            // Unarmed primary: longer stun duration on kick attacks.
            Register(TORPerks.Roguery.Unarmed, SkillEffectType.KickStunDuration,
                isPrimary: true,
                asFactor: true);
        }
    }
}
