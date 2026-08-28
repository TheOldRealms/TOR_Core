using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Security;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.CustomBattle;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Models;
using TOR_Core.Utilities;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.Extensions
{
    public static class AgentExtensions
    {
        public static CharacterObject GetCaptainCharacter(this Agent agent)
        {
            if (agent?.Formation != null && agent.Formation.Captain != null && agent.Formation.Captain.Character != null)
            {
                var character = agent.Formation.Captain.Character as CharacterObject;
                if (character != null) return character;
            }
            return null;
        }

        public static CharacterObject GetPartyLeaderCharacter(this Agent agent)
        {
            if (agent?.Origin?.BattleCombatant is PartyBase)
            {
                var party = (PartyBase)agent.Origin.BattleCombatant;
                return party.LeaderHero?.CharacterObject;
            }
            return null;
        }

        public static CharacterObject GetArmyCommanderCharacter(this Agent agent)
        {
            if (agent?.Origin?.BattleCombatant is PartyBase)
            {
                var party = (PartyBase)agent.Origin.BattleCombatant;
                var character = party.General as CharacterObject;
                return character;
            }
            return null;
        }

        public static MobileParty GetOriginMobileParty(this Agent agent)
        {
            var party = agent?.GetOriginPartyBase();
            if (party == null) return null;
            return party.MobileParty;
        }

        public static PartyBase GetOriginPartyBase(this Agent agent)
        {
            if (agent?.Origin?.BattleCombatant is PartyBase) return agent.Origin.BattleCombatant as PartyBase;
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

        public static bool IsTreeSpirit(this Agent agent)
        {
            if (Campaign.Current != null && agent.IsHero)
            {
                return agent.GetHero().IsTreeSpirit();
            }

            return agent.Character.IsTreeSpirit();
        }

        public static bool IsTreeman(this Agent agent)
        {
            if (Campaign.Current != null && agent.IsHero)
            {
                return agent.GetHero().CharacterObject.IsTreeman();
            }

            return agent.Character.IsTreeman();
        }
        
        public static bool IsTroll(this Agent agent)
        {
            if (Campaign.Current != null && agent.IsHero)
            {
                return agent.GetHero().CharacterObject.IsTroll();
            }

            return agent.Character.IsTroll();
        }

        public static bool IsSlayer(this Agent agent)
        {
            return agent.Character.Culture.StringId == TORConstants.Cultures.DAWI && agent.Character.StringId.Contains("slayer");
        }

        public static bool IsDamageShruggedOff(this Agent agent, int inflictedDamage = 0)
        {
            if (Campaign.Current == null) return false;

            if (agent.IsMainAgent && agent.GetHero().HasAnyCareer())
            {
                if (CareerHelper.ShruggedOffDamage(Hero.MainHero, inflictedDamage))
                {
                    return true;
                }
            }

            if (Hero.MainHero.HasCareer(TORCareers.Necromancer))
            {
                if (Hero.MainHero.HasCareerChoice("BookofWsoranKeystone") && agent.HasAttribute(CharacterAttributes.NECROMANCER_CHAMPION))
                    return true;
            }

            return false;
        }
        public static int GetRegenerationTier(this Agent agent)
        {
            var attributes = agent.GetAttributes();
            if (attributes.Contains("Regeneration3"))
            {
                return 3;
            }

            if (attributes.Contains("Regeneration2"))
            {
                return 2;
            }

            return attributes.Contains("Regeneration") ? 1 : 0;
        }

        public static float GetPiercingArmorReduction(this Agent agent)
        {
            var attributes = agent.GetAttributes();
            if (attributes.Contains("Piercing2"))
            {
                return 0.40f;
            }

            return attributes.Contains("Piercing") ? 0.30f : 0f;
        }

        public static float GetBulwarkRangedDamageReduction(this Agent agent)
        {
            var attributes = agent.GetAttributes();
            if (attributes.Contains("Bulwark3"))
            {
                return 0.60f;
            }

            if (attributes.Contains("Bulwark2"))
            {
                return 0.40f;
            }

            return attributes.Contains("Bulwark") ? 0.20f : 0f;
        }

        public static float GetSwiftMovementSpeedBonus(this Agent agent)
        {
            var attributes = agent.GetAttributes();
            if (attributes.Contains("Swift3"))
            {
                return 0.40f;
            }

            if (attributes.Contains("Swift2"))
            {
                return 0.30f;
            }

            return attributes.Contains("Swift") ? 0.20f : 0f;
        }

        public static float GetPoisonousHitChance(this Agent agent)
        {
            var attributes = agent.GetAttributes();
            if (attributes.Contains("Poisonous2"))
            {
                return 0.35f;
            }

            return attributes.Contains("Poisonous") ? 0.20f : 0f;
        }

        public static bool HasShieldPenetration(this Agent agent)
        {
            return agent.GetAttributes().Contains("ShieldPenetration");
        }

        public static bool HasHorseLink(this Agent agent)
        {
            return agent.GetAttributes().Contains("HorseLink");
        }

        public static bool HasHorseSteady(this Agent agent)
        {
            return agent.GetAttributes().Contains("HorseSteady");
        }

        public static bool IsUnstoppable(this Agent agent)
        {
            return agent.GetAttributes().Contains("Unstoppable");
        }

        public static bool HasSlice(this Agent agent)
        {
            return agent.GetAttributes().Contains("Slice");
        }

        public static bool HasMonsterAttack(this Agent agent)
        {
            return agent.GetAttributes().Contains("MonsterAttack");
        }

        public static bool HasCrushThrough(this Agent agent)
        {
            return agent.GetAttributes().Contains("CrushThrough");
        }
        public static bool HasBrute(this Agent agent)
        {
            return agent.GetAttributes().Contains("Brute");
        }

        public static bool HasTheHunger(this Agent agent)
        {
            return agent.GetAttributes().Contains("TheHunger");
        }

        public static bool HasFrenzy(this Agent agent)
        {
            return agent.GetAttributes().Contains("Frenzy");
        }

        public static bool HasWightKingTrait(this Agent agent)
        {
            var hero = agent.GetHero();
            return hero != null && hero.HasAttribute("WightKing");
        }

        public static float GetUndeadSlayerDamageBonus(this Agent agent)
        {
            var attributes = agent.GetAttributes();
            if (attributes.Contains("UndeadSlayer2"))
            {
                return 0.60f;
            }

            return attributes.Contains("UndeadSlayer") ? 0.30f : 0f;
        }

        public static bool HasImmortality(this Agent agent)
        {
            return agent.GetAttributes().Contains("Immortality");
        }

        public static bool HasDeadeye(this Agent agent)
        {
            return agent.GetAttributes().Contains("Deadeye");
        }

        public static float GetEtherealIgnoreChance(this Agent agent)
        {
            var attributes = agent.GetAttributes();
            if (attributes.Contains("Ethereal2"))
            {
                return 0.40f;
            }

            return attributes.Contains("Ethereal") ? 0.25f : 0f;
        }

        public static bool HasKillingBlow(this Agent agent)
        {
            return agent.GetAttributes().Contains("KillingBlow");
        }

        public static float GetMonsterSlayerDamageBonus(this Agent agent)
        {
            var attributes = agent.GetAttributes();
            if (attributes.Contains("MonsterSlayer2"))
            {
                return 1.50f;
            }

            return attributes.Contains("MonsterSlayer") ? 0.75f : 0f;
        }

        public static bool IsMonstrous(this Agent agent)
        {
            return agent.Character != null && agent.Character.IsMonstrous();
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
        public static bool HasPartyAnvilOfDoom(this Agent agent)
        {
            var party = agent.GetHero()?.PartyBelongedTo;
            if (party == null) return false;

            return party.HasAnvilOfDoom();
        }

        public static bool HasAttribute(this Agent agent, string attributeName)
        {
            return agent.GetAttributes().Contains(attributeName);
        }

        public static bool TryCastCurrentAbility(this Agent agent, out TextObject failureReason)
        {
            var abilitycomponent = agent.GetComponent<AbilityComponent>();

            if (abilitycomponent != null)
            {
                if (abilitycomponent.CurrentAbility != null) return abilitycomponent.CurrentAbility.TryCast(agent, out failureReason);
            }
            failureReason = TORTextHelper.GetTextObject("tor_cast_fail_comp_null", "Abilitycomponent is null!");
            return false;
        }

        public static AgentPropertyContainer GetProperties(this Agent agent, PropertyMask propertyMask, AttackTypeMask attackTypeMask)
        {
            if (!(MissionGameModels.Current.AgentApplyDamageModel is TORAgentApplyDamageModel damageModel))
            {
                return AgentPropertyContainer.InitNew();
            }

            return damageModel.CreateAgentPropertyContainer(agent, propertyMask, attackTypeMask);
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

        public static void SelectAbility(this Agent agent, int abilityindex)
        {
            var abilitycomponent = agent.GetComponent<AbilityComponent>();
            if (abilitycomponent != null)
            {
                abilitycomponent.SelectAbility(abilityindex);
            }
        }

        public static void SelectAbility(this Agent agent, Ability ability)
        {
            var abilitycomponent = agent.GetComponent<AbilityComponent>();
            if (abilitycomponent != null)
            {
                abilitycomponent.SelectAbility(ability);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Heroes only exist in the context of campaign games.
        /// Custom battles will receive null from this.
        /// </remarks>
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

        public static bool IsSummoned(this Agent agent)
        {
            if (agent == null) return false;
            return agent.Origin != null && agent.Origin.GetType() == typeof(SummonedAgentOrigin);
        }

        /// <summary>
        /// A backup used for finding artillery count in custom battles. Campaign missions find artillery count via the party origin.
        /// </summary>
        public static int GetPlaceableArtilleryCount(this Agent agent)
        {
            int count = 0;

            if (agent == null) return count;//leaderless parties pass null agents in.

            if (!agent.IsHero) return count;

            if (Game.Current.GameType is Campaign)
            {
                var hero = agent.GetHero();
                if (!hero.IsArtilleryHero()) return count;

                count = hero.GetPlaceableArtilleryCount();
                TORCommon.Log("AgentExtensions : artillery count for campaign battle derived from an agent; use the MobileParty extension instead.", LogLevel.Error);
            }
            else if (Game.Current.GameType is CustomGame)
            {
                count = 3;
            }
            return count;
        }

        public static List<string> GetAbilities(this Agent agent)
        {
            var hero = agent?.GetHero();
            var character = agent?.Character;
            if (hero != null)
            {
                return hero.GetExtendedInfo().AllAbilities;
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
            var hero = agent?.GetHero();
            var character = agent.Character;
            var abilities = new List<string>();
            if (hero != null)
            {
                abilities.AddRange(hero.GetExtendedInfo().SelectedAbilities);
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

        public static bool BelongsToMainParty(this Agent agent)
        {
            var party = agent.GetOriginMobileParty();
            return party != null && party.IsMainParty;
        }

        public static List<string> GetAttributes(this Agent agent)
        {
            List<string> result = new List<string>();
            var hero = agent.GetHero();
            var character = agent.Character;
            if (hero != null)
            {
                var info = hero.GetExtendedInfo();
                if (info != null && info.AllAttributes.Count > 0)
                {
                    foreach (var attribute in info.AllAttributes)
                    {
                        if (!result.Contains(attribute)) result.Add(attribute);
                    }
                }
            }
            else if (character != null)
            {
                foreach (var attribute in character.GetAttributes())
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
        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        public static void ApplyDamage(this Agent agent, int damageAmount, Vec3 impactPosition, Agent damager = null, bool doBlow = true, bool hasShockWave = false, bool originatesFromAbility = true)
        {
            if (agent == null)
            {
                TORCommon.Log("ApplyDamage: attempted to apply damage to a null agent.", LogLevel.Warn);
                if (damager != null) { TORCommon.Log("ApplyDamage: damager was : " + damager.ToString() + ".", LogLevel.Warn); }
                return;
            }
            if (!agent.IsHuman || !agent.IsActive() || agent.Health < 1)
            {
                TORCommon.Log("ApplyDamage: attempted to apply damage to a dead or non-human agent : " + agent.ToString() + ".", LogLevel.Warn);
                return;
            }
            try
            {
                if (agent.IsFadingOut()) return;
                // Registering a blow causes the agent to react/stagger. Manipulate health directly if the damage won't kill the agent.
                if (agent.State == AgentState.Active || agent.State == AgentState.Routed)
                {
                    if (!doBlow && agent.Health > damageAmount)
                    {
                        agent.Health -= damageAmount;
                        return;
                    }


                    var damagerAgent = damager ?? agent;

                    var blow = new Blow(damagerAgent.Index);
                    blow.DamageType = DamageTypes.Blunt;
                    blow.BoneIndex = agent.Monster.HeadLookDirectionBoneIndex;
                    blow.GlobalPosition = agent.GetChestGlobalPosition();
                    blow.BaseMagnitude = damageAmount;
                    blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
                    blow.WeaponRecord.Weight = 5f;
                    blow.InflictedDamage = damageAmount;
                    var direction = blow.GlobalPosition == impactPosition ? -agent.LookDirection : blow.GlobalPosition - impactPosition;
                    direction.Normalize();
                    blow.Direction = direction;
                    blow.SwingDirection = direction;
                    blow.DamageCalculated = true;  // Damage already calculated in TORAbilityModel.CalculateFinalSpellDamage()
                    blow.AttackType = AgentAttackType.Kick;
                    blow.BlowFlag = BlowFlags.NoSound;
                    blow.VictimBodyPart = BoneBodyPartType.Chest;
                    blow.StrikeType = StrikeType.Thrust;
                    if (hasShockWave)
                    {
                        if (agent.HasMount) blow.BlowFlag |= BlowFlags.CanDismount;
                        else blow.BlowFlag |= BlowFlags.KnockDown;
                        blow.BaseMagnitude = 1000;

                    }
                    if (!originatesFromAbility)
                    {
                        blow.AttackType = AgentAttackType.Standard;
                    }
                    blow.WeaponRecord.Velocity = direction * blow.BaseMagnitude;

                    if (agent.Health <= damageAmount && !doBlow)
                    {
                        agent.Die(blow);
                        return;
                    }
                    sbyte mainHandItemBoneIndex = damagerAgent.Monster.MainHandItemBoneIndex;
                    // Use spell sentinel value to identify this as a spell blow in damage model methods
                    int weaponSlotOrMissileIndex = originatesFromAbility ? TORSpellBlowHelper.SpellBlowSentinel : -1;
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
                        weaponSlotOrMissileIndex,
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
            catch (AccessViolationException a)
            {
                TORCommon.Log("ApplyDamage: attempted to damage agent, but application quit with access violation.", LogLevel.Error);
                TORCommon.Log(a.ToString(), LogLevel.Error);
                Process.GetCurrentProcess().Kill();
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

        public static void ApplyStatusEffect(this Agent agent, string effectId, Agent applierAgent, float duration = 5, bool append = true, bool isMutated = false, bool stack = false, int castId = -1)
        {
            var comp = agent?.GetComponent<StatusEffectComponent>();
            if (comp != null) comp.RunStatusEffect(effectId, applierAgent, duration, append, isMutated, stack, castId);
        }

        public static void RemoveStatusEffect(this Agent agent, string effectId)
        {
            if (agent == null) return;
            var comp = agent.GetComponent<StatusEffectComponent>();
            if (comp != null) comp.RemoveStatusEffect(effectId);
        }

        public static void FallDown(this Agent agent)
        {
            agent.SetActionChannel(0, ActionIndexCache.Create("act_strike_fall_back_heavy_back_rise_continue"));
        }

        public static bool HasUsableVisuals(this Agent agent)
        {
            return agent != null &&
                   !agent.IsFadingOut() &&
                   agent.AgentVisuals != null &&
                   agent.AgentVisuals.IsValid();
        }

        public static bool TrySetContourColor(this Agent agent, uint? color, bool alwaysVisible = true)
        {
            if (!agent.HasUsableVisuals())
            {
                return false;
            }

            agent.AgentVisuals.SetContourColor(color, alwaysVisible);
            return true;
        }

        public static bool TryRemoveChildEntity(this Agent agent, GameEntity entity, int removeReason = 0)
        {
            if (!agent.HasUsableVisuals() || entity == null)
            {
                return false;
            }

            agent.AgentVisuals.RemoveChildEntity(entity, removeReason);
            return true;
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