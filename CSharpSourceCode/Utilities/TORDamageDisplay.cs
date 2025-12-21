using System;
using System.Globalization;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.BattleMechanics.DamageSystem;

namespace TOR_Core.Utilities
{
    public static class TORDamageDisplay
    {
        public static void DisplaySpellDamageResult(DamageType additionalDamageType,
            int resultDamage, float damageAmplifier, float wardsaveFactor)
        {
            var displayColor = Color.White;
            string displayDamageType = "";

            switch (additionalDamageType)
            {
                case DamageType.Fire:
                    displayColor = Colors.Red;
                    displayDamageType = "fire";
                    break;
                case DamageType.Holy:
                    displayColor = Colors.Yellow;
                    displayDamageType = "holy";
                    break;
                case DamageType.Lightning:
                    displayColor = Color.FromUint(5745663);
                    displayDamageType = "Lightning";
                    break;
                case DamageType.Magical:
                    displayColor = Colors.Cyan;
                    displayDamageType = "Magical";
                    break;
                case DamageType.Physical:
                    displayColor = Color.White;
                    displayDamageType = "Physical";
                    break;
                case DamageType.Frost:
                    displayColor = Color.FromUint(8909823);
                    displayDamageType = "Frost";
                    break;
            }
            InformationManager.DisplayMessage(new InformationMessage(resultDamage + " cast damage consisting of  " + " (" + displayDamageType + ") was applied " + "which was modified by " + (1 + damageAmplifier).ToString("##%", CultureInfo.InvariantCulture), displayColor));
        }

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

        public static void DisplayDamageResult(int resultDamage, float[] categories, float[] percentages, float wardsaveFactor, bool isVictim)
        {
            var displaycolor = Color.White;
            var dominantAdditionalEffect = DamageType.Physical;
            float dominantCategory = 0;
            string additionalDamageTypeText = "";

            string sign = "";

            for (int i = 2; i < categories.Length; i++) //starting from first real additional damage type
            {
                if (dominantCategory < categories[i])
                {
                    dominantCategory = categories[i];
                    dominantAdditionalEffect = (DamageType)i;
                }

                if (categories[i] > 0)
                {
                    var categorysign = "";
                    if (percentages[i] > 0) categorysign = "+";

                    DamageType t = (DamageType)i;
                    string s = $", {(int)categories[i]} was dealt in {t} [{categorysign}{percentages[i].ToString(".%")}]";
                    if (additionalDamageTypeText == "")
                        additionalDamageTypeText = s;
                    else
                        additionalDamageTypeText = additionalDamageTypeText.Add(s, false);
                }
            }

            if (isVictim)
            {
                displaycolor = Color.FromUint(9856100);
            }
            else
            {
                switch (dominantAdditionalEffect)
                {
                    case DamageType.Fire:
                        displaycolor = Colors.Red;
                        break;
                    case DamageType.Holy:
                        displaycolor = Colors.Yellow;
                        break;
                    case DamageType.Lightning:
                        displaycolor = Color.FromUint(5745663);
                        break;
                    case DamageType.Magical:
                        displaycolor = Colors.Cyan;
                        break;
                }
            }

            if (percentages[1] > 0)
                sign = "+";

            var wardsaveFactorText = "";
            if (wardsaveFactor < 1)
            {
                wardsaveFactorText = $", {(1 - wardsaveFactor).ToString(".%")} was absorbed";
            }

            var resultText = $"{resultDamage} damage was dealt which was {(int)categories[1]}{sign}{(percentages[1] != 0 ? "(" + percentages[1].ToString(".%") + ")" : "")} {DamageType.Physical}{additionalDamageTypeText}{wardsaveFactorText}";
            InformationManager.DisplayMessage(new InformationMessage(resultText, displaycolor));


        }
    }
}