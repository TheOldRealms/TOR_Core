using HarmonyLib;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class BaseGameDebugPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TroopRoster), "EnsureLength")]
        public static bool EnsureLengthProper(int length, ref TroopRosterElement[] ___data, int ____count)
        {
            if (length > 0 && (___data == null || length > ___data.Length))
            {
                TroopRosterElement[] array = new TroopRosterElement[length];
                for (int i = 0; i < ____count; i++)
                {
                    array[i] = ___data[i];
                }
                ___data = array;
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Game), "InitializeParameters")]
        public static bool LoadTORManagedParameters(Game __instance)
        {
            ManagedParameters.Instance.Initialize(ModuleHelper.GetXmlPath("TOR_Core", "tor_managed_core_parameters"));
            __instance.GameType.InitializeParameters();
            return false;
        }
    }
}
