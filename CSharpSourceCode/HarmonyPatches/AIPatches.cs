using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.FormationMarker;
using TOR_Core.Extensions;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class AIPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MissionFormationMarkerTargetVM), "Refresh")]
        private static void PostfixRefresh(MissionFormationMarkerTargetVM __instance)
        {
            __instance.FormationType = ChooseFormationIcon.ChooseIcon(__instance.Formation);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TacticComponent), "SetDefaultBehaviorWeights")]
        private static bool PrefixSetDefaultBehaviorWeights(ref Formation f)
        {
            if (f != null)
            {
                f.AI.SetBehaviorWeight<BehaviorRegroup>(1f);
            }
            return false;
        }
    }

    public static class ChooseFormationIcon
    {
        public static string ChooseIcon(Formation formation)
        {
            if (formation != null)
            {
                if (formation.QuerySystem.IsInfantryFormation)
                {
                    return TargetIconType.Special_Swordsman.ToString();
                }
                if (formation.QuerySystem.IsRangedFormation)
                {
                    return TargetIconType.Archer_Heavy.ToString();
                }
                if (formation.QuerySystem.IsRangedCavalryFormation)
                {
                    return TargetIconType.HorseArcher_Light.ToString();
                }
                if (formation.QuerySystem.IsCavalryFormation && !formation.IsMountedSkirmishFormation())
                {
                    return TargetIconType.Cavalry_Light.ToString();
                }
                if (formation.QuerySystem.IsCavalryFormation && formation.IsMountedSkirmishFormation())
                {
                    return TargetIconType.Special_JavelinThrower.ToString();
                }
            }
            return TargetIconType.None.ToString();
        }
    }
}
