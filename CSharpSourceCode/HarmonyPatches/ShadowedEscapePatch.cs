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
    /// <para><b>Problem:</b> Shadowed secondary (Roguery 275, Personal
    /// +0.3 AddFactor): "+30% escape chance when you are captured by
    /// mobile parties." Vanilla's equivalent <c>FleetFooted</c> perk is
    /// stripped by <c>VanillaPerkStripper</c> (its application site at
    /// <c>PlayerCaptivityCampaignBehavior.CheckCaptivityChange</c> is now
    /// dead code). Multiplicative inline on a local <c>num5</c> escape
    /// chance — not reachable without a transpiler.</para>
    /// <para><b>Fix:</b> add an independent re-roll postfix on
    /// <c>CheckCaptivityChange</c>. When vanilla's own check ticked (we
    /// detect this via <c>PlayerCaptivity.LastCheckTime</c> delta), the
    /// player is still captive, and the captor is a non-at-sea mobile
    /// party with the perk held: roll
    /// <c>Shadowed.SecondaryBonus</c> (0.3) chance to switch to the
    /// appropriate escape menu. Not a mathematically perfect match for
    /// vanilla's multiplicative FleetFooted semantics (vanilla did
    /// <c>num5 *= 1.3</c>, we add +30% as an independent trial), but
    /// close enough to the perk's stated intent and avoids a transpiler
    /// on a complex private-local method.</para>
    /// </summary>
    [HarmonyPatch(typeof(PlayerCaptivityCampaignBehavior),
        nameof(PlayerCaptivityCampaignBehavior.CheckCaptivityChange))]
    public static class PlayerCaptivityCampaignBehavior_CheckCaptivityChange_Patch
    {
        public static void Prefix(out CampaignTime __state)
        {
            __state = PlayerCaptivity.IsCaptive
                ? PlayerCaptivity.LastCheckTime
                : CampaignTime.Zero;
        }

        public static void Postfix(CampaignTime __state)
        {
            if (!PlayerCaptivity.IsCaptive) return;
            // Vanilla didn't tick this frame — bail (its CheckTimeElapsedMoreThanHours
            // gate returned false, LastCheckTime didn't update).
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
