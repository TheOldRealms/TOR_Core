using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.HarmonyPatches
{
    /// <summary>
    /// Cancels hero prisoner escapes for captors that hold the Negotiator perk.
    /// <para><b>Problem:</b> the escape chance is computed inside the private
    /// <c>PrisonerReleaseCampaignBehavior.DailyHeroTick</c> and is not routed
    /// through a <c>GameModel</c>.</para>
    /// <para><b>Fix:</b> prefix the escape action and reroll on the perk's
    /// <c>SecondaryBonus</c> (0.3). Filter is narrow: lord prisoner, mobile
    /// captor, perk held on the Quartermaster.</para>
    /// </summary>
    [HarmonyPatch(typeof(EndCaptivityAction), nameof(EndCaptivityAction.ApplyByEscape))]
    public static class EndCaptivityAction_ApplyByEscape_Patch
    {
        /// <summary>Returns false to cancel the escape when the perk roll passes.</summary>
        public static bool Prefix(Hero character)
        {
            if (character == null || !character.IsLord || !character.IsPrisoner) return true;
            var captorParty = character.PartyBelongedToAsPrisoner?.MobileParty;
            if (captorParty == null) return true;
            if (!captorParty.HasPerk(TORPerks.Roguery.Negotiator, checkSecondaryRole: true)) return true;

            if (MBRandom.RandomFloat < TORPerks.Roguery.Negotiator.SecondaryBonus)
            {
                return false;
            }
            return true;
        }
    }
}
