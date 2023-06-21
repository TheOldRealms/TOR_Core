using Helpers;
using SandBox.GameComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ink.Parsed;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORAgentApplyDamageModel : SandboxAgentApplyDamageModel
    {
        public override void DecideMissileWeaponFlags(Agent attackerAgent, MissionWeapon missileWeapon, ref WeaponFlags missileWeaponFlags)
        {
            base.DecideMissileWeaponFlags(attackerAgent, missileWeapon, ref missileWeaponFlags);
            var character = attackerAgent.Character as CharacterObject;
            if(character != null && !missileWeapon.IsEmpty)
            {
                if(missileWeapon.CurrentUsageItem.WeaponClass == WeaponClass.Cartridge && character.GetPerkValue(TORPerks.GunPowder.PiercingShots))
                {
                    missileWeaponFlags |= WeaponFlags.CanPenetrateShield;
                }
            }
        }

        public override float CalculateDamage(in AttackInformation attackInformation, in AttackCollisionData collisionData, in MissionWeapon weapon, float baseDamage)
        {
            var result = base.CalculateDamage(attackInformation, collisionData, weapon, baseDamage);
            var attacker = (attackInformation.IsAttackerAgentMount ? attackInformation.AttackerRiderAgentCharacter : attackInformation.AttackerAgentCharacter) as CharacterObject;
            var attackerCaptain = attackInformation.AttackerCaptainCharacter as CharacterObject;
            var defender = (attackInformation.IsVictimAgentMount ? attackInformation.VictimRiderAgentCharacter : attackInformation.VictimAgentCharacter) as CharacterObject;
            var defenderCaptain = attackInformation.VictimCaptainCharacter as CharacterObject;

            ExplainedNumber resultDamage = new ExplainedNumber(result);
            if (attacker != null && defender != null && !weapon.IsEmpty)
            {
                if (attacker.GetPerkValue(TORPerks.GunPowder.CloseQuarters) && weapon.CurrentUsageItem.WeaponClass == WeaponClass.Cartridge)
                {
                    var shotLength = (collisionData.CollisionGlobalPosition - collisionData.MissileStartingPosition).Length;
                    if(shotLength <= 7)
                    {
                        PerkHelper.AddPerkBonusForCharacter(TORPerks.GunPowder.CloseQuarters, attacker, true, ref resultDamage);
                    }
                }
                if(weapon.Item.StringId.Contains("longrifle") && attacker.GetPerkValue(TORPerks.GunPowder.DeadEye))
                {
                    PerkHelper.AddPerkBonusForCharacter(TORPerks.GunPowder.DeadEye, attacker, true, ref resultDamage);
                }
                if(weapon.Item.IsSmallArmsAmmunition() && defender.GetPerkValue(TORPerks.GunPowder.BulletProof))
                {
                    PerkHelper.AddPerkBonusForCharacter(TORPerks.GunPowder.BulletProof, defender, true, ref resultDamage);
                    if(defenderCaptain != null && defenderCaptain.GetPerkValue(TORPerks.GunPowder.BulletProof))
                    {
                        PerkHelper.AddPerkBonusFromCaptain(TORPerks.GunPowder.BulletProof, defenderCaptain, ref resultDamage);
                    }
                }
                if(weapon.Item.IsExplosiveAmmunition() && defender.GetPerkValue(TORPerks.GunPowder.BombingSuit))
                {
                    PerkHelper.AddPerkBonusForCharacter(TORPerks.GunPowder.BombingSuit, defender, true, ref resultDamage);
                    if (defenderCaptain != null && defenderCaptain.GetPerkValue(TORPerks.GunPowder.BombingSuit))
                    {
                        PerkHelper.AddPerkBonusFromCaptain(TORPerks.GunPowder.BombingSuit, defenderCaptain, ref resultDamage);
                    }
                }
                if (weapon.Item.IsExplosiveAmmunition() && attackerCaptain != null && attackerCaptain.GetPerkValue(TORPerks.GunPowder.PackItIn))
                {
                    PerkHelper.AddPerkBonusFromCaptain(TORPerks.GunPowder.PackItIn, attackerCaptain, ref resultDamage);
                }

                WeaponComponentData weaponComponentData = weapon.CurrentUsageItem;
                
                if (attacker.IsHero&&attacker.HeroObject == Hero.MainHero)
                {
                    if (Hero.MainHero.HasAnyCareer())
                    {
                        var choices = Hero.MainHero.GetAllCareerChoices();

                        if (baseDamage>50&& choices.Contains("MartiallePassive4"))
                        {
                            weaponComponentData.WeaponFlags |= WeaponFlags.CanPenetrateShield;
                        }
                    }
                
                
                }
                
            }

            if (collisionData.IsHorseCharge)
            {
                if (attacker.IsMounted && attacker.IsPlayerCharacter)
                {
                    if (attacker.HeroObject.HasAnyCareer())
                    {
                        var choices = attacker.HeroObject.GetAllCareerChoices();

                        if (choices.Contains("DreadKnightPassive2"))
                        {
                            var choice = TORCareerChoices.GetChoice("DreadKnightPassive2");
                            if(choice!=null)
                                resultDamage.AddFactor(choice.GetPassiveValue());
                        }
                    }
                }
            }
            return resultDamage.ResultNumber;
        }

        public float CalculateWardSaveFactor(Agent victim, AttackTypeMask attackTypeMask)
        {
            ExplainedNumber result = new ExplainedNumber(1f);
            var victimCharacter = victim.Character as CharacterObject;
            if(victimCharacter != null)
            {
                var container = victim.GetProperties(PropertyMask.Defense, attackTypeMask);

                if (container.ResistancePercentages[(int)DamageType.All] > 0)
                {
                    result.Add(-container.ResistancePercentages[(int)DamageType.All] / 100);
                }

                if (victimCharacter.GetPerkValue(TORPerks.SpellCraft.Dampener))
                {
                    result.AddFactor(TORPerks.SpellCraft.Dampener.SecondaryBonus);
                }
                SkillHelper.AddSkillBonusForCharacter(TORSkills.Faith, TORSkillEffects.FaithWardSave, victimCharacter, ref result, -1, false);
            }
            return result.ResultNumber;
        }
    }
}
