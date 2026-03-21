using Helpers;
using SandBox.GameComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CharacterDevelopment.CareerSystem.Choices;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORAgentApplyDamageModel : SandboxAgentApplyDamageModel
    {
        // Crush through block (ctb)
        // extra crush through chance for high energy swings when attacker outskills the defender
        private const int EXTRA_CTB_SKILL_DEAD_ZONE = 30; //skill diff over 30 doesn't do ctb

        private const int EXTRA_CTB_TARGET_SKILL_DIFF = 200; //max skill diff contribution
        private const float EXTRA_CTB_TARGET_OVERHEAD_CHANCE_AT_TARGET_DIFF = 0.50f; // ^^ it's effect

        private const float EXTRA_CTB_NON_OVERHEAD_CHANCE_MULTIPLIER = 0.5f; // non overhead swings are twice as less likely
        private const float EXTRA_CTB_SKILL_GROWTH_RATE = 0.008097f; // 30 energy, 50 diff 50% ctb, 200 diff 50% ctb

        // how quickly extra ctb reaches full effect after passing the base energy threshold
        // reduce this number if you want momentum to matter less on ctb
        private const float EXTRA_CTB_ENERGY_MARGIN_FOR_FULL_EFFECT = 0.27f;

        private static readonly float ExtraCtbSkillGrowthRate = EXTRA_CTB_SKILL_GROWTH_RATE;
        public override void DecideMissileWeaponFlags(Agent attackerAgent, in MissionWeapon missileWeapon, ref WeaponFlags missileWeaponFlags)
        {
            base.DecideMissileWeaponFlags(attackerAgent, missileWeapon, ref missileWeaponFlags);
            if (attackerAgent.Character is CharacterObject character && !missileWeapon.IsEmpty)
            {
                //GetPartyLeaderCharacter can return a null and therefore a NRE on .GetPerkValue; I assume this occurs in the case of a gunpowder troop that's in a garrison
                CharacterObject partyLeader = attackerAgent.GetPartyLeaderCharacter();
                //Piercing Shot states only troops, the conditional is less restrictive and allows other heroes in the party to pierce as long as the leader has the perk
                //because this only checks for cartridges, a flamethrower or a launched grenade would also gain the flag; unsure if that even matters for those weapons
                if (missileWeapon.CurrentUsageItem.WeaponClass == WeaponClass.Cartridge && character.GetPerkValue(TORPerks.GunPowder.PiercingShots) || (partyLeader != null && partyLeader.GetPerkValue(TORPerks.GunPowder.PiercingShots))) missileWeaponFlags |= WeaponFlags.CanPenetrateShield;

                if (attackerAgent.IsMainAgent && Hero.MainHero.HasAnyCareer())
                {
                    var choices = Hero.MainHero.GetAllCareerChoices();

                    // StarfireEssencePassive4: Arrows can penetrate shields
                    if (choices.Contains("StarfireEssencePassive4"))
                    {
                        missileWeaponFlags |= WeaponFlags.CanPenetrateShield;
                    }

                    // WardenOfTalsynPassive4: Javelins can penetrate multiple targets
                    if (missileWeapon.CurrentUsageItem.WeaponClass == WeaponClass.Javelin && choices.Contains("WardenOfTalsynPassive4"))
                    {
                        missileWeaponFlags |= WeaponFlags.MultiplePenetration;
                    }
                }

                if (attackerAgent.HasAttribute("ShieldPenetration"))
                {
                    missileWeaponFlags |= WeaponFlags.CanPenetrateShield;
                }

                // Check for ShieldPenetration trait - use missile weapon for thrown weapons, wielded weapon for bows/guns
                ItemObject traitSourceItem = null;
                switch (missileWeapon.CurrentUsageItem.WeaponClass)
                {
                    case WeaponClass.Javelin:
                    case WeaponClass.ThrowingAxe:
                    case WeaponClass.ThrowingKnife:
                    case WeaponClass.Stone:
                        // Thrown weapons: traits are on the thrown item itself
                        traitSourceItem = missileWeapon.Item;
                        break;
                    default:
                        // Ranged weapons (bows, crossbows, guns): traits are on the wielded weapon
                        if (!attackerAgent.WieldedWeapon.IsEmpty)
                            traitSourceItem = attackerAgent.WieldedWeapon.Item;
                        break;
                }

                if (traitSourceItem != null)
                {
                    var traits = traitSourceItem.GetTraits(attackerAgent);
                    if (traits.Any(t => t.StatsTuple?.StatType == ItemTraitStatType.ShieldPenetration))
                    {
                        missileWeaponFlags |= WeaponFlags.CanPenetrateShield;
                    }
                }
            }
        }


        public override bool CanWeaponDealSneakAttack(in AttackInformation attackInformation, WeaponComponentData weapon)
        {
            var value = base.CanWeaponDealSneakAttack(in attackInformation, weapon);

            // ForestStalkerPassive1: Bows and throwing weapons can perform stealth attacks
            var attacker = attackInformation.AttackerAgent;

            // Check vanilla sneak attack conditions (excluding weapon type check)
            // Victim must be human, not the player, and either:
            // 1. Not alarmed and can get alarmed, OR
            // 2. Not fully alarmed and attacker is behind them
            if (!attackInformation.IsVictimAgentHuman || attackInformation.IsVictimPlayer)
                return value;

            bool isVictimUnaware =
                ((attackInformation.VictimAgentAIStateFlags & Agent.AIStateFlag.Alarmed) == Agent.AIStateFlag.None
                 && attackInformation.VictimAgentFlags.HasAnyFlag(AgentFlag.CanGetAlarmed))
                ||
                (!attackInformation.VictimAgentAIStateFlags.HasAllFlags(Agent.AIStateFlag.Alarmed)
                 && !attackInformation.IsAttackerAgentNull
                 && Vec2.DotProduct((attackInformation.AttackerAgentPosition - attackInformation.VictimAgentPosition).AsVec2.Normalized(),
                                    attackInformation.VictimAgentMovementDirection) < 0.174f);

            if (!isVictimUnaware)
                return value;

            if (attacker == Agent.Main && Hero.MainHero.HasCareerChoice("ForestStalkerPassive1") && weapon != null)
            {
                if (weapon.WeaponClass == WeaponClass.Arrow ||
                    weapon.WeaponClass == WeaponClass.Javelin || weapon.WeaponClass == WeaponClass.ThrowingAxe ||
                    weapon.WeaponClass == WeaponClass.ThrowingKnife)
                {
                    value = true;
                }
            }

            return value;
        }

        public override MeleeCollisionReaction DecidePassiveAttackCollisionReaction(
            Agent attacker,
            Agent defender,
            bool isFatalHit)
        {
            var collisionReaction = base.DecidePassiveAttackCollisionReaction(attacker, defender, isFatalHit);

            if (isFatalHit && attacker.IsMainAgent)
            {
                if (Hero.MainHero.HasCareer(TORCareers.KnightOldWorld) && attacker.HasAttribute("KnightlyStrike") && Hero.MainHero.HasCareerChoice("PathOfViliganceKeystone"))
                {
                    attacker.ApplyStatusEffect("knightly_strike", attacker, 30, false, false, true);
                }
            }

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

        public override float CalculateShieldDamage(
            in AttackInformation attackInformation,
            float baseDamage)
        {
            var result = new ExplainedNumber(base.CalculateShieldDamage(attackInformation, baseDamage));

            var attacker = attackInformation.AttackerAgent;
            if (attacker == null) return result.ResultNumber;

            if (attacker.IsHero)
            {
                if (attacker.GetHero() == Hero.MainHero)
                {
                    CareerHelper.ApplyBasicCareerPassives(attacker.GetHero(), ref result, PassiveEffectType.BonusDamageShield, true);
                }


                var traits = attacker.WieldedWeapon.Item.GetTraits();
                if (traits.Count > 0)
                {
                    foreach (var trait in traits.Where(trait => trait.StatsTuple?.StatType == ItemTraitStatType.ShieldDamage))
                    {
                        result.AddFactor(trait.StatsTuple.Value / 100);
                    }
                }
            }
            return result.ResultNumber;
        }

        /// <summary>
        /// Calculates damage for custom missiles (e.g., scatter shot, career ability projectiles).
        /// Called from Harmony prefix patches on AddMissileAux/AddMissileSingleUsageAux.
        /// </summary>
        public static float CalculateCustomMissileDamage(Agent shooterAgent)
        {
            if (shooterAgent == null || shooterAgent.WieldedWeapon.IsEmpty)
                return 0f;
            

            var weapon = shooterAgent.WieldedWeapon;
            if (weapon.CurrentUsageItem == null || weapon.Item == null)
                return 0f;

            // Get base weapon damage
            float baseDamage = weapon.GetModifiedThrustDamageForCurrentUsage();

            if (weapon.Item.StringId.Contains("drake"))
            {
                baseDamage *= 0.1f;
                return baseDamage;
            }
            
            // Check for damage modifiers from traits
            var traits = weapon.Item.GetTraits(shooterAgent);

            // Scatter shot: reduce damage per projectile
            var scatterTrait = traits.FirstOrDefault(t => t.StatsTuple?.StatType == ItemTraitStatType.ScatterShot);
            if (scatterTrait != null)
            {
                baseDamage *= 0.4f; // Each scatter projectile deals 40% damage
            }

            return baseDamage;
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

        /// <summary>
        /// Applies TOR's damage type system (proportions, amplifications, resistances, ward save).
        /// Called last in the CalculateDamage chain, after all vanilla modifiers.
        /// </summary>
        public override float ApplyGeneralDamageModifiers(
            in AttackInformation attackInformation,
            in AttackCollisionData collisionData,
            float baseDamage)
        {
            // First apply vanilla general modifiers
            float vanillaDamage = base.ApplyGeneralDamageModifiers(attackInformation, collisionData, baseDamage);

            // Get agents
            var attackerAgent = attackInformation.IsAttackerAgentMount ? attackInformation.AttackerAgent?.RiderAgent : attackInformation.AttackerAgent;
            var victimAgent = attackInformation.IsVictimAgentMount ? attackInformation.VictimAgent?.RiderAgent : attackInformation.VictimAgent;

            if (attackerAgent == null || victimAgent == null)
                return vanillaDamage;

            if (!attackerAgent.IsHuman || !victimAgent.IsHuman)
                return vanillaDamage;

            if (attackerAgent == victimAgent)
                return vanillaDamage;

            // Skip arena missions
            if (Mission.Current.IsArenaMission())
                return vanillaDamage;

            // Determine attack type mask
            AttackTypeMask attackTypeMask = collisionData.IsMissile ? AttackTypeMask.Ranged : AttackTypeMask.Melee;

            // Get property containers
            var attackerPropertyContainer = CreateAgentPropertyContainer(attackerAgent, PropertyMask.Attack, attackTypeMask);
            var victimPropertyContainer = CreateAgentPropertyContainer(victimAgent, PropertyMask.Defense, attackTypeMask);

            var damageProportions = attackerPropertyContainer.DamageProportions;
            var damageAmplifications = attackerPropertyContainer.DamagePercentages;
            var additionalDamagePercentages = attackerPropertyContainer.AdditionalDamagePercentages;
            var resistancePercentages = victimPropertyContainer.ResistancePercentages;

            bool friendlyFire = attackerAgent.Team == victimAgent.Team;

            // Apply career passives for damage values
            TORDamageHelper.ApplyCareerPassives(attackerAgent, victimAgent, attackTypeMask, additionalDamagePercentages, resistancePercentages);

            // Career-specific bonuses for melee/ranged
            if (Game.Current.GameType is Campaign && attackerAgent.IsMainAgent)
            {
                if (attackTypeMask == AttackTypeMask.Ranged && Hero.MainHero.HasCareer(TORCareers.Waywatcher))
                {
                    if (Hero.MainHero.HasCareerChoice("HailOfArrowsPassive4"))
                    {
                        CareerPerkMissionBehavior careerPerkBehavior = Mission.Current.GetMissionBehavior<CareerPerkMissionBehavior>();
                        if (careerPerkBehavior != null)
                        {
                            damageProportions[(int)DamageType.Magical] += 0.01f * careerPerkBehavior.CareerMissionVariables[0];
                        }
                    }
                }

                if (attackTypeMask == AttackTypeMask.Melee && Hero.MainHero.HasCareer(TORCareers.Slayer))
                {
                    CareerPerkMissionBehavior careerPerkBehavior = Mission.Current.GetMissionBehavior<CareerPerkMissionBehavior>();
                    if (careerPerkBehavior != null)
                    {
                        damageProportions[(int)DamageType.Physical] += 0.01f * careerPerkBehavior.CareerMissionVariables[0];
                        if (Hero.MainHero.HasCareerChoice("BaneOfChaosKeystone"))
                        {
                            resistancePercentages[(int)DamageType.Physical] += 0.001f * careerPerkBehavior.CareerMissionVariables[0];
                        }
                    }
                }
            }

            // Calculate damage with TOR's damage type system
            float resultDamage = TORDamageHelper.CalculateDamageWithProportions(
                vanillaDamage, damageProportions, damageAmplifications,
                additionalDamagePercentages, resistancePercentages, out float[] damageCategories);

            // Apply ward save
            float wardSaveFactor = CalculateWardSaveFactor(attackerAgent, victimAgent, resistancePercentages, friendlyFire);
            resultDamage *= wardSaveFactor;

            return resultDamage;
        }

        /// <summary>
        /// Determines if the victim should shrug off the blow (no stagger/reaction).
        /// Moved from DamagePatch to use the proper model override.
        /// </summary>
        public override bool DecideAgentShrugOffBlow(Agent victimAgent, in AttackCollisionData collisionData, in Blow blow)
        {
            // First check base game logic
            var baseShrugOff = base.DecideAgentShrugOffBlow(victimAgent, collisionData, blow);
            if (baseShrugOff)
                return true;
            
            if (victimAgent.HasAttribute("Unstoppable"))
                return true;

            // Agent's own damage threshold check
            if (victimAgent.IsDamageShruggedOff(blow.InflictedDamage))
                return true;
            
            if (collisionData.IsMissile && victimAgent.IsMainAgent && Hero.MainHero.HasCareer(TORCareers.Waywatcher))
            {
                if (Hero.MainHero.HasCareerChoice("PathfinderPassive4"))
                {
                    return true;
                }
            }

            return false;
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
            var result = new AgentPropertyContainer(damageProportions, damageAmplifications, damageResistances, additionalDamagePercentages);

            return result;
        }

        public void AssignCharacterProperties(
            Agent agent,
            PropertyMask propertyMask,
            AttackTypeMask attackTypeMask,
            out float[] damageProportions,
            out float[] damageAmplifications,
            out float[] damageResistances,
            out float[] additionalDamagePercentages)
        {
            damageProportions = new float[(int)DamageType.All + 1];
            damageAmplifications = new float[(int)DamageType.All + 1];
            damageResistances = new float[(int)DamageType.All + 1];
            additionalDamagePercentages = new float[(int)DamageType.All + 1];
            var statusEffectComp = agent.GetComponent<StatusEffectComponent>();

            if (propertyMask == PropertyMask.Attack || propertyMask == PropertyMask.All)
            {
                //Hero item level attributes 
                List<ItemTrait> itemTraits = new List<ItemTrait>();
                List<ItemObject> armorItems;
                // get all equipment Pieces - here only armor
                armorItems = agent.Character.GetCharacterEquipment(EquipmentIndex.ArmorItemBeginSlot);
                foreach (var item in armorItems)
                {
                    if (item.HasAnyTrait())
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

                if(statusEffectComp != null)
                {
                    var statusEffectAmplifiers = statusEffectComp.GetAmplifiers(attackTypeMask);

                    for (int i = 0; i < damageAmplifications.Length; i++)
                    {
                        damageAmplifications[i] += statusEffectAmplifiers[i];
                    }
                }

                //Weapon properties
                if (attackTypeMask == AttackTypeMask.Ranged)
                {
                    if (agent.WieldedWeapon.Item != null)
                    {
                        var ammoItem = Mission.Current.MissilesList.FirstOrDefault(x => x.ShooterAgent == agent)?.Weapon.Item;

                        if (ammoItem != null)
                        {
                            var weapon = agent.WieldedWeapon.Item;
                            List<ItemTrait> rangeItemTraits = [.. ammoItem.GetTraits(), .. weapon.GetTraits(agent)];

                            foreach (var itemTrait in rangeItemTraits)
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
                            //range damage Propotions

                            var ammoTuple = ammoItem.GetTorSpecificData().DamageProportions;

                            var weaponProperty = ammoTuple;
                            if (weaponProperty != null)
                            {
                                foreach (var tuple in weaponProperty)
                                {
                                    damageProportions[(int)tuple.DamageType] = tuple.Percent;
                                }
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
                        meleeItemTraits.AddRange(weapon.GetTraits(agent));
                        if (offhand != null)
                            meleeItemTraits.AddRange(offhand.GetTraits());

                        foreach (var itemTrait in meleeItemTraits)
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

                            if (property != null)
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
                    if (item.HasAnyTrait())
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
                if (statusEffectComp != null)
                {
                    var statusEffectResistances = agent.GetComponent<StatusEffectComponent>().GetResistances(attackTypeMask);

                    for (int i = 0; i < damageResistances.Length; i++)
                    {
                        damageResistances[i] += statusEffectResistances[i];
                    }
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

        public void AddPerkEffectsToAgentProperties(Agent agent, PropertyMask mask, AttackTypeMask attackMask, ref float[] propotions, ref float[] damageAmps, ref float[] damageBonuses, ref float[] resistances)
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

                if ((attackMask == AttackTypeMask.Ranged && agentParty.HasBlessing("cult_of_kurnous")))
                {
                    damageBonuses[(int)DamageType.Physical] += 0.1f;
                }

                if (agentParty.HasBlessing("cult_of_vaul"))
                {
                    resistances[(int)DamageType.Physical] += 0.1f;
                }

                if ((attackMask == AttackTypeMask.Melee && agentParty.HasBlessing("cult_of_gork")))
                {
                    damageBonuses[(int)DamageType.Physical] += 0.1f;
                }

                if (agentParty.HasBlessing("cult_of_mork"))
                {
                    damageBonuses[(int)DamageType.Fire] += 0.1f;
                }

                if (agentParty.HasBlessing("cult_of_grimnir"))
                {
                    damageBonuses[(int)DamageType.Physical] += 0.15f;
                }

                if (agentParty.HasBlessing("cult_of_valaya"))
                {
                    resistances[(int)DamageType.All] += 0.05f;
                }
            }

            var wieldedItem = agent.WieldedWeapon.Item;

            // Waaagh damage modifiers for Greenskins
            if (agentLeader != null && agentLeader.Culture.StringId == TORConstants.Cultures.GREENSKIN &&
                (mask == PropertyMask.Attack || mask == PropertyMask.All))
            {
                if (agentLeader.HasAttribute("Waaagh0"))
                {
                    damageBonuses[(int)DamageType.Physical] -= 0.2f;
                }
                else if (agentLeader.HasAttribute("Waaagh1"))
                {
                    damageBonuses[(int)DamageType.Physical] -= 0.1f;
                }
                else if (agentLeader.HasAttribute("Waaagh2"))
                {
                    damageBonuses[(int)DamageType.Physical] += 0.1f;
                }
                else if (agentLeader.HasAttribute("Waaagh3"))
                {
                    damageBonuses[(int)DamageType.Physical] += 0.2f;
                }
            }

            if (agentCharacter != null)
            {
                if (mask == PropertyMask.Attack || mask == PropertyMask.All)
                {
                    if (agentCaptain != null && agentCaptain.GetPerkValue(TORPerks.Spellcraft.ArcaneLink))
                    {
                        damageBonuses[(int)DamageType.Magical] += (TORPerks.Spellcraft.ArcaneLink.SecondaryBonus);
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

                    // Brewers Guild Level III bonus: +15% physical resistance for ranger units
                    if (agent.BelongsToMainParty() && agentLeader != null && agentLeader.HasAttribute("DwarfBrewersIII"))
                    {
                        if (agentCharacter.StringId.Contains("tor_dw_ranger"))
                        {
                            resistances[(int)DamageType.Physical] += 0.15f;
                        }
                    }
                }
            }

            if (agent.IsHero && agent.BelongsToMainParty())
            {

                if (Hero.MainHero.HasCareer(TORCareers.Spellsinger))
                {
                    if (Hero.MainHero.HasCareerChoice("FuryOfTheForestPassive1"))
                    {
                        var choice = TORCareerChoices.GetChoice("FuryOfTheForestPassive1");
                        var heroes = Hero.MainHero.PartyBelongedTo.GetMemberHeroes();

                        if (heroes.Any(x => x.HasKnownLore("DarkMagic") && x.CharacterObject.IsElf()))
                        {
                            damageBonuses[(int)DamageType.Magical] += choice.GetPassiveValue();
                        }
                    }

                    if (Hero.MainHero.HasCareerChoice("FuryOfTheForestPassive2"))
                    {
                        var choice = TORCareerChoices.GetChoice("FuryOfTheForestPassive2");
                        var heroes = Hero.MainHero.PartyBelongedTo.GetMemberHeroes();

                        if (heroes.Any(x => x.HasKnownLore("HighMagic") && x.CharacterObject.IsElf()))
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
                resistances[(int)DamageType.Fire] -= 0.2f;
            }

            var choices = Agent.Main.GetHero().GetAllCareerChoices();

            if (mask == PropertyMask.Attack && attackMask == AttackTypeMask.Melee)
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

            if (mask == PropertyMask.Defense && (attackMask == AttackTypeMask.Melee || attackMask == AttackTypeMask.Ranged))
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
                if ((attackMask == AttackTypeMask.Melee && mask == PropertyMask.Attack))
                {
                    if (agent.Controller == AgentControllerType.Player)
                    {

                        if (mask == PropertyMask.Attack && agent.Character.HasAttribute("NecromancerChampion") && choices.Contains("LiberMortisKeystone"))
                        {
                            var choice = TORCareerChoices.GetChoice("LiberMortisKeystone");
                            damageBonuses[(int)DamageType.Physical] += choice.GetPassiveValue();
                        }

                        if (mask == PropertyMask.Attack && agent.Character.HasAttribute("NecromancerChampion") && choices.Contains("BooksOfNagashKeystone"))
                        {
                            var choice = TORCareerChoices.GetChoice("BooksOfNagashKeystone");
                            damageBonuses[(int)DamageType.Magical] += choice.GetPassiveValue();
                        }
                    }
                }

                if (mask == PropertyMask.Defense && choices.Contains("BookofWsoranKeystone"))
                {
                    var choice = TORCareerChoices.GetChoice("BookofWsoranKeystone");
                    resistances[(int)DamageType.All] += choice.GetPassiveValue();
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
             out float[] additionalDamagePercentages)
        {
            damageProportions = new float[(int)DamageType.All + 1];
            damageAmplifications = new float[(int)DamageType.All + 1];
            damageResistances = new float[(int)DamageType.All + 1];
            additionalDamagePercentages = new float[(int)DamageType.All + 1];
            var statusEffectComp = agent.GetComponent<StatusEffectComponent>();

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
                List<ItemTrait> dynamicTraits = agent.GetComponent<ItemTraitAgentComponent>()?.GetDynamicTraits(agent.WieldedWeapon.Item);

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

                if(statusEffectComp != null)
                {
                    var statusEffectAmplifiers = agent.GetComponent<StatusEffectComponent>().GetAmplifiers(attackTypeMask);
                    for (int i = 0; i < damageAmplifications.Length; i++)
                    {
                        damageAmplifications[i] += statusEffectAmplifiers[i];
                    }
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
                List<ItemTrait> dynamicTraits = agent.GetComponent<ItemTraitAgentComponent>()?.GetDynamicTraits(agent.WieldedWeapon.Item);

                foreach (var dynamicTrait in dynamicTraits)
                {
                    var defenseProperty = dynamicTrait.ResistanceTuple;
                    if (defenseProperty != null)
                    {
                        damageResistances[(int)defenseProperty.ResistedDamageType] += defenseProperty.ReductionPercent;
                    }
                }

                //status effects
                if(statusEffectComp == null)
                {
                    var statusEffectResistances = agent.GetComponent<StatusEffectComponent>().GetResistances(attackTypeMask);

                    for (int i = 0; i < damageResistances.Length; i++)
                    {
                        damageResistances[i] += statusEffectResistances[i];
                    }
                }
            }
        }

        public float CalculateWardSaveFactor(Agent attacker, Agent victim, float[] resistances, bool friendlyFire)
        {
            var result = new ExplainedNumber(1f);
            var victimCharacter = victim.Character as CharacterObject;
            if (victimCharacter != null)
            {

                if (resistances[(int)DamageType.All] > 0)
                {
                    result.AddFactor(-resistances[(int)DamageType.All]);
                }


                if (victimCharacter.GetPerkValue(TORPerks.Spellcraft.Dampener))
                {
                    result.AddFactor(TORPerks.Spellcraft.Dampener.SecondaryBonus);
                }
                SkillHelper.AddSkillBonusForCharacter(TORSkillEffects.FaithWardSave, victimCharacter, ref result);
            }

            if (friendlyFire && !attacker.IsMainAgent && victimCharacter.IsDwarf())
            {
                result.AddFactor(-0.9f);
            } 

            result.LimitMax(1);
            if (!friendlyFire)
            {
                result.LimitMin(0.11f);
            }
            return result.ResultNumber;
        }

        public bool ShouldCutThrough(AttackCollisionData collisionData, Agent attacker, Agent victim)
        {

            if (attacker.IsHero && attacker.WieldedWeapon.Item.HasAnyTrait())
            {
                var traits = attacker.WieldedWeapon.Item.GetTraits();

                if (traits.AnyQ(trait => trait.StatsTuple?.StatType == ItemTraitStatType.Cleave))
                {
                    return true;
                }
            }
            if (attacker.HasAttribute("Slice"))
                return true;

            return false;
        }

        public override float CalculateRemainingMomentum(float originalMomentum, in Blow b, in AttackCollisionData collisionData, Agent attacker, Agent victim, in MissionWeapon attackerWeapon, bool isCrushThrough)
        {
            float newMomentum = 0f;

            if (isCrushThrough || ShouldCutThrough(collisionData, attacker, victim))//Sly : native uses 0.3 multiplier for CrushThrough, I've put it here directly to skip a method call and to have an equality between CrushThrough and CutThrough for remaining momentum. It would probably be more realistic if CutThrough had higher momentum as the weapon has less resistance from the body and armour and it has a far smaller contact area to mitigate received force. Future balance thought.
            {
                newMomentum = originalMomentum * 0.3f;
            }
            else
            {
                newMomentum = base.CalculateRemainingMomentum(originalMomentum, in b, in collisionData, attacker, victim, in attackerWeapon, isCrushThrough);
            }
                
            return newMomentum;
        }

        // for crush through blocks converts the target overhead chance at skill diff into an exponential growth rate
        private static float CalculateExtraCtbSkillGrowthRate()
        {
            int delta = EXTRA_CTB_TARGET_SKILL_DIFF - EXTRA_CTB_SKILL_DEAD_ZONE;
            if (delta <= 0)
            {
                return 0f;
            }

            float targetChance = MBMath.ClampFloat(EXTRA_CTB_TARGET_OVERHEAD_CHANCE_AT_TARGET_DIFF, 0.001f, 0.999f);
            return (float)(-Math.Log(1.0 - targetChance) / delta);
        }

        // for crush through blocks returns the overhead swing base chance coming purely from skill diff and ramps up with a exponential curve
        private static float CalculateExtraCtbOverheadChanceFromSkillDiff(int attackerSkillValue, int defenderSkillValue)
        {
            int skillDiff = attackerSkillValue - defenderSkillValue;
            if (skillDiff <= EXTRA_CTB_SKILL_DEAD_ZONE)
            {
                return 0f;
            }

            int delta = skillDiff - EXTRA_CTB_SKILL_DEAD_ZONE;
            int targetDelta = EXTRA_CTB_TARGET_SKILL_DIFF - EXTRA_CTB_SKILL_DEAD_ZONE;
            if (targetDelta <= 0)
            {
                return 0f;
            }

            float numerator = 1f - (float)Math.Exp(-delta * ExtraCtbSkillGrowthRate);
            float denominator = 1f - (float)Math.Exp(-targetDelta * ExtraCtbSkillGrowthRate);
            if (denominator <= 0f)
            {
                return 0f;
            }

            float normalized = numerator / denominator;
            float chance = EXTRA_CTB_TARGET_OVERHEAD_CHANCE_AT_TARGET_DIFF * normalized;

            return MBMath.ClampFloat(chance, 0f, 1f);
        }


        // for crush through blocks scales chance based on how far the attack is past the energy threshold. 0 at threshold, 1 at full effect
        private static float CalculateExtraCtbEnergyFactor(float totalAttackEnergy, float threshold)
        {
            float fullEffectMargin = threshold * EXTRA_CTB_ENERGY_MARGIN_FOR_FULL_EFFECT;
            float energyFactor = (totalAttackEnergy - threshold) / fullEffectMargin;
            return MBMath.ClampFloat(energyFactor, 0f, 1f);
        }

        public override bool DecideCrushedThrough(Agent attackerAgent, Agent defenderAgent, float totalAttackEnergy, Agent.UsageDirection attackDirection, StrikeType strikeType, WeaponComponentData defendItem, bool isPassiveUsage)
        {
            // Monster attacks (trolls, minotaurs, etc.) can only be blocked with shields
            bool isShieldBlock = defendItem != null && defendItem.IsShield;
            if (attackerAgent != null && attackerAgent.HasAttribute("MonsterAttack"))
            {
                if (defendItem == null || !defendItem.IsShield)
                {
                    return true;
                }

                EquipmentIndex equipmentIndex = attackerAgent.GetOffhandWieldedItemIndex();
                if (equipmentIndex == EquipmentIndex.None)
                {
                    equipmentIndex = attackerAgent.GetPrimaryWieldedItemIndex();
                }

                if (((equipmentIndex != EquipmentIndex.None) ? attackerAgent.Equipment[equipmentIndex].CurrentUsageItem : null) == null || isPassiveUsage || strikeType != 0 || (attackDirection != 0 && !attackerAgent.HasAttribute("CrushThrough")))
                {
                    return false;
                }

                float num = 58f;
                if (defendItem != null && defendItem.IsShield)
                {
                    num *= 1.2f;
                }

                bool passed = totalAttackEnergy > num;
                return passed;
            }

            // no ctb against shields
            if (defendItem != null && defendItem.IsShield)
            {
                return false;
            }

            EquipmentIndex equipmentIndexNonMonster = attackerAgent.GetOffhandWieldedItemIndex();
            if (equipmentIndexNonMonster == EquipmentIndex.None)
            {
                equipmentIndexNonMonster = attackerAgent.GetPrimaryWieldedItemIndex();
            }

            WeaponComponentData attackerUsageItem = (equipmentIndexNonMonster != EquipmentIndex.None)
                ? attackerAgent.Equipment[equipmentIndexNonMonster].CurrentUsageItem
                : null;

            // base ctb
            if (attackerUsageItem != null
                && !isPassiveUsage
                && strikeType == 0
                && (attackDirection == 0 || attackerAgent.HasAttribute("CrushThrough"))
                && totalAttackEnergy > 58f)
            {
            #if TOR_CTB_LOG
                CrushThroughDecisionTrace.Trace(attackerAgent, defenderAgent, "BASE_PASS",
                    $"E={totalAttackEnergy:0.0} thr=58.0 dir={attackDirection} shield={isShieldBlock}");
            #endif

                return true;
            }

            // only attempt extra ctb for valid swing attacks with enough energy
            // skill diff over deadzone increases the chance
            // extra energy past threshold pushes it toward full effect
            // non overhand swings take a penalty
            // custom ctb
            const float threshold = 25f;

            if (attackerUsageItem == null || isPassiveUsage || strikeType != 0 || totalAttackEnergy <= threshold)
            {
            #if TOR_CTB_LOG
                string reason = attackerUsageItem == null ? "noUsageItem"
                    : isPassiveUsage ? "passiveUsage"
                    : strikeType != 0 ? "notSwing"
                    : $"energyBelowThreshold need={(threshold - totalAttackEnergy):0.0}";

                CrushThroughDecisionTrace.Trace(attackerAgent, defenderAgent, "EXTRA_FAIL_GATE",
                    $"reason={reason} E={totalAttackEnergy:0.0} thr={threshold:0.0} dir={attackDirection}");
            #endif

                return false;
            }

            int attackerSkillValue = attackerUsageItem.RelevantSkill != null
                ? attackerAgent.Character.GetSkillValue(attackerUsageItem.RelevantSkill)
                : 0;

            int defenderSkillValue = (defendItem != null && defendItem.RelevantSkill != null)
                ? defenderAgent.Character.GetSkillValue(defendItem.RelevantSkill)
                : 0;

            // deadzone
            float overheadChanceFromSkill = CalculateExtraCtbOverheadChanceFromSkillDiff(attackerSkillValue, defenderSkillValue);
            if (overheadChanceFromSkill <= 0f)
            {
            #if TOR_CTB_LOG
                int skillDiff = attackerSkillValue - defenderSkillValue;
                CrushThroughDecisionTrace.Trace(attackerAgent, defenderAgent, "EXTRA_FAIL_SKILL",
                    $"diff={skillDiff} deadzone<={EXTRA_CTB_SKILL_DEAD_ZONE} A={attackerSkillValue} D={defenderSkillValue} E={totalAttackEnergy:0.0} dir={attackDirection}");
            #endif

                return false;
            }

            // momentum still matters after threshold
            float energyFactor = CalculateExtraCtbEnergyFactor(totalAttackEnergy, threshold);

            float chance = overheadChanceFromSkill * energyFactor;

            if (attackDirection != 0)
            {
                chance *= EXTRA_CTB_NON_OVERHEAD_CHANCE_MULTIPLIER;
            }

            chance = MBMath.ClampFloat(chance, 0f, 1f);
            float roll = MBRandom.RandomFloat;
            bool passedExtra = roll < chance;

        #if TOR_CTB_LOG
            int skillDiffRoll = attackerSkillValue - defenderSkillValue;
            bool isNonOverhead = attackDirection != 0;

            CrushThroughDecisionTrace.Trace(attackerAgent, defenderAgent, passedExtra ? "EXTRA_PASS" : "EXTRA_FAIL_ROLL",
                $"diff={skillDiffRoll} A={attackerSkillValue} D={defenderSkillValue} " +
                $"E={totalAttackEnergy:0.0} thr={threshold:0.0} energyFactor={energyFactor:0.00} " +
                $"skillChanceOH={overheadChanceFromSkill:0.00} dir={(isNonOverhead ? "nonOH" : "OH")} " +
                $"mult={(isNonOverhead ? EXTRA_CTB_NON_OVERHEAD_CHANCE_MULTIPLIER : 1f):0.00} " +
                $"chance={chance:0.00} roll={roll:0.00}");
        #endif

            return passedExtra;
        }
    }
}