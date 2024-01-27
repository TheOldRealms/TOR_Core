using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.BattleMechanics.AI.Decision;
using TOR_Core.BattleMechanics.TriggeredEffect;

namespace TOR_Core.AbilitySystem.Scripts
{
    public abstract class AbilityScript : ScriptComponentBehavior
    {
        private Ability _ability;
        private int _soundIndex = -1;
        private SoundEvent _sound;
        private Agent _casterAgent;
        private float _abilityLife = -1;
        private float _timeSinceLastTick = 0;
        private bool _hasCollided;
        private bool _hasTickedOnce;
        private bool _hasTriggered;
        private float _minArmingTimeForCollision = 0.1f;
        private bool _canCollide;
        private SeekerController _controller;
        private bool _soundStarted;
        private MBList<Agent> _targetAgents;
        private float _additionalDuration = 0;
        private Vec3 _previousFrameOrigin = Vec3.Zero;
        private bool _lifeTimeExpired;

        public Agent CasterAgent => _casterAgent;
        public MBReadOnlyList<Agent> ExplicitTargetAgents
        {
            get
            {
                if (_targetAgents == null) return new MBReadOnlyList<Agent>();
                else return new MBReadOnlyList<Agent>(_targetAgents);
            }
        }
        public Ability Ability => _ability;
        public bool IsFading { get; private set; }
        public Vec3 CurrentGlobalPosition => GameEntity.GetGlobalFrame().origin;
        public Vec3 LastFrameGlobalPosition => _previousFrameOrigin;
        public bool HasTickedOnce => _hasTickedOnce;

        public void SetTargetSeeking(Target target, SeekerParameters parameters) => _controller = new SeekerController(target, parameters);

        public void SetCasterAgent(Agent agent) => _casterAgent = agent;
        
        public void SetExplicitTargetAgents(MBList<Agent> agents) => _targetAgents = agents;

        protected override bool MovesEntity() => true;

        protected virtual bool ShouldMove()
        {
            return _ability.Template.AbilityEffectType == AbilityEffectType.Missile ||
                   _ability.Template.AbilityEffectType == AbilityEffectType.SeekerMissile ||
                   _ability.Template.AbilityEffectType == AbilityEffectType.Vortex ||
                   _ability.Template.AbilityEffectType == AbilityEffectType.Wind;
        }

        private bool IsSingleTarget() => _ability.Template.AbilityTargetType == AbilityTargetType.SingleAlly || _ability.Template.AbilityTargetType == AbilityTargetType.SingleEnemy;

        protected override void OnInit()
        {
            SetScriptComponentToTick(GetTickRequirement());
        }

        public override TickRequirement GetTickRequirement()
        {
            return TickRequirement.Tick;
        }

        public virtual void Initialize(Ability ability)
        {
            _ability = ability;
            if (_ability.Template.SoundEffectToPlay != "none" && _ability.Template.SoundEffectToPlay != null)
            {
                _soundIndex = SoundEvent.GetEventIdFromString(_ability.Template.SoundEffectToPlay);
                _sound = SoundEvent.CreateEvent(_soundIndex, Scene);
            }
        }

        protected virtual void OnBeforeTick(float dt) { } 
        protected virtual void OnAfterTick(float dt) { }

        protected sealed override void OnTick(float dt)
        {
            if (Mission.Current.CurrentState != Mission.State.Continuing || 
                Mission.Current.MissionEnded || 
                Mission.Current.IsMissionEnding ||
                Mission.Current.MissionIsEnding)
            {
                Stop(); 
                return;
            }

            if (_ability == null) return;

            OnBeforeTick(dt);

            _timeSinceLastTick += dt;
            UpdateLifeTime(dt);

            if (IsFading) return;

            var frame = GameEntity.GetGlobalFrame();
            if (_controller != null) frame = _controller.CalculateRotatedFrame(frame, dt);
            UpdateSound(frame.origin);

            if (_ability.Template.TriggerType == TriggerType.OnCollision && CollidedWithAgent())
            {
                HandleCollision(frame.origin, frame.origin.NormalizedCopy());
            }
            else if (_ability.Template.TriggerType == TriggerType.EveryTick && !_hasTriggered)
            {
                TriggerEffects(frame.origin, frame.origin.NormalizedCopy());
                _hasTriggered = true;
            }
            else if (_ability.Template.TriggerType == TriggerType.EveryTick && _timeSinceLastTick > _ability.Template.TickInterval)
            {
                _timeSinceLastTick = 0;
                TriggerEffects(frame.origin, frame.origin.NormalizedCopy());
            }
            else if (_ability.Template.TriggerType == TriggerType.TickOnce && _abilityLife > _ability.Template.TickInterval && !_hasTriggered)
            {
                var position = frame.origin;
                var normal = frame.origin.NormalizedCopy();
                if(_ability.Template.AbilityEffectType == AbilityEffectType.Blast)
                {
                    position = frame.Advance(_ability.Template.Offset).origin;
                    normal = frame.rotation.f.NormalizedCopy();
                }
                TriggerEffects(position, normal);
                _hasTriggered = true;
            }
            _hasTickedOnce = true;

            if (ShouldMove())
            {
                UpdatePosition(frame, dt);
            }

            OnAfterTick(dt);

            if (_lifeTimeExpired)
            {
                Stop();
            }
        }

        private void UpdatePosition(MatrixFrame frame, float dt)
        {
            _previousFrameOrigin = frame.origin;
            var newframe = GetNextGlobalFrame(frame, dt);
            GameEntity.SetGlobalFrame(newframe);
            if (GameEntity.GetBodyShape() != null) GameEntity.GetBodyShape().ManualInvalidate();
        }

        protected virtual MatrixFrame GetNextGlobalFrame(MatrixFrame oldFrame, float dt)
        {
            return oldFrame.Advance(_ability.Template.BaseMovementSpeed * dt);
        }

        private void UpdateLifeTime(float dt)
        {
            if (_abilityLife < 0) _abilityLife = 0;
            else _abilityLife += dt;
            if (_ability != null)
            {
                if (_abilityLife > (_ability.Template.Duration + _additionalDuration) && !IsFading)
                {
                    _lifeTimeExpired = true;
                }
            }
            if (_abilityLife > _minArmingTimeForCollision)
            {
                _canCollide = true;
            }
        }

        public void ExtendLifeTime(float time)
        {
            _additionalDuration += time;
        }

        private void UpdateSound(Vec3 position)
        {
            if(_sound != null)
            {
                _sound.SetPosition(position);
                if (IsSoundPlaying()) return;
                else
                {
                    if (!_soundStarted)
                    {
                        _sound.Play();
                        _soundStarted = true;
                    }
                    else if (_ability.Template.ShouldSoundLoopOverDuration)
                    {
                        _sound.Play();
                    }
                    else
                    {
                        _sound.Release();
                        _sound = null;
                    }
                }
            }
        }

        private bool IsSoundPlaying()
        {
            return _sound != null && _sound.IsValid && _sound.IsPlaying();
        }

        protected virtual bool CollidedWithAgent()
        {
            if(!_canCollide) return false;
            var collisionRadius = _ability.Template.Radius + 1;
            MBList<Agent> agents = new MBList<Agent>();
            agents = Mission.Current.GetNearbyAgents(GameEntity.GetGlobalFrame().origin.AsVec2, collisionRadius, agents);
            return agents.Any(agent => agent != _casterAgent && Math.Abs(GameEntity.GetGlobalFrame().origin.Z - agent.Position.Z) < collisionRadius);
        }

        protected sealed override void OnPhysicsCollision(ref PhysicsContact contact)
        {
            base.OnPhysicsCollision(ref contact);
            if (_ability.Template.TriggerType == TriggerType.OnCollision && _canCollide)
            {
                HandleCollision(contact.ContactPair0.Contact0.Position, contact.ContactPair0.Contact0.Normal);
            }
        }

        protected virtual void HandleCollision(Vec3 position, Vec3 normal)
        {
            if (!_hasTickedOnce) return;
            if (!_hasCollided && position.IsValid && position.IsNonZero)
            {
                TriggerEffects(position, normal);
                _hasCollided = true;
                Stop();
            }
        }

        private void TriggerEffects(Vec3 position, Vec3 normal)
        {
            var effects = GetEffectsToTrigger();
            foreach(var effect in effects)
            {
                if (effect != null)
                {
                    if (_ability.Template.AbilityTargetType == AbilityTargetType.Self)
                    {
                        effect.Trigger(position, normal, _casterAgent, _ability.Template, new MBList<Agent>(1) { _casterAgent });
                    }
                    else if(_targetAgents != null && _targetAgents.Count() > 0)
                    {
                        effect.Trigger(position, normal, _casterAgent, _ability.Template, _targetAgents.ToMBList());
                    }
                    else effect.Trigger(position, normal, _casterAgent, _ability.Template);
                }
            }
        }

        protected virtual List<TriggeredEffect> GetEffectsToTrigger()
        {
            List<TriggeredEffect> effects = new List<TriggeredEffect>();
            if (_ability == null) return effects; 
            foreach(var effect in _ability.Template.AssociatedTriggeredEffectTemplates)
            {
                effects.Add(new TriggeredEffect(effect));
            }
            return effects;
        }

        protected sealed override void OnRemoved(int removeReason)
        {
            OnBeforeRemoved(removeReason);
            _sound?.Release();
            _sound = null;
            _ability = null;
            _casterAgent = null;
        }

        protected virtual void OnBeforeRemoved(int removeReason) { }

        public void Stop()
        {
            if(IsFading) return;
            if(GameEntity != null && Ability.Template.TriggerType == TriggerType.OnStop)
            {
                var frame = GameEntity.GetGlobalFrame();
                TriggerEffects(frame.origin, frame.origin.NormalizedCopy());
            }
            IsFading = true;
            GameEntity?.FadeOut(0.05f, true);
        }
    }
}
