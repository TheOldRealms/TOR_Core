using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.AI.ArtilleryAI;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics.Artillery
{
    public class FieldTrebuchet : BaseFieldSiegeWeapon
    {
		public override float DirectionRestriction => 1.3962635f;

		public override float ProjectileVelocity => ShootingSpeed;

        protected override float MaximumBallisticError => 0.1f;

        public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
		{
			TextObject textObject;
			if (usableGameObject.GameEntity.HasTag(this.AmmoPickUpTag))
			{
				textObject = new TextObject("{=bNYm3K6b}{KEY} Pick Up", null);
			}
			else if (usableGameObject.GameEntity.HasTag("reload"))
			{
				textObject = new TextObject((base.PilotStandingPoint == usableGameObject) ? "{=fEQAPJ2e}{KEY} Use" : "{=Na81xuXn}{KEY} Rearm", null);
			}
			else if (usableGameObject.GameEntity.HasTag("rotate"))
			{
				textObject = new TextObject("{=5wx4BF5h}{KEY} Rotate", null);
			}
			else if (usableGameObject.GameEntity.HasTag("ammoload"))
			{
				textObject = new TextObject("{=ibC4xPoo}{KEY} Load Ammo", null);
			}
			else
			{
				textObject = TextObject.Empty;
			}
			textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
			return textObject;
		}

		public override string GetDescriptionText(GameEntity gameEntity = null)
		{
			if (!gameEntity.HasTag(this.AmmoPickUpTag))
			{
				return new TextObject("{=4Skg9QhO}Trebuchet", null).ToString();
			}
			return new TextObject("{=pzfbPbWW}Boulder", null).ToString();
		}

		protected override void RegisterAnimationParameters()
		{
			this.SkeletonOwnerObjects = new SynchedMissionObject[3];
			this.Skeletons = new Skeleton[3];
			this.SkeletonNames = new string[3];
			this.FireAnimations = new string[3];
			this.FireAnimationIndices = new int[3];
			this.SetUpAnimations = new string[3];
			this.SetUpAnimationIndices = new int[3];
			this.SkeletonOwnerObjects[0] = this._body;
			this.Skeletons[0] = this._body.GameEntity.Skeleton;
			this.SkeletonNames[0] = "trebuchet_a_skeleton";
			this.FireAnimations[0] = this.BodyFireAnimation;
			this.FireAnimationIndices[0] = MBAnimation.GetAnimationIndexWithName(this.BodyFireAnimation);
			this.SetUpAnimations[0] = this.BodySetUpAnimation;
			this.SetUpAnimationIndices[0] = MBAnimation.GetAnimationIndexWithName(this.BodySetUpAnimation);
			this.SkeletonOwnerObjects[1] = this._sling;
			this.Skeletons[1] = this._sling.GameEntity.Skeleton;
			this.SkeletonNames[1] = "trebuchet_a_sling_skeleton";
			this.FireAnimations[1] = this.SlingFireAnimation;
			this.FireAnimationIndices[1] = MBAnimation.GetAnimationIndexWithName(this.SlingFireAnimation);
			this.SetUpAnimations[1] = this.SlingSetUpAnimation;
			this.SetUpAnimationIndices[1] = MBAnimation.GetAnimationIndexWithName(this.SlingSetUpAnimation);
			this.SkeletonOwnerObjects[2] = this._rope;
			this.Skeletons[2] = this._rope.GameEntity.Skeleton;
			this.SkeletonNames[2] = "trebuchet_a_rope_skeleton";
			this.FireAnimations[2] = this.RopeFireAnimation;
			this.FireAnimationIndices[2] = MBAnimation.GetAnimationIndexWithName(this.RopeFireAnimation);
			this.SetUpAnimations[2] = this.RopeSetUpAnimation;
			this.SetUpAnimationIndices[2] = MBAnimation.GetAnimationIndexWithName(this.RopeSetUpAnimation);
		}

		public override SiegeEngineType GetSiegeEngineType()
		{
			return DefaultSiegeEngineTypes.Trebuchet;
		}

		protected override void GetSoundEventIndices()
		{
			this.MoveSoundIndex = SoundEvent.GetEventIdFromString("event:/mission/siege/trebuchet/move");
			this.ReloadSoundIndex = SoundEvent.GetEventIdFromString("event:/mission/siege/trebuchet/reload");
			this.ReloadEndSoundIndex = SoundEvent.GetEventIdFromString("event:/mission/siege/trebuchet/reload_end");
		}

		protected override float ShootingSpeed => ProjectileSpeed;

		public override UsableMachineAIBase CreateAIBehaviorObject()
		{
			return new FieldSiegeWeaponAI(this);
		}

		protected override void OnInit()
		{
			List<SynchedMissionObject> list = base.GameEntity.CollectObjectsWithTag<SynchedMissionObject>("body");
			this._body = list[0];
			list = base.GameEntity.CollectObjectsWithTag<SynchedMissionObject>("sling");
			this._sling = list[0];
			list = base.GameEntity.CollectObjectsWithTag<SynchedMissionObject>("rope");
			this._rope = list[0];
			List<GameEntity> list2 = base.GameEntity.CollectChildrenEntitiesWithTag("vertical_adjuster");
			this._verticalAdjuster = list2[0];
			this._verticalAdjusterSkeleton = this._verticalAdjuster.Skeleton;
			this._verticalAdjusterSkeleton.SetAnimationAtChannel(this.VerticalAdjusterAnimation, 0, 1f, -1f, 0f);
			this._verticalAdjusterStartingLocalFrame = this._verticalAdjuster.GetFrame();
			this._verticalAdjusterStartingLocalFrame = this._body.GameEntity.GetBoneEntitialFrameWithIndex(0).TransformToLocal(this._verticalAdjusterStartingLocalFrame);
			list = base.GameEntity.CollectObjectsWithTag<SynchedMissionObject>("rotate_entity");
			this.RotationObject = list[0];
			base.OnInit();
			this.timeGapBetweenShootActionAndProjectileLeaving = this.TimeGapBetweenShootActionAndProjectileLeaving;
			this.timeGapBetweenShootingEndAndReloadingStart = 0f;
			this._ammoLoadPoints = new List<StandingPointWithWeaponRequirement>();
			if (base.StandingPoints != null)
			{
				for (int i = 0; i < base.StandingPoints.Count; i++)
				{
					if (base.StandingPoints[i].GameEntity.HasTag("ammoload"))
					{
						this._ammoLoadPoints.Add(base.StandingPoints[i] as StandingPointWithWeaponRequirement);
					}
				}
				MatrixFrame globalFrame = this._body.GameEntity.GetGlobalFrame();
				this._standingPointLocalIKFrames = new MatrixFrame[base.StandingPoints.Count];
				for (int j = 0; j < base.StandingPoints.Count; j++)
				{
					this._standingPointLocalIKFrames[j] = base.StandingPoints[j].GameEntity.GetGlobalFrame().TransformToLocal(globalFrame);
					base.StandingPoints[j].AddComponent(new ClearHandInverseKinematicsOnStopUsageComponent());
				}
			}
			this.ApplyAimChange();
			if (!GameNetwork.IsClientOrReplay)
			{
				this.SetActivationLoadAmmoPoint(false);
				this.EnemyRangeToStopUsing = 11f;
				this.MachinePositionOffsetToStopUsingLocal = new Vec2(-2.05f, -1.9f);
				this._sling.SetAnimationAtChannelSynched((base.State == RangedSiegeWeapon.WeaponState.Idle) ? this.IdleWithAmmoAnimation : this.IdleEmptyAnimation, 0, 1f);
			}
			this._missileBoneIndex = Skeleton.GetBoneIndexFromName(this._sling.GameEntity.Skeleton.GetName(), "bn_projectile_holder");
			this._shootAnimPlayed = false;
			this.UpdateAmmoMesh();
			base.SetScriptComponentToTick(this.GetTickRequirement());
			this.UpdateProjectilePosition();
		}

		/*
		public override void AfterMissionStart()
		{
			if (this.AmmoPickUpStandingPoints != null)
			{
				foreach (StandingPointWithWeaponRequirement standingPointWithWeaponRequirement in this.AmmoPickUpStandingPoints)
				{
					standingPointWithWeaponRequirement.LockUserFrames = true;
				}
			}
			if (this._ammoLoadPoints != null)
			{
				foreach (StandingPointWithWeaponRequirement standingPointWithWeaponRequirement2 in this._ammoLoadPoints)
				{
					standingPointWithWeaponRequirement2.LockUserFrames = true;
				}
			}
		}
		*/

		protected override void OnRangedSiegeWeaponStateChange()
		{
			base.OnRangedSiegeWeaponStateChange();
			if (base.State == RangedSiegeWeapon.WeaponState.WaitingBeforeIdle)
			{
				this.UpdateProjectilePosition();
			}
			if (GameNetwork.IsClientOrReplay)
			{
				return;
			}
			RangedSiegeWeapon.WeaponState state = base.State;
			if (state <= RangedSiegeWeapon.WeaponState.Shooting)
			{
				if (state == RangedSiegeWeapon.WeaponState.Idle)
				{
					base.Projectile.SetVisibleSynched(true, false);
					return;
				}
				if (state != RangedSiegeWeapon.WeaponState.Shooting)
				{
					return;
				}
				base.Projectile.SetVisibleSynched(false, false);
				return;
			}
			else
			{
				if (state == RangedSiegeWeapon.WeaponState.LoadingAmmo)
				{
					this._sling.SetAnimationAtChannelSynched(this.IdleEmptyAnimation, 0, 1f);
					return;
				}
				if (state != RangedSiegeWeapon.WeaponState.Reloading)
				{
					return;
				}
				this._shootAnimPlayed = false;
				return;
			}
		}

		protected override float HorizontalAimSensitivity => 0.1f;

		protected override float VerticalAimSensitivity => 0.075f;

		protected override Vec3 ShootingDirection
		{
			get
			{
				Mat3 rotation = this.RotationObject.GameEntity.GetGlobalFrame().rotation;
				rotation.RotateAboutSide(-this.currentReleaseAngle);
				return rotation.TransformToParent(new Vec3(0f, -1f, 0f, -1f));
			}
		}

		protected override bool HasAmmo
		{
			get
			{
				return base.HasAmmo || base.CurrentlyUsedAmmoPickUpPoint != null || this.LoadAmmoStandingPoint.HasUser || this.LoadAmmoStandingPoint.HasAIMovingTo;
			}
			set
			{
				base.HasAmmo = value;
			}
		}

		public override float ProcessTargetValue(float baseValue, TargetFlags flags)
		{
			if (flags.HasAnyFlag(TargetFlags.NotAThreat))
			{
				return -1000f;
			}
			if (flags.HasAnyFlag(TargetFlags.None))
			{
				baseValue *= 1.5f;
			}
			if (flags.HasAnyFlag(TargetFlags.IsSiegeEngine))
			{
				baseValue *= 2.5f;
			}
			if (flags.HasAnyFlag(TargetFlags.IsStructure))
			{
				baseValue *= 0.1f;
			}
			if (flags.HasAnyFlag(TargetFlags.DebugThreat))
			{
				baseValue *= 10000f;
			}
			return baseValue;
		}

		public override TargetFlags GetTargetFlags()
		{
			TargetFlags targetFlags = TargetFlags.None;
			targetFlags |= TargetFlags.IsFlammable;
			targetFlags |= TargetFlags.IsSiegeEngine;
			targetFlags |= TargetFlags.IsAttacker;
			if (base.IsDestroyed || this.IsDeactivated)
			{
				targetFlags |= TargetFlags.NotAThreat;
			}
			if (this.Side == BattleSideEnum.Attacker && DebugSiegeBehavior.DebugDefendState == DebugSiegeBehavior.DebugStateDefender.DebugDefendersToMangonels)
			{
				targetFlags |= TargetFlags.DebugThreat;
			}
			if (this.Side == BattleSideEnum.Defender && DebugSiegeBehavior.DebugAttackState == DebugSiegeBehavior.DebugStateAttacker.DebugAttackersToMangonels)
			{
				targetFlags |= TargetFlags.DebugThreat;
			}
			return targetFlags;
		}

		public override float GetTargetValue(List<Vec3> weaponPos)
		{
			return 40f * base.GetUserMultiplierOfWeapon() * this.GetDistanceMultiplierOfWeapon(weaponPos[0]) * base.GetHitPointMultiplierOfWeapon();
		}

		protected override bool CanRotate()
		{
			return base.State == RangedSiegeWeapon.WeaponState.Idle || base.State == RangedSiegeWeapon.WeaponState.LoadingAmmo || base.State == RangedSiegeWeapon.WeaponState.WaitingBeforeIdle;
		}

		public override ScriptComponentBehavior.TickRequirement GetTickRequirement()
		{
			if (base.GameEntity.IsVisibleIncludeParents())
			{
				return base.GetTickRequirement() | ScriptComponentBehavior.TickRequirement.Tick | ScriptComponentBehavior.TickRequirement.TickParallel;
			}
			return base.GetTickRequirement();
		}

		protected override void OnTick(float dt)
		{
			base.OnTick(dt);
			ForceAmmoPointUsage();
			HandleAITeamUsage();
            if (!base.GameEntity.IsVisibleIncludeParents())
			{
				return;
			}
			if (!GameNetwork.IsClientOrReplay)
			{
				foreach (StandingPointWithWeaponRequirement standingPointWithWeaponRequirement in this.AmmoPickUpStandingPoints)
				{
					if (standingPointWithWeaponRequirement.HasUser)
					{
						Agent userAgent = standingPointWithWeaponRequirement.UserAgent;
						ActionIndexValueCache currentActionValue = userAgent.GetCurrentActionValue(1);
						if (!(currentActionValue == act_pickup_boulder_begin))
						{
							if (currentActionValue == act_pickup_boulder_end)
							{
								MissionWeapon missionWeapon = new MissionWeapon(this.OriginalMissileItem, null, null);
								userAgent.EquipWeaponToExtraSlotAndWield(ref missionWeapon);
								userAgent.StopUsingGameObject(true, Agent.StopUsingGameObjectFlags.None);
								this.ConsumeAmmo();
								if (userAgent.IsAIControlled)
								{
									if (!this.LoadAmmoStandingPoint.HasUser && !this.LoadAmmoStandingPoint.IsDeactivated)
									{
										userAgent.AIMoveToGameObjectEnable(this.LoadAmmoStandingPoint, this, Agent.AIScriptedFrameFlags.NoAttack);
									}
									else if (this.ReloaderAgentOriginalPoint != null && !this.ReloaderAgentOriginalPoint.HasUser && !this.ReloaderAgentOriginalPoint.HasAIMovingTo)
									{
										userAgent.AIMoveToGameObjectEnable(this.ReloaderAgentOriginalPoint, this, Agent.AIScriptedFrameFlags.NoAttack);
									}
									else
									{
										Agent reloaderAgent = this.ReloaderAgent;
										if (reloaderAgent != null)
										{
											Formation formation = reloaderAgent.Formation;
											if (formation != null)
											{
												formation.AttachUnit(this.ReloaderAgent);
											}
										}
										this.ReloaderAgent = null;
									}
								}
							}
							else if (!userAgent.SetActionChannel(1, act_pickup_boulder_begin, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true) && userAgent.Controller != Agent.ControllerType.AI)
							{
								userAgent.StopUsingGameObject(true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
							}
						}
					}
				}
			}
			switch (base.State)
			{
				case RangedSiegeWeapon.WeaponState.LoadingAmmo:
					if (!GameNetwork.IsClientOrReplay)
					{
						bool flag = false;
						foreach (StandingPointWithWeaponRequirement standingPointWithWeaponRequirement2 in this._ammoLoadPoints)
						{
							if (flag)
							{
								if (standingPointWithWeaponRequirement2.IsDeactivated)
								{
									if ((standingPointWithWeaponRequirement2.HasUser || standingPointWithWeaponRequirement2.HasAIMovingTo) && (standingPointWithWeaponRequirement2.UserAgent == this.ReloaderAgent || standingPointWithWeaponRequirement2.MovingAgent == this.ReloaderAgent))
									{
										base.SendReloaderAgentToOriginalPoint();
									}
									standingPointWithWeaponRequirement2.SetIsDeactivatedSynched(true);
								}
							}
							else if (standingPointWithWeaponRequirement2.HasUser)
							{
								flag = true;
								Agent userAgent2 = standingPointWithWeaponRequirement2.UserAgent;
								ActionIndexValueCache currentActionValue2 = userAgent2.GetCurrentActionValue(1);
								if (currentActionValue2 == act_usage_trebuchet_load_ammo && userAgent2.GetCurrentActionProgress(1) > 0.56f)
								{
									EquipmentIndex wieldedItemIndex = userAgent2.GetWieldedItemIndex(Agent.HandIndex.MainHand);
									if (wieldedItemIndex != EquipmentIndex.None && userAgent2.Equipment[wieldedItemIndex].CurrentUsageItem.WeaponClass == this.OriginalMissileItem.PrimaryWeapon.WeaponClass)
									{
										base.ChangeProjectileEntityServer(userAgent2, userAgent2.Equipment[wieldedItemIndex].Item.StringId);
										userAgent2.RemoveEquippedWeapon(wieldedItemIndex);
										this._timeElapsedAfterLoading = 0f;
										base.Projectile.SetVisibleSynched(true, false);
										this._sling.SetAnimationAtChannelSynched(this.IdleWithAmmoAnimation, 0, 1f);
										base.State = RangedSiegeWeapon.WeaponState.WaitingBeforeIdle;
									}
									else
									{
										userAgent2.StopUsingGameObject(true, Agent.StopUsingGameObjectFlags.None);
										if (!userAgent2.IsPlayerControlled)
										{
											base.SendAgentToAmmoPickup(userAgent2);
										}
									}
								}
								else if (currentActionValue2 != act_usage_trebuchet_load_ammo && !userAgent2.SetActionChannel(1, act_usage_trebuchet_load_ammo, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true))
								{
									for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
									{
										if (!userAgent2.Equipment[equipmentIndex].IsEmpty && userAgent2.Equipment[equipmentIndex].CurrentUsageItem.WeaponClass == this.OriginalMissileItem.PrimaryWeapon.WeaponClass)
										{
											userAgent2.RemoveEquippedWeapon(equipmentIndex);
										}
									}
									userAgent2.StopUsingGameObject(true, Agent.StopUsingGameObjectFlags.None);
									if (!userAgent2.IsPlayerControlled)
									{
										base.SendAgentToAmmoPickup(userAgent2);
									}
								}
							}
							else if (standingPointWithWeaponRequirement2.HasAIMovingTo)
							{
								Agent movingAgent = standingPointWithWeaponRequirement2.MovingAgent;
								EquipmentIndex wieldedItemIndex2 = movingAgent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
								if (wieldedItemIndex2 == EquipmentIndex.None || movingAgent.Equipment[wieldedItemIndex2].CurrentUsageItem.WeaponClass != this.OriginalMissileItem.PrimaryWeapon.WeaponClass)
								{
									movingAgent.StopUsingGameObject(true, Agent.StopUsingGameObjectFlags.None);
									base.SendAgentToAmmoPickup(movingAgent);
								}
							}
						}
					}
					break;
				case RangedSiegeWeapon.WeaponState.WaitingBeforeIdle:
					this._timeElapsedAfterLoading += dt;
					if (this._timeElapsedAfterLoading > 1f)
					{
						base.State = RangedSiegeWeapon.WeaponState.Idle;
						return;
					}
					break;
				case RangedSiegeWeapon.WeaponState.Reloading:
				case RangedSiegeWeapon.WeaponState.ReloadingPaused:
					break;
				default:
					return;
			}
		}

		protected override void OnTickParallel(float dt)
		{
			base.OnTickParallel(dt);
			if (!base.GameEntity.IsVisibleIncludeParents())
			{
				return;
			}
			if (base.State == RangedSiegeWeapon.WeaponState.WaitingBeforeProjectileLeaving)
			{
				this.UpdateProjectilePosition();
			}
			float parameter = MBMath.ClampFloat((this.currentReleaseAngle - this.BottomReleaseAngleRestriction) / (this.TopReleaseAngleRestriction - this.BottomReleaseAngleRestriction), 0f, 1f);
			this._verticalAdjusterSkeleton.SetAnimationParameterAtChannel(0, parameter);
			MatrixFrame matrixFrame = this._body.GameEntity.GetBoneEntitialFrameWithIndex(0).TransformToParent(this._verticalAdjusterStartingLocalFrame);
			this._verticalAdjuster.SetFrame(ref matrixFrame);
			MatrixFrame globalFrame = this._body.GameEntity.GetGlobalFrame();
			for (int i = 0; i < base.StandingPoints.Count; i++)
			{
				if (base.StandingPoints[i].HasUser)
				{
					if (base.StandingPoints[i].UserAgent.IsInBeingStruckAction)
					{
						base.StandingPoints[i].UserAgent.ClearHandInverseKinematics();
					}
					else if (base.StandingPoints[i] != base.PilotStandingPoint)
					{
						if (base.StandingPoints[i].UserAgent.GetCurrentActionValue(1) == act_usage_trebuchet_reload_2)
						{
							base.StandingPoints[i].UserAgent.SetHandInverseKinematicsFrameForMissionObjectUsage(this._standingPointLocalIKFrames[i], globalFrame, 0f);
						}
						else
						{
							base.StandingPoints[i].UserAgent.ClearHandInverseKinematics();
						}
					}
					else
					{
						base.StandingPoints[i].UserAgent.SetHandInverseKinematicsFrameForMissionObjectUsage(this._standingPointLocalIKFrames[i], globalFrame, 0f);
					}
				}
			}
			if (!GameNetwork.IsClientOrReplay)
			{
				if (base.PilotAgent != null)
				{
					ActionIndexValueCache currentActionValue = base.PilotAgent.GetCurrentActionValue(1);
					if (base.State == RangedSiegeWeapon.WeaponState.WaitingBeforeProjectileLeaving || base.State == RangedSiegeWeapon.WeaponState.Shooting || base.State == RangedSiegeWeapon.WeaponState.WaitingBeforeReloading)
					{
						if (!this._shootAnimPlayed && currentActionValue != act_usage_trebuchet_shoot)
						{
							this._shootAnimPlayed = base.PilotAgent.SetActionChannel(1, act_usage_trebuchet_shoot, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
						}
						else if (currentActionValue != act_usage_trebuchet_shoot && !base.PilotAgent.SetActionChannel(1, act_usage_trebuchet_reload_idle, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true) && base.PilotAgent.Controller != Agent.ControllerType.AI)
						{
							base.PilotAgent.StopUsingGameObjectMT(true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
						}
					}
					else if (currentActionValue != act_usage_trebuchet_reload && currentActionValue != act_usage_trebuchet_shoot && !base.PilotAgent.SetActionChannel(1, act_usage_trebuchet_idle, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true) && base.PilotAgent.Controller != Agent.ControllerType.AI)
					{
						base.PilotAgent.StopUsingGameObjectMT(true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
					}
				}
				if (base.State != RangedSiegeWeapon.WeaponState.Reloading)
				{
					foreach (StandingPoint standingPoint in this.ReloadStandingPoints)
					{
						if (standingPoint.HasUser && standingPoint != base.PilotStandingPoint)
						{
							Agent userAgent = standingPoint.UserAgent;
							if (!userAgent.SetActionChannel(1, act_usage_trebuchet_reload_2_idle, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true) && userAgent.Controller != Agent.ControllerType.AI)
							{
								userAgent.StopUsingGameObjectMT(true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
							}
						}
					}
				}
				foreach (StandingPoint standingPoint2 in base.StandingPoints)
				{
					if (standingPoint2.HasUser && this.ReloadStandingPoints.IndexOf(standingPoint2) < 0 && (!(standingPoint2 is StandingPointWithWeaponRequirement) || (this._ammoLoadPoints.IndexOf((StandingPointWithWeaponRequirement)standingPoint2) < 0 && this.AmmoPickUpStandingPoints.IndexOf((StandingPointWithWeaponRequirement)standingPoint2) < 0)))
					{
						Agent userAgent2 = standingPoint2.UserAgent;
						if (!userAgent2.SetActionChannel(1, act_usage_trebuchet_reload_2_idle, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true) && userAgent2.Controller != Agent.ControllerType.AI)
						{
							userAgent2.StopUsingGameObjectMT(true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
						}
					}
				}
			}
			RangedSiegeWeapon.WeaponState state = base.State;
			if (state == RangedSiegeWeapon.WeaponState.Reloading)
			{
				for (int j = 0; j < this.ReloadStandingPoints.Count; j++)
				{
					if (this.ReloadStandingPoints[j].HasUser)
					{
						Agent userAgent3 = this.ReloadStandingPoints[j].UserAgent;
						ActionIndexValueCache currentActionValue2 = userAgent3.GetCurrentActionValue(1);
						if (currentActionValue2 == act_usage_trebuchet_reload || currentActionValue2 == act_usage_trebuchet_reload_2)
						{
							userAgent3.SetCurrentActionProgress(1, this.Skeletons[0].GetAnimationParameterAtChannel(0));
						}
						else if (!GameNetwork.IsClientOrReplay)
						{
							ActionIndexCache actionIndexCache = act_usage_trebuchet_reload;
							if (this.ReloadStandingPoints[j].GameEntity.HasTag("right"))
							{
								actionIndexCache = act_usage_trebuchet_reload_2;
							}
							if (!userAgent3.SetActionChannel(1, actionIndexCache, false, 0UL, 0f, 1f, -0.2f, 0.4f, this.Skeletons[0].GetAnimationParameterAtChannel(0), false, -0.2f, 0, true) && userAgent3.Controller != Agent.ControllerType.AI)
							{
								userAgent3.StopUsingGameObjectMT(true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
							}
						}
					}
				}
			}
		}

        private void HandleAITeamUsage()
        {
            if (Team != null)
            {
                if (UserFormations.Count == 0)
                {
                    var form = Team.GetFormations().ToList().FirstOrDefault(x => x.CountOfDetachableNonplayerUnits >= 4);
                    if (form != null) form.StartUsingMachine(this, true);
                }
				else if(!HasAnyUser())
				{
                    var form = Team.GetFormations().ToList().FirstOrDefault(x => x.CountOfDetachableNonplayerUnits >= 4 && !UserFormations.Contains(x));
                    if (form != null) form.StartUsingMachine(this, true);
                }
            }
        }

		private bool HasAnyUser()
		{
			return StandingPoints.Any(x => x.HasUser || x.HasAIMovingTo);
        }

        protected override void SetActivationLoadAmmoPoint(bool activate)
		{
			foreach (StandingPointWithWeaponRequirement standingPointWithWeaponRequirement in this._ammoLoadPoints)
			{
				standingPointWithWeaponRequirement.SetIsDeactivatedSynched(!activate);
			}
		}

		protected override void UpdateProjectilePosition()
		{
			MatrixFrame boneEntitialFrameWithIndex = this._sling.GameEntity.GetBoneEntitialFrameWithIndex(this._missileBoneIndex);
			base.Projectile.GameEntity.SetFrame(ref boneEntitialFrameWithIndex);
		}

		protected override bool IsStandingPointNotUsedOnAccountOfBeingAmmoLoad(StandingPoint standingPoint)
		{
			return (Enumerable.Contains<StandingPoint>(this._ammoLoadPoints, standingPoint) && this.LoadAmmoStandingPoint != standingPoint) || base.IsStandingPointNotUsedOnAccountOfBeingAmmoLoad(standingPoint);
		}

		private static readonly ActionIndexCache act_usage_trebuchet_idle = ActionIndexCache.Create("act_usage_trebuchet_idle");
		public const float TrebuchetDirectionRestriction = 1.3962635f;
		private static readonly ActionIndexCache act_usage_trebuchet_reload = ActionIndexCache.Create("act_usage_trebuchet_reload");
		private static readonly ActionIndexCache act_usage_trebuchet_reload_2 = ActionIndexCache.Create("act_usage_trebuchet_reload_2");
		private static readonly ActionIndexCache act_usage_trebuchet_reload_idle = ActionIndexCache.Create("act_usage_trebuchet_reload_idle");
		private static readonly ActionIndexCache act_usage_trebuchet_reload_2_idle = ActionIndexCache.Create("act_usage_trebuchet_reload_2_idle");
		private static readonly ActionIndexCache act_usage_trebuchet_load_ammo = ActionIndexCache.Create("act_usage_trebuchet_load_ammo");
		private static readonly ActionIndexCache act_usage_trebuchet_shoot = ActionIndexCache.Create("act_usage_trebuchet_shoot");
		private static readonly ActionIndexCache act_strike_bent_over = ActionIndexCache.Create("act_strike_bent_over");
		private static readonly ActionIndexCache act_pickup_boulder_begin = ActionIndexCache.Create("act_pickup_boulder_begin");
		private static readonly ActionIndexCache act_pickup_boulder_end = ActionIndexCache.Create("act_pickup_boulder_end");
		private const string BodyTag = "body";
		private const string SlideTag = "slide";
		private const string SlingTag = "sling";
		private const string RopeTag = "rope";
		private const string RotateTag = "rotate";
		private const string VerticalAdjusterTag = "vertical_adjuster";
		private const string MissileBoneName = "bn_projectile_holder";
		private const string LeftTag = "left";
		private const string _rotateObjectTag = "rotate_entity";
		public float ProjectileSpeed = 45f;
		public string AIAmmoLoadTag = "ammoload_ai";
		private SynchedMissionObject _body;
		private SynchedMissionObject _sling;
		private SynchedMissionObject _rope;
		public string IdleWithAmmoAnimation;
		public string IdleEmptyAnimation;
		public string BodyFireAnimation;
		public string BodySetUpAnimation;
		public string SlingFireAnimation;
		public string SlingSetUpAnimation;
		public string RopeFireAnimation;
		public string RopeSetUpAnimation;
		public string VerticalAdjusterAnimation;
		public float TimeGapBetweenShootActionAndProjectileLeaving = 1.6f;
		private GameEntity _verticalAdjuster;
		private Skeleton _verticalAdjusterSkeleton;
		private MatrixFrame _verticalAdjusterStartingLocalFrame;
		private float _timeElapsedAfterLoading;
		private bool _shootAnimPlayed;
		private MatrixFrame[] _standingPointLocalIKFrames;
		private List<StandingPointWithWeaponRequirement> _ammoLoadPoints;
		private sbyte _missileBoneIndex;
	}
}
