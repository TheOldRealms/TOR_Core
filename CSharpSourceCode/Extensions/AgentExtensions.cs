using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.CustomBattle;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items;
using TOR_Core.Models;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions
{
    public static class AgentExtensions
    {
        public static CharacterObject GetCaptainCharacter(this Agent agent)
        {
            if(agent.Formation != null && agent.Formation.Captain != null && agent.Formation.Captain.Character != null)
            {
                var character = agent.Formation.Captain.Character as CharacterObject;
                if (character != null) return character;
            }
            return null;
        }

        public static CharacterObject GetPartyLeaderCharacter(this Agent agent)
        {
            if(agent.Origin?.BattleCombatant is PartyBase)
            {
                var party = (PartyBase)agent.Origin.BattleCombatant;
                return party.LeaderHero?.CharacterObject;
            }
            return null;
        }

        public static CharacterObject GetArmyCommanderCharacter(this Agent agent)
        {
            if (agent.Origin?.BattleCombatant is PartyBase)
            {
                var party = (PartyBase)agent.Origin.BattleCombatant;
                var character = party.General as CharacterObject;
                return character;
            }
            return null;
        }

        public static MobileParty GetOriginMobileParty(this Agent agent)
        {
            var party = agent.GetOriginPartyBase();
            if(party == null) return null;
            return party.MobileParty;
        }

        public static PartyBase GetOriginPartyBase(this Agent agent)
        {
            if (agent.Origin?.BattleCombatant is PartyBase) return agent.Origin.BattleCombatant as PartyBase;
            return null;
        }

        public static bool IsExpendable(this Agent agent)
        {
            return agent.GetAttributes().Contains("Expendable");
        }

        public static bool IsUnbreakable(this Agent agent)
        {
            return agent.GetAttributes().Contains("Unbreakable");
        }

        public static bool IsUndead(this Agent agent)
        {
            return agent.GetAttributes().Contains("Undead");
        }

        public static bool IsDamageShruggedOff(this Agent agent, int inflictedDamge=0)
        {
            if (inflictedDamge > 15) return false;
            
            if (agent.IsMainAgent && agent.GetHero().HasAnyCareer())
            {
                if (agent.GetHero().GetAllCareerChoices().Contains("ProtectorOfTheWeakPassive4"))
                    return true;
                if (agent.GetHero().GetAllCareerChoices().Contains("BladeMasterPassive3"))
                    return true;
                if (agent.GetHero().GetAllCareerChoices().Contains("CommanderPassive3"))
                    return true;
                if (agent.GetHero().GetAllCareerChoices().Contains("QuestingVowPassive4"))
                    return true;
            }

            return false;
        }

       

        public static bool ShouldNotBleed(this Agent agent)
        {
            return agent.GetAttributes().Contains("ClearBloodBurst");
        }


        public static bool IsVampire(this Agent agent)
        {
            return agent.Character != null ? agent.Character.IsVampire() : false;
        }

        public static bool IsAbilityUser(this Agent agent)
        {
            return agent.GetAttributes().Contains("AbilityUser");
        }

        public static bool IsSpellCaster(this Agent agent)
        {
            return agent.GetAttributes().Contains("SpellCaster");
        }

        public static bool CanPlaceArtillery(this Agent agent)
        {
            return agent.GetAttributes().Contains("CanPlaceArtillery");
        }

        public static bool HasAttribute(this Agent agent, string attributeName)
        {
            return agent.GetAttributes().Contains(attributeName);
        }

        public static void CastCurrentAbility(this Agent agent)
        {
            var abilitycomponent = agent.GetComponent<AbilityComponent>();

            if (abilitycomponent != null)
            {
                if (abilitycomponent.CurrentAbility != null) abilitycomponent.CurrentAbility.TryCast(agent);
            }
        }

        /// <summary>
        /// Get Extended Character properties based on dynamic properties like temporary weapon enhancements
        /// and status effects and static properties  like equipment and unit information.
        /// 
        /// </summary>
        /// <param name="agent">The agent are accessing. Note that Units are handled differently from Heroes.
        /// Hero equipment information that gets read out for Heroes specifically resides in the 'tor_extendeditemproperties.xml'
        /// Unit information gets read out from the tor_extendedunitproperties.xml
        /// </param>
        /// <paramref name="agent"/>
        /// <param name="propertyMask">Which properties needs to be accessed? be mindful about which information is really required for your specific situation. Not used properties are returned as empty arrays</param>
        ///<returns>A struct containing 3 arrays, each containing the  properties:  proportions, damage amplifications and resistances.</returns>
        public static AgentPropertyContainer GetProperties(this Agent agent, PropertyMask propertyMask, AttackTypeMask attackTypeMask)
        {
            if (agent.IsMount)
                return new AgentPropertyContainer();

            float[] damageProportions = new float[(int)DamageType.All + 1];
            float[] damageAmplifications = new float[(int)DamageType.All + 1];
            float[] damageResistances = new float[(int)DamageType.All + 1];
            float[] additionalDamagePercentages = new float[(int)DamageType.All + 1];

            #region Unit
            if (!agent.IsHero)
            {
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
            #endregion

            #region Character

            else
            {
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
            #endregion

            var result = new AgentPropertyContainer(damageProportions, damageAmplifications, damageResistances, additionalDamagePercentages);

            if (Game.Current.GameType is Campaign)
            {
                var model = MissionGameModels.Current.AgentStatCalculateModel as TORAgentStatCalculateModel;
                if(model != null)
                {
                    result = model.AddPerkEffectsToAgentPropertyContainer(agent, propertyMask, attackTypeMask, result);
                }
            }

            return result;
        }

        public static Ability GetCurrentAbility(this Agent agent)
        {
            var abilitycomponent = agent.GetComponent<AbilityComponent>();
            if (abilitycomponent != null)
            {
                return abilitycomponent.CurrentAbility;
            }
            else return null;
        }

        public static void SelectNextAbility(this Agent agent)
        {
            var abilitycomponent = agent.GetComponent<AbilityComponent>();
            if (abilitycomponent != null)
            {
                abilitycomponent.SelectNextAbility();
            }
        }

        public static void SelectPreviousAbility(this Agent agent)
        {
            var abilitycomponent = agent.GetComponent<AbilityComponent>();
            if (abilitycomponent != null)
            {
                abilitycomponent.SelectPreviousAbility();
            }
        }

        public static void SelectAbility(this Agent agent, int abilityindex)
        {
            var abilitycomponent = agent.GetComponent<AbilityComponent>();
            if (abilitycomponent != null)
            {
                abilitycomponent.SelectAbility(abilityindex);
            }
        }

        public static void SetSpellCasterMode(this Agent agent)
        {
            if(agent!=Agent.Main) return;
            if (Mission.Current == null) return;
            
            Mission.Current.GetMissionBehavior<AbilityManagerMissionLogic>().ActivateSpellcasterMode();
        }
        
        public static void UnsetSpellCasterMode(this Agent agent)
        {
            if(agent!=Agent.Main) return;
            if (Mission.Current == null) return;
            
            Mission.Current.GetMissionBehavior<AbilityManagerMissionLogic>().DeactivateSpellcasterMode();
        }
        

        public static Hero GetHero(this Agent agent)
        {
            if (agent == null) return null;
            
            if (agent.Character == null) return null;
            Hero hero = null;
            if (Game.Current.GameType is Campaign)
            {
                var character = agent.Character as CharacterObject;
                if (character != null && character.IsHero) hero = character.HeroObject;
            }
            return hero;
        }

        public static int GetPlaceableArtilleryCount(this Agent agent)
        {
            int count = 0;
            if (agent.CanPlaceArtillery())
            {
                if (Game.Current.GameType is Campaign && agent.GetHero() != null)
                {
                    count = agent.GetHero().GetPlaceableArtilleryCount();
                }
                else if (Game.Current.GameType is CustomGame)
                {
                    count = 5;
                }
            }
            return count;
        }

        public static List<string> GetAbilities(this Agent agent)
        {
            var hero = agent.GetHero();
            var character = agent.Character;
            if (hero != null)
            {
                return hero.GetExtendedInfo().AllAbilites;
            }
            else if (character != null)
            {
                return agent.Character.GetAbilities();
            }
            else return new List<string>();
        }

        public static CareerAbility GetCareerAbility(this Agent agent)
        {
            if (agent.IsMainAgent && agent.GetHero().HasAnyCareer())
            {
                return agent.GetComponent<AbilityComponent>().CareerAbility;
            }

            return null;
        }
        

        public static List<string> GetSelectedAbilities(this Agent agent)
        {
            var hero = agent.GetHero();
            var character = agent.Character;
            var abilities = new List<string>();
            if (hero != null)
            {
                abilities.AddRange(hero.GetExtendedInfo().SelectedAbilities);
                
                if (agent.HasAttribute("Priest"))
                {
                    abilities.AddRange(hero.GetExtendedInfo().GetAllPrayers());
                }
            }
            else if (character != null)
            {
                abilities.AddRange(agent.Character.GetAbilities());
            }
            
            return abilities;
        }

        public static Ability GetAbility(this Agent agent, int abilityindex)
        {
            var abilitycomponent = agent.GetComponent<AbilityComponent>();
            if (abilitycomponent != null)
            {
                return abilitycomponent.GetAbility(abilityindex);
            }

            return null;
        }

        

        public static List<string> GetAttributes(this Agent agent)
        {
            List<string> result = new List<string>();
            var hero = agent.GetHero();
            var character = agent.Character;
            if (hero != null)
            {
                var info = hero.GetExtendedInfo();
                if(info != null && info.AllAttributes.Count > 0)
                {
                    foreach(var attribute in info.AllAttributes)
                    {
                        if (!result.Contains(attribute)) result.Add(attribute);
                    }
                }
            }
            else if (character != null)
            {
                foreach(var attribute in character.GetAttributes())
                {
                    if (!result.Contains(attribute)) result.Add(attribute);
                }
            }
            var comp = agent.GetComponent<StatusEffectComponent>();
            if (comp != null)
            {
                foreach (var attribute in comp.GetTemporaryAttributes())
                {
                    if (!result.Contains(attribute)) result.Add(attribute);
                }
            }
            return result;
        }

        /// <summary>
        /// Apply damage to an agent. 
        /// </summary>
        /// <param name="agent">The agent that will be damaged</param>
        /// <param name="damageAmount">How much damage the agent will receive.</param>
        /// <param name="damager">The agent who is applying the damage</param>
        /// <param name="doBlow">A mask that controls whether the unit receives a blow or direct health manipulation</param>
        public static void ApplyDamage(this Agent agent, int damageAmount, Vec3 impactPosition, Agent damager = null, bool doBlow = true, bool hasShockWave = false)
        {
            if (agent == null || !agent.IsHuman || !agent.IsActive() || agent.Health < 1)
            {
                TORCommon.Log("ApplyDamage: attempted to apply damage to a null, dead or non-human agent.", LogLevel.Warn);
                return;
            }
            try
            {
                // Registering a blow causes the agent to react/stagger. Manipulate health directly if the damage won't kill the agent.
                if (agent.State == AgentState.Active || agent.State == AgentState.Routed)
                {
                    if (!doBlow && agent.Health > damageAmount)
                    {
                        agent.Health -= damageAmount;
                        return;
                    }

                    if (agent.IsFadingOut())
                        return;

                    var damagerAgent = damager != null ? damager : agent;

                    var blow = new Blow(damagerAgent.Index);
                    blow.DamageType = DamageTypes.Blunt;
                    blow.BoneIndex = agent.Monster.HeadLookDirectionBoneIndex;
                    blow.GlobalPosition = agent.GetChestGlobalPosition();
                    blow.BaseMagnitude = damageAmount;
                    blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
                    blow.InflictedDamage = damageAmount;
                    var direction = agent.Position == impactPosition ? agent.LookDirection : agent.Position - impactPosition;
                    direction.Normalize();
                    blow.Direction = direction;
                    blow.SwingDirection = direction;
                    blow.DamageCalculated = true;
                    blow.AttackType = AgentAttackType.Kick;
                    blow.BlowFlag = BlowFlags.NoSound;
                    blow.VictimBodyPart = BoneBodyPartType.Chest;
                    blow.StrikeType = StrikeType.Thrust;
                    if (hasShockWave)
                    {
                        if (agent.HasMount) blow.BlowFlag |= BlowFlags.CanDismount;
                        else blow.BlowFlag |= BlowFlags.KnockDown;
                    }

                    if (agent.Health <= damageAmount && !doBlow)
                    {
                        agent.Die(blow);
                        return;
                    }
                    sbyte mainHandItemBoneIndex = damagerAgent.Monster.MainHandItemBoneIndex;
                    AttackCollisionData attackCollisionData = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(
                        false, 
                        false, 
                        false, 
                        true, 
                        false, 
                        false, 
                        false, 
                        false, 
                        false, 
                        false, 
                        false, 
                        false, 
                        CombatCollisionResult.StrikeAgent, 
                        -1,
                        1,
                        2,
                        blow.BoneIndex, 
                        blow.VictimBodyPart, 
                        mainHandItemBoneIndex, 
                        Agent.UsageDirection.AttackUp, 
                        -1, 
                        CombatHitResultFlags.NormalHit, 
                        0.5f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, 
                        Vec3.Up, 
                        blow.Direction, 
                        blow.GlobalPosition, 
                        Vec3.Zero, 
                        Vec3.Zero, 
                        agent.Velocity, 
                        Vec3.Up);
                    agent.RegisterBlow(blow, attackCollisionData);
                }
            }
            catch (Exception e)
            {
                TORCommon.Log("ApplyDamage: attempted to damage agent, but: " + e.Message, LogLevel.Error);
            }
        }

        /// <summary>
        /// Apply healing to an agent.
        /// </summary>
        /// <param name="agent">The agent that will be healed</param>
        /// <param name="healingAmount">How much healing the agent will receive</param>
        public static void Heal(this Agent agent, float healingAmount)
        {
            //Cap healing at the agent's max hit points
            agent.Health = Math.Min(agent.Health + healingAmount, agent.HealthLimit);
        }
        


        public static void ApplyStatusEffect(this Agent agent, string effectId, Agent applierAgent, float duration = 5, bool append = true, bool isMutated = false)
        {
            TORCommon.Say(effectId+" "+duration);
            var comp = agent.GetComponent<StatusEffectComponent>();
            if (comp != null) comp.RunStatusEffect(effectId, applierAgent, duration, append, isMutated);
        }

        public static void FallDown(this Agent agent)
        {
            agent.SetActionChannel(0, ActionIndexCache.Create("act_strike_fall_back_heavy_back_rise_continue"));
        }

        public static void Appear(this Agent agent)
        {
            agent.AgentVisuals?.SetVisible(true);
        }
        
        

        public static void Disappear(this Agent agent)
        {
            agent.AgentVisuals?.SetVisible(false);
        }
    }
}
