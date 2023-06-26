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
            int resultDamage, float damageAmplifier)
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
                    displayColor = Colors.Blue;
                    displayDamageType = "lightning";
                    break;
                case DamageType.Magical:
                    displayColor = Colors.Cyan;
                    displayDamageType = "magical";
                    break;
                case DamageType.Physical:
                    displayColor = Color.White;
                    displayDamageType = "Physical";
                    break;
            }
            InformationManager.DisplayMessage(new InformationMessage(resultDamage + "cast damage consisting of  " + " (" + displayDamageType + ") was applied " + "which was modified by " + (1 + damageAmplifier).ToString("##%", CultureInfo.InvariantCulture), displayColor));
        }

        public static void DisplayDamageResult(int resultDamage, float[] categories, float[] percentages, bool isVictim)
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
                        additionalDamageTypeText= additionalDamageTypeText.Add(s, false);
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

            var resultText = $"{resultDamage} damage was dealt which was {(int)categories[1]}[{sign}{percentages[1].ToString(".%")}]{additionalDamageTypeText}";
            InformationManager.DisplayMessage(new InformationMessage(resultText, displaycolor));


        }
    }
}
