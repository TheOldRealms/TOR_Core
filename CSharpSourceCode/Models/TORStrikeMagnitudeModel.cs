using Helpers;
using SandBox.GameComponents;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CharacterDevelopment.CareerSystem.Choices;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items;
using TOR_Core.Utilities;
using static TaleWorlds.MountAndBlade.Mission;

namespace TOR_Core.Models
{
    public class TORStrikeMagnitudeModel : SandboxStrikeMagnitudeModel
    {

        public override float CalculateAdjustedArmorForBlow(in AttackInformation attackInformation, in AttackCollisionData collisionData, float baseArmor, BasicCharacterObject attackerCharacter, BasicCharacterObject attackerCaptainCharacter, BasicCharacterObject victimCharacter, BasicCharacterObject victimCaptainCharacter, WeaponComponentData weaponComponent)
        {
            var result = base.CalculateAdjustedArmorForBlow(attackInformation, collisionData, baseArmor, attackerCharacter, attackerCaptainCharacter, victimCharacter, victimCaptainCharacter, weaponComponent);
            ExplainedNumber resultArmor = new(result);
            var attacker = attackerCharacter as CharacterObject;
            var attackerAgent = attackInformation.AttackerAgent;
            if (weaponComponent != null && attackerAgent!=null && attacker != null && !collisionData.IsHorseCharge && !collisionData.IsFallDamage)
            {
                if (attacker.GetPerkValue(TORPerks.GunPowder.PiercingShots) && weaponComponent.IsGunPowderWeapon())
                {
                    PerkHelper.AddPerkBonusForCharacter(TORPerks.GunPowder.PiercingShots, attacker, true, ref resultArmor);
                }

                if (attacker.IsPlayerCharacter && attacker.HeroObject == Hero.MainHero)
                {
                    //Sly : spells are also affected by armor. In the future this could be improved and perks added that grant armour pen to them.
                    //this will miscategorize certain spells as well like amber spear as it strikes like a ranged weapon but is sourced from a spell
                    var attackMask = AttackTypeMask.Melee;
                    if (weaponComponent.IsRangedWeapon || weaponComponent.IsAmmo) attackMask = AttackTypeMask.Ranged;

                    CareerHelper.ApplyBasicCareerPassives(attacker.HeroObject, ref resultArmor, PassiveEffectType.ArmorPenetration, attackMask, true);

                    // EyeOfTheHunterPassive4: Roguery skill reduces target armor by up to 60%
                    if (attackMask == AttackTypeMask.Ranged && Hero.MainHero.HasCareerChoice("EyeOfTheHunterPassive4"))
                    {
                        var roguerySkill = attacker.HeroObject.GetSkillValue(DefaultSkills.Roguery);
                        var maxSkill = 300f;
                        var armorReduction = (roguerySkill / maxSkill) * 0.6f;
                        resultArmor.AddFactor(-armorReduction);
                    }
                }

                if (attacker.IsHero) // never remove this check. operations for item traits can be very heavy
                {
                    var weapon = attackerAgent.WieldedWeapon;
                    //Sly : was there even any reason to split them out like this, or I could have just done attackerAgent.WieldedWeapon and called it a day for the current patch?
                    //if (weaponComponent.IsRangedWeapon)//thrown weapons only as weaponComponent derives from attackInformation which only holds the ammo weapon, not the launching one
                    //{
                    //    weapon = attackInformation.AttackerWeapon;
                    //}
                    //else if (weaponComponent.IsAmmo)//projectiles launched from another weapon
                    //{
                    //    if (attackerAgent.WieldedWeapon.CurrentUsageItem != null && attackerAgent.WieldedWeapon.CurrentUsageItem.AmmoClass == weaponComponent.WeaponClass)
                    //    {
                    //        weapon = attackerAgent.WieldedWeapon;
                    //    }
                    //}
                    //else if (weaponComponent.IsMeleeWeapon)
                    //{
                    //    weapon = attackerAgent.WieldedWeapon;
                    //}


                    var traits = weapon.Item?.GetTraits(attackerAgent);

                    if (weaponComponent.IsAmmo)
                    {
                        var ammoTraits = attackInformation.AttackerWeapon.Item?.GetTraits(attackerAgent);//Sly : this assumes that no effects may be applied to multiple weapons in the equipment which would allow ranged weapons to benefit multiple times from a single source. This can otherwise fetch just the traits of the ammo item itself if bugs occur.
                        if (ammoTraits.Count > 0)
                        {
                            traits.AddRange(ammoTraits);
                        }
                    }

                    if (traits != null)
                    {
                        foreach (var trait in traits)
                        {
                            if (trait?.StatsTuple?.StatType == ItemTraitStatType.ArmorPenetration)
                            {
                                resultArmor.AddFactor(-trait.StatsTuple.Value / 100);
                            }
                        }
                    }
                }

                
                if (attackerAgent.Team.TeamSide == TeamSideEnum.PlayerTeam && attackerCharacter.IsUndead())//Filters for undead that belong to the player, or summoners in the player's party.
                {
                    Hero attackerPartyLeader = new();
                    if (attackInformation.AttackerAgentOrigin is PartyAgentOrigin partyOrigin)
                    {
                        attackerPartyLeader = partyOrigin.Party.LeaderHero;
                    }
                    else if (attackInformation.AttackerAgentOrigin is SummonedAgentOrigin summonedPartyOrigin)
                    {
                         attackerPartyLeader = summonedPartyOrigin.OwnerParty?.LeaderHero;//party shouldn't be null unless a summoned troop summons other troops in turn and it could get fucky
                    }

                    if (attackerPartyLeader == Hero.MainHero)
                    {
                        if (Hero.MainHero.HasCareer(TORCareers.Necromancer))
                        {
                            if (Hero.MainHero.HasCareerChoice("LiberMortisPassive2"))
                            {
                                var choice = TORCareerChoices.GetChoice("LiberMortisPassive2");
                                resultArmor.AddFactor(-choice.GetPassiveValue());
                            }
                        }
                    }
                }
                
                // Tree spirits always ignore 80% of enemy armor
                if (attacker.IsTreeSpirit())
                {
                    resultArmor.AddFactor(-0.8f);
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