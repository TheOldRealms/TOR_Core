using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORPartyTroopUpgradeModel : DefaultPartyTroopUpgradeModel
    {
        //public override int MaxCharacterTier => 9;

        public override int GetGoldCostForUpgrade(PartyBase party, CharacterObject characterObject, CharacterObject upgradeTarget)
        {
            var value =  base.GetGoldCostForUpgrade(party, characterObject, upgradeTarget);

            var explainedNumber = new ExplainedNumber();
            
            explainedNumber.Add(value);
            
            if (party.LeaderHero != null && party.LeaderHero == Hero.MainHero)
            {
                CareerHelper.ApplyBasicCareerPassives(party.LeaderHero,ref explainedNumber,PassiveEffectType.TroopUpgradeCost, true);
            }
          
            return (int) explainedNumber.ResultNumber;
        }
    }
}
