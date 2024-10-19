using Helpers;
using SandBox.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.Models
{
    public class TORStrikeMagnitudeModel : SandboxStrikeMagnitudeModel
    {
        public override float CalculateAdjustedArmorForBlow(float baseArmor, BasicCharacterObject attackerCharacter, BasicCharacterObject attackerCaptainCharacter, BasicCharacterObject victimCharacter, BasicCharacterObject victimCaptainCharacter, WeaponComponentData weaponComponent)
        {
            var result = base.CalculateAdjustedArmorForBlow(baseArmor, attackerCharacter, attackerCaptainCharacter, victimCharacter, victimCaptainCharacter, weaponComponent);
            ExplainedNumber resultArmor = new ExplainedNumber(result);
            var attacker = attackerCharacter as CharacterObject;
            var attackerCaptain = attackerCharacter as CharacterObject;
            if (weaponComponent != null && attacker != null)
            {
                if (attacker.GetPerkValue(TORPerks.GunPowder.PiercingShots) && weaponComponent.IsGunPowderWeapon())
                {
                    PerkHelper.AddPerkBonusForCharacter(TORPerks.GunPowder.PiercingShots, attacker, true, ref resultArmor);
                }

                if (attacker.IsPlayerCharacter&& attacker.HeroObject == Hero.MainHero)
                {
                    var attackMask = AttackTypeMask.Melee;
                    if (weaponComponent.IsRangedWeapon) attackMask = AttackTypeMask.Ranged;
                    CareerHelper.ApplyBasicCareerPassives(attacker.HeroObject, ref resultArmor, PassiveEffectType.ArmorPenetration, attackMask, true);
                }

                if (attackerCharacter.IsUndead() &&attackerCaptain.IsPlayerCharacter&& attackerCaptain.HeroObject == Hero.MainHero)
                {
                    if (Hero.MainHero.HasCareer(TORCareers.Necromancer))
                    {
                        if (Hero.MainHero.HasCareerChoice("LiberMortisPassive2"))
                        {
                            var choice = TORCareerChoices.GetChoice("LiberMortisPassive2");
                            resultArmor.AddFactor(choice.GetPassiveValue());
                        }
                    }

                    if (attackerCaptain.IsPlayerCharacter && Hero.MainHero.HasCareer(TORCareers.Spellsinger))
                    {
                        if (Hero.MainHero.HasCareerChoice("HeartOfTheTreePassive4"))
                        {
                            if (attacker.IsTreeSpirit())
                            {
                                resultArmor.AddFactor(-0.8f);
                            } 
                        }
                        
                    }
                    
                    
                }

                if (attackerCharacter.HasAttribute("Piercing"))
                {
                    resultArmor.AddFactor(-0.4f);
                }
                
            }
            
            return resultArmor.ResultNumber;
        }
    }
}
