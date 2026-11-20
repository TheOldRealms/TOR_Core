using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TOREncounterModel : DefaultEncounterModel
    {
        public override Hero GetLeaderOfSiegeEvent(SiegeEvent siegeEvent, BattleSideEnum side)
        {
            return base.GetLeaderOfSiegeEvent(siegeEvent, side);
        }

        public override float GetSurrenderChance(MobileParty defenderParty, MobileParty attackerParty)
        {
            float baseChance = base.GetSurrenderChance(defenderParty, attackerParty);

            // Villager / caravan parties stop surrendering if a lord is helping them.
            if (defenderParty != null && (defenderParty.IsCaravan || defenderParty.IsVillager))
            {
                var mapEvent = defenderParty.MapEvent;
                if (mapEvent != null)
                {
                    foreach (var mapEventParty in mapEvent.DefenderSide.Parties)
                    {
                        var mobileParty = mapEventParty.Party?.MobileParty;
                        if (mobileParty != null && mobileParty != defenderParty && mobileParty.IsLordParty)
                        {
                            baseChance = 0f;
                            break;
                        }
                    }
                }
            }

            // Intimidator primary (Roguery 150): "+30% chance for villagers,
            // caravans, and bandits to surrender when you demand it." Mirrors
            // the shape of the stripped vanilla Scarface perk (main hero, not
            // at sea, multiplicative with a 1.0 clamp) so Intimidator plugs in
            // as its direct replacement — see VanillaPerkStripper.
            if (baseChance > 0f && defenderParty != null
                && Hero.MainHero != null
                && !MobileParty.MainParty.IsCurrentlyAtSea
                && Hero.MainHero.GetPerkValue(TORPerks.Roguery.Intimidator)
                && (defenderParty.IsCaravan || defenderParty.IsVillager || defenderParty.IsBandit))
            {
                baseChance = MathF.Min(1f, baseChance * (1f + TORPerks.Roguery.Intimidator.PrimaryBonus));
            }

            return baseChance;
        }

        /// <summary>Intimidator primary (Roguery 150) companion effect.
        /// Vanilla's stripped Scarface applied its primary bonus to BOTH
        /// <c>GetSurrenderChance</c> and <c>GetBribeChance</c> inside
        /// <c>DefaultEncounterModel</c>. This override restores the bribe
        /// side under the Intimidator key, scoped to the same villager /
        /// caravan / bandit filter as the surrender side so the two
        /// effects stay consistent. Applied via
        /// <see cref="PerkHelper.AddPerkBonusForCharacter"/> (vanilla's
        /// pattern on this method) so the bonus shows up in the
        /// <c>ExplainedNumber</c> tooltip.</summary>
        public override ExplainedNumber GetBribeChance(MobileParty defenderParty, MobileParty attackerParty)
        {
            var result = base.GetBribeChance(defenderParty, attackerParty);

            if (defenderParty != null
                && Hero.MainHero != null
                && !MobileParty.MainParty.IsCurrentlyAtSea
                && Hero.MainHero.GetPerkValue(TORPerks.Roguery.Intimidator)
                && (defenderParty.IsCaravan || defenderParty.IsVillager || defenderParty.IsBandit))
            {
                PerkHelper.AddPerkBonusForCharacter(TORPerks.Roguery.Intimidator,
                    Hero.MainHero.CharacterObject, isPrimaryBonus: true, ref result);
                result.LimitMax(1f);
            }

            return result;
        }
    }
}