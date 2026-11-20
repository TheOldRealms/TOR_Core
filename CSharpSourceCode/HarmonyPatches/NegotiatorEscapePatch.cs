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
    /// <para><b>Problem:</b> vanilla's hero-prisoner escape-chance calculation
    /// lives inside <c>PrisonerReleaseCampaignBehavior.DailyHeroTick</c>, a
    /// private method with an inline <c>ExplainedNumber</c> that accumulates
    /// perk hooks (<c>SweetTalker</c>, <c>MountedPatrols</c>, <c>RansomBroker</c>,
    /// <c>KeenSight</c>) directly on the local. No <c>GameModel</c> dispatch;
    /// no clean subclass path. <c>RansomBroker</c> is the vanilla equivalent
    /// of TOR's Negotiator secondary ("hero prisoners are 30% less likely to
    /// escape"), but it's stripped by VanillaPerkStripper so its hook is
    /// dead code.</para>
    /// <para><b>Fix:</b> intercept at the escape ACTION instead of the
    /// probability calculation. When <c>EndCaptivityAction.ApplyByEscape</c>
    /// is about to fire for a hero prisoner whose captor is a mobile party
    /// with TOR's Negotiator on the Quartermaster, roll the perk's
    /// <c>SecondaryBonus</c> (0.3) chance to cancel the escape. Matches
    /// the vanilla intent (reduce escape chance by 30%) via a single
    /// targeted prefix on one action method; avoids transpiling the inline
    /// probability math inside a vanilla private method. Filter is narrow
    /// (lord hero + mobile captor + perk held) so quest-driven escapes
    /// from settlements or non-perk-holding captors are unaffected.</para>
    /// </summary>
    [HarmonyPatch(typeof(EndCaptivityAction), nameof(EndCaptivityAction.ApplyByEscape))]
    public static class EndCaptivityAction_ApplyByEscape_Patch
    {
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
