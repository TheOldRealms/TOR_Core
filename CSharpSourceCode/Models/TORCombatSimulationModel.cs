using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.Models
{
    public class TORCombatSimulationModel : DefaultCombatSimulationModel
    {
        public override (float defenderAdvantage, float attackerAdvantage) GetBattleAdvantage(PartyBase defenderParty, PartyBase attackerParty, MapEvent.BattleTypes mapEventType, Settlement settlement)
        {
            var advantage = base.GetBattleAdvantage(defenderParty, attackerParty, mapEventType, settlement);
            var defenderLeader = defenderParty.LeaderHero;
            var attackerLeader = attackerParty.LeaderHero;
            if (defenderLeader != null && defenderLeader.GetPerkValue(TORPerks.SpellCraft.WellControlled))
            {
                advantage.defenderAdvantage += TORPerks.SpellCraft.WellControlled.SecondaryBonus * 0.01f;
            }
            if (attackerLeader != null && attackerLeader.GetPerkValue(TORPerks.SpellCraft.WellControlled))
            {
                advantage.attackerAdvantage += TORPerks.SpellCraft.WellControlled.SecondaryBonus * 0.01f;
            }
            return advantage;
        }
    }
}
