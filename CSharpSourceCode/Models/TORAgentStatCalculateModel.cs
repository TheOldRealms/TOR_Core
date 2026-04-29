using Helpers;
using SandBox.GameComponents;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Hideout;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.AbilitySystem;
using TOR_Core.Battle.CrosshairMissionBehavior;
using TOR_Core.BattleMechanics.Crosshairs;
using TOR_Core.BattleMechanics.CustomArenaModes;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Items;
using TOR_Core.Missions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORAgentStatCalculateModel : SandboxAgentStatCalculateModel
    {
        private const float OrcSpeedModificatior = 0.95f;
        private const float GoblinSpeedModificatior = 1.1f;
        private const float DwarfSpeedModificatior = 0.8f;
        private float vampireDaySpeedModificator = 1.1f;
        private float vampireNightSpeedModificator = 1.2f;
        private CustomCrosshairMissionBehavior _crosshairBehavior;
        private const float OrcHandlingMultiplier = 2.0f; // will result in more or less 4 times the energy for interrupted swings combined with orc energy bonus

        private bool _checkedMissionType = false;
        private bool _isDuelMission = false;
        private bool _isJoustMission = false;

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
        
        public override bool CanAgentRideMount(Agent agent, Agent targetMount)
        {
            if (agent.Character.IsDwarf())
            {
                return false;
            }
            return agent.CheckSkillForMounting(targetMount);
        }

        public override float GetWeaponInaccuracy(Agent agent, WeaponComponentData weapon, int weaponSkill)
        {
            var result = base.GetWeaponInaccuracy(agent, weapon, weaponSkill);
            ExplainedNumber accuracy = new ExplainedNumber(result, false, null);
            var captain = agent.GetCaptainCharacter();
            if (agent.Character is CharacterObject character)
            {
                if (weapon.IsRangedWeapon && weapon.RelevantSkill == TORSkills.GunPowder)
                {
                    SkillHelper.AddSkillBonusForCharacter(TORSkillEffects.GunAccuracy, character, ref accuracy);
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

        public override float GetKnockDownResistance(Agent agent, StrikeType strikeType)
        {
            if (agent.HasAttribute("Tubthumping"))
            {
                return 1;
            }
            return base.GetKnockDownResistance(agent, strikeType);
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
                    if (Mission.Current.IsSiegeBattle || (Mission.Current.IsFriendlyMission && Mission.Current.GetMissionBehavior<JoustTournamentBehavior>() == null) || Mission.Current.GetMissionBehavior<HideoutMissionController>() != null)
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
                            if (currentUsageItem != null && (currentUsageItem.IsAmmo || currentUsageItem.AmmoClass == WeaponClass.Stone) && currentUsageItem.RelevantSkill != null)
                            {
                                ExplainedNumber ammoCount = new ExplainedNumber(missionWeapon.Amount);

                                if (agent.IsMainAgent && !missionWeapon.Item.IsSpecialAmmunitionItem())
                                {
                                    CareerHelper.ApplyBasicCareerPassives(character.HeroObject, ref ammoCount, PassiveEffectType.Ammo, false);
                                }

                                if (agent.IsMainAgent && character.HeroObject.HasAnyCareer())
                                {
                                    var choices = character.HeroObject.GetAllCareerChoices();

                                    if (missionWeapon.Item.IsSpecialAmmunitionItem() && choices.Contains("MercenaryLordPassive1"))
                                    {
                                        var choice = TORCareerChoices.GetChoice("MercenaryLordPassive1");
                                        if (choice != null)
                                        {
                                            ammoCount.Add(choice.GetPassiveValue());
                                        }
                                    }
                                }

                                if (Hero.MainHero.HasCareer(TORCareers.Ironbreaker))
                                {
                                    if (missionWeapon.HasAnyUsageWithWeaponClass(WeaponClass.Stone) &&
                                         currentUsageItem.ItemUsage.Contains("dwarf_hand_grenade")
                                       )
                                    {
                                        if (agent.Character.IsHero && agent.GetHero() == Hero.MainHero && Hero.MainHero.HasCareerChoice("NestCleansingPassive3"))
                                        {
                                            ammoCount.Add(2);
                                        }
                                        if (agent.Character.IsIronbreakerUnit() && !agent.Character.IsHero && Hero.MainHero.HasCareerChoice("NestCleansingPassive4") && MBRandom.RandomFloat < 0.5f)
                                        {
                                            ammoCount.Add(1);
                                        }


                                    }

                                    if (Hero.MainHero.HasCareerChoice("IronDrakesPassive4") && agent.GetOriginMobileParty() == MobileParty.MainParty && agent.Character.IsIronbreakerUnit())
                                    {
                                        foreach (var elem in MobileParty.MainParty.MemberRoster.GetTroopRoster())
                                        {
                                            if (elem.Character.StringId == "tor_dw_ironbeard")
                                            {
                                                ammoCount.AddFactor(0.1f);
                                            }
                                        }
                                    }

                                    if (agent.Character.IsHero && agent.GetHero() == Hero.MainHero)
                                    {

                                        if (missionWeapon.Item.IsFlameThrowerItem())
                                        {
                                            if (Hero.MainHero.HasCareerChoice("IronDrakesPassive3"))
                                            {
                                                ammoCount.Add(12);
                                            }
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

                            if (currentUsageItem.IsShield && agent.IsHero)
                            {
                                int hitPoints = missionWeapon.HitPoints;
                                if (agent == Agent.Main && Hero.MainHero.HasCareer(TORCareers.Ironbreaker) && Hero.MainHero.HasCareerChoice("ShieldwallPassive4"))
                                {
                                    var smithingSkill = Hero.MainHero.GetSkillValue(DefaultSkills.Crafting);
                                    hitPoints += (int)(smithingSkill * 0.5f);

                                }

                                var traits = agent.WieldedOffhandWeapon.Item.GetTraits().Where(x => x.StatsTuple.StatType == ItemTraitStatType.ShieldHealth).ToList();

                                if (traits.Count > 0)
                                {
                                    hitPoints += traits.Sum(trait => (int)trait.StatsTuple.Value);
                                }

                                equipment.SetHitPointsOfSlot(equipmentIndex, (short)hitPoints, true);
                            }
                        }
                    }

                    if (agent.BelongsToMainParty() && agent.Character.IsIronbreakerUnit() && !agent.Character.IsRanged)
                    {
                        if (!Hero.MainHero.HasCareer(TORCareers.Ironbreaker)) return;

                        if (Hero.MainHero.HasCareerChoice("NestCleansingPassive4") && MBRandom.RandomFloat < 0.5f)
                        {
                            MissionEquipment troopEquipment = agent.Equipment;
                            for (int i = 0; i < 5; i++)
                            {
                                EquipmentIndex equipmentIndex = (EquipmentIndex)i;
                                MissionWeapon missionWeapon = equipment[equipmentIndex];

                                if (missionWeapon.IsEmpty)
                                {
                                    var item = MBObjectManager.Instance.GetObject<ItemObject>("tor_dwarf_weapon_grenade_dwarf_hand_grenade");
                                    MissionWeapon weapon = new MissionWeapon(item, null, Hero.MainHero.ClanBanner);
                                    agent.EquipWeaponWithNewEntity((EquipmentIndex)i, ref weapon);
                                    break;
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

            if (agent.GetHero() is Hero hero)
            {
                var skillEffectFromEquipment = hero.GetAggregatedSkillEffectFromEquipment(skill.StringId);
                if (skillEffectFromEquipment != 0)
                {
                    resultNumber.Add(skillEffectFromEquipment, GameTexts.FindText("tor_generic_enchantedEquipment"));
                }
            }

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
                            if (!agent.IsMainAgent && !agent.Character.IsHero)
                            {
                                CareerHelper.ApplySkillBonusForTroops(ref resultNumber, skill, agent.Character);
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

                if (agent.RiderAgent != null && agent.RiderAgent.IsHero && agent.RiderAgent.GetHero() == Hero.MainHero)
                {
                    CareerHelper.ApplyBasicCareerPassives(agent.RiderAgent.GetHero(), ref explainedNumber, PassiveEffectType.HorseHealth, true);
                }
            }

            return explainedNumber.ResultNumber;
        }

        private void UpdateAgentDrivenProperties(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            if (!_checkedMissionType && Mission.Current != null)
            {
                _isDuelMission = Mission.Current.IsDuelMission();
                _isJoustMission = Mission.Current.IsJoustMission();
                _checkedMissionType = true;
            }

            if (_isJoustMission)
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

            if (_isDuelMission && agent.Character != null && agent.Character.StringId == "tor_ti_vittorio")
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

            if (agent.Character != null && !agent.WieldedWeapon.IsEmpty)
            {
                if (agent.WieldedWeapon.CurrentUsageItem.IsRangedWeapon)
                {
                    var weaponId = agent.WieldedWeapon.Item.StringId;
                
                    if (weaponId == "tor_dw_weapon_gun_drakegun" || weaponId == "tor_dw_weapon_gun_trollhammer")
                    {
                        agentDrivenProperties.ReloadSpeed *= 0.2f;
                    }
                }
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

                    if (character.IsTroll())
                    {
                        agent.SetAgentFlags(agent.GetAgentFlags() & ~AgentFlag.CanDefend);//Sly : I set to false directly in their monster entry which applies to both custom battles and sandbox. Wasn't that causing a crash due to missing human component in custom battles and it was supposed to have been put back to true in the xml and set to false here?
                        agent.Defensiveness = 0.001f;
                    }

                    if (character.IsDwarf())
                    {
                        agent.SetAgentFlags(agent.GetAgentFlags() & ~AgentFlag.CanRide);

                        if (character.IsIronbreakerUnit())
                        {
                            if (!character.StringId.Contains("trollhammer"))
                            {
                                agentDrivenProperties.WeaponInaccuracy += 0.095f;
                            }
                            else
                            {
                                agentDrivenProperties.WeaponInaccuracy += 0.02f;
                            }
                        }

                        agentDrivenProperties.MaxSpeedMultiplier *= DwarfSpeedModificatior;
                    }

                    if (character.IsOrc())
                    {
                        agentDrivenProperties.MaxSpeedMultiplier *= OrcSpeedModificatior;
                    }

                    if (character.IsGoblin())
                    {
                        agentDrivenProperties.MaxSpeedMultiplier *= GoblinSpeedModificatior;
                    }

                }
            }

            if (agent.IsHero)
            {
                foreach (var equipmentItem in agent.Character.GetCharacterEquipment(EquipmentIndex.ArmorItemBeginSlot, EquipmentIndex.ArmorItemEndSlot))
                {
                    foreach (var trait in equipmentItem.GetTraits())
                    {
                        if (trait.StatsTuple?.StatType == ItemTraitStatType.MovementSpeed)
                        {
                            agentDrivenProperties.MaxSpeedMultiplier *= 1f + trait.StatsTuple.Value / 100f;
                        }
                    }
                }

                if (!agent.WieldedWeapon.IsEmpty)
                {
                    if (agent.WieldedWeapon.Item.IsMeleeWeapon())
                    {
                        foreach (var trait in agent.WieldedWeapon.Item.GetTraits())
                        {
                            if (trait.StatsTuple?.StatType == ItemTraitStatType.SwingSpeed)
                            {
                                agentDrivenProperties.SwingSpeedMultiplier *= 1f + trait.StatsTuple.Value / 100f;
                            }
                        }
                    }

                    if (agent.WieldedWeapon.CurrentUsageItem.IsRangedWeapon)
                    {
                        foreach (var trait in agent.WieldedWeapon.Item.GetTraits())
                        {
                            if (trait.StatsTuple?.StatType == ItemTraitStatType.ReloadSpeed)
                            {
                                agentDrivenProperties.ReloadSpeed *= 1f + trait.StatsTuple.Value / 100f;
                            }

                            if (trait.StatsTuple?.StatType == ItemTraitStatType.MissileSpeed)
                            {
                                agentDrivenProperties.MissileSpeedMultiplier *= 1f + trait.StatsTuple.Value / 100f;
                            }
                        }

                        if (!agent.WieldedWeapon.AmmoWeapon.IsEmpty)
                        {
                            foreach (var trait in agent.WieldedWeapon.AmmoWeapon.Item.GetTraits())
                            {
                                if (trait.StatsTuple?.StatType == ItemTraitStatType.MissileSpeed)
                                {
                                    agentDrivenProperties.MissileSpeedMultiplier *= 1f + trait.StatsTuple.Value / 100f;
                                }
                            }
                        }
                        
                    }
                }
            }

            UpdateDynamicAgentDrivenProperties(agent, agentDrivenProperties);
            ApplyOrcMeleeHandlingBoost(agent, agentDrivenProperties);
        }
        private static void ApplyOrcMeleeHandlingBoost(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            MissionWeapon activeMeleeWeapon;
            if (!TryGetActiveOrcMeleeWeapon(agent, out activeMeleeWeapon))
            {
                return;
            }

            agentDrivenProperties.HandlingMultiplier *= OrcHandlingMultiplier;
        }

        private static bool TryGetActiveOrcMeleeWeapon(Agent agent, out MissionWeapon activeMeleeWeapon)
        {
            activeMeleeWeapon = default(MissionWeapon);

            if (agent == null || !agent.IsHuman)
            {
                return false;
            }

            CharacterObject character = agent.Character as CharacterObject;
            if (character == null || !character.IsOrc())
            {
                return false;
            }

            MissionWeapon primaryWieldedWeapon = agent.WieldedWeapon;
            if (!primaryWieldedWeapon.IsEmpty)
            {
                WeaponComponentData primaryUsageItem = primaryWieldedWeapon.CurrentUsageItem;
                if (primaryUsageItem != null && primaryUsageItem.IsMeleeWeapon && !primaryUsageItem.IsShield)
                {
                    activeMeleeWeapon = primaryWieldedWeapon;
                    return true;
                }
            }

            MissionWeapon offhandWieldedWeapon = agent.WieldedOffhandWeapon;
            if (offhandWieldedWeapon.IsEmpty)
            {
                return false;
            }

            WeaponComponentData offhandUsageItem = offhandWieldedWeapon.CurrentUsageItem;
            if (offhandUsageItem == null || !offhandUsageItem.IsMeleeWeapon || offhandUsageItem.IsShield)
            {
                return false;
            }

            activeMeleeWeapon = offhandWieldedWeapon;
            return true;
        }

        private void UpdateDynamicAgentDrivenProperties(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            var statusEffectComponent = agent.IsMount
                ? agent.RiderAgent?.GetComponent<StatusEffectComponent>()
                : agent.GetComponent<StatusEffectComponent>();

            if (statusEffectComponent == null)
                return;

            if (!statusEffectComponent.AreBaseValuesInitialized() || !statusEffectComponent.ModifiedDrivenProperties)
                return;

            bool isMount = agent.IsMount;

            float speedModifier = statusEffectComponent.GetMovementSpeedModifier();
            float speedMultiplier = speedModifier != 0f
                ? Mathf.Clamp(speedModifier + 1f, 0f, 2f) //to set in the right offset, where -100% would actually result in 0% movement speed
                : 1f;

            if (isMount)
            {
                agentDrivenProperties.SetDynamicMountMovementProperties(statusEffectComponent, speedMultiplier);
            }
            else
            {
                agentDrivenProperties.SetDynamicHumanoidMovementProperties(statusEffectComponent, speedMultiplier);
            }

            float weaponSwingSpeedModifier = statusEffectComponent.GetAttackSpeedModifier();
            if (weaponSwingSpeedModifier != 0f)
            {
                if (isMount)
                    return;

                float swingSpeedMultiplier = Mathf.Clamp(weaponSwingSpeedModifier + 1f, 0.05f, 2f);
                agentDrivenProperties.SetDynamicCombatProperties(statusEffectComponent, swingSpeedMultiplier);
            }
            else
            {
                agentDrivenProperties.SetDynamicCombatProperties(statusEffectComponent, 1f); //I have the feeling this call is not necessary given the many updates that are done per frame.
            }

            float reloadSpeedModifier = statusEffectComponent.GetReloadSpeedModifier();
            if (reloadSpeedModifier != 0f)
            {
                if (isMount)
                    return;

                float reloadSpeedMultiplier = Mathf.Clamp(reloadSpeedModifier + 1f, 0.05f, 2f);
                agentDrivenProperties.SetDynamicReloadProperties(statusEffectComponent, reloadSpeedMultiplier);
            }
            else
            {
                agentDrivenProperties.SetDynamicReloadProperties(statusEffectComponent, 1f);
            }
        }

        private void AddSkillEffectsForAgent(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            EquipmentIndex wieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
            WeaponComponentData weapon = wieldedItemIndex != EquipmentIndex.None ? agent.Equipment[wieldedItemIndex].CurrentUsageItem : null;
            CharacterObject character = agent.Character as CharacterObject;

            if (weapon == null || character == null || weapon.RelevantSkill != TORSkills.GunPowder)
                return;

            int effectiveSkill = GetEffectiveSkill(agent, weapon.RelevantSkill);
            ExplainedNumber reloadSpeed = new ExplainedNumber(agentDrivenProperties.ReloadSpeed);
            SkillHelper.AddSkillBonusForCharacter(TORSkillEffects.GunReloadSpeed, character, ref reloadSpeed);
            agentDrivenProperties.ReloadSpeed = reloadSpeed.ResultNumber;
        }

        private void AddPerkEffectsForAgent(Agent agent, AgentDrivenProperties agentDrivenProperties)
        {
            EquipmentIndex wieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
            WeaponComponentData weapon = wieldedItemIndex != EquipmentIndex.None ? agent.Equipment[wieldedItemIndex].CurrentUsageItem : null;
            CharacterObject character = agent.Character as CharacterObject;

            bool applyRunAndGun = weapon != null
                                  && character != null
                                  && weapon.WeaponClass == WeaponClass.Pistol
                                  && !agent.HasMount;

            bool applyMainAgentCareerPassives = agent.IsMainAgent && agent.GetHero().HasAnyCareer();
            bool applyWardancerSymbol = agent.IsMainAgent && Hero.MainHero.HasAttribute("WEWardancerSymbol");

            if (!applyRunAndGun && !applyMainAgentCareerPassives && !applyWardancerSymbol)
                return;

            if (applyRunAndGun || applyMainAgentCareerPassives)
            {
                ExplainedNumber movementAccuracyPenalty = new ExplainedNumber(agentDrivenProperties.WeaponMaxMovementAccuracyPenalty);

                if (applyRunAndGun)
                {
                    PerkHelper.AddPerkBonusForCharacter(TORPerks.GunPowder.RunAndGun, character, true, ref movementAccuracyPenalty);
                }

                if (applyMainAgentCareerPassives)
                {
                    CareerHelper.ApplyBasicCareerPassives(agent.GetHero(), ref movementAccuracyPenalty, PassiveEffectType.RangedMovementPenalty);
                }

                agentDrivenProperties.WeaponMaxMovementAccuracyPenalty = movementAccuracyPenalty.ResultNumber;
            }

            if (applyMainAgentCareerPassives)
            {
                ExplainedNumber accuracyPenalty = new ExplainedNumber(agentDrivenProperties.WeaponInaccuracy);
                CareerHelper.ApplyBasicCareerPassives(agent.GetHero(), ref accuracyPenalty, PassiveEffectType.AccuracyPenalty);
                agentDrivenProperties.WeaponInaccuracy = accuracyPenalty.ResultNumber;
            }

            if (applyMainAgentCareerPassives || applyWardancerSymbol)
            {
                ExplainedNumber swingSpeed = new ExplainedNumber(agentDrivenProperties.SwingSpeedMultiplier);

                if (applyMainAgentCareerPassives)
                {
                    CareerHelper.ApplyBasicCareerPassives(agent.GetHero(), ref swingSpeed, PassiveEffectType.SwingSpeed);
                }

                if (applyWardancerSymbol)
                {
                    swingSpeed.AddFactor(0.10f);
                }

                agentDrivenProperties.SwingSpeedMultiplier = swingSpeed.ResultNumber;
            }

            if (applyMainAgentCareerPassives)
            {
                ExplainedNumber movementSpeed = new ExplainedNumber(agentDrivenProperties.MaxSpeedMultiplier);
                CareerHelper.ApplyBasicCareerPassives(agent.GetHero(), ref movementSpeed, PassiveEffectType.MovementSpeed);
                agentDrivenProperties.MaxSpeedMultiplier = movementSpeed.ResultNumber;
            }
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
        
        public override float GetSneakAttackMultiplier(Agent agent, WeaponComponentData weapon)
        {
            var number = base.GetSneakAttackMultiplier(agent, weapon);

            // Witch Hunter Silver Hammer perk: adds Faith skill to sneak attack multiplier
            // TODO: Ideally this should only apply against Undead/Chaos, but GetSneakAttackMultiplier
            // doesn't receive victim info - would be updated if this is the case. for example against chaos and undead TWTODO
            if (agent == Agent.Main && Hero.MainHero.HasCareerChoice("SilverHammerPassive3"))
            {
                var choice = TORCareerChoices.GetChoice("SilverHammerPassive3");
                if (choice != null)
                {
                    var faithSkill = Hero.MainHero.GetSkillValue(TORSkills.Faith);
                    number += faithSkill * choice.GetPassiveValue();
                }
            }
            
            return number;
        }


        public override float GetEquipmentStealthBonus(Agent agent)
        {
            var bonus = new ExplainedNumber(base.GetEquipmentStealthBonus(agent));

            if (agent == Agent.Main && Hero.MainHero.HasAnyCareer())
            {
                CareerHelper.ApplyBasicCareerPassives(Hero.MainHero, ref bonus, PassiveEffectType.StealthBonus);
            }

            return bonus.ResultNumber;
        }

        //The moment you realize they forget to add an override statement. if they do it needs to be moved on the EffectiveArmorEncumbrance
        public float GetTOREffectiveEquipmentEncumbrance(Agent agent, float value)
        {
            if (agent == null) return 0;
            if (agent.IsMount) return 0;
            var number = new ExplainedNumber(value);
            if (agent.GetHero() == Hero.MainHero)
            {
                CareerHelper.ApplyBasicCareerPassives(agent.GetHero(), ref number, PassiveEffectType.EquipmentWeightReduction);
            }


            return number.ResultNumber;
        }
    }
}