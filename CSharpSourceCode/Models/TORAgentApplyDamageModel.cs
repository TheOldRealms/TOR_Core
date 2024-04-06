using Helpers;
using SandBox.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.Models
{
    public class TORAgentApplyDamageModel : SandboxAgentApplyDamageModel
    {
        public override void DecideMissileWeaponFlags(Agent attackerAgent, MissionWeapon missileWeapon, ref WeaponFlags missileWeaponFlags)
        {
            base.DecideMissileWeaponFlags(attackerAgent, missileWeapon, ref missileWeaponFlags);
            var character = attackerAgent.Character as CharacterObject;
            if (character != null && !missileWeapon.IsEmpty)
            {
                if (missileWeapon.CurrentUsageItem.WeaponClass == WeaponClass.Cartridge && character.GetPerkValue(TORPerks.GunPowder.PiercingShots)) missileWeaponFlags |= WeaponFlags.CanPenetrateShield;

                if (attackerAgent.IsMainAgent && Hero.MainHero.HasAnyCareer())
                {
                    var choices = Hero.MainHero.GetAllCareerChoices();

                    if (choices.Contains("MercenaryLordPassive4") || choices.Contains("EndsJustifiesMeansPassive4") )
                    {
                        missileWeaponFlags |= WeaponFlags.MultiplePenetration;
                    }
                }

                if (attackerAgent.HasAttribute("ShieldPenetration"))
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

            var resultDamage = new ExplainedNumber(result);
            if (attacker != null && defender != null && !weapon.IsEmpty)
            {
                if (attacker.GetPerkValue(TORPerks.GunPowder.CloseQuarters) && weapon.CurrentUsageItem.WeaponClass == WeaponClass.Cartridge)
                {
                    var shotLength = (collisionData.CollisionGlobalPosition - collisionData.MissileStartingPosition).Length;
                    if (shotLength <= 7)
                        PerkHelper.AddPerkBonusForCharacter(TORPerks.GunPowder.CloseQuarters, attacker, true, ref resultDamage);
                }

                if (weapon.Item.StringId.Contains("longrifle") && attacker.GetPerkValue(TORPerks.GunPowder.DeadEye))
                {
                    PerkHelper.AddPerkBonusForCharacter(TORPerks.GunPowder.DeadEye, attacker, true, ref resultDamage);
                }

                if (weapon.Item.IsSmallArmsAmmunition() && defender.GetPerkValue(TORPerks.GunPowder.BulletProof))
                {
                    PerkHelper.AddPerkBonusForCharacter(TORPerks.GunPowder.BulletProof, defender, true, ref resultDamage);
                    if (defenderCaptain != null && defenderCaptain.GetPerkValue(TORPerks.GunPowder.BulletProof))
                        PerkHelper.AddPerkBonusFromCaptain(TORPerks.GunPowder.BulletProof, defenderCaptain, ref resultDamage);
                }

                if (weapon.Item.IsExplosiveAmmunition() && defender.GetPerkValue(TORPerks.GunPowder.BombingSuit))
                {
                    PerkHelper.AddPerkBonusForCharacter(TORPerks.GunPowder.BombingSuit, defender, true, ref resultDamage);
                    if (defenderCaptain != null && defenderCaptain.GetPerkValue(TORPerks.GunPowder.BombingSuit))
                        PerkHelper.AddPerkBonusFromCaptain(TORPerks.GunPowder.BombingSuit, defenderCaptain, ref resultDamage);
                }

                if (weapon.Item.IsExplosiveAmmunition() && attackerCaptain != null && attackerCaptain.GetPerkValue(TORPerks.GunPowder.PackItIn)) PerkHelper.AddPerkBonusFromCaptain(TORPerks.GunPowder.PackItIn, attackerCaptain, ref resultDamage);

                var weaponComponentData = weapon.CurrentUsageItem;

                if (attacker.IsHero && attacker.HeroObject == Hero.MainHero)
                    if (Hero.MainHero.HasAnyCareer())
                    {
                        var choices = Hero.MainHero.GetAllCareerChoices();

                        if (choices.Contains("MartiallePassive4") || choices.Contains("NightRiderPassive4") || choices.Contains("CrusherOfTheWeakPassive4"))
                        {
                            weaponComponentData.WeaponFlags |= WeaponFlags.BonusAgainstShield;
                        }
                    }
            }

            if (collisionData.IsHorseCharge && attacker != null && attacker.IsMounted && attacker.IsPlayerCharacter && attacker.HeroObject.HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(attacker.HeroObject, ref resultDamage, PassiveEffectType.HorseChargeDamage);
            }
             
            return resultDamage.ResultNumber;
        }


        public override MeleeCollisionReaction DecidePassiveAttackCollisionReaction(
            Agent attacker,
            Agent defender,
            bool isFatalHit)
        {
            var collisionReaction = base.DecidePassiveAttackCollisionReaction(attacker, defender, isFatalHit);
            if (collisionReaction == MeleeCollisionReaction.Bounced)
                if (attacker.Character.IsPlayerCharacter || attacker.GetPartyLeaderCharacter() == CharacterObject.PlayerCharacter)
                {
                    var component = attacker.GetComponent<StatusEffectComponent>();
                    var steadinessModifier = 0f;
                    if (component != null) steadinessModifier = component.GetLanceSteadinessModifier();

                    if (steadinessModifier <= 0) return collisionReaction;

                    var chance = MBRandom.RandomFloatRanged(0, 1);

                    if (chance < steadinessModifier)
                    {
                        collisionReaction = MeleeCollisionReaction.SlicedThrough;
                        return collisionReaction;
                    }
                }

            return collisionReaction;
        }

        public override bool DecideMountRearedByBlow(Agent attackerAgent, Agent victimAgent, in AttackCollisionData collisionData, WeaponComponentData attackerWeapon, in Blow blow)
        {
            var value = base.DecideMountRearedByBlow(attackerAgent, victimAgent, collisionData, attackerWeapon, blow);

            if (victimAgent.RiderAgent != null && victimAgent.RiderAgent.HasAttribute("HorseSteady"))
            {
                return false;
            }

            return value;
        }

        public float CalculateWardSaveFactor(Agent victim,float[] resistances, bool friendlyFire)
        {
            var result = new ExplainedNumber(1f);
            var victimCharacter = victim.Character as CharacterObject;
            if (victimCharacter != null)
            {

                if (resistances[(int)DamageType.All] > 0)
                {
                    result.Add(-resistances[(int)DamageType.All]);
                }
                
                
                if (victimCharacter.GetPerkValue(TORPerks.SpellCraft.Dampener))
                {
                    result.AddFactor(TORPerks.SpellCraft.Dampener.SecondaryBonus);
                }
                SkillHelper.AddSkillBonusForCharacter(TORSkills.Faith, TORSkillEffects.FaithWardSave, victimCharacter, ref result, -1, false);
            }

            result.LimitMax(1);
            if (!friendlyFire)
            {
                result.LimitMin (0.11f);
            }
            return result.ResultNumber;
        }
    }
}