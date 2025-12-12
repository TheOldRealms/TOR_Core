using System;
using System.Linq;
using System.Timers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.AbilitySystem.Scripts;
using TOR_Core.BattleMechanics.AI.CastingAI.Components;
using TOR_Core.BattleMechanics.AI.CommonAIFunctions;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;
using Timer = System.Timers.Timer;

namespace TOR_Core.AbilitySystem
{
    public abstract class Ability : IDisposable
    {
        private int _coolDownLeft = 0;
        private Timer _timer = null;
        private float _cooldown_end_time;

        protected bool _isLocked = false;
        public bool IsCasting { get; private set; }
        public string StringID { get; }
        public AbilityTemplate Template { get; protected set; }
        public AbilityScript AbilityScript { get; protected set; }
        public AbilityCrosshair Crosshair { get; private set; }
        public bool IsActivationPending { get; private set; }
        public bool IsActive => IsCasting || IsActivationPending || (AbilityScript != null && !AbilityScript.IsFading);
        public AbilityEffectType AbilityEffectType => Template.AbilityEffectType;
        public bool IsOnCooldown() => _timer.Enabled;
        public int GetCoolDownLeft() => _coolDownLeft;
        public bool IsSingleTarget => Template.AbilityTargetType == AbilityTargetType.SingleAlly || Template.AbilityTargetType == AbilityTargetType.SingleEnemy; //Sly : Self isn't necessarily SingleTarget because some abilities have triggered effects that are aoe even if the target is self-centered
        public bool RequiresTargeting => Template.AbilityTargetType != AbilityTargetType.Self;

        public delegate void OnCastCompleteHandler(Ability ability);

        public event OnCastCompleteHandler OnCastComplete;

        public delegate void OnCastStartHandler(Ability ability);

        public event OnCastStartHandler OnCastStart;

        public Ability(AbilityTemplate template)
        {
            StringID = template.StringID;
            Template = template;
            _timer = new Timer(1000);
            _timer.Elapsed += TimerElapsed;
            _timer.Enabled = false;
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (Mission.Current == null)
            {
                FinalizeTimer();
                return;
            }

            _coolDownLeft = (int)(_cooldown_end_time - Mission.Current.CurrentTime);
            if (_coolDownLeft <= 0)
            {
                FinalizeTimer();
            }
        }

        private void FinalizeTimer()
        {
            _coolDownLeft = 0;
            _timer.Stop();
        }

        public virtual bool IsDisabled(Agent casterAgent, out TextObject disabledReason)
        {
            disabledReason = new TextObject("{=str_tor_ability_enabled}Enabled");
            if (casterAgent == null) return false;

            if (casterAgent.GetHero()?.Culture.StringId == TORConstants.Cultures.DAWI && this.Template.BelongsToLoreID == "RuneMagic")
            {
                if (!casterAgent.HasPartyAnvilOfDoom()) //The check could be maybe expensive. We should maybe reiterate after all components are set
                                                        //Sly : attribute on player/runesmith hero that is added or removed when items are discarded/sold/acquired/etc? Rechecking the party's inventory for every attempt to use rune magic be problematic because player's have a tendancy to : 1) hoard items, and 2) play pokemon with caster companions.
                {
                    disabledReason = new TextObject("{=str_tor_ability_missing_anvil}Missing Anvil of Doom");
                    return true;
                }
            }

            if (IsOnCooldown())
            {
                disabledReason = new TextObject("{=str_tor_ability_on_cooldown}On cooldown");
                return true;
            }
            if (_isLocked)
            {
                disabledReason = new TextObject("{=str_tor_ability_mission_over}Mission is over");
                return true;
            }
            if (IsCasting)
            {
                disabledReason = new TextObject("{=str_tor_ability_casting}Casting");
                return true;
            }
            if (casterAgent.IsMainAgent && casterAgent.GetHero().HasAnyCareer())
            {
                if (casterAgent.GetCareerAbility().RequiresDisabledCrosshairDuringAbility && casterAgent.GetCareerAbility().IsActive)
                {
                    disabledReason = new TextObject("{=str_tor_ability_in_mistform}In Mistform");
                    return true;
                }
            }



            return false;
        }

