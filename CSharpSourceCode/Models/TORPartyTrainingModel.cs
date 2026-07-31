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
            ExplainedNumber result = default(ExplainedNumber);
            if (troop.Character.IsHero) { return result; } //this method doesn't apply to heroes; return default and save calculations

            result = base.GetEffectiveDailyExperience(mobileParty, troop);

            if (!mobileParty.IsLordParty) return result;


            if (mobileParty != MobileParty.MainParty)
            {
                result.Add((float)troop.Character.Tier * 10f);//base adds 10+2*Tier, or 15+3*Tier if clan leader
            }

            if (mobileParty.HasPerk(TORPerks.GunPowder.FiringDrills, true) && troop.Character.Equipment.HasWeaponOfClass(WeaponClass.Cartridge))
            {
                result.Add(TORPerks.GunPowder.FiringDrills.SecondaryBonus);
            }
            if (mobileParty.HasPerk(TORPerks.Faith.Blessed, true) && troop.Character.IsReligiousUnit() && mobileParty.HasAnyActiveBlessing())
            {
                result.Add(TORPerks.Faith.Blessed.SecondaryBonus);
            }

            return result;
        }
    }
}