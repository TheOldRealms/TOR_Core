using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORPartyHealingModel : DefaultPartyHealingModel
    {
        public override float GetSurvivalChance(PartyBase party, CharacterObject character, DamageTypes damageType, PartyBase enemyParty = null)
        {
            return character.IsUndead() ? 0f : base.GetSurvivalChance(party, character, damageType, enemyParty);
        }
    }
}
