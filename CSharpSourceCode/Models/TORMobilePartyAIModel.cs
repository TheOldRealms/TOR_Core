using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TOR_Core.Extensions;

namespace TOR_Core.Models;

public class TORMobilePartyAIModel : DefaultMobilePartyAIModel
{
    public virtual void AdjustEnemyInitiativeScores(MobileParty mobileParty, MobileParty enemyParty, float localAdvantage, float maxAggressiveness, ref float avoidScore, ref float attackScore)
    {
        var detectionRangeMultiplier = 1f;
        var avoidScoreMultiplier = 1f;
        var attackScoreMultiplier = 1f;

        if (enemyParty == MobileParty.MainParty)
        {
            var scoutEquipment = enemyParty.EffectiveScout.CharacterObject.GetCharacterEquipment(
                EquipmentIndex.ArmorItemBeginSlot,
                EquipmentIndex.ArmorItemEndSlot);

            var hasGhostwalker = scoutEquipment.Any(item =>
                item.GetTraits().Any(trait =>
                    trait.ItemTraitStringId == "asrai_enchant_ghostwalker"));

            if (hasGhostwalker)
            {
                detectionRangeMultiplier *= 0.5f;
            }
        }

        if (detectionRangeMultiplier < 1f)
        {
            var normalDetectionRange =
                Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius * 3f;

            var adjustedDetectionRange =
                normalDetectionRange * detectionRangeMultiplier;

            if (mobileParty.Position.Distance(enemyParty.Position) > adjustedDetectionRange)
            {
                avoidScore = 0f;
                attackScore = 0f;
                return;
            }
        }
        avoidScore *= avoidScoreMultiplier;
        attackScore *= attackScoreMultiplier;
    }
}
