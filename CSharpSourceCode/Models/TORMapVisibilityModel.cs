using Helpers;
using System.Collections.Generic;
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

            // Keen Senses gates the stealth-stance spotting bonus: crouching
            // helps you spot approaching hostiles only if you've trained for
            // it. Main-party only because AmbushCampaignBehavior.IsPlayerInStealth
            // tracks the player; AI stealth uses IgnoreByOtherPartiesTill
            // and never passes through this branch.
            if (party.IsMainParty && party.HasPerk(TORPerks.Roguery.KeenSenses))
            {
                var ambush = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
                if (ambush != null && ambush.IsPlayerInStealth)
                {
                    PerkHelper.AddPerkBonusForParty(TORPerks.Roguery.KeenSenses, party, isPrimaryBonus: true, ref result);
                }
            }

            // Per-troop spotting score bump for scout-heavy rosters.
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model != null)
            {
                float rosterSpottingBonus = AmbushTroopSpottingScore.GetBasePartyScore(
                    party, model.SpottingScoreSizeDampingReference)
                    * model.SpottingScoreSightingWeight;
                if (rosterSpottingBonus > 0.01f || rosterSpottingBonus < -0.01f)
                {
                    result.AddFactor(rosterSpottingBonus, new TextObject("{=tor_spotting_troop_composition}Troop composition"));
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
        public override float GetPartySpottingRatioForMainPartySeeingRange(MobileParty party)
        {
            float baseRatio = base.GetPartySpottingRatioForMainPartySeeingRange(party);
            if (party.HasPerk(TORPerks.Roguery.LowProfile))
            {
                return baseRatio * (1f - TORPerks.Roguery.LowProfile.PrimaryBonus);
            }
            return baseRatio;
        }

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
        /// Keeps stealthed and revealed ambushers' visibility flags stable
        /// against vanilla's distance pass.
        /// </summary>
        public override bool IsVisibilityPersistent(MobileParty mobileParty)
        {
            var ai = Campaign.Current?.GetCampaignBehavior<AiAmbushBehavior>();
            if (ai != null)
            {
                if (!mobileParty.IsVisible && ai.IsPartyStealthed(mobileParty))
                {
                    _wasPersisted.Add(mobileParty);
                    return true;
                }
                if (ai.IsPartyRevealed(mobileParty))
                {
                    _wasPersisted.Add(mobileParty);
                    return true;
                }
                if (ai.IsRecentlyPlayerDetected(mobileParty))
                {
                    _wasPersisted.Add(mobileParty);
                    return true;
                }
            }
            bool baseResult = base.IsVisibilityPersistent(mobileParty);
            if (baseResult)
            {
                _wasPersisted.Add(mobileParty);
            }
            else if (_wasPersisted.Remove(mobileParty))
            {
                // Vanilla's distance pass only reaches parties inside the
                // LocatorGrid around the main party. For parties beyond that,
                // correct IsVisible here so previously-pinned parties don't
                // stay stuck visible once persistence drops.
                var mp = MobileParty.MainParty;
                if (mp != null)
                {
                    float r = mp.SeeingRange * 2f;
                    if (mobileParty.Position.DistanceSquared(mp.Position) > r * r
                        && mobileParty.IsVisible)
                    {
                        mobileParty.IsVisible = false;
                    }
                }
            }
            return baseResult;
        }

        /// <summary>Tracks parties this model previously pinned so a transition
        /// off persistence can trigger the manual distance correction.</summary>
        private static readonly HashSet<MobileParty> _wasPersisted
            = new HashSet<MobileParty>();
    }
}
