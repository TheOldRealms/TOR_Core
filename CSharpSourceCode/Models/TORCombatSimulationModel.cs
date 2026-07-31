using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.Models
{
    public class TORCombatSimulationModel : DefaultCombatSimulationModel
    {
        public override void GetBattleAdvantage(MapEvent mapEvent, out ExplainedNumber defenderAdvantage, out ExplainedNumber attackerAdvantage)
        {
            base.GetBattleAdvantage(mapEvent, out defenderAdvantage, out attackerAdvantage);
            var defenderLeader = mapEvent.GetLeaderParty(TaleWorlds.Core.BattleSideEnum.Defender).LeaderHero;
            var attackerLeader = mapEvent.GetLeaderParty(TaleWorlds.Core.BattleSideEnum.Attacker).LeaderHero;
            if (defenderLeader != null && defenderLeader.GetPerkValue(TORPerks.Spellcraft.WellControlled))
            {
                defenderAdvantage.Add(TORPerks.Spellcraft.WellControlled.SecondaryBonus);
            }
            if (attackerLeader != null && attackerLeader.GetPerkValue(TORPerks.Spellcraft.WellControlled))
            {
                attackerAdvantage.Add(TORPerks.Spellcraft.WellControlled.SecondaryBonus);
            }

            // Myrmidia Seal 1 - Strategic Advantage per sealed unit type
            ApplyMyrmidiaSealAdvantage(mapEvent, BattleSideEnum.Attacker, ref attackerAdvantage);
            ApplyMyrmidiaSealAdvantage(mapEvent, BattleSideEnum.Defender, ref defenderAdvantage);
        }

        private void ApplyMyrmidiaSealAdvantage(MapEvent mapEvent, BattleSideEnum side, ref ExplainedNumber advantage)
        {
            var party = mapEvent.GetLeaderParty(side);
            if (party?.MobileParty == null) return;

            var info = ExtendedInfoManager.Instance.GetPartyInfoFor(party.MobileParty.StringId);
            if (info == null) return;

            var sealedUnitTypes = 0;
            foreach (var troop in party.MobileParty.MemberRoster.GetTroopRoster())
            {
                if (info.TroopAttributes.TryGetValue(troop.Character.StringId, out var attrs))
                {
                    if (attrs.Contains("MyrmidiaSeal1"))
                    {
                        sealedUnitTypes++;
                    }
                }
            }

            if (sealedUnitTypes > 0)
            {
                advantage.Add(0.02f * sealedUnitTypes, TORTextHelper.GetTextObject("tor_myrmidia_seal", "Myrmidia Seal"));
            }
        }
    }
}