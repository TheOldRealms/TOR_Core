using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class NotablesCampaignBehaviorPatches
    {
        /// <summary>
        /// Removes the HeroKilled listener which is responsible in native for spawning new notables to replace dead ones immediately after they die. The replacements copy from the old notable which interferes with the culture swap we perform when settlements change cultures. AssimilationCampaignBehavior is responsible for reimplementing the relevant replacements.
        /// </summary>
        /// <remarks>
        /// Sly : This actually works which surprises me in some ways - it effectively only runs once because after the listener is removed, there will be no future method call that it can prefix.
        /// </remarks>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(NotablesCampaignBehavior), "OnHeroKilled")]
        public static bool HeroKilledListenerRemover(NotablesCampaignBehavior __instance)
        {
            CampaignEvents.HeroKilledEvent.ClearListeners(__instance);
            return false;
        }
    }
}