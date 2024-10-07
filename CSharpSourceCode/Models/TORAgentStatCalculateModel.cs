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
using TOR_Core.BattleMechanics.CustomArenaModes;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
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

            var equipmentEncumbrance = GetTOREffectiveEquipmentEncumbrance(agent, agentDrivenProperties.WeaponsEncumbrance);
            agentDrivenProperties.WeaponsEncumbrance = equipmentEncumbrance;

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
            var captain = agent.GetPartyLeaderCharacter();
            if (agent.Character is CharacterObject character)
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
                
                
                if (character != null && mobileParty != null && !Mission.Current.IsArenaMission())
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
                                    CareerHelper.ApplyBasicCareerPassives(character.HeroObject,ref ammoCount,PassiveEffectType.Ammo, false);
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

            if ((agent.Origin?.BattleCombatant) is PartyBase partyBase && partyBase.IsMobile)
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

                            if ((skill == DefaultSkills.OneHanded || skill == DefaultSkills.TwoHanded) && choices.Contains("ErrantryWarPassive3") && agent.Character.IsKnightUnit())
                            {
                                var choice = TORCareerChoices.GetChoice("ErrantryWarPassive3");
                                if (choice.Passive != null)
                                    resultNumber.Add(choice.GetPassiveValue(), choice.BelongsToGroup.Name);
                            }

                            if ((skill == DefaultSkills.OneHanded || skill == DefaultSkills.TwoHanded) && choices.Contains("SwampRiderPassive4") && agent.Character.IsKnightUnit())
                            {
                                var choice = TORCareerChoices.GetChoice("SwampRiderPassive4");
                                if (choice.Passive != null)
                                    resultNumber.Add(choice.GetPassiveValue(), choice.BelongsToGroup.Name);
                            }

                            if (skill == DefaultSkills.Polearm && choices.Contains("EnhancedHorseCombatPassive4") && agent.Character.IsKnightUnit())
                            {
                                var choice = TORCareerChoices.GetChoice("EnhancedHorseCombatPassive4");
                                if (choice.Passive != null)
                                    resultNumber.Add(choice.GetPassiveValue(), choice.BelongsToGroup.Name);
                            }

                            if (skill == DefaultSkills.Polearm && choices.Contains("CurseOfMousillonPassive2") && agent.Character.IsKnightUnit())
                            {
                                var choice = TORCareerChoices.GetChoice("CurseOfMousillonPassive2");
                                if (choice.Passive != null)
                                    resultNumber.Add(choice.GetPassiveValue(), choice.BelongsToGroup.Name);
                            }

                            if ((skill == DefaultSkills.OneHanded || skill == DefaultSkills.TwoHanded || skill == DefaultSkills.Polearm) && choices.Contains("NightRider2") && (agent.Character.IsUndead() || agent.Character.IsVampire()))
                            {
                                var choice = TORCareerChoices.GetChoice("NightRider2");
                                if (choice.Passive != null)
                                    resultNumber.Add(choice.GetPassiveValue(), choice.BelongsToGroup.Name);
                            }

                            if ((skill == DefaultSkills.Bow || skill == DefaultSkills.Throwing || skill == DefaultSkills.Crossbow || skill == TORSkills.GunPowder) && choices.Contains("NoRestAgainstEvilPassive2"))
                            {
                                var choice = TORCareerChoices.GetChoice("NoRestAgainstEvilPassive2");
                                if (choice.Passive != null)
                                    resultNumber.Add(choice.GetPassiveValue(), choice.BelongsToGroup.Name);
                            }

                            if (skill == DefaultSkills.Bow && choices.Contains("EyeOfTheHunterPassive3"))
                            {
                                var choice = TORCareerChoices.GetChoice("EyeOfTheHunterPassive3");
                                if (choice.Passive != null)
                                    resultNumber.Add(choice.GetPassiveValue(), choice.BelongsToGroup.Name);
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
                if (character != null)
                {
                    if (character.IsVampire())
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

                    if (character.IsMinotaur())
                    {
                        agent.SetAgentFlags(agent.GetAgentFlags() & ~AgentFlag.CanDefend);
                        agent.Defensiveness = 0.001f;
                        agentDrivenProperties.SwingSpeedMultiplier *= 1.5f;
                    }

                    if (character.IsTreeSpirit())
                    {
                        agent.SetAgentFlags(agent.GetAgentFlags() & ~AgentFlag.CanDefend);
                        agent.Defensiveness = 0.001f;
                    }
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
                
                CareerHelper.ApplyBasicCareerPassives(agent.GetHero(), ref accuracyPenalty, PassiveEffectType.AccuracyPenalty);
                
                CareerHelper.ApplyBasicCareerPassives(agent.GetHero(), ref swingSpeed, PassiveEffectType.SwingSpeed);
            }

            agentDrivenProperties.WeaponMaxMovementAccuracyPenalty = movementAccuracyPenalty.ResultNumber;
            agentDrivenProperties.WeaponInaccuracy = accuracyPenalty.ResultNumber;
            agentDrivenProperties.SwingSpeedMultiplier = swingSpeed.ResultNumber;

        }

        public override float GetMaxCameraZoom(Agent agent)
        {
            _crosshairBehavior ??= Mission.Current.GetMissionBehavior<CustomCrosshairMissionBehavior>();

            if (_crosshairBehavior != null && _crosshairBehavior.CurrentCrosshair is SniperScope && _crosshairBehavior.CurrentCrosshair.IsVisible)
            {
                return 3;
            }

            return base.GetMaxCameraZoom(agent);
        }
        
        //The moment you realize they forget to add an override statement. if they do it needs to be moved on the EffectiveArmorEncumbrance
        public float GetTOREffectiveEquipmentEncumbrance(Agent agent, float value)
        {
            if (agent == null) return 0;
            if (agent.IsMount) return 0;
            var number =  new ExplainedNumber(value);
            if (agent.GetHero() == Hero.MainHero)
            {
                CareerHelper.ApplyBasicCareerPassives(agent.GetHero(), ref number, PassiveEffectType.EquipmentWeightReduction);
            }
  

            return number.ResultNumber;
        }
    }
}