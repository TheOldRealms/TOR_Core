using Helpers;
using SandBox.GameComponents;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CharacterDevelopment.CareerSystem.Choices;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORStrikeMagnitudeModel : SandboxStrikeMagnitudeModel
    {

        public override float CalculateAdjustedArmorForBlow(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseArmor, BasicCharacterObject attackerCharacter, BasicCharacterObject attackerCaptainCharacter, BasicCharacterObject victimCharacter, BasicCharacterObject victimCaptainCharacter, WeaponComponentData weaponComponent)
        {
            var result = base.CalculateAdjustedArmorForBlow(attackInformation, collisionData, baseArmor, attackerCharacter, attackerCaptainCharacter, victimCharacter, victimCaptainCharacter, weaponComponent);
            ExplainedNumber resultArmor = new ExplainedNumber(result);
            var attacker = attackerCharacter as CharacterObject;
            var attackerCaptain = attackerCharacter as CharacterObject;
            if (weaponComponent != null && attacker != null)
            {
                if (attacker.GetPerkValue(TORPerks.GunPowder.PiercingShots) && weaponComponent.IsGunPowderWeapon())
                {
                    PerkHelper.AddPerkBonusForCharacter(TORPerks.GunPowder.PiercingShots, attacker, true, ref resultArmor);
                }

                if (attacker.IsPlayerCharacter && attacker.HeroObject == Hero.MainHero)
                {
                    var attackMask = AttackTypeMask.Melee;
                    if (weaponComponent.IsRangedWeapon) attackMask = AttackTypeMask.Ranged;

                    CareerHelper.ApplyBasicCareerPassives(attacker.HeroObject, ref resultArmor, PassiveEffectType.ArmorPenetration, attackMask, true);
                }

                if (attacker.IsHero) // never remove this check. operations for item traits can be very heavy
                {
                    var agent = Mission.Current.Agents.FirstOrDefault(x => x.IsHero && x.Character == attacker);



                    if (weaponComponent.IsAmmo || weaponComponent.IsRangedWeapon)
                    {
                        var missile = Mission.Current.MissilesList.FirstOrDefault(x => x.ShooterAgent == agent && x.Weapon.CurrentUsageItem.GetItemUsageIndex() == weaponComponent.GetItemUsageIndex());

                        if (missile != null)
                        {
                            var traits = missile.Weapon.Item.GetTraits();

                            foreach (var trait in traits)
                            {
                                if (trait?.StatsTuple?.StatType == ItemTraitStatType.ArmorPenetration)
                                {
                                    resultArmor.AddFactor(-trait.StatsTuple.Value / 100);
                                }
                            }
                        }
                    }

                    if (agent.WieldedWeapon.CurrentUsageItem != null && agent.WieldedWeapon.CurrentUsageItem.GetItemUsageIndex() == weaponComponent.GetItemUsageIndex())
                    {
                        if (!agent.WieldedWeapon.IsEmpty)
                        {
                            var traits = agent.WieldedWeapon.Item.GetTraits();

                            foreach (var trait in traits)
                            {
                                if (trait?.StatsTuple?.StatType == ItemTraitStatType.ArmorPenetration)
                                {
                                    resultArmor.AddFactor(-trait.StatsTuple.Value / 100);
                                }
                            }
                        }
                    }
                }

                if (attackerCharacter.IsUndead() && attackerCaptain.IsPlayerCharacter && attackerCaptain.HeroObject == Hero.MainHero)
                {
                    if (Hero.MainHero.HasCareer(TORCareers.Necromancer))
                    {
                        if (Hero.MainHero.HasCareerChoice("LiberMortisPassive2"))
                        {
                            var choice = TORCareerChoices.GetChoice("LiberMortisPassive2");
                            resultArmor.AddFactor(choice.GetPassiveValue());
                        }
                    }

                    // Tree spirits always ignore 80% of enemy armor
                    if (attacker.IsTreeSpirit())
                    {
                        resultArmor.AddFactor(-0.8f);
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