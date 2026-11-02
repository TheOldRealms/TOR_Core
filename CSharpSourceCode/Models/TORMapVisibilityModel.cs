using Helpers;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Ambush;
using TOR_Core.CampaignMechanics.ServeAsAHireling;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.Models
{
    public class TORMapVisibilityModel : DefaultMapVisibilityModel
    {
        public override ExplainedNumber GetPartySpottingRange(MobileParty party, bool includeDescriptions = false)
        {
            var result = base.GetPartySpottingRange(party, includeDescriptions);
            if (party.HasPerk(TORPerks.Faith.ForeSight, checkSecondaryRole: false)) PerkHelper.AddPerkBonusForParty(TORPerks.Faith.ForeSight, party, false, ref result);

            if (party.IsMainParty && party.LeaderHero != null && party.LeaderHero.HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(party.LeaderHero, ref result, PassiveEffectType.PartySpottingRange, true);
            }

            // Ambush stance: a crouched, watchful party notices approaching
            // hostiles sooner. +25 % to the player's spotting range while in
            // stealth. Player-only — AI stealth is handled separately via
            // IgnoreByOtherPartiesTill, and we don't want a world-wide
            // sighting bonus firing every tick for every AI stealther.
            if (party.IsMainParty)
            {
                var ambush = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
                if (ambush != null && ambush.IsPlayerInStealth)
                {
                    result.AddFactor(0.25f, new TextObject("{=!}Ambush stance"));
                }
            }

            // DwarfBrewers handling
            if (party.IsMainParty && party.LeaderHero == Hero.MainHero)
            {
                if (TORCommon.FindSettlementsAroundPosition(party.Position.ToVec2(), 30, (x) => x.Culture.StringId == TORConstants.Cultures.DAWI).Any())
                {
                    if (Hero.MainHero.HasAttribute(CharacterAttributes.GUILD_BREWERS_3))
                    {
                        result.AddFactor(0.30f, new TextObject("Brewers Guild"));
                    }
                    else if (Hero.MainHero.HasAttribute(CharacterAttributes.GUILD_BREWERS_2))
                    {
                        result.AddFactor(0.2f, new TextObject("Brewers Guild"));
                    }
                    else if (Hero.MainHero.HasAttribute(CharacterAttributes.GUILD_BREWERS_1))
                    {
                        result.AddFactor(0.1f, new TextObject("Brewers Guild"));
                    }
                }
            }

            return result;
        }

        /// <remarks>
        /// 1.5.4 dropped the isDistanceDependent out-parameter from this overload (it survives only on
        /// GetSettlementInspectedState), so the 1.5.3 three-out signature no longer overrides anything.
        /// </remarks>
        public override void GetMobilePartyVisibilityAndInspectedState(MobileParty mobileParty, Vec2[] points, float seeingRange, out bool isVisible, out bool isInspected)
        {
            if (mobileParty == MobileParty.MainParty && Hero.MainHero.IsEnlisted())
            {
                isVisible = false;
                isInspected = false;
                return;
            }

            base.GetMobilePartyVisibilityAndInspectedState(mobileParty, points, seeingRange, out isVisible, out isInspected);
        }

        /// <summary>
        /// Overrides vanilla's fog-of-war distance pass for two
        /// ambush-related cases, both using the same mechanism
        /// <c>PlayerCaptivity.CaptorParty</c> uses — returning
        /// <c>true</c> here makes <c>UpdateVisibilitiesBasedOnPoint</c>
        /// skip the distance check for this party and leave its
        /// current <c>IsVisible</c> value in place:
        ///
        /// • <b>Stealthed AI party</b> — <c>EnterAiStealth</c> sets
        ///   <c>IsVisible = false</c> once; without this persistence
        ///   override vanilla would flip it back to <c>true</c> on the
        ///   next tick as soon as the player's party is in seeing
        ///   range. The whole stealth fiction depends on them staying
        ///   off the campaign map.
        /// • <b>Repelled ambusher</b> — <c>LeaveAiStealth</c> sets
        ///   <c>IsVisible = true</c> before the mission opens; this
        ///   persistence keeps them visible to the player regardless
        ///   of fog-of-war until the record's cooldown lapses.
        /// </summary>
        public override bool IsVisibilityPersistent(MobileParty mobileParty)
        {
            var ai = Campaign.Current?.GetCampaignBehavior<AiAmbushBehavior>();
            if (ai != null)
            {
                // Stealth persistence: only relevant when the party is
                // already flagged invisible (set by EnterAiStealth). Fast
                // path for the 99 % of parties that are visible — skips
                // the dict lookup. Also self-healing: if stealth state
                // and IsVisible get desynced, vanilla's distance check
                // runs this tick and re-aligns the IsVisible side;
                // persistence resumes on the next tick when they match.
                if (!mobileParty.IsVisible && ai.IsPartyStealthed(mobileParty)) return true;
                // Reveal persistence: specifically overrides distance so
                // visible-but-far parties stay visible. Can't short-circuit
                // on IsVisible since the whole point is keeping them
                // visible when vanilla would otherwise hide them.
                if (ai.IsPartyRevealed(mobileParty)) return true;
                // Player-detection persistence (§6, deferred-landed): a
                // party the player just spotted stays visible for
                // PlayerDetectionRevealHours even if they move out of
                // Scouting range. Short-lived vs the full IsPartyRevealed
                // window so a genuinely-stealthy party can still fade if
                // the player loses them for a while.
                if (ai.IsRecentlyPlayerDetected(mobileParty)) return true;
            }
            return base.IsVisibilityPersistent(mobileParty);
        }
    }
}