        public bool TryCast(Agent casterAgent, out TextObject failureReason)
        {
            if (CanCast(casterAgent, out failureReason))
            {
                DoCast(casterAgent);
                failureReason = null;
                return true;
            }
            return false;
        }

        public virtual bool CanCast(Agent casterAgent, out TextObject failureReason)
        {
            if (IsDisabled(casterAgent, out failureReason))
            {
                return false;
            }
            if (casterAgent.IsPlayerControlled && !IsRightAngleToCast())
            {
                failureReason = new TextObject("Can only cast in a frontal cone");
                return false;
            }
            if (!casterAgent.IsActive() || casterAgent.Health <= 0 || (casterAgent.IsAIControlled && casterAgent.GetMorale() <= 1) || !casterAgent.IsAbilityUser())
            {
                failureReason = new TextObject("Caster is dead or routed");
                return false;
            }
            failureReason = null;
            return true;
        }

        protected virtual void DoCast(Agent casterAgent)
        {
            OnCastStart?.Invoke(this);
            SetAnimationAction(casterAgent);
            if (Template.CastType == CastType.Instant)
            {
                ActivateAbility(casterAgent);
            }
            else if (Template.CastType == CastType.WindUp)
            {
                IsCasting = true;
                var timer = new Timer(Template.CastTime * 1000)
                {
                    AutoReset = false
                };
                timer.Elapsed += (s, e) => { IsActivationPending = true; };
                timer.Start();
            }
        }

        public void SetCoolDown(int cooldownTime)
        {
            _coolDownLeft = cooldownTime;
            _cooldown_end_time = Mission.Current.CurrentTime + _coolDownLeft + 0.8f; //Adjustment was needed for natural tick on UI
            _timer.Start();
        }

        public virtual void DeactivateAbility()
        {
            _isLocked = true;
            AbilityScript?.Stop();
        }
        public virtual void ActivateAbility(Agent casterAgent)
        {
            if (casterAgent == null) return; //could something die right before casting?
            IsActivationPending = false;
            IsCasting = false;
            bool prayerCoolSeperated = false;
            ExplainedNumber cooldown = new(Template.CoolDown);
            if (Game.Current.GameType is Campaign)
            {
                if (casterAgent.IsMainAgent)
                {
                    var player = Hero.MainHero;
                    prayerCoolSeperated = CareerHelper.PrayerCooldownIsNotShared(casterAgent);

                    var type = Template.AbilityType;
                    if (type == AbilityType.Spell)
                    {
                        CareerHelper.ApplyBasicCareerPassives(player, ref cooldown, PassiveEffectType.WindsCooldownReduction, true);

                        if (casterAgent.IsMainAgent && casterAgent.GetHero().HasCareer(TORCareers.GreyLord))
                        {
                            var choice = TORCareerChoices.GetChoice("SecretOfFellfangPassive1");
                            if (choice != null)
                            {
                                var component = Agent.Main.GetComponent<AbilityComponent>();
                                var count = component.KnownAbilitySystem.Count;
                                count--; // reduced because of career ability 

                                if (count < choice.GetPassiveValue())
                                {
                                    cooldown.AddFactor(-0.5f);
                                }
                            }
                        }

                        if (casterAgent.IsMainAgent && casterAgent.GetHero().HasCareer(TORCareers.Runelord))
                        {
                            var choice = TORCareerChoices.GetChoice("AnvilOfDoomPassive3");
                            if (choice != null)
                            {
                                var agents = Mission.Current.Agents.WhereQ(x => x.IsHero && x.BelongsToMainParty());
                                foreach (var agent in agents)
                                {
                                    if (agent.GetHero().CharacterObject.IsRunesmith())
                                    {
                                        cooldown.AddFactor(-0.02f);
                                    }
                                }
                            }
                        }
                    }

                    if (type == AbilityType.Prayer)
                    {
                        CareerHelper.ApplyBasicCareerPassives(player, ref cooldown, PassiveEffectType.PrayerCoolDownReduction, true);
                    }
                }
            }

            if (Template.AbilityType == AbilityType.Prayer && !prayerCoolSeperated)
                casterAgent.GetComponent<AbilityComponent>().SetPrayerCoolDown((int)cooldown.ResultNumber);
            else
            {
                SetCoolDown((int)cooldown.ResultNumber);
            }

            var frame = GetSpawnFrame(casterAgent);

            if (Template.DoNotAlignParticleEffectPrefab)
            {
                frame = new MatrixFrame(Mat3.CreateMat3WithForward(Vec3.Forward), frame.origin);
            }

            GameEntity parentEntity = GameEntity.CreateEmpty(Mission.Current.Scene, false);
            parentEntity.SetGlobalFrame(frame);

            AddLight(ref parentEntity);

            if (ShouldAddPhyics())
                AddPhysics(ref parentEntity);

            AddBehaviour(ref parentEntity, casterAgent);
            OnCastComplete?.Invoke(this);
        }

        private bool IsGroundAbility()
        {
            return Template.AbilityTargetType == AbilityTargetType.GroundAtPosition;
        }

        private bool IsMissileAbility()
        {
            return Template.AbilityEffectType == AbilityEffectType.SeekerMissile ||
                   Template.AbilityEffectType == AbilityEffectType.Missile;
        }

        private bool ShouldAddPhyics()
        {
            return Template.TriggerType == TriggerType.OnCollision;
        }

        protected MatrixFrame GetSpawnFrame(Agent casterAgent)
        {
            if (casterAgent.IsPlayerControlled)
            {
                var comp = casterAgent.GetComponent<AbilityComponent>();
                if (comp != null)
                {
                    return comp.LastCastWasQuickCast ? CalculateQuickCastMatrixFrame(casterAgent) : CalculatePlayerCastMatrixFrame(casterAgent);
                }
                else throw new NullReferenceException("casterAgent's abilitycomponent is null");
            }
            else if (casterAgent.IsAIControlled) return CalculateAICastMatrixFrame(casterAgent);
            else throw new ArgumentException("casterAgent's controller is none");
        }

        private MatrixFrame CalculateQuickCastMatrixFrame(Agent casterAgent)
        {
            var frame = casterAgent.LookFrame;
            switch (AbilityEffectType)
            {
                case AbilityEffectType.Missile:
                case AbilityEffectType.SeekerMissile:
                    {
                        frame.origin = casterAgent.GetEyeGlobalPosition();
                        break;
                    }
                // Quick cast setup
                case AbilityEffectType.Augment:
                case AbilityEffectType.TacticalReposition:
                    frame.origin = Agent.Main.GetWorldPosition().GetGroundVec3MT();
                    break;
                case AbilityEffectType.ArtilleryPlacement:
                case AbilityEffectType.Summoning:
                    frame.origin =
                        Mission.Current.GetRandomPositionAroundPoint(Agent.Main.GetWorldPosition().GetGroundVec3MT(), 3, 6, false);
                    break;
                case AbilityEffectType.Heal when IsGroundAbility():
                    frame.origin = Agent.Main.GetWorldPosition().GetGroundVec3MT();
                    break;
                case AbilityEffectType.Heal:
                    {
                        float height = 0.0f;
                        var pos = Agent.Main.LookFrame.Advance(15).origin;
                        Mission.Current.Scene.GetHeightAtPoint(pos.AsVec2, BodyFlags.CommonCollisionExcludeFlagsForCombat, ref height);
                        pos.z = height;

                        var targetAgent = Mission.Current.GetClosestAllyAgent(Agent.Main.Team, pos, 5);

                        if (targetAgent != null)
                        {
                            frame.origin = targetAgent.Frame.origin;
                        }
                        else
                        {
                            targetAgent = Agent.Main;
                            frame.origin = targetAgent.Frame.origin;
                        }
                        break;
                    }
                case AbilityEffectType.Hex:
                    {
                        var height = 0.0f;
                        var pos = Agent.Main.LookFrame.Advance(15).origin;
                        Mission.Current.Scene.GetHeightAtPoint(pos.AsVec2, BodyFlags.CommonCollisionExcludeFlagsForCombat, ref height);
                        pos.z = height;

                        MBList<Agent> targets = [];
                        targets = Mission.Current.GetNearbyAgents(pos.AsVec2, 5, targets);

                        foreach (var agent in targets)
                        {
                            if (agent.Team != Mission.Current.Teams.PlayerEnemy) continue;
                            frame.origin = agent.Frame.origin;
                            break;
                        }

                        break;
                    }
                case AbilityEffectType.Bombardment:
                case AbilityEffectType.Vortex:
                    {
                        float height = 0.0f;
                        var pos = Agent.Main.LookFrame.Advance(15).origin;
                        Mission.Current.Scene.GetHeightAtPoint(pos.AsVec2, BodyFlags.CommonCollisionExcludeFlagsForCombat, ref height);

                        if (this.AbilityEffectType == AbilityEffectType.Bombardment)
                            pos.z = height + this.Template.Offset;
                        else
                            pos.z = height;

                        frame.origin = pos;
                        frame.rotation = Agent.Main.LookFrame.rotation;

                        break;
                    }
                case AbilityEffectType.Blast:
                case AbilityEffectType.Wind:
                    {
                        var height = 0.0f;
                        Vec3 pos;
                        pos = this.AbilityEffectType == AbilityEffectType.Wind ? Agent.Main.LookFrame.Advance(5).origin : Agent.Main.LookFrame.Advance(3).origin;

                        Mission.Current.Scene.GetHeightAtPoint(pos.AsVec2, BodyFlags.CommonCollisionExcludeFlagsForCombat, ref height);
                        if (this.AbilityEffectType == AbilityEffectType.Blast)
                            pos.z = height + 1;
                        else
                            pos.z = height;
                        frame.origin = pos;
                        frame.rotation = Agent.Main.LookFrame.rotation;
                        break;
                    }
                case AbilityEffectType.CareerAbilityEffect:
                    frame.origin = Agent.Main.GetChestGlobalPosition();
                    frame.rotation = Agent.Main.LookFrame.rotation;
                    break;
                default:
                    break;
            }

            return frame;
        }

        private MatrixFrame CalculatePlayerCastMatrixFrame(Agent casterAgent)
        {
            var frame = casterAgent.LookFrame;
            switch (Template.AbilityEffectType)
            {
                case AbilityEffectType.Missile:
                case AbilityEffectType.SeekerMissile:
                    {
                        frame.origin = casterAgent.GetEyeGlobalPosition();
                        break;
                    }
                case AbilityEffectType.Wind:
                    {
                        frame = Crosshair.Frame;
                        break;
                    }
                case AbilityEffectType.Blast:
                    {
                        frame = Crosshair.Frame.Elevate(1);
                        break;
                    }
                case AbilityEffectType.Vortex:
                    {
                        frame = Crosshair.Frame;
                        frame.rotation = casterAgent.Frame.rotation;
                        break;
                    }
                case AbilityEffectType.CareerAbilityEffect:
                    {
                        frame.origin = casterAgent.GetChestGlobalPosition();
                        frame.rotation = casterAgent.LookFrame.rotation;
                        if (this.IsGroundAbility())
                        {
                            frame = new MatrixFrame(Mat3.Identity, Crosshair.Position);
                        }
                        break;
                    }
                case AbilityEffectType.ArtilleryPlacement:
                case AbilityEffectType.Hex:
                case AbilityEffectType.Augment:
                case AbilityEffectType.TacticalReposition:
                case AbilityEffectType.Heal:
                case AbilityEffectType.Summoning:
                    {
                        frame = new MatrixFrame(Mat3.Identity, Crosshair.Position);
                        break;
                    }
                case AbilityEffectType.Bombardment:
                    {
                        frame = new MatrixFrame(Mat3.Identity, Crosshair.Position);
                        frame.origin.z += Template.Offset;
                        break;
                    }
                default:
                    break;
            }

            return frame;
        }

        private MatrixFrame CalculateAICastMatrixFrame(Agent casterAgent)
        {
            var frame = casterAgent.LookFrame;
            var wizardAIComponent = casterAgent.GetComponent<WizardAIComponent>();
            var target = wizardAIComponent.CurrentCastingBehavior.CurrentTarget;

            switch (Template.AbilityEffectType)
            {
                case AbilityEffectType.Missile:
                case AbilityEffectType.SeekerMissile:
                    {
                        frame = frame.Elevate(casterAgent.GetEyeGlobalHeight()).Advance(Template.Offset);
                        frame.rotation = wizardAIComponent.CurrentCastingBehavior.CalculateSpellRotation(target.GetPositionPrioritizeCalculated(), frame.origin);
                        break;
                    }
                case AbilityEffectType.Blast:
                    {
                        frame = new MatrixFrame(frame.rotation, target.GetPositionPrioritizeCalculated()).Advance(-Template.Offset).Elevate(1);
                        break;
                    }
                case AbilityEffectType.Wind:
                case AbilityEffectType.Vortex:
                    {
                        frame = new MatrixFrame(Mat3.Identity, target.GetPositionPrioritizeCalculated())
                        {
                            rotation = casterAgent.Frame.rotation
                        };
                        break;
                    }
                case AbilityEffectType.CareerAbilityEffect:
                    {
                        frame.origin = casterAgent.GetChestGlobalPosition();
                        frame.rotation = casterAgent.LookFrame.rotation;
                        break;
                    }
                case AbilityEffectType.ArtilleryPlacement:
                    {
                        frame = new MatrixFrame(Mat3.Identity, target.GetPositionPrioritizeCalculated());
                        target.SelectedWorldPosition = Vec3.Zero;
                        break;
                    }
                case AbilityEffectType.Hex:
                case AbilityEffectType.Augment:
                case AbilityEffectType.TacticalReposition:
                case AbilityEffectType.Heal:
                case AbilityEffectType.Summoning:
                case AbilityEffectType.Bombardment:
                    {
                        frame = new MatrixFrame(Mat3.Identity, target.GetPositionPrioritizeCalculated());
                        break;
                    }
                default:
                    break;
            }

            if (IsGroundAbility())
            {
                frame.origin.z = Mission.Current.Scene.GetGroundHeightAtPosition(frame.origin);
                if (Template.AbilityEffectType == AbilityEffectType.Bombardment)
                {
                    frame.origin.z += Template.Offset;
                }
            }

            return frame;
        }

        protected GameEntity SpawnEntity()
        {
            GameEntity entity = null;
            if (Template.ParticleEffectPrefab != "none")
            {
                entity = GameEntity.Instantiate(Mission.Current.Scene, Template.ParticleEffectPrefab, true);
            }

            if (entity == null)
            {
                entity = GameEntity.CreateEmpty(Mission.Current.Scene);
            }

            return entity;
        }

        private void AddLight(ref GameEntity entity)
        {
            if (Template.HasLight)
            {
                var light = Light.CreatePointLight(Template.LightRadius);
                light.Intensity = Template.LightIntensity;
                light.LightColor = Template.LightColorRGB;
                light.SetShadowType(Light.ShadowType.DynamicShadow);
                light.ShadowEnabled = Template.ShadowCastEnabled;
                light.SetLightFlicker(Template.LightFlickeringMagnitude, Template.LightFlickeringInterval);
                light.Frame = MatrixFrame.Identity;
                light.SetVisibility(true);
                entity.AddLight(light);
            }
        }

        private void AddPhysics(ref GameEntity entity)
        {
            using (new TWSharedMutexWriteLock(Scene.PhysicsAndRayCastLock))
            {
                var mass = 1;
                entity.AddSphereAsBody(Vec3.Zero, Template.Radius, BodyFlags.Moveable | BodyFlags.DoNotCollideWithRaycast);
                entity.AddPhysics(mass, entity.CenterOfMass, entity.GetBodyShape(), Vec3.Zero, Vec3.Zero, PhysicsMaterial.GetFromName("missile"), false, -1);
                //entity.SetPhysicsState(true, false);
            }
        }

        protected void AddBehaviour(ref GameEntity entity, Agent casterAgent)
        {
            switch (Template.AbilityEffectType)
            {
                case AbilityEffectType.Projectile:
                    AddExactBehaviour<ProjectileScript>(ref entity, casterAgent);
                    break;
                case AbilityEffectType.SeekerMissile:
                case AbilityEffectType.Missile:
                    AddExactBehaviour<MissileScript>(ref entity, casterAgent);
                    break;
                case AbilityEffectType.Wind:
                    AddExactBehaviour<WindScript>(ref entity, casterAgent);
                    break;
                case AbilityEffectType.Heal:
                    AddExactBehaviour<HealScript>(ref entity, casterAgent);
                    break;
                case AbilityEffectType.Augment:
                case AbilityEffectType.TacticalReposition:
                    AddExactBehaviour<AugmentScript>(ref entity, casterAgent);
                    break;
                case AbilityEffectType.Summoning:
                    AddExactBehaviour<SummoningScript>(ref entity, casterAgent);
                    break;
                case AbilityEffectType.
                    CareerAbilityEffect:
                    AddExactBehaviour<CareerAbilityScript>(ref entity, casterAgent);
                    break;
                case AbilityEffectType.Hex:
                    AddExactBehaviour<HexScript>(ref entity, casterAgent);
                    break;
                case AbilityEffectType.Vortex:
                    AddExactBehaviour<VortexScript>(ref entity, casterAgent);
                    break;
                case AbilityEffectType.Blast:
                    AddExactBehaviour<BlastScript>(ref entity, casterAgent);
                    break;
                case AbilityEffectType.Bombardment:
                    AddExactBehaviour<BombardmentScript>(ref entity, casterAgent);
                    break;
                case AbilityEffectType.ArtilleryPlacement:
                    AddExactBehaviour<ArtilleryPlacementScript>(ref entity, casterAgent);
                    break;
                case AbilityEffectType.TimeWarpEffect:
                    AddExactBehaviour<TimeWarpScript>(ref entity, casterAgent);
                    break;
            }

            if (IsSingleTarget)
            {
                if (casterAgent.IsAIControlled)
                {
                    var wizardAIComponent = casterAgent.GetComponent<WizardAIComponent>();
                    var target = wizardAIComponent.CurrentCastingBehavior.CurrentTarget;
                    AbilityScript.SetExplicitTargetAgents([target.Agent]);
                }
                else if (Crosshair.CrosshairType == CrosshairType.SingleTarget)
                {
                    AbilityScript.SetExplicitTargetAgents([(Crosshair as SingleTargetCrosshair).CachedTarget]);
                }
            }

            if (Template.SeekerParameters != null)
            {
                if (casterAgent.IsAIControlled)
                {
                    AbilityScript.SetTargetSeeking(casterAgent.GetComponent<WizardAIComponent>().CurrentCastingBehavior.CurrentTarget, Template.SeekerParameters);
                }
                else
                {
                    Target target;
                    if (Crosshair.CrosshairType == CrosshairType.SingleTarget)
                    {
                        target = new Target { Agent = (Crosshair as SingleTargetCrosshair).CachedTarget };
                    }
                    else
                    {
                        target = new Target { Formation = casterAgent.Formation.CachedClosestEnemyFormation.Formation };
                    }

                    AbilityScript.SetTargetSeeking(target, Template.SeekerParameters);
                }
            }
        }

        protected virtual void AddExactBehaviour<TAbilityScript>(ref GameEntity parentEntity, Agent casterAgent)
            where TAbilityScript : AbilityScript
        {
            parentEntity.CreateAndAddScriptComponent(typeof(TAbilityScript).Name, false);
            AbilityScript = parentEntity.GetFirstScriptOfType<TAbilityScript>();
            var prefabEntity = SpawnEntity();
            parentEntity.AddChild(prefabEntity);
            AbilityScript?.Initialize(this, ref parentEntity);
            AbilityScript?.SetCasterAgent(casterAgent);
            parentEntity.CallScriptCallbacks(true);
        }

        private void SetAnimationAction(Agent casterAgent)
        {
            if (Template.AnimationActionName != "none")
            {
                casterAgent.SetActionChannel(1, ActionIndexCache.Create(Template.AnimationActionName));
            }
        }

        public void SetCrosshair(AbilityCrosshair crosshair)
        {
            Crosshair = crosshair;
        }

        public void Dispose()
        {
            _timer.Dispose();
            _timer = null;
            Template = null;
            OnCastComplete = null;
            OnCastStart = null;
        }

        private bool IsRightAngleToCast()
        {
            if (Agent.Main.HasMount)
            {
                double xa = Agent.Main.LookDirection.X;
                double ya = Agent.Main.LookDirection.Y;
                double xb = Agent.Main.GetMovementDirection().X;
                double yb = Agent.Main.GetMovementDirection().Y;

                double angle = Math.Acos((xa * xb + ya * yb) / (Math.Sqrt(Math.Pow(xa, 2) + Math.Pow(ya, 2)) * Math.Sqrt(Math.Pow(xb, 2) + Math.Pow(yb, 2))));

                return true ? angle < 1.4 : angle >= 1.4;
            }
            else
            {
                return true;
            }
        }
    }
}