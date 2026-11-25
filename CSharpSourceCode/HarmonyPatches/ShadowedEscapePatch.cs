using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.HarmonyPatches
{
    /// <summary>
    /// Gives the player a Shadowed-perk reroll each time vanilla rolls the
    /// captivity escape check.
    /// <para><b>Problem:</b> the escape chance lives in a private local
    /// inside <c>PlayerCaptivityCampaignBehavior.CheckCaptivityChange</c>
    /// and is not reachable without a transpiler.</para>
    /// <para><b>Fix:</b> wrap the method. Detect that vanilla actually
    /// ticked this frame by watching <c>PlayerCaptivity.LastCheckTime</c>,
    /// then roll the perk bonus and switch to the escape menu on pass.</para>
    /// </summary>
    [HarmonyPatch(typeof(PlayerCaptivityCampaignBehavior),
        nameof(PlayerCaptivityCampaignBehavior.CheckCaptivityChange))]
    public static class PlayerCaptivityCampaignBehavior_CheckCaptivityChange_Patch
    {
        /// <summary>
        /// Captures the pre-call tick timestamp.
        /// </summary>
        public static void Prefix(out CampaignTime __state)
        {
            __state = PlayerCaptivity.IsCaptive
                ? PlayerCaptivity.LastCheckTime
                : CampaignTime.Zero;
        }

        /// <summary>
        /// Rolls the perk bonus when vanilla's tick fired.
        /// </summary>
        public static void Postfix(CampaignTime __state)
        {
            if (!PlayerCaptivity.IsCaptive) return;
            // Vanilla's gate returned false this frame.
            if (PlayerCaptivity.LastCheckTime == __state) return;

            var captor = PlayerCaptivity.CaptorParty;
            if (captor == null || !captor.IsMobile) return;
            if (captor.MobileParty.IsCurrentlyAtSea) return;
            if (Hero.MainHero == null
                || !Hero.MainHero.GetPerkValue(TORPerks.Roguery.Shadowed)) return;

            if (MBRandom.RandomFloat >= TORPerks.Roguery.Shadowed.SecondaryBonus) return;

            if (captor.MapEvent != null)
                GameMenu.SwitchToMenu("menu_escape_captivity_during_battle");
            else if (Hero.MainHero.CurrentSettlement == null)
                GameMenu.SwitchToMenu("menu_captivity_end_wilderness_escape");
            else
                GameMenu.SwitchToMenu("menu_captivity_end_prison_escape");
        }
    }
}
