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
    /// <summary>Encounter model override with villager-surrender guards and Intimidator perk bonuses.</summary>
    public class TOREncounterModel : DefaultEncounterModel
    {
        /// <summary>Surrender chance override. Villagers and caravans stop surrendering when a lord is helping them; Intimidator perk boosts the chance against vulnerable parties.</summary>
        public override float GetSurrenderChance(MobileParty defenderParty, MobileParty attackerParty)
        {
            float baseChance = base.GetSurrenderChance(defenderParty, attackerParty);

            // Villager and caravan parties stop surrendering if a lord is helping them.
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

            // Intimidator perk: bonus surrender chance for villagers, caravans, and bandits.
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

        /// <summary>Bribe chance override. Intimidator perk boosts the bribe chance against villagers, caravans, and bandits.</summary>
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