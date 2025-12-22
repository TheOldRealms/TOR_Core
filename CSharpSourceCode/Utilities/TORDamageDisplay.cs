using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.Extensions;

namespace TOR_Core.Utilities
{
    public static class TORDamageDisplay
    {
        /// <summary>
        /// Displays aggregate spell damage for all targets hit by a single spell cast.
        /// </summary>
        public static void DisplayAggregateSpellDamage(DamageType damageType, int totalDamage, int agentsAffected, int agentsKilled, string spellName = null)
        {
            var displayColor = GetDamageTypeColor(damageType);
            string damageTypeIcon = GetDamageTypeIcon(damageType);

            var spellNameText = string.IsNullOrEmpty(spellName)
                ? TORTextHelper.GetTextObject("tor_damage_display_spell", "Spell")
                : new TextObject(spellName);

            var damageTypeText = GetDamageTypeTextObject(damageType);
            var targetText = GetTargetTextObject(agentsAffected);

            TextObject resultText;
            if (agentsKilled > 0)
            {
                resultText = TORTextHelper.GetTextObject("tor_damage_display_spell_damage_with_kills",
                    "{ICON} {SPELL_NAME} dealt {DAMAGE} {DAMAGE_TYPE} damage to {COUNT} {TARGET_TEXT}, {KILL_COUNT} eliminated");
                resultText.SetTextVariable("KILL_COUNT", agentsKilled);
            }
            else
            {
                resultText = TORTextHelper.GetTextObject("tor_damage_display_spell_damage",
                    "{ICON} {SPELL_NAME} dealt {DAMAGE} {DAMAGE_TYPE} damage to {COUNT} {TARGET_TEXT}");
            }

            resultText.SetTextVariable("ICON", damageTypeIcon);
            resultText.SetTextVariable("SPELL_NAME", spellNameText);
            resultText.SetTextVariable("DAMAGE", totalDamage);
            resultText.SetTextVariable("DAMAGE_TYPE", damageTypeText);
            resultText.SetTextVariable("COUNT", agentsAffected);
            resultText.SetTextVariable("TARGET_TEXT", targetText);

            InformationManager.DisplayMessage(new InformationMessage(resultText.ToString(), displayColor));
        }

        private static Color GetDamageTypeColor(DamageType damageType)
        {
            return damageType switch
            {
                DamageType.Fire => Colors.Red,
                DamageType.Holy => Colors.Yellow,
                DamageType.Lightning => Color.FromUint(5745663),
                DamageType.Magical => Colors.Cyan,
                DamageType.Frost => Color.FromUint(8909823),
                _ => Color.White
            };
        }

        private static string GetDamageTypeIcon(DamageType damageType)
        {
            string iconName = damageType switch
            {
                DamageType.Fire => "traits_fire_icon",
                DamageType.Holy => "traits_holy_icon",
                DamageType.Lightning => "traits_lightning_icon",
                DamageType.Magical => "traits_magic_icon",
                DamageType.Frost => "traits_frost_icon",
                _ => null
            };

            return string.IsNullOrEmpty(iconName) ? "" : $"<img src=\"{iconName}\"/>";
        }

        /// <summary>
        /// Displays aggregate spell healing for all targets healed by a single spell cast.
        /// </summary>
        public static void DisplayAggregateSpellHealing(int totalHealing, int agentsAffected, string spellName = null)
        {
            var displayColor = Colors.Green;

            var spellNameText = string.IsNullOrEmpty(spellName)
                ? TORTextHelper.GetTextObject("tor_damage_display_spell", "Spell")
                : new TextObject(spellName);

            var targetText = GetTargetTextObject(agentsAffected);

            var resultText = TORTextHelper.GetTextObject("tor_damage_display_spell_healing",
                "{ICON} {SPELL_NAME} healed {HEALING} health to {COUNT} {TARGET_TEXT}");

            resultText.SetTextVariable("ICON", "<img src=\"heart_icon\"/>");
            resultText.SetTextVariable("SPELL_NAME", spellNameText);
            resultText.SetTextVariable("HEALING", totalHealing);
            resultText.SetTextVariable("COUNT", agentsAffected);
            resultText.SetTextVariable("TARGET_TEXT", targetText);

            InformationManager.DisplayMessage(new InformationMessage(resultText.ToString(), displayColor));
        }

        private static TextObject GetTargetTextObject(int count)
        {
            return count == 1
                ? TORTextHelper.GetTextObject("tor_damage_display_target_singular", "target")
                : TORTextHelper.GetTextObject("tor_damage_display_target_plural", "targets");
        }

        private static TextObject GetDamageTypeTextObject(DamageType damageType)
        {
            return damageType switch
            {
                DamageType.Fire => TORTextHelper.GetTextObject("tor_damage_type_fire", "Fire"),
                DamageType.Holy => TORTextHelper.GetTextObject("tor_damage_type_holy", "Holy"),
                DamageType.Lightning => TORTextHelper.GetTextObject("tor_damage_type_lightning", "Lightning"),
                DamageType.Magical => TORTextHelper.GetTextObject("tor_damage_type_magical", "Magical"),
                DamageType.Frost => TORTextHelper.GetTextObject("tor_damage_type_frost", "Frost"),
                _ => TORTextHelper.GetTextObject("tor_damage_type_physical", "Physical")
            };
        }
    }
}
