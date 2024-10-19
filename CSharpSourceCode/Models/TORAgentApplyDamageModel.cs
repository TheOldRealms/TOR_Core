using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items;

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

                    if (choices.Contains("MercenaryLordPassive4") || choices.Contains("EndsJustifiesMeansPassive4") || choices.Contains("EyeOfTheHunterPassive2") )
                    {
                        missileWeaponFlags |= WeaponFlags.MultiplePenetration;
                    }
                    
                    if (choices.Contains("StarfireEssencePassive3") )
                    {
                        missileWeaponFlags |= WeaponFlags.CanPenetrateShield;
                    }
                    
                    if ( Hero.MainHero.HasCareer(TORCareers.Waywatcher) && choices.Contains("StarfireEssencePassive4"))
                    {
                        CareerPerkMissionBehavior careerPerkBehavior = Mission.Current.GetMissionBehavior<CareerPerkMissionBehavior>();
                        if (careerPerkBehavior != null)
                        {
                            var value = careerPerkBehavior.CareerMissionVariables[2] *= 0.05f;
                            if (MBRandom.RandomFloat < value)
                            {
                                missileWeaponFlags |= WeaponFlags.AffectsAreaBig;
                            }
                        }  
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
            var attackerAgent = attackInformation.AttackerAgent;
            var result = base.CalculateDamage(attackInformation, collisionData, weapon, baseDamage);
            var attacker = (attackInformation.IsAttackerAgentMount ? attackInformation.AttackerRiderAgentCharacter : attackInformation.AttackerAgentCharacter) as CharacterObject;
            var attackerCaptain = attackInformation.AttackerCaptainCharacter as CharacterObject;
            var defender = (attackInformation.IsVictimAgentMount ? attackInformation.VictimRiderAgentCharacter : attackInformation.VictimAgentCharacter) as CharacterObject;
            var defenderCaptain = attackInformation.VictimCaptainCharacter as CharacterObject;

            var resultDamage = new ExplainedNumber(result);

            if (attacker == null)
            {
                return resultDamage.ResultNumber;
            }

            
            if (defender != null && !weapon.IsEmpty)
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

                        if (choices.Contains("MartiallePassive4") || choices.Contains("NightRiderPassive4") || choices.Contains("TeachingsOfTheWinterFatherPassive3"))
                        {
                            weaponComponentData.WeaponFlags |= WeaponFlags.BonusAgainstShield;
                        }
                    }
            }
            
            if (attacker.IsTreeSpirit())
            {
                resultDamage.AddFactor(20f);
            }

            if ( collisionData.IsHorseCharge && attacker.IsMounted && attacker.IsPlayerCharacter && attacker.HeroObject.HasAnyCareer())
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

        public AgentPropertyContainer CreateAgentPropertyContainer(Agent agent, PropertyMask propertyMask, AttackTypeMask attackTypeMask)
        {
            if (agent.IsMount)
                return AgentPropertyContainer.InitNew();
            
            if (Mission.Current.IsArenaMission())
                return AgentPropertyContainer.InitNew();
            
            float[] damageProportions;
            float[] damageAmplifications;
            float[] damageResistances;
            float[] additionalDamagePercentages;
            
            if (agent.IsHero)
            {
                AssignCharacterProperties(agent, propertyMask, attackTypeMask, out damageProportions, out damageAmplifications, out damageResistances, out additionalDamagePercentages);
            }
            else
            { 
                AssignUnitProperties(agent, propertyMask, attackTypeMask, out damageProportions, out damageAmplifications, out damageResistances, out additionalDamagePercentages);
            }

            AddPerkEffectsToAgentProperties(agent, propertyMask, attackTypeMask, ref damageProportions, ref damageAmplifications,
                ref additionalDamagePercentages, ref damageResistances);
            var result =  new AgentPropertyContainer(damageProportions, damageAmplifications, damageResistances, additionalDamagePercentages);
            
            return result;
        }

        public void AssignCharacterProperties(             
            Agent agent,
            PropertyMask propertyMask,
            AttackTypeMask attackTypeMask,
            out float[] damageProportions, 
            out float[] damageAmplifications, 
            out float[] damageResistances, 
            out float[]additionalDamagePercentages)
        {
            damageProportions = new float[(int)DamageType.All + 1];
            damageAmplifications = new float[(int)DamageType.All + 1];
            damageResistances = new float[(int)DamageType.All + 1]; 
            additionalDamagePercentages = new float[(int)DamageType.All + 1];
             if (propertyMask == PropertyMask.Attack || propertyMask == PropertyMask.All)
             {
                 //Hero item level attributes 
                 List<ItemTrait> itemTraits = new List<ItemTrait>();
                 List<ItemObject> armorItems;
                 // get all equipment Pieces - here only armor
                 armorItems = agent.Character.GetCharacterEquipment(EquipmentIndex.ArmorItemBeginSlot);
                 foreach (var item in armorItems)
                 {
                     if (item.HasTrait())
                         itemTraits.AddRange(item.GetTraits(agent));
                 }
                 //equipment amplifiers, also implies dynamic traits
                 foreach (var itemTrait in itemTraits)
                 {
                     var property = itemTrait.AmplifierTuple;
                     if (property != null)
                         damageAmplifications[(int)property.AmplifiedDamageType] += property.DamageAmplifier;

                     var additionalDamageProperty = itemTrait.AdditionalDamageTuple;
                     if (additionalDamageProperty != null)
                     {
                         additionalDamagePercentages[(int)additionalDamageProperty.DamageType] += additionalDamageProperty.Percent;
                     }

                 }

                 var statusEffectAmplifiers = agent.GetComponent<StatusEffectComponent>().GetAmplifiers(attackTypeMask);

                 for (int i = 0; i < damageAmplifications.Length; i++)
                 {
                     damageAmplifications[i] += statusEffectAmplifiers[i];
                 }
                    
                 //Weapon properties
                 if (attackTypeMask == AttackTypeMask.Ranged)
                 {
                     if (agent.WieldedWeapon.Item != null)
                     {
                         var ammoItem = Mission.Current.Missiles.FirstOrDefault(x => x.ShooterAgent == agent)?.Weapon.Item;
                         var weapon = agent.WieldedWeapon.Item;
                         List<ItemTrait> rangeItemTraits = new List<ItemTrait>();
                            
                         if(ammoItem!=null)
                             rangeItemTraits.AddRange(ammoItem.GetTraits());
                         rangeItemTraits.AddRange(weapon.GetTraits());
                         foreach (var itemTrait in rangeItemTraits)
                         {
                             var property = itemTrait.AmplifierTuple;
                             if(property!=null)
                                 damageAmplifications[(int)property.AmplifiedDamageType] += property.DamageAmplifier;

                             var additionalDamageProperty = itemTrait.AdditionalDamageTuple;
                             if (additionalDamageProperty != null)
                             {
                                 additionalDamagePercentages[(int)additionalDamageProperty.DamageType] += additionalDamageProperty.Percent;
                             }
                         }
                         //range damage Propotions
                         var weaponProperty = weapon.GetTorSpecificData().DamageProportions;
                         if (weaponProperty != null)
                         {
                             foreach (var tuple in weaponProperty)
                             {
                                 damageProportions[(int)tuple.DamageType] = tuple.Percent;
                             }
                         }
                     }
                     else
                     {
                         damageProportions[(int)DamageType.Physical] = 1f; //memo , this is for siege weapons
                     }
                 }

                 if (attackTypeMask == AttackTypeMask.Melee)
                 {
                     if (agent.WieldedWeapon.Item != null)
                     {
                         var weapon = agent.WieldedWeapon.Item;
                         var offhand = agent.WieldedOffhandWeapon.Item;
                         List<ItemTrait> meleeItemTraits = new List<ItemTrait>();
                         meleeItemTraits.AddRange(weapon.GetTraits());
                         if (offhand != null)
                             meleeItemTraits.AddRange(offhand.GetTraits());
                            
                         foreach (var itemTrait in meleeItemTraits)
                         {
                             var property = itemTrait.AmplifierTuple;
                                
                             if(property!=null)
                                 damageAmplifications[(int)property.AmplifiedDamageType] += property.DamageAmplifier;

                             var additionalDamageProperty = itemTrait.AdditionalDamageTuple;
                             if (additionalDamageProperty != null)
                             {
                                 additionalDamagePercentages[(int)additionalDamageProperty.DamageType] += additionalDamageProperty.Percent;
                             }
                         }
                            
                         var weaponProperty = weapon.GetTorSpecificData().DamageProportions;
                         if (weaponProperty != null)
                         {
                             foreach (var tuple in weaponProperty)
                             {
                                 damageProportions[(int)tuple.DamageType] = tuple.Percent;
                             }
                         }
                     }
                     else
                     {
                         damageProportions[(int)DamageType.Physical] = 1f; //memo , this is for siege weapons, in principle a wielded Item shouldn't be found either in case of spell casting - yet it is found.
                     }
                 }
                    
                 if (attackTypeMask == AttackTypeMask.Spell)
                 {
                     if (!agent.WieldedOffhandWeapon.IsEmpty && agent.WieldedOffhandWeapon.Item != null && agent.WieldedOffhandWeapon.Item.StringId.Contains("staff"))
                     {
                         List<ItemTrait> staffItemTraits = agent.WieldedOffhandWeapon.Item.GetTraits();

                         foreach (var itemTrait in staffItemTraits)
                         {
                             var property = itemTrait.AmplifierTuple;
                                
                             if(property!=null)
                                 damageAmplifications[(int)property.AmplifiedDamageType] += property.DamageAmplifier;

                             var additionalDamageProperty = itemTrait.AdditionalDamageTuple;
                             if (additionalDamageProperty != null)
                             {
                                 additionalDamagePercentages[(int)additionalDamageProperty.DamageType] += additionalDamageProperty.Percent;
                             }
                         }
                     }
                 }
                    
             }
             if (propertyMask == PropertyMask.Defense || propertyMask == PropertyMask.All)
             {
                 //Hero item level attributes 

                 

                 List<ItemTrait> itemTraits = new List<ItemTrait>();
                 List<ItemObject> items;

                 items = agent.Character.GetCharacterEquipment(EquipmentIndex.ArmorItemBeginSlot);
                 foreach (var item in items)
                 {
                     if (item.HasTrait())
                         itemTraits.AddRange(item.GetTraits(agent));
                 }

                 //equipment resistances , also implies dynamic traits
                 foreach (var itemTrait in itemTraits)
                 {
                     var defenseProperty = itemTrait.ResistanceTuple;
                     if (defenseProperty == null)
                         continue;
                     damageResistances[(int)defenseProperty.ResistedDamageType] += defenseProperty.ReductionPercent;
                 }

                 //statuseffects
                 var statusEffectResistances = agent.GetComponent<StatusEffectComponent>().GetResistances(attackTypeMask);

                 for (int i = 0; i < damageResistances.Length; i++)
                 {
                     damageResistances[i] += statusEffectResistances[i];
                 }

                 if (agent.WieldedWeapon.Item != null)
                 {
                     List<ItemTrait> wieldedItemTraits = new List<ItemTrait>();
                     var weapon = agent.WieldedWeapon.Item;

                     var offHand = agent.WieldedOffhandWeapon.Item;
                        
                     wieldedItemTraits.AddRange(weapon.GetTraits());
                     if (offHand != null)
                     {
                         wieldedItemTraits.AddRange(offHand.GetTraits());
                     }
                        
                     foreach (var itemTrait in wieldedItemTraits)
                     {
                         var defenseProperty = itemTrait.ResistanceTuple;
                         if (defenseProperty == null)
                             continue;
                         damageResistances[(int)defenseProperty.ResistedDamageType] += defenseProperty.ReductionPercent;
                     }   
                 }
                 
                 
                 
             }
        }
        
        public void AddPerkEffectsToAgentProperties(Agent agent, PropertyMask mask, AttackTypeMask attackMask, ref float[] propotions,ref  float[] damageAmps, ref float[]damageBonuses, ref float[]resistances)
        {
            //this code is odd, it can be also be mostly realized via Careerchoices
            var agentCharacter = agent.Character as CharacterObject;
            var agentCaptain = agent.GetCaptainCharacter();
            var agentLeader = agent.GetPartyLeaderCharacter();
            var agentParty = agent.GetOriginMobileParty();

            if (agentParty != null && agentParty.HasAnyActiveBlessing())
            {
                if (agentParty.HasBlessing("cult_of_manaan"))
                {
                    damageBonuses[(int)DamageType.Lightning] += 0.10f;
                }
                
                if (agentParty.HasBlessing("cult_of_asuryan"))
                {
                    damageBonuses[(int)DamageType.Fire] += 0.10f;
                }

                if ((attackMask  == AttackTypeMask.Ranged && agentParty.HasBlessing("cult_of_kurnous")))
                {
                    damageBonuses[(int)DamageType.Physical] += 0.1f;
                }
                
                if (agentParty.HasBlessing("cult_of_vaul"))
                {
                    resistances[(int)DamageType.Physical] += 0.1f;
                }
            }

            var wieldedItem = agent.WieldedWeapon.Item;

            if (agentCharacter != null)
            {
                if (mask == PropertyMask.Attack || mask == PropertyMask.All)
                {
                    if (agentCharacter.GetPerkValue(TORPerks.SpellCraft.Exchange))
                    {
                        damageBonuses[(int)DamageType.Magical] += damageBonuses[(int)DamageType.Physical];
                    }

                    if (agentCaptain != null && agentCaptain.GetPerkValue(TORPerks.SpellCraft.ArcaneLink))
                    {
                        damageBonuses[(int)DamageType.Magical] += (TORPerks.SpellCraft.ArcaneLink.SecondaryBonus);
                    }

                    if (agentLeader != null && agentLeader.GetPerkValue(TORPerks.Faith.Superstitious) && agentCharacter.IsReligiousUnit())
                    {
                        damageBonuses[(int)DamageType.Physical] += (TORPerks.Faith.Superstitious.SecondaryBonus);
                    }

                    if (wieldedItem != null && wieldedItem.HasWeaponComponent && wieldedItem.IsSpecialAmmunitionItem() && attackMask.HasAnyFlag(AttackTypeMask.Ranged))
                    {
                        if (agentCaptain != null && agentCaptain.GetPerkValue(TORPerks.GunPowder.PackItIn))
                        {
                            propotions[(int)DamageType.Fire] = propotions[(int)DamageType.Physical];
                            propotions[(int)DamageType.Physical] = 0;
                        }
                    }
                }

                if (mask == PropertyMask.Defense || mask == PropertyMask.All)
                {
                    if (agentLeader != null && agentLeader.GetPerkValue(TORPerks.Faith.Imperturbable) && agentCharacter.IsReligiousUnit())
                    {
                        resistances[(int)DamageType.Physical] += (TORPerks.Faith.Imperturbable.SecondaryBonus);
                    }
                }
            }

            if (agent.BelongsToMainParty() && agent.IsHero)
            {
                
                if (Hero.MainHero.HasCareer(TORCareers.Spellsinger))
                {
                    if (Hero.MainHero.HasCareerChoice("FuryOfTheForestPassive1"))
                    {
                        var choice = TORCareerChoices.GetChoice("FuryOfTheForestPassive1");
                        var heroes = Hero.MainHero.PartyBelongedTo.GetMemberHeroes();

                        if (heroes.Any(x =>x.HasKnownLore("DarkMagic") && x.CharacterObject.IsElf()))
                        {
                            damageBonuses[(int)DamageType.Magical] += choice.GetPassiveValue();
                        }
                    }
                    
                    if (Hero.MainHero.HasCareerChoice("FuryOfTheForestPassive2"))
                    {
                        var choice = TORCareerChoices.GetChoice("FuryOfTheForestPassive2");
                        var heroes = Hero.MainHero.PartyBelongedTo.GetMemberHeroes();

                        if (heroes.Any(x =>  x.HasKnownLore("HighMagic") && x.CharacterObject.IsElf()))
                        {
                            resistances[(int)DamageType.All] += choice.GetPassiveValue();
                        }
                    }
                   
                }
            }
            
            
            if (agent != Agent.Main)
            {
                return;
            }
            
            if (Hero.MainHero.HasAttribute("WEDurthuSymbol"))
            {
                resistances[(int)DamageType.Fire]-=0.2f;
            }
                
            var choices = Agent.Main.GetHero().GetAllCareerChoices();
                
                if(mask== PropertyMask.Attack&& attackMask == AttackTypeMask.Melee)
                {
                    if (choices.Contains("HuntTheWickedPassive3"))
                    {
                        var equipment = agent.Character.GetCharacterEquipment(EquipmentIndex.Weapon0, EquipmentIndex.Weapon3);
                        var choice = TORCareerChoices.GetChoice("HuntTheWickedPassive3");
                        foreach (var weapon in equipment)
                        {
                            foreach (var data in weapon.Weapons)
                            {
                                if (data.IsRangedWeapon)
                                {
                                    damageBonuses[(int)DamageType.Physical] += choice.GetPassiveValue();
                                }
                            }
                        }
                    }
                    if (choices.Contains("FuryOfWarPassive1"))
                    {
                        var equipment = agent.Character.GetCharacterEquipment(EquipmentIndex.Weapon0, EquipmentIndex.Weapon3);
                        var choice = TORCareerChoices.GetChoice("FuryOfWarPassive1");
                        foreach (var weapon in equipment)
                        {
                            foreach (var data in weapon.Weapons)
                            {
                                if (data.IsMeleeWeapon)
                                {
                                    damageBonuses[(int)DamageType.Physical] += choice.GetPassiveValue();
                                }
                            }
                        }
                    }
                }

                if (mask == PropertyMask.Defense && attackMask == AttackTypeMask.Melee || attackMask == AttackTypeMask.Ranged)
                {
                    if (choices.Contains("RunesOfTheWhiteWolfPassive1"))
                    {
                        var equipment = agent.Character.GetCharacterEquipment(EquipmentIndex.Head, EquipmentIndex.Head);
                        equipment.AddRange(agent.Character.GetCharacterEquipment(EquipmentIndex.Cape, EquipmentIndex.Cape));
                        var choice = TORCareerChoices.GetChoice("RunesOfTheWhiteWolfPassive1");
                        if (!equipment.IsEmpty())
                        {
                            foreach (var item in equipment)
                            {
                                if (item.StringId.Contains("wolf") || item.StringId.Contains("cape_kotww"))
                                {
                                    resistances[(int)DamageType.All] += choice.GetPassiveValue();
                                    break;
                                }
                            }
        
                        }
                    }
                }
            
            if (agent.Character.HasAttribute("NecromancerChampion"))
            {
                if(( attackMask == AttackTypeMask.Melee&& mask == PropertyMask.Attack))
                {
                    if (agent.Controller == Agent.ControllerType.Player)
                    {
                        
                        if (mask == PropertyMask.Attack&&agent.Character.HasAttribute("NecromancerChampion")&&choices.Contains("LiberMortisKeystone"))
                        {
                            var choice = TORCareerChoices.GetChoice("LiberMortisKeystone");
                            damageBonuses[(int)DamageType.Physical] += choice.GetPassiveValue();
                        }
                
                        if (mask == PropertyMask.Attack&&agent.Character.HasAttribute("NecromancerChampion")&&choices.Contains("BooksOfNagashKeystone"))
                        {
                            var choice = TORCareerChoices.GetChoice("BooksOfNagashKeystone");
                            damageBonuses[(int)DamageType.Magical] += choice.GetPassiveValue();
                        }
                    }
                }

                if (mask == PropertyMask.Defense&&choices.Contains("BookofWsoranKeystone"))
                {
                    var choice = TORCareerChoices.GetChoice("BookofWsoranKeystone");
                    resistances[(int)DamageType.All]+= choice.GetPassiveValue();
                }
            }
        }
        
        
        public void AssignUnitProperties(
             Agent agent,
             PropertyMask propertyMask,
             AttackTypeMask attackTypeMask,
             out float[] damageProportions, 
             out float[] damageAmplifications, 
             out float[] damageResistances, 
             out float[]additionalDamagePercentages)
        {
            damageProportions = new float[(int)DamageType.All + 1];
            damageAmplifications = new float[(int)DamageType.All + 1];
            damageResistances = new float[(int)DamageType.All + 1]; 
            additionalDamagePercentages = new float[(int)DamageType.All + 1];
            
           if (propertyMask == PropertyMask.Attack || propertyMask == PropertyMask.All)
           {
               var unitDamageProportion = agent.Character.GetUnitDamageProportions();
               foreach (var proportionTuple in unitDamageProportion)
               {
                   damageProportions[(int)proportionTuple.DamageType] = proportionTuple.Percent;
               }
               var offenseProperties = agent.Character.GetAttackProperties();

               //add all offense properties of the Unit
               foreach (var property in offenseProperties)
               {
                   damageAmplifications[(int)property.AmplifiedDamageType] += property.DamageAmplifier;
               }
               //add temporary effects like buffs to attack bonuses on items
               List<ItemTrait> dynamicTraits = agent.GetComponent<ItemTraitAgentComponent>()
                   .GetDynamicTraits(agent.WieldedWeapon.Item);
               foreach (var dynamicTrait in dynamicTraits)
               {
                   var attackProperty = dynamicTrait.AmplifierTuple;

                   if (attackProperty != null)
                   {
                       damageAmplifications[(int)attackProperty.AmplifiedDamageType] += attackProperty.DamageAmplifier;
                   }
                   var additionalDamageProperty = dynamicTrait.AdditionalDamageTuple;
                   if (additionalDamageProperty != null)
                   {
                       additionalDamagePercentages[(int)additionalDamageProperty.DamageType] += additionalDamageProperty.Percent;
                   }
               }
               var statusEffectAmplifiers = agent.GetComponent<StatusEffectComponent>().GetAmplifiers(attackTypeMask);
               for (int i = 0; i < damageAmplifications.Length; i++)
               {
                   damageAmplifications[i] += statusEffectAmplifiers[i];
               }
           }
           if (propertyMask == PropertyMask.Defense || propertyMask == PropertyMask.All)
           {
               //add all defense properties of the Unit
               var defenseProperties = agent.Character.GetDefenseProperties();

               foreach (var property in defenseProperties)
               {
                   damageResistances[(int)property.ResistedDamageType] += property.ReductionPercent;
               }

               //add temporary effects like buffs to defense bonuses
               List<ItemTrait> dynamicTraits = agent.GetComponent<ItemTraitAgentComponent>()
                   .GetDynamicTraits(agent.WieldedWeapon.Item);

               foreach (var dynamicTrait in dynamicTraits)
               {
                   var defenseProperty = dynamicTrait.ResistanceTuple;
                   if (defenseProperty != null)
                   {
                       damageResistances[(int)defenseProperty.ResistedDamageType] += defenseProperty.ReductionPercent;
                   }
               }

               //status effects
               var statusEffectResistances = agent.GetComponent<StatusEffectComponent>().GetResistances(attackTypeMask);

               for (int i = 0; i < damageResistances.Length; i++)
               {
                   damageResistances[i] += statusEffectResistances[i];
               }
           }
        }

        public float CalculateWardSaveFactor(Agent victim,float[] resistances, bool friendlyFire)
        {
            var result = new ExplainedNumber(1f);
            var victimCharacter = victim.Character as CharacterObject;
            if (victimCharacter != null)
            {

                if (resistances[(int)DamageType.All] > 0)
                {
                    result.AddFactor(-resistances[(int)DamageType.All]);
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