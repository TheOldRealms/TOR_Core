using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.AI.ArtilleryAI;
using TOR_Core.BattleMechanics.AI.TeamAI.FormationBehavior;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.Artillery
{
    public class ArtilleryRangedSiegeWeapon : BaseFieldSiegeWeapon
    {
        #region animations
        private ActionIndexCache _idleAnimationActionIndex;
        private ActionIndexCache _shootAnimationActionIndex;
        private ActionIndexCache _reload1AnimationActionIndex;
        private ActionIndexCache _reload2AnimationActionIndex;
        private ActionIndexCache _rotateLeftAnimationActionIndex;
        private ActionIndexCache _rotateRightAnimationActionIndex;
        private ActionIndexCache _loadAmmoBeginAnimationActionIndex;
        private ActionIndexCache _loadAmmoEndAnimationActionIndex;
        private ActionIndexCache _reload2IdleActionIndex;
        private static readonly ActionIndexCache act_pickup_boulder_begin = ActionIndexCache.Create("act_pickup_boulder_begin");
        private static readonly ActionIndexCache act_pickup_boulder_end = ActionIndexCache.Create("act_pickup_boulder_end");

        public string IdleActionName;
        public string ShootActionName;
        public string Reload1ActionName;
        public string Reload2ActionName;
        public string RotateLeftActionName;
        public string RotateRightActionName;
        public string LoadAmmoBeginActionName;
        public string LoadAmmoEndActionName;
        public string Reload2IdleActionName;
        #endregion

        private readonly string _barrelTag = "Barrel";
        private readonly string _baseTag = "Battery_Base";
        private readonly string _leftWheelTag = "Wheel_L";
        private readonly string _rightWheelTag = "Wheel_R";
        public string FireSoundID = "mortar_shot_1";
        public string FireSoundID2 = "mortar_shot_2";
        public float RecoilDuration = 0.1f;
        public float Recoil2Duration = 0.8f;
        public string DisplayName = "Artillery";
        public float BaseMuzzleVelocity = 40f;
        private int _fireSoundIndex;
        private int _fireSoundIndex2;
        private SynchedMissionObject _body;
        private SynchedMissionObject _barrel;
        private SynchedMissionObject _wheel_R;
        private SynchedMissionObject _wheel_L;
        private readonly string _leftGearTag = "Gear_L";
        private readonly string _rightGearTag = "Gear_R";
        private List<SynchedMissionObject> _gearsLeft = new List<SynchedMissionObject>();
        private List<SynchedMissionObject> _gearsRight = new List<SynchedMissionObject>();
        private bool _isDwarfCannon = false;
        private float _verticalOffsetAngle;
        private MatrixFrame _barrelInitialLocalFrame;
        private Agent _lastLoaderAgent;
        private StandingPoint _waitStandingPoint;
        private Timer _timer;
        private SoundEvent _fireSound;
        private MatrixFrame _currentSlideBackFrameOrig;
        private MatrixFrame _currentSlideBackFrame;
        private float _lastRecoilTimeStart;
        private float _currentRecoilTimer;
        private bool _isRotating;
        private int _rotationDirection = 0;
        private float _lastCurrentDirection;
        
        protected override float ShootingSpeed => BaseMuzzleVelocity;
        public override float ProjectileVelocity => ShootingSpeed;
        protected override Vec3 ShootingDirection => Projectile.GameEntity.GetGlobalFrame().rotation.f;
        
        protected override float MaximumBallisticError => 0.2f;

        public override UsableMachineAIBase CreateAIBehaviorObject()
        {
            return new FieldSiegeWeaponAI(this);
        }
        protected override void OnInit()
        {
            CollectEntities();
            base.OnInit();
            Projectile.SetVisibleSynched(false);
            if(MissileStartingPositionEntityForSimulation == null)
            {
                List<WeakGameEntity> entities = new List<WeakGameEntity>();
                GameEntity.GetChildrenRecursive(ref entities);
                var weakEntity = Enumerable.FirstOrDefault(entities, x => x.Name == "projectile_leaving_position");
                MissileStartingPositionEntityForSimulation = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(weakEntity);
            }
            foreach(var sp in StandingPoints)
            {
                if (sp.GameEntity.HasTag(WaitStandingPointTag))
                {
                    _waitStandingPoint = sp;
                    break;
                }
            }
            TimeGapBetweenShootActionAndProjectileLeaving = 0f;
            TimeGapBetweenShootingEndAndReloadingStart = 0f;
            EnemyRangeToStopUsing = 5f;
            PilotStandingPoint.AddComponent(new ClearHandInverseKinematicsOnStopUsageComponent());
            _barrelInitialLocalFrame = _barrel.GameEntity.GetFrame();
            Vec3 v = new Vec3(0f, ShootingDirection.AsVec2.Length, ShootingDirection.Z);
            _verticalOffsetAngle = Vec3.AngleBetweenTwoVectors(v, Vec3.Forward);
            _lastCurrentDirection = CurrentDirection;
            ApplyAimChange();
        }

        protected override void OnTick(float dt)
        {
            CheckNullReloaderOriginalPoint();
            if (Target != null)
            {
                base.OnTick(dt);
            }

            HandleAnimations();
            HandleAmmoPickup();
            HandleAmmoLoad();
            CheckAmmoPointUsage();
            HandleWaitingTimer();
            UpdateRecoilEffect(dt);
            UpdateWheelRotation(dt);
            HandleAITeamUsage();
            EnsureCrewDetachedFromFormation();
        }

        private void CheckAmmoPointUsage()
        {
            if (ReloaderAgent == null)
            {
                ForceAmmoPointUsage();
            }
            if (ReloaderAgent!=null && ReloaderAgent.IsActive())
            {
                foreach (var sp in StandingPoints)
                {
                    if ((sp.HasUser && sp.UserAgent == ReloaderAgent) || (sp.HasAIMovingTo && sp.MovingAgent == ReloaderAgent))
                    {
                        return;
                    }
                }
            }
            ForceAmmoPointUsage();
        }
        
        

        private void EnsureCrewDetachedFromFormation()
        {
            // Detach crew members from formation orders while they're actively working on the cannon
            // This prevents them from running off when the formation receives attack/move orders
            foreach (var sp in StandingPoints)
            {
                // Skip the wait standing point - agents there can respond to orders
                if (sp.GameEntity.HasTag(WaitStandingPointTag)) continue;

                if (sp.HasUser && sp.UserAgent != null && sp.UserAgent.IsAIControlled)
                {
                    var agent = sp.UserAgent;
                    if (agent.Formation != null && !agent.IsDetachedFromFormation)
                    {
                        agent.Formation.DetachUnit(agent, false);
                    }
                }
            }

            // Also detach the ReloaderAgent if they exist and are moving to a point
            if (ReloaderAgent != null && ReloaderAgent.IsAIControlled && ReloaderAgent.Formation != null && !ReloaderAgent.IsDetachedFromFormation)
            {
                ReloaderAgent.Formation.DetachUnit(ReloaderAgent, false);
            }
        }
        
        private void HandleAITeamUsage()
        {


            if (!Team?.IsPlayerTeam ?? false)
            {
                if (UserFormations.Count > 0 && UserFormations.All(formation => formation.Index != (int) TORFormationClass.Artillery))
                {
                    UserFormations[0]?.StopUsingMachine(this);
                }

                if (UserFormations.Count == 0)
                {
                    var formation = Team.FormationsIncludingSpecialAndEmpty.ToList()
                        .FirstOrDefault(form => form.Index == (int)TORFormationClass.Artillery);
                        formation.StartUsingMachine(this);



                }
            }
            else if(Team?.IsPlayerTeam ?? false)
            {
                if (UserFormations.Count == 0)
                {
                    var form = Team.GetFormations().ToList().FirstOrDefault(formation => formation.Arrangement.GetAllUnits().FindAll(unit => ((Agent)unit).HasAttribute("ArtilleryCrew")).Count() > 2);
                    if (form != null) form.StartUsingMachine(this, true);
                }
            }
        }

        private void CheckNullReloaderOriginalPoint()
        {
            if (ReloaderAgentOriginalPoint == null && ReloaderAgent != null)
            {
                TORCommon.Say($"[Artillery DEBUG] CheckNullReloaderOriginalPoint: Clearing ReloaderAgent '{ReloaderAgent.Name}' because OriginalPoint is null");
                ReloaderAgent.StopUsingGameObject(true);
                ReloaderAgent = null;
            }
        }

        private void HandleWaitingTimer()
        {
            if(State == WeaponState.WaitingBeforeIdle)
            {
                if(_timer != null && _timer.Check(Mission.Current.CurrentTime))
                {
                    _timer = null;
                    State = WeaponState.Idle;
                }
            }
        }

        private void HandleAnimations()
        {
            return;
        }

        private void HandleAmmoLoad()
        {
            if (LoadAmmoStandingPoint != null && LoadAmmoStandingPoint.HasUser)
            {
                var user = LoadAmmoStandingPoint.UserAgent;
                _lastLoaderAgent = user;
                if (user.GetCurrentAction(1) == _loadAmmoEndAnimationActionIndex)
                {
                    EquipmentIndex wieldedItemIndex = user.GetPrimaryWieldedItemIndex();
                    if (wieldedItemIndex != EquipmentIndex.None && user.Equipment[wieldedItemIndex].CurrentUsageItem.WeaponClass == OriginalMissileItem.PrimaryWeapon.WeaponClass)
                    {
                        TORCommon.Say($"[Artillery DEBUG] HandleAmmoLoad: '{user.Name}' completed loading, transitioning to WaitingBeforeIdle");
                        user.RemoveEquippedWeapon(wieldedItemIndex);
                        user.StopUsingGameObject(true, Agent.StopUsingGameObjectFlags.None);
                        State = WeaponState.WaitingBeforeIdle;
                    }
                    user.StopUsingGameObject(true);
                    user.TryRemoveAllDetachmentScores();
                }
                else
                {
                    if (user.GetCurrentAction(1) != _loadAmmoBeginAnimationActionIndex && !LoadAmmoStandingPoint.UserAgent.SetActionChannel(1, _loadAmmoBeginAnimationActionIndex))
                    {
                        for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
                        {
                            if (!user.Equipment[equipmentIndex].IsEmpty && user.Equipment[equipmentIndex].CurrentUsageItem.WeaponClass == OriginalMissileItem.PrimaryWeapon.WeaponClass)
                            {
                                user.RemoveEquippedWeapon(equipmentIndex);
                            }
                        }
                        user.StopUsingGameObject(true);
                        user.TryRemoveAllDetachmentScores();
                    }
                }
            }
        }

        private void HandleAmmoPickup()
        {
            foreach (var sp in AmmoPickUpPoints)
            {
                if (sp is StandingPointWithWeaponRequirement)
                {
                    var point = sp as StandingPointWithWeaponRequirement;
                    if (point.HasUser)
                    {
                        var user = point.UserAgent;
                        var action = user.GetCurrentAction(1);
                        if (!(action == act_pickup_boulder_begin))
                        {
                            if (action == act_pickup_boulder_end)
                            {
                                TORCommon.Say($"[Artillery DEBUG] HandleAmmoPickup: '{user.Name}' finished picking up ammo. LoadAmmoPoint.HasUser={LoadAmmoStandingPoint.HasUser}, IsDeactivated={LoadAmmoStandingPoint.IsDeactivated}");
                                MissionWeapon missionWeapon = new MissionWeapon(LoadedMissileItem, null, null, 1);
                                user.EquipWeaponToExtraSlotAndWield(ref missionWeapon);
                                user.StopUsingGameObject(true, Agent.StopUsingGameObjectFlags.None);
                                if (user.IsAIControlled)
                                {
                                    if (!LoadAmmoStandingPoint.HasUser && !LoadAmmoStandingPoint.IsDeactivated)
                                    {
                                        TORCommon.Say($"[Artillery DEBUG] HandleAmmoPickup: Sending '{user.Name}' to LoadAmmoStandingPoint");
                                        user.AIMoveToGameObjectEnable(LoadAmmoStandingPoint, this, Agent.AIScriptedFrameFlags.NoAttack);
                                    }
                                    else if (ReloaderAgentOriginalPoint != null && !ReloaderAgentOriginalPoint.HasUser && !ReloaderAgentOriginalPoint.HasAIMovingTo)
                                    {
                                        TORCommon.Say($"[Artillery DEBUG] HandleAmmoPickup: Sending '{user.Name}' to ReloaderAgentOriginalPoint");
                                        user.AIMoveToGameObjectEnable(ReloaderAgentOriginalPoint, this, Agent.AIScriptedFrameFlags.NoAttack);
                                    }
                                    else
                                    {
                                        // DEBUG: Log why we're clearing the reloader
                                        string reason = ReloaderAgentOriginalPoint == null
                                            ? "OriginalPoint is null"
                                            : (ReloaderAgentOriginalPoint.HasUser
                                                ? $"OriginalPoint has user '{ReloaderAgentOriginalPoint.UserAgent?.Name}'"
                                                : $"OriginalPoint has AI moving to it");
                                        TORCommon.Say($"[Artillery DEBUG] HandleAmmoPickup ELSE: User='{user.Name}', ReloaderAgent='{ReloaderAgent?.Name}', Reason: {reason}");

                                        Agent reloaderAgent = ReloaderAgent;
                                        if (reloaderAgent != null)
                                        {
                                            Formation formation = reloaderAgent.Formation;
                                            if (formation != null)
                                            {
                                                TORCommon.Say($"[Artillery DEBUG] HandleAmmoPickup: Detaching ReloaderAgent '{reloaderAgent.Name}' back to formation");
                                                formation.AttachUnit(ReloaderAgent);
                                            }
                                        }
                                        ReloaderAgent = null;
                                    }
                                }
                            }
                            else if (!user.SetActionChannel(1, act_pickup_boulder_begin))
                            {
                                user.StopUsingGameObject(true);
                            }
                        }
                    }
                }
            }
        }

        protected override void OnRangedSiegeWeaponStateChange()
        {
            base.OnRangedSiegeWeaponStateChange();
            switch (State)
            {
                case WeaponState.Shooting:
                    {
                        PlayFireProjectileEffects();
                        State = WeaponState.WaitingAfterShooting;
                        return;
                    }
                case WeaponState.WaitingAfterShooting:
                    {
                        DoSlideBack();
                        return;
                    }
                case WeaponState.WaitingBeforeIdle:
                    {
                        SendLoaderAgentToWaitingPoint(); 
                        SetWaitingTimer();
                       //ClearFiringArea();
                        return;
                    }
                case WeaponState.LoadingAmmo:
                    {
                        SetActivationWaitingPoint(false);
                        return;
                    }
                case WeaponState.Idle:
                    {
                        return;
                    }
            }
        }

        private void ClearFiringArea()
        {
            // Find detached agents near the loading point who aren't actively using the cannon
            // and re-attach them to their formation so they return to normal behavior
            if (Mission.Current == null) return;
            if (LoadAmmoStandingPoint == null) return;

            var loadingPos = LoadAmmoStandingPoint.GameEntity.GlobalPosition;
            float checkRadius = 5f;

            var nearbyAgents = new MBList<Agent>();
            Mission.Current.GetNearbyAgents(loadingPos.AsVec2, checkRadius, nearbyAgents);

            foreach (var agent in nearbyAgents)
            {
                if (agent == null || !agent.IsActive() || !agent.IsAIControlled) continue;
                if (agent == PilotAgent) continue;
                if (agent.Formation == null) continue;

                // Check if agent is actively using this cannon's standing points - if so, skip
                bool isUsingCannon = false;
                foreach (var sp in StandingPoints)
                {
                    if ((sp.HasUser && sp.UserAgent == agent) || (sp.HasAIMovingTo && sp.MovingAgent == agent))
                    {
                        isUsingCannon = true;
                        break;
                    }
                }
                if (isUsingCannon) continue;

                // If agent is detached from formation and not using the cannon, re-attach them
                
                if(agent == this.ReloaderAgent) return;
                
                if (!agent.IsDetachedFromFormation)
                {
                    agent.SetShouldCatchUpWithFormation(true);
                    
                }
            }
        }

        private void SendLoaderAgentToWaitingPoint()
        {
            if(_lastLoaderAgent != null && _waitStandingPoint != null)
            {
                SetActivationWaitingPoint(true);
                _lastLoaderAgent.AIMoveToGameObjectEnable(_waitStandingPoint, this, Agent.AIScriptedFrameFlags.NoAttack);
            }
        }

        protected override void ApplyAimChange()
        {
            base.ApplyAimChange();
            MatrixFrame barrelFrame = _barrelInitialLocalFrame;
            barrelFrame.rotation.RotateAboutSide(-CurrentReleaseAngle + _verticalOffsetAngle);
            _barrel.GameEntity.SetFrame(ref barrelFrame);
        }

        protected override void ApplyCurrentDirectionToEntity()
        {
            if(_lastCurrentDirection != CurrentDirection)
            {
                _isRotating = true;
                if (CurrentDirection - _lastCurrentDirection > 0) _rotationDirection = 1;
                else if(CurrentDirection - _lastCurrentDirection < 0) _rotationDirection = -1;
                else _rotationDirection = 0;
            }
            else
            {
                _isRotating = false;
                _rotationDirection = 0;
            }
            base.ApplyCurrentDirectionToEntity();
            _lastCurrentDirection = CurrentDirection;
        }

        private void CollectEntities()
        {
            _body = GameEntity.CollectScriptComponentsWithTagIncludingChildrenRecursive<SynchedMissionObject>(_baseTag)[0];
            _barrel = GameEntity.CollectScriptComponentsWithTagIncludingChildrenRecursive<SynchedMissionObject>(_barrelTag)[0];
            _wheel_L = GameEntity.CollectScriptComponentsWithTagIncludingChildrenRecursive<SynchedMissionObject>(_leftWheelTag)[0];
            _wheel_R = GameEntity.CollectScriptComponentsWithTagIncludingChildrenRecursive<SynchedMissionObject>(_rightWheelTag)[0];
            RotationObject = _body;

            // Collect gears for dwarf cannon
            CollectGears();
        }

        private void CollectGears()
        {
            _gearsLeft.Clear();
            _gearsRight.Clear();

            var leftGears = GameEntity.CollectScriptComponentsWithTagIncludingChildrenRecursive<SynchedMissionObject>(_leftGearTag);
            var rightGears = GameEntity.CollectScriptComponentsWithTagIncludingChildrenRecursive<SynchedMissionObject>(_rightGearTag);

            _gearsLeft.AddRange(leftGears);
            _gearsRight.AddRange(rightGears);

            _isDwarfCannon = _gearsLeft.Count > 0 || _gearsRight.Count > 0;
        }

        public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
        {
            TextObject textObject;
            if (usableGameObject.GameEntity.HasTag(AmmoLoadTag))
            {
                textObject = TORTextHelper.GetTextObject("tor_artillery_reload", "{KEY} Reload");
            }
            else if (usableGameObject.GameEntity.HasTag(AmmoPickUpTag))
            {
                textObject = TORTextHelper.GetTextObject("tor_artillery_pick_up", "{KEY} Pick Up");
            }
            else
            {
                textObject = TORTextHelper.GetTextObject("tor_artillery_use", "{KEY} Use");
            }
            textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
            return textObject;
        }

        public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
        {
            return new TextObject(DisplayName);
        }

        public override SiegeEngineType GetSiegeEngineType() => Side != BattleSideEnum.Attacker ? DefaultSiegeEngineTypes.Catapult : DefaultSiegeEngineTypes.Onager;

        public override TargetFlags GetTargetFlags()
        {
            TargetFlags targetFlags = (TargetFlags)(0 | 2 | 8 | 16);
            if (IsDestroyed || IsDeactivated)
                targetFlags |= TargetFlags.NotAThreat;
            if (Side == BattleSideEnum.Attacker && DebugSiegeBehavior.DebugDefendState == DebugSiegeBehavior.DebugStateDefender.DebugDefendersToMangonels)
                targetFlags |= TargetFlags.DebugThreat;
            if (Side == BattleSideEnum.Defender && DebugSiegeBehavior.DebugAttackState == DebugSiegeBehavior.DebugStateAttacker.DebugAttackersToMangonels)
                targetFlags |= TargetFlags.DebugThreat;
            return targetFlags;
        }

        public override float GetTargetValue(List<Vec3> weaponPos) => 40f * GetUserMultiplierOfWeapon() * GetDistanceMultiplierOfWeapon(weaponPos[0]) * GetHitPointMultiplierOfWeapon();

        public override float ProcessTargetValue(float baseValue, TargetFlags flags)
        {
            if (flags.HasAnyFlag(TargetFlags.NotAThreat))
            {
                return -1000f;
            }
            if (flags.HasAnyFlag(TargetFlags.IsSiegeEngine))
            {
                baseValue *= 0.2f;
            }
            if (flags.HasAnyFlag(TargetFlags.IsStructure))
            {
                baseValue *= 0.05f;
            }
            if (flags.HasAnyFlag(TargetFlags.DebugThreat))
            {
                baseValue *= 10000f;
            }
            return baseValue;
        }

        protected override void GetSoundEventIndices()
        {
            MoveSoundIndex = SoundEvent.GetEventIdFromString("event:/mission/siege/mangonel/move");
            _fireSoundIndex = SoundEvent.GetEventIdFromString(FireSoundID);
            _fireSoundIndex2 = SoundEvent.GetEventIdFromString(FireSoundID2);
        }

        protected override void SetActivationLoadAmmoPoint(bool activate)
        {
            LoadAmmoStandingPoint.SetIsDeactivatedSynched(!activate);
        }

        private void SetActivationWaitingPoint(bool activate)
        {
            _waitStandingPoint.SetIsDeactivatedSynched(!activate);
        }

        private void SetWaitingTimer()
        {
            _timer = new Timer(Mission.Current.CurrentTime, 2f, false);
        }

        protected override void RegisterAnimationParameters()
        {
            SkeletonOwnerObjects = new SynchedMissionObject[0];
            Skeletons = new Skeleton[0];
            _idleAnimationActionIndex = ActionIndexCache.Create(IdleActionName);
            _shootAnimationActionIndex = ActionIndexCache.Create(ShootActionName);
            _reload1AnimationActionIndex = ActionIndexCache.Create(Reload1ActionName);
            _reload2AnimationActionIndex = ActionIndexCache.Create(Reload2ActionName);
            _rotateLeftAnimationActionIndex = ActionIndexCache.Create(RotateLeftActionName);
            _rotateRightAnimationActionIndex = ActionIndexCache.Create(RotateRightActionName);
            _loadAmmoBeginAnimationActionIndex = ActionIndexCache.Create(LoadAmmoBeginActionName);
            _loadAmmoEndAnimationActionIndex = ActionIndexCache.Create(LoadAmmoEndActionName);
            _reload2IdleActionIndex = ActionIndexCache.Create(Reload2IdleActionName);
        }

        private void PlayFireProjectileEffects()
        {
            var frame = MissileStartingPositionEntityForSimulation.GetGlobalFrame();
            Mission.Current.AddParticleSystemBurstByName("psys_cannon_shot_1", frame, false);
            if (_fireSound == null || !_fireSound.IsValid)
            {
                if (MBRandom.RandomFloat > 0.5f)
                {
                    _fireSound = SoundEvent.CreateEvent(_fireSoundIndex, Scene);
                }
                else
                {
                    _fireSound = SoundEvent.CreateEvent(_fireSoundIndex2, Scene);
                }

                _fireSound.PlayInPosition(GameEntity.GlobalPosition);
            }
        }

        private void DoSlideBack()
        {
            var frame = _body.GameEntity.GetFrame();
            _currentSlideBackFrameOrig = frame;
            _currentSlideBackFrame = frame.Advance(0.6f);
            _lastRecoilTimeStart = Mission.Current.CurrentTime;
            _currentRecoilTimer = 0;
        }

        private void UpdateRecoilEffect(float dt)
        {
            if (State != WeaponState.WaitingAfterShooting) return;
            _currentRecoilTimer += dt;
            if (_currentRecoilTimer > RecoilDuration + Recoil2Duration)
            {
                State = WeaponState.LoadingAmmo;
                if (_fireSound != null)
                {
                    _fireSound.Stop();
                    _fireSound.Release();
                    _fireSound = null;
                }
                return;
            }

            if (_currentRecoilTimer < RecoilDuration)
            {
                var frame = _body.GameEntity.GetFrame();
                var amount = _currentRecoilTimer / RecoilDuration;
                frame = MatrixFrame.Lerp(_currentSlideBackFrameOrig, _currentSlideBackFrame, amount);
                if (amount < 0.5f)
                {
                    frame.origin.z = MBMath.Lerp(frame.origin.z, frame.origin.z + 0.2f, amount * 2);
                }
                else
                {
                    frame.origin.z = MBMath.Lerp(frame.origin.z, frame.origin.z + 0.2f, 1 - amount);
                }

                _body.GameEntity.SetFrame(ref frame);
                DoWheelRotation(dt, 1, -1, 5);
            }
            else if (_currentRecoilTimer < Recoil2Duration)
            {
                var frame = _body.GameEntity.GetFrame();
                var amount = (_currentRecoilTimer - RecoilDuration) / Recoil2Duration;
                frame = MatrixFrame.Lerp(_currentSlideBackFrame, _currentSlideBackFrameOrig, amount);
                _body.GameEntity.SetFrame(ref frame);
                DoWheelRotation(dt, -1, 1);
            }
        }

        private void DoWheelRotation(float dt, float leftwheeldirection, float rightwheeldirection, float speed = 1)
        {
            var frame = _wheel_L.GameEntity.GetFrame();
            frame.rotation.RotateAboutSide(leftwheeldirection * dt * speed);
            _wheel_L.GameEntity.SetFrame(ref frame);
            var frame2 = _wheel_R.GameEntity.GetFrame();
            frame2.rotation.RotateAboutSide(rightwheeldirection * dt * speed);
            _wheel_R.GameEntity.SetFrame(ref frame2);

            // Rotate gears for dwarf cannon
            if (_isDwarfCannon)
            {
                DoGearRotation(dt, leftwheeldirection, rightwheeldirection, speed);
            }
        }

        private void DoGearRotation(float dt, float leftDirection, float rightDirection, float speed = 1)
        {
            float gearSpeedMultiplier = 2f; // Gears spin faster than wheels

            foreach (var gear in _gearsLeft)
            {
                var gearFrame = gear.GameEntity.GetFrame();
                gearFrame.rotation.RotateAboutSide(leftDirection * dt * speed * gearSpeedMultiplier);
                gear.GameEntity.SetFrame(ref gearFrame);
            }

            foreach (var gear in _gearsRight)
            {
                var gearFrame = gear.GameEntity.GetFrame();
                gearFrame.rotation.RotateAboutSide(rightDirection * dt * speed * gearSpeedMultiplier);
                gear.GameEntity.SetFrame(ref gearFrame);
            }
        }

        private void UpdateWheelRotation(float dt)
        {
            if(!CanRotate()) _isRotating = false;
            if (_isRotating)
            {
                DoWheelRotation(dt, _rotationDirection, _rotationDirection);
            }
        }
    }
}
