using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORPartyTrainingModel : DefaultPartyTrainingModel
    {
        public override ExplainedNumber GetEffectiveDailyExperience(MobileParty mobileParty, TroopRosterElement troop)
        {
            var result = base.GetEffectiveDailyExperience(mobileParty, troop);

            if(mobileParty != null && mobileParty.IsActive)
            {
                if(mobileParty.HasPerk(TORPerks.GunPowder.FiringDrills, true) && troop.Character.Equipment.HasWeaponOfClass(WeaponClass.Cartridge))
                {
                    result.Add(TORPerks.GunPowder.FiringDrills.SecondaryBonus);
                }
                if (mobileParty.HasPerk(TORPerks.Faith.Blessed, true) && troop.Character.IsReligiousUnit() && mobileParty.HasAnyActiveBlessing())
                {
                    result.Add(TORPerks.Faith.Blessed.SecondaryBonus);
                }
            }
            return result;
        }
    }
}
