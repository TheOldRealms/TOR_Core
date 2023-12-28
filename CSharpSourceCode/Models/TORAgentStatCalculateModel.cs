using Helpers;
using SandBox.GameComponents;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;
using TOR_Core.AbilitySystem;
using TOR_Core.Battle.CrosshairMissionBehavior;
using TOR_Core.BattleMechanics.Crosshairs;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.Jousting;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Missions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORAgentStatCalculateModel : SandboxAgentStatCalculateModel
    {
        private float vampireDaySpeedModificator = 1.1f;
        private float vampireNightSpeedModificator = 1.2f;
        private CustomCrosshairMissionBehavior _crosshairBehavior;
        
        public override void InitializeAgentStats(Agent agent, Equipment spawnEquipment, AgentDrivenProperties agentDrivenProperties, AgentBuildData agentBuildData)
        {
            base.InitializeAgentStats(agent, spawnEquipment, agentDrivenProperties, agentBuildData);
            UpdateAgentDrivenProperties(agent, agentDrivenProperties);
        }

        public override void UpdateAgentStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            base.UpdateAgentStats(agent, agentDrivenProperties);
            UpdateAgentDrivenProperties(agent, agentDrivenProperties);
        }

        public override float GetWeaponInaccuracy(Agent agent, WeaponComponentData weapon, int weaponSkill)
        {
            var result = base.GetWeaponInaccuracy(agent, weapon, weaponSkill);
            ExplainedNumber accuracy = new ExplainedNumber(result, false, null);
            var character = agent.Character as CharacterObject;
            var captain = agent.GetPartyLeaderCharacter();
            if (character != null)
            {
                if (weapon.IsRangedWeapon && weapon.RelevantSkill == TORSkills.GunPowder)
                {
                    SkillHelper.AddSkillBonusForCharacter(TORSkills.GunPowder, TORSkillEffects.GunAccuracy, character, ref accuracy, weaponSkill, false, 0);
                    if (agent.HasMount)
                    {
                        PerkHelper.AddPerkBonusForCharacter(TORPerks.GunPowder.MountedHeritage, character, true, ref accuracy);
                    }

                    if (weapon.WeaponClass == WeaponClass.Musket)
                    {
                        PerkHelper.AddPerkBonusFromCaptain(TORPerks.GunPowder.DeadEye, captain, ref accuracy);
                    }
                }
            }

            return accuracy.ResultNumber;
        }

        public override void InitializeMissionEquipment(Agent agent)
        {
            if (agent.Origin is SummonedAgentOrigin) return;
            base.InitializeMissionEquipment(agent);
            if (agent.IsHuman)
            {
                var character = agent.Character as CharacterObject;
                var mobileParty = agent.GetOriginMobileParty();
                
                if (agent != Agent.Main && character != null)
                {
                    //Lance removal Behavior
                    if(Mission.Current.IsSiegeBattle || (Mission.Current.IsFriendlyMission && Mission.Current.GetMissionBehavior<JoustTournamentBehavior>() == null) || Mission.Current.GetMissionBehavior<HideoutMissionController>() != null)
                        TOREquipmentHelper.RemoveLanceFromEquipment(agent, Mission.Current.IsFriendlyMission);      //i would like to change that to knights not beeing in guard position anyhow
                }
                
                
                if (character != null && mobileParty != null)
                {
                    MissionEquipment equipment = agent.Equipment;
                    for (int i = 0; i < 5; i++)
                    {
                        EquipmentIndex equipmentIndex = (EquipmentIndex)i;
                        MissionWeapon missionWeapon = equipment[equipmentIndex];
                        if (!missionWeapon.IsEmpty)
                        {
                            WeaponComponentData currentUsageItem = missionWeapon.CurrentUsageItem;
                            if (currentUsageItem != null && currentUsageItem.IsAmmo && currentUsageItem.RelevantSkill != null)
                            {
                                ExplainedNumber ammoCount = new ExplainedNumber(missionWeapon.Amount);

                                if (agent.IsMainAgent&&!missionWeapon.Item.IsSpecialAmmunitionItem())
                                {
                                    CareerHelper.ApplyBasicCareerPassives(character.HeroObject,ref ammoCount,PassiveEffectType.Ammo);
                                }

                                if (agent.IsMainAgent && character.HeroObject.HasAnyCareer())
                                {
                                    var choices = character.HeroObject.GetAllCareerChoices();

                                    if (missionWeapon.Item.IsSpecialAmmunitionItem()&&choices.Contains("MercenaryLordPassive1"))
                                    {
                                        var choice = TORCareerChoices.GetChoice("MercenaryLordPassive1");
                                        if (choice != null)
                                        {
                                            ammoCount.Add(choice.GetPassiveValue());
                                        }
                                    }
                                }
                                
                                if (currentUsageItem.RelevantSkill == TORSkills.GunPowder && currentUsageItem.WeaponClass == WeaponClass.Cartridge)
                                {
                                    PerkHelper.AddPerkBonusForParty(TORPerks.GunPowder.AmmoWagons, mobileParty, true, ref ammoCount);
                                }

                                var result = MathF.Round(ammoCount.ResultNumber);
                                if (result != missionWeapon.Amount)
                                {
                                    equipment.SetAmountOfSlot(equipmentIndex, (short)result, true);
                                }
                            }
                        }
                    }
                }
            }
        }

        public override int GetEffectiveSkill(Agent agent, SkillObject skill)
        {
            if (agent.Origin is SummonedAgentOrigin) return agent.Character.GetSkillValue(skill);
            var result = base.GetEffectiveSkill(agent, skill);
            ExplainedNumber resultNumber = new ExplainedNumber(result, false, null);

            var partyBase = ((agent.Origin != null) ? agent.Origin.BattleCombatant : null) as PartyBase;
            if (partyBase != null && partyBase.IsMobile)
            {
                var mobileParty = partyBase.MobileParty;
                if (mobileParty != null)
                {
                    if (skill == TORSkills.GunPowder && agent.Character.Equipment.HasWeaponOfClass(WeaponClass.Cartridge))
                    {
                        PerkHelper.AddPerkBonusForParty(TORPerks.GunPowder.RunAndGun, mobileParty, false, ref resultNumber);
                    }

                    if (skill == DefaultSkills.OneHanded && agent.Character.Equipment.HasWeaponOfClass(WeaponClass.Cartridge))
                    {
                        PerkHelper.AddPerkBonusForParty(TORPerks.GunPowder.CloseQuarters, mobileParty, false, ref resultNumber);
                    }

                    if (skill == DefaultSkills.Riding && agent.Character.IsMounted && agent.Character.Equipment.HasWeaponOfClass(WeaponClass.Cartridge))
                    {
                        PerkHelper.AddPerkBonusForParty(TORPerks.GunPowder.MountedHeritage, mobileParty, false, ref resultNumber);
                    }

                    if (mobileParty == MobileParty.MainParty)
                    {
                        if (mobileParty.LeaderHero.HasAnyCareer())
                        {
                            var choices = Agent.Main.GetHero().GetAllCareerChoices();

                            if ((skill == DefaultSkills.OneHanded||skill == DefaultSkills.TwoHanded)&& choices.Contains("ErrantryWarPassive3") && agent.Character.IsKnightUnit())
                            {
                                var choice = TORCareerChoices.GetChoice("ErrantryWarPassive3");
                                if(choice.Passive!=null)
                                    resultNumber.Add(choice.GetPassiveValue(),choice.BelongsToGroup.Name);
                            }
                            
                            if (skill == DefaultSkills.Polearm&&choices.Contains("EnhancedHorseCombatPassive4") && agent.Character.IsKnightUnit())
                            {
                                var choice = TORCareerChoices.GetChoice("EnhancedHorseCombatPassive4");
                                if(choice.Passive!=null)
                                    resultNumber.Add(choice.GetPassiveValue(),choice.BelongsToGroup.Name);
                            }
                            
                            if ((skill == DefaultSkills.OneHanded||skill == DefaultSkills.TwoHanded||skill == DefaultSkills.Polearm)&& choices.Contains("NightRider2") && (agent.Character.IsUndead()||agent.Character.IsVampire()))
                            {
                                var choice = TORCareerChoices.GetChoice("NightRider2");
                                if(choice.Passive!=null)
                                    resultNumber.Add(choice.GetPassiveValue(),choice.BelongsToGroup.Name);
                            }
                            
                            if ((skill == DefaultSkills.Bow||skill == DefaultSkills.Throwing || skill == DefaultSkills.Crossbow || skill == TORSkills.GunPowder)&& choices.Contains("NoRestAgainstEvilPassive2"))
                            {
                                var choice = TORCareerChoices.GetChoice("NoRestAgainstEvilPassive2");
                                if(choice.Passive!=null)
                                    resultNumber.Add(choice.GetPassiveValue(),choice.BelongsToGroup.Name);
                            }
                        }
                    }
                }
            }

            return (int)resultNumber.ResultNumber;
        }

        public override string GetMissionDebugInfoForAgent(Agent agent)
        {
            if (agent.Origin is SummonedAgentOrigin) return "Impossible to debug summoned units. Base implementation has invalid IAgentOriginBase to PartyBase type cast.";
            else return base.GetMissionDebugInfoForAgent(agent);
        }

        public override float GetEffectiveMaxHealth(Agent agent)
        {
            if (agent == null) return 0;
            if (agent.Origin is SummonedAgentOrigin) 
                return agent.BaseHealthLimit;
            
            var explainedNumber = new ExplainedNumber(base.GetEffectiveMaxHealth(agent));
            
            if (agent.IsMount)
            {
                
                if (agent.RiderAgent!=null&&agent.RiderAgent.IsHero&&agent.RiderAgent.GetHero()==Hero.MainHero)
                {
                    CareerHelper.ApplyBasicCareerPassives(agent.RiderAgent.GetHero(),ref explainedNumber,PassiveEffectType.HorseHealth,true);
                }
            }
            
            return explainedNumber.ResultNumber;
        }

        private void UpdateAgentDrivenProperties(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            if (Mission.Current != null && Mission.Current.GetMissionBehavior<JoustFightMissionController>() != null)
            {
                if (agent.IsMount)
                {
                    agentDrivenProperties.TopSpeedReachDuration = 0.8f;
                    agentDrivenProperties.MaxSpeedMultiplier = 1.5f;
                    agentDrivenProperties.CombatMaxSpeedMultiplier = 1.5f;
                }
                else if (agent.IsHuman && !agent.IsPlayerControlled)
                {
                    agentDrivenProperties.AttributeRiding = 350;
                    agentDrivenProperties.AIHoldingReadyMaxDuration = 0.8f;
                    agentDrivenProperties.AiChargeHorsebackTargetDistFactor = 2f;
                }
            }
            //Specific settings for the tilean duelist
            if(agent.Character != null && agent.Character.StringId == "tor_ti_vittorio")
            {
                agentDrivenProperties.TopSpeedReachDuration = 0.8f;
                agentDrivenProperties.MaxSpeedMultiplier = 1.5f;
                agentDrivenProperties.CombatMaxSpeedMultiplier = 1.5f;
                agentDrivenProperties.AIEstimateStunDurationPrecision = 0.95f;
                agentDrivenProperties.KickStunDurationMultiplier = 0.1f;
                agentDrivenProperties.ShieldBashStunDurationMultiplier = 0.1f;
                agentDrivenProperties.AiMovementDelayFactor = 0.5f;
                return;
            }
            if (agent.IsHuman)
            {
                AddSkillEffectsForAgent(agent, agentDrivenProperties);
                AddPerkEffectsForAgent(agent, agentDrivenProperties);
                var character = agent.Character as CharacterObject;
                if (character != null && character.IsVampire())
                {
                    float modificator = vampireDaySpeedModificator;
                    if (Campaign.Current != null && Campaign.Current.IsNight)
                    {
                        modificator = vampireNightSpeedModificator;
                    }

                    agentDrivenProperties.TopSpeedReachDuration *= modificator;
                    agentDrivenProperties.MaxSpeedMultiplier *= modificator;
                    agentDrivenProperties.CombatMaxSpeedMultiplier *= modificator;
                }
            }

            UpdateDynamicAgentDrivenProperties(agent, agentDrivenProperties);
        }

        private void UpdateDynamicAgentDrivenProperties(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            var statusEffectComponent = agent.IsMount ? agent.RiderAgent?.GetComponent<StatusEffectComponent>() : agent.GetComponent<StatusEffectComponent>();
            if (statusEffectComponent == null)
                return;

            if (!statusEffectComponent.AreBaseValuesInitialized() || !statusEffectComponent.ModifiedDrivenProperties) return;
            var speedModifier = statusEffectComponent.GetMovementSpeedModifier();
            if (speedModifier != 0f)
            {
                var speedMultiplier = Mathf.Clamp(speedModifier + 1, 0, 2); //to set in the right offset, where -100% would actually result in 0% movement speed
                if (agent.IsMount)
                {
                    agentDrivenProperties.SetDynamicMountMovementProperties(statusEffectComponent, speedMultiplier);
                }
                else
                {
                    agentDrivenProperties.SetDynamicHumanoidMovementProperties(statusEffectComponent, speedMultiplier);
                }
            }
            else
            {
                if (agent.IsMount)
                {
                    agentDrivenProperties.SetDynamicMountMovementProperties(statusEffectComponent, 1);
                }
                else
                {
                    agentDrivenProperties.SetDynamicHumanoidMovementProperties(statusEffectComponent, 1);
                }
            }

            var weaponSwingSpeedModifier = statusEffectComponent.GetAttackSpeedModifier();
            if (weaponSwingSpeedModifier != 0)
            {
                var swingSpeedMultiplier = Mathf.Clamp(weaponSwingSpeedModifier + 1, 0.05f, 2); //I guess its better to set here a minimum, just in case something breaks.
                if (agent.IsMount) return;

                agentDrivenProperties.SetDynamicCombatProperties(statusEffectComponent, swingSpeedMultiplier);
            }
            else
            {
                agentDrivenProperties.SetDynamicCombatProperties(statusEffectComponent, 1); //I have the feeling this call is not necessary given the many updates that are done per frame.
            }

            var reloadSpeedModifier = statusEffectComponent.GetReloadSpeedModifier();
            
            if (reloadSpeedModifier != 0)
            {
                var reloadSpeed = Mathf.Clamp(reloadSpeedModifier + 1, 0.05f, 2); 
                if (agent.IsMount) return;

                agentDrivenProperties.SetDynamicReloadProperties(statusEffectComponent, reloadSpeed);
            }
            else
            {
                agentDrivenProperties.SetDynamicReloadProperties(statusEffectComponent, 1); 
            }
        }

        private void AddSkillEffectsForAgent(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            EquipmentIndex wieldedItemIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            WeaponComponentData weapon = (wieldedItemIndex != EquipmentIndex.None) ? agent.Equipment[wieldedItemIndex].CurrentUsageItem : null;
            var character = agent.Character as CharacterObject;
            var captain = agent.GetCaptainCharacter();
            if (weapon != null && character != null)
            {
                int effectiveSkill = GetEffectiveSkill(agent, weapon.RelevantSkill);
                ExplainedNumber reloadSpeed = new ExplainedNumber(agentDrivenProperties.ReloadSpeed);
                if (weapon.RelevantSkill == TORSkills.GunPowder)
                {
                    SkillHelper.AddSkillBonusForCharacter(TORSkills.GunPowder, TORSkillEffects.GunReloadSpeed, character, ref reloadSpeed, effectiveSkill, true, 0);
                }

                agentDrivenProperties.ReloadSpeed = reloadSpeed.ResultNumber;
            }
        }

        private void AddPerkEffectsForAgent(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            EquipmentIndex wieldedItemIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            WeaponComponentData weapon = (wieldedItemIndex != EquipmentIndex.None) ? agent.Equipment[wieldedItemIndex].CurrentUsageItem : null;
            var character = agent.Character as CharacterObject;
            var captain = agent.GetCaptainCharacter();
            ExplainedNumber movementAccuracyPenalty = new ExplainedNumber(agentDrivenProperties.WeaponMaxMovementAccuracyPenalty);
            ExplainedNumber accuracyPenalty = new ExplainedNumber(agentDrivenProperties.WeaponInaccuracy);
            ExplainedNumber swingSpeed = new ExplainedNumber(agentDrivenProperties.SwingSpeedMultiplier);
            if (weapon != null && character != null)
            {
                if (weapon.WeaponClass == WeaponClass.Pistol && !agent.HasMount)
                {
                    PerkHelper.AddPerkBonusForCharacter(TORPerks.GunPowder.RunAndGun, character, true, ref movementAccuracyPenalty);
                }
            }

            if (agent.IsMainAgent && agent.GetHero().HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(agent.GetHero(), ref movementAccuracyPenalty, PassiveEffectType.RangedMovementPenalty);
                
                CareerHelper.ApplyBasicCareerPassives(agent.GetHero(), ref accuracyPenalty, PassiveEffectType.AccuracyPenalty,true);
                
                CareerHelper.ApplyBasicCareerPassives(agent.GetHero(), ref swingSpeed, PassiveEffectType.SwingSpeed,true);
            }

            agentDrivenProperties.WeaponMaxMovementAccuracyPenalty = movementAccuracyPenalty.ResultNumber;
            agentDrivenProperties.WeaponInaccuracy = accuracyPenalty.ResultNumber;
            agentDrivenProperties.SwingSpeedMultiplier = swingSpeed.ResultNumber;

        }

        public override float GetMaxCameraZoom(Agent agent)
        {
            if (_crosshairBehavior == null)
            {
                _crosshairBehavior = Mission.Current.GetMissionBehavior<CustomCrosshairMissionBehavior>();
            }

            if (_crosshairBehavior != null && _crosshairBehavior.CurrentCrosshair is SniperScope && _crosshairBehavior.CurrentCrosshair.IsVisible)
            {
                return 3;
            }

            return base.GetMaxCameraZoom(agent);
        }

        public AgentPropertyContainer AddPerkEffectsToAgentPropertyContainer(Agent agent, PropertyMask mask, AttackTypeMask attackMask, AgentPropertyContainer container)
        {
            
            var proportions = container.DamageProportions;
            var damageamps = container.DamagePercentages;
            var damagebonuses = container.AdditionalDamagePercentages;
            var resistances = container.ResistancePercentages;

            var agentCharacter = agent.Character as CharacterObject;
            var agentCaptain = agent.GetCaptainCharacter();
            var agentLeader = agent.GetPartyLeaderCharacter();

            var wieldedItem = agent.WieldedWeapon.Item;

            if (agentCharacter != null)
            {
                if (mask == PropertyMask.Attack || mask == PropertyMask.All)
                {
                    if (agentCharacter.GetPerkValue(TORPerks.SpellCraft.Exchange))
                    {
                        damagebonuses[(int)DamageType.Magical] += proportions[(int)DamageType.Physical];
                    }

                    if (agentCaptain != null && agentCaptain.GetPerkValue(TORPerks.SpellCraft.ArcaneLink))
                    {
                        damagebonuses[(int)DamageType.Magical] += (TORPerks.SpellCraft.ArcaneLink.SecondaryBonus);
                    }

                    if (agentLeader != null && agentLeader.GetPerkValue(TORPerks.Faith.Superstitious) && agentCharacter.IsReligiousUnit())
                    {
                        damagebonuses[(int)DamageType.Physical] += (TORPerks.Faith.Superstitious.SecondaryBonus);
                    }

                    if (wieldedItem != null && wieldedItem.HasWeaponComponent && wieldedItem.IsSpecialAmmunitionItem() && attackMask.HasAnyFlag(AttackTypeMask.Ranged))
                    {
                        if (agentCaptain != null && agentCaptain.GetPerkValue(TORPerks.GunPowder.PackItIn))
                        {
                            proportions[(int)DamageType.Fire] = proportions[(int)DamageType.Physical];
                            proportions[(int)DamageType.Physical] = 0;
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

            var result = new AgentPropertyContainer(proportions, damageamps, resistances, damagebonuses);
            if (!Hero.MainHero.HasAnyCareer()) return result;
            if (agent == Agent.Main)
            {
                
                result = CareerHelper.AddBasicCareerPassivesToPropertyContainerForMainAgent(agent, result, attackMask, mask);
                
                var choices = Agent.Main.GetHero().GetAllCareerChoices();
                
           
                if (choices.Contains("InspirationOfTheLadyPassive4") && mask == PropertyMask.Defense)
                {
                    var weight = agent.Character.Equipment.GetTotalWeightOfArmor(true);
                    if (weight <= 11)
                    {
                        var choice = TORCareerChoices.GetChoice("InspirationOfTheLadyPassive4");
                        if (choice != null)
                        {
                            result.ResistancePercentages[(int)DamageType.All] += choice.GetPassiveValue();
                        }
                    }
                }
                
                if(( mask== PropertyMask.Attack&& attackMask == AttackTypeMask.Ranged &&choices.Contains("ToolsOfJudgementPassive4")))
                {
                    var equipment = agent.Character.GetCharacterEquipment(EquipmentIndex.Weapon0, EquipmentIndex.Weapon3);

                    var choice = TORCareerChoices.GetChoice("ToolsOfJudgementPassive4");
                    foreach (var weapon in equipment)
                    {
                        foreach (var data in weapon.Weapons)
                        {
                            if (data.IsMeleeWeapon)
                            {
                                result.DamagePercentages[(int)DamageType.All] += choice.GetPassiveValue();
                            }
                        }
                    }
                }
                
                if(( mask== PropertyMask.Attack&& attackMask == AttackTypeMask.Melee &&choices.Contains("HuntTheWickedPassive3")))
                {
                    var equipment = agent.Character.GetCharacterEquipment(EquipmentIndex.Weapon0, EquipmentIndex.Weapon3);

                    var choice = TORCareerChoices.GetChoice("HuntTheWickedPassive3");
                    foreach (var weapon in equipment)
                    {
                        foreach (var data in weapon.Weapons)
                        {
                            if (data.IsRangedWeapon)
                            {
                                result.DamagePercentages[(int)DamageType.All] += choice.GetPassiveValue();
                            }
                        }
                    }
                }
                
                if (choices.Contains("JustCausePassive3") && mask == PropertyMask.Defense)
                {
                    var weight = agent.Character.Equipment.GetTotalWeightOfArmor(true);
                    if (weight <= 11)
                    {
                        var choice = TORCareerChoices.GetChoice("JustCausePassive3");
                        if (choice != null)
                        {
                            result.ResistancePercentages[(int)DamageType.All] += choice.GetPassiveValue();
                        }
                    }
                }

                if (agent.HasMount&&choices.Contains("EnhancedHorseCombatPassive2") && mask == PropertyMask.Attack )
                {
                    var choice = TORCareerChoices.GetChoice("EnhancedHorseCombatPassive2");
                    if (choice != null)
                    {
                        result.AdditionalDamagePercentages[(int)DamageType.Physical] += choice.GetPassiveValue();
                    }
                    
                }
            }
            else if (agentLeader != null && agentLeader == CharacterObject.PlayerCharacter)
            {
                if (!Agent.Main.GetHero().HasAnyCareer()) return result;

                var choices = Agent.Main.GetHero().GetAllCareerChoices();

                if (choices.Contains("RelentlessFanaticPassive3") && mask == PropertyMask.Defense && attackMask == AttackTypeMask.Ranged)
                {
                    if (agent.Character.UnitBelongsToCult("cult_of_sigmar")||(!agent.Character.IsReligiousUnit() && choices.Contains("Archlector2")))
                    {
                        var choice = TORCareerChoices.GetChoice("RelentlessFanaticPassive3");
                        if (choice != null)
                        {
                            result.ResistancePercentages[(int)DamageType.Physical] += choice.GetPassiveValue();
                        }
                        
                    }
                }
                
                if (!agent.IsHero&&mask == PropertyMask.Attack && attackMask == AttackTypeMask.Ranged&& choices.Contains("HuntTheWickedPassive4") )
                {
                    var choice = TORCareerChoices.GetChoice("HuntTheWickedPassive4");
                    if (choice != null)
                    {
                        result.AdditionalDamagePercentages[(int)DamageType.Physical] += choice.GetPassiveValue();
                    }
                }
                
                if (!agent.IsHero&&mask == PropertyMask.Attack && choices.Contains("GuiltyByAssociationPassive2") )
                {
                    var choice = TORCareerChoices.GetChoice("GuiltyByAssociationPassive2");
                    if (choice != null)
                    {
                        result.AdditionalDamagePercentages[(int)DamageType.Holy] += choice.GetPassiveValue();
                    }
                }
                
                if (choices.Contains("FeyEntchantmentPassive4") && mask == PropertyMask.Defense)
                {
                    if (agent.Character.IsKnightUnit())
                    {
                        var choice = TORCareerChoices.GetChoice("FeyEntchantmentPassive4");
                        if (choice != null)
                        {
                            result.ResistancePercentages[(int)DamageType.All] += choice.GetPassiveValue();
                        }
                        
                    }
                }
                
                if (choices.Contains("TalesOfGilesPassive3") && mask == PropertyMask.Defense)
                {
                    if (agentCharacter.Culture.Name.ToString() =="vlandia")
                    {
                        var choice = TORCareerChoices.GetChoice("TalesOfGilesPassive3");
                        if (choice != null)
                        {
                            result.ResistancePercentages[(int)DamageType.All] += choice.GetPassiveValue();
                        }
                        
                    }
                }
                
                
                if (choices.Contains("HolyPurgePassive2") && mask == PropertyMask.Defense)
                {
                    if (agent.Character.UnitBelongsToCult("cult_of_sigmar")||(!agent.Character.IsReligiousUnit() && choices.Contains("Archlector2")))
                    {
                        var choice = TORCareerChoices.GetChoice("HolyPurgePassive2");
                        if (choice != null)
                        {
                            result.ResistancePercentages[(int)DamageType.All] += choice.GetPassiveValue();
                        }
                        
                    }
                }
                
                if (choices.Contains("HolyPurgePassive4") && mask == PropertyMask.Attack)
                {
                    bool isSigmariteTroop = agent.Character.UnitBelongsToCult("cult_of_sigmar") || (!agent.Character.IsReligiousUnit() && choices.Contains("Archlector2"));
                    if (isSigmariteTroop)
                    {
                        var choice = TORCareerChoices.GetChoice("HolyPurgePassive4");
                        if (choice != null)
                        {
                            result.DamagePercentages[(int)DamageType.Holy] += choice.GetPassiveValue();
                        }
                    }
                }
                
                if (choices.Contains("FeyEntchantmentPassive3") && mask == PropertyMask.Attack && attackMask == AttackTypeMask.Ranged||attackMask == AttackTypeMask.Melee)
                {
                    var choice = TORCareerChoices.GetChoice("FeyEntchantmentPassive3");
                    if (choice!= null)
                    {
                        result.AdditionalDamagePercentages[(int)DamageType.Magical] += choice.GetPassiveValue();
                    }
                }
                
                if (choices.Contains("MercenaryLordPassive2") && mask == PropertyMask.Attack && attackMask == AttackTypeMask.Ranged)
                {
                    var choice = TORCareerChoices.GetChoice("MercenaryLordPassive2");
                    if (choice!= null)
                    {
                        result.AdditionalDamagePercentages[(int)DamageType.Physical] += choice.GetPassiveValue();
                    }
                }
                
                if (choices.Contains("CommanderPassive2") && mask == PropertyMask.Attack && attackMask == AttackTypeMask.Melee)
                {
                    var choice = TORCareerChoices.GetChoice("MercenaryLordPassive2");
                    if (choice!= null)
                    {
                        result.AdditionalDamagePercentages[(int)DamageType.Physical] += choice.GetPassiveValue();
                    }
                }
                
                if (agent.IsUndead() && mask == PropertyMask.Defense)
                {
                    if (choices.Contains("MasterOfDeadPassive2"))
                    {
                        var choice = TORCareerChoices.GetChoice("MasterOfDeadPassive2");
                        if (choice != null)
                        {
                            result.ResistancePercentages[(int)DamageType.All] += choice.GetPassiveValue();
                        }
                    }
                    
                    if (choices.Contains("BookofWsoranPassive3"))
                    {
                        var choice = TORCareerChoices.GetChoice("BookofWsoranPassive3");
                        if (choice != null)
                        {
                            result.ResistancePercentages[(int)DamageType.All] += choice.GetPassiveValue();
                        }
                    }
                    
                }

                if (agent.IsUndead()&&choices.Contains("DeArcanisKadonPassive1") && mask == PropertyMask.Attack)
                {
                    var choice = TORCareerChoices.GetChoice("DeArcanisKadonPassive1");
                    if (choice != null)
                    {
                        result.AdditionalDamagePercentages[(int)DamageType.Magical] += choice.GetPassiveValue();
                    }
                }
                
                if (agent.IsUndead()&&choices.Contains("LiberMortisPassive3") && mask == PropertyMask.Attack)
                {
                    var choice = TORCareerChoices.GetChoice("LiberMortisPassive3");
                    if (choice != null)
                    {
                        result.AdditionalDamagePercentages[(int)DamageType.Physical] += choice.GetPassiveValue();
                    }
                }
                
                if (!agent.IsHero&&agent.IsVampire()&&choices.Contains("ControlledHungerPassive4") && mask == PropertyMask.Defense)
                {
                    var choice = TORCareerChoices.GetChoice("ControlledHungerPassive4");
                    if (choice != null)
                    {
                        result.ResistancePercentages[(int)DamageType.All] += choice.GetPassiveValue();
                    }
                    
                }

                if (choices.Contains("GrailVowPassive2") && mask == PropertyMask.Attack)
                {
                    bool isbattlePilgrim = agent.Character.UnitBelongsToCult("cult_of_lady");   //might need more love later
                    if (isbattlePilgrim)
                    {
                        var choice = TORCareerChoices.GetChoice("GrailVowPassive2");
                        if (choice != null)
                        {
                            result.DamagePercentages[(int)DamageType.Holy] += choice.GetPassiveValue();
                        }
                    }
                }
                
                if (attackMask==AttackTypeMask.Melee&&mask == PropertyMask.Defense&&!agent.IsHero&&choices.Contains("QuestingVow3")&&agent.Character.IsKnightUnit())
                {
                    var choice = TORCareerChoices.GetChoice("QuestingVow3");
                    if (choice != null)
                    {
                        result.ResistancePercentages[(int)DamageType.Physical] += choice.GetPassiveValue();
                    }
                        
                }
            }


            if (agent.Character.HasAttribute("NecromancerChampion"))
            {
                var choices = Hero.MainHero.GetAllCareerChoices();
                if(( attackMask == AttackTypeMask.Melee&& mask == PropertyMask.Attack))
                {
                    if (agent.Controller == Agent.ControllerType.Player)
                    {
                        
                        if (mask == PropertyMask.Attack&&agent.Character.HasAttribute("NecromancerChampion")&&choices.Contains("LiberMortisKeystone"))
                        {
                            var choice = TORCareerChoices.GetChoice("LiberMortisKeystone");
                            result.AdditionalDamagePercentages[(int)DamageType.Physical] += choice.GetPassiveValue();
                        }
                
                        if (mask == PropertyMask.Attack&&agent.Character.HasAttribute("NecromancerChampion")&&choices.Contains("BooksOfNagashKeystone"))
                        {
                            var choice = TORCareerChoices.GetChoice("BooksOfNagashKeystone");
                            result.AdditionalDamagePercentages[(int)DamageType.Magical] += choice.GetPassiveValue();
                        }
                    }
                }

                if (mask == PropertyMask.Defense&&choices.Contains("BookofWsoranKeystone"))
                {
                    var choice = TORCareerChoices.GetChoice("BookofWsoranKeystone");
                    result.ResistancePercentages[(int)DamageType.All]+= choice.GetPassiveValue();
                }
            }
            
            

            return result;
        }
    }
}