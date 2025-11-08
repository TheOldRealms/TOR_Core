using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.MapEvents;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.Models
{
    public class TORCombatSimulationModel : DefaultCombatSimulationModel
    {
        public override void GetBattleAdvantage(MapEvent mapEvent, out ExplainedNumber defenderAdvantage, out ExplainedNumber attackerAdvantage)
        {
            base.GetBattleAdvantage(mapEvent, out defenderAdvantage, out attackerAdvantage);
            var defenderLeader = mapEvent.GetLeaderParty(TaleWorlds.Core.BattleSideEnum.Attacker).LeaderHero;
            var attackerLeader = mapEvent.GetLeaderParty(TaleWorlds.Core.BattleSideEnum.Defender).LeaderHero;
            if (defenderLeader != null && defenderLeader.GetPerkValue(TORPerks.SpellCraft.WellControlled))
            {
                defenderAdvantage.Add(TORPerks.SpellCraft.WellControlled.SecondaryBonus);
            }
            if (attackerLeader != null && attackerLeader.GetPerkValue(TORPerks.SpellCraft.WellControlled))
            {
                attackerAdvantage.Add(TORPerks.SpellCraft.WellControlled.SecondaryBonus);
            }
        }
    }
}
