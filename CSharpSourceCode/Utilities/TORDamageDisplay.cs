using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.BattleMechanics.DamageSystem;

namespace TOR_Core.Utilities
{
    public static class TORDamageDisplay
    {
        /// <summary>
        /// Displays aggregate spell damage for all targets hit by a single spell cast.
        /// </summary>
        public static void DisplayAggregateSpellDamage(DamageType damageType, int totalDamage, int agentsAffected, string spellName = null)
        {
            var displayColor = GetDamageTypeColor(damageType);
            string damageTypeIcon = GetDamageTypeIcon(damageType);
            string damageTypeName = damageType.ToString();

            string targetText = agentsAffected == 1 ? "target" : "targets";
            string spellPart = string.IsNullOrEmpty(spellName) ? "Spell" : spellName;
            var resultText = $"{damageTypeIcon} {spellPart} dealt {totalDamage} {damageTypeName} damage to {agentsAffected} {targetText}";

            InformationManager.DisplayMessage(new InformationMessage(resultText, displayColor));
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
            string targetText = agentsAffected == 1 ? "target" : "targets";
            string spellPart = string.IsNullOrEmpty(spellName) ? "Spell" : spellName;
            var resultText = $"<img src=\"heart_icon\"/> {spellPart} healed {totalHealing} health to {agentsAffected} {targetText}";

            InformationManager.DisplayMessage(new InformationMessage(resultText, displayColor));
        }
    }
}
