namespace TOR_Core.BattleMechanics.Artillery.BA
{
  // Decompiled with JetBrains decompiler
// Type: BattleArtillery.Ballista3AI
// Assembly: BattleArtillery, Version=0.0.1.0, Culture=neutral, PublicKeyToken=null
// MVID: EA72AF5B-A980-4CBB-884A-388452EB316A
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\TOR_Core\BattleArtillery.dll

using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;


namespace BattleArtillery
{
  internal class Ballista3AI : UsableMachineAIBase
  {
    private MBList<Agent> Targets = new MBList<Agent>();
    public static bool showSphereEffect = true;
    private Ballista69 MyBallista;
    private const int MaxTargetEvaluationCount = 17;
    private Agent _target;
    private float _delayTimer;
    private SoundEvent _fireSound;
    private float _delayDuration = 2f;
    private int _cannotShootCounter;
    private Timer _targetEvaluationTimer;

    public Ballista3AI(Ballista69 ballista) : base((UsableMachine) ballista)
    {
      (this.UsableMachine as RangedSiegeWeapon).OnReloadDone += new RangedSiegeWeapon.OnSiegeWeaponReloadDone(this.FindNextTarget);
      this.MyBallista = ballista;
      this._delayTimer = this._delayDuration;
      this._targetEvaluationTimer = new Timer(MBCommon.GetTotalMissionTime(), 0.5f);
    }

    public void FindNextTarget()
    {
      if (this.UsableMachine.PilotAgent == null)
        return;
      this._target = this.GetTarget();
      this.SetTargetingTimer();
    }

    private Agent GetTarget()
    {
      Agent target = (Agent) null;
      float num1 = 150f;
      float num2 = 999f;
      Vec3 currentGlobalPosition = this.MyBallista.ProjectileEntityCurrentGlobalPosition;
      Vec2 asVec2_1 = currentGlobalPosition.AsVec2;
      Team team = this.MyBallista.PilotAgent.Team;
      if (!team.IsPlayerTeam)
        this.MyBallista.TargetArea = Vec3.Invalid;
      Vec3 targetArea = this.MyBallista.TargetArea;
      if (targetArea.IsValid)
      {
        ref Vec2 local = ref asVec2_1;
        targetArea = this.MyBallista.TargetArea;
        Vec2 asVec2_2 = targetArea.AsVec2;
        if ((double) local.Distance(asVec2_2) < 150.0)
        {
          targetArea = this.MyBallista.TargetArea;
          asVec2_1 = targetArea.AsVec2;
          num1 = 15f;
        }
        else
          this.MyBallista.TargetArea = Vec3.Invalid;
      }
      int num3 = 1;
      foreach (Agent nearbyEnemyAgent in (List<Agent>) Mission.Current.GetNearbyEnemyAgents(asVec2_1, 230f, team, this.Targets))
      {
        if (nearbyEnemyAgent.IsActive())
        {
          Vec3 position = nearbyEnemyAgent.Position;
          position.z += nearbyEnemyAgent.GetEyeGlobalHeight();
          float num4 = position.Distance(currentGlobalPosition);
          Agent agent = Mission.Current.RayCastForClosestAgent(currentGlobalPosition, position, out float _, this.MyBallista.PilotAgent.Index);
          if (agent == null || agent.Index == nearbyEnemyAgent.Index)
          {
            if ((double) num4 > 0.0)
            {
              num2 = num4;
              target = nearbyEnemyAgent;
            }
            ++num3;
            if (num3 > 17)
              break;
          }
        }
      }
      return target;
    }

    protected override void OnTick(
      Agent agentToCompareTo,
      Formation formationToCompareTo,
      Team potentialUsersTeam,
      float dt)
    {
      base.OnTick(agentToCompareTo, formationToCompareTo, potentialUsersTeam, dt);
      if (this.UsableMachine == null || this.UsableMachine.PilotAgent == null)
        return;
      if (!this.UsableMachine.PilotAgent.IsActive())
        this._target = (Agent) null;
      else if (this.MyBallista.isMoving)
      {
        this._target = (Agent) null;
        this._delayTimer -= dt;
        this.MyBallista.AimAtRotation(0.0f, 0.0f);
      }
      else
      {
        if (Agent.Main != null && this.UsableMachine.PilotAgent == Agent.Main)
          return;
        if (this.UsableMachine.PilotAgent.Formation.FiringOrder.OrderType == OrderType.HoldFire)
        {
          this._target = (Agent) null;
          this._delayTimer -= dt;
        }
        else
        {
          RangedSiegeWeapon usableMachine1 = this.UsableMachine as RangedSiegeWeapon;
          if (usableMachine1.State == RangedSiegeWeapon.WeaponState.WaitingAfterShooting && !usableMachine1.PilotAgent.IsPlayerControlled)
            usableMachine1.ManualReload();
          if ((double) dt > 0.0 && this._target == null && usableMachine1.State == RangedSiegeWeapon.WeaponState.Idle)
          {
            if ((double) this._delayTimer <= 0.0)
              this.FindNextTarget();
            this._delayTimer -= dt;
          }
          if (this._target != null)
          {
            if (!this._target.IsActive())
              this._target = (Agent) null;
            else if (usableMachine1.State == RangedSiegeWeapon.WeaponState.Idle)
            {
              int num;
              if (this._targetEvaluationTimer.Check(MBCommon.GetTotalMissionTime()))
              {
                RangedSiegeWeapon usableMachine2 = this.UsableMachine as RangedSiegeWeapon;
                CapsuleData collisionCapsule = this._target.CollisionCapsule;
                Vec3 boxMin = collisionCapsule.GetBoxMin();
                collisionCapsule = this._target.CollisionCapsule;
                Vec3 boxMax = collisionCapsule.GetBoxMax();
                num = !usableMachine2.CanShootAtBox(boxMin, boxMax) ? 1 : 0;
              }
              else
                num = 0;
              if (num != 0)
                ++this._cannotShootCounter;
              if (this._cannotShootCounter >= 4)
              {
                this._target = (Agent) null;
                this.SetTargetingTimer();
                this._cannotShootCounter = 0;
              }
              else
              {
                Threat threat = new Threat()
                {
                  Agent = this._target
                };
                if (usableMachine1.AimAtThreat(threat))
                {
                  this._delayTimer -= dt;
                  if ((double) this._delayTimer <= 0.0)
                  {
                    usableMachine1.Shoot();
                    MatrixFrame frame1 = usableMachine1.PilotAgent.Frame;
                    frame1.origin.z = Mission.Current.Scene.GetTerrainHeight(frame1.origin.AsVec2) + 2f;
                    MatrixFrame frame2 = usableMachine1.PilotAgent.Frame;
                    frame2.Advance(8f);
                    frame2.Elevate(6f);
                    this.PlayFireProjectileEffects(usableMachine1.PilotAgent.Position, usableMachine1.PilotAgent.Frame);
                    this.PlayFireProjectileEffects(usableMachine1.PilotAgent.Position, frame2);
                    this._target = (Agent) null;
                    this.SetTargetingTimer();
                    this._cannotShootCounter = 0;
                    this._targetEvaluationTimer.Reset(MBCommon.GetTotalMissionTime());
                  }
                }
              }
            }
            else
              this._targetEvaluationTimer.Reset(MBCommon.GetTotalMissionTime());
          }
        }
      }
    }

    private void PlayFireProjectileEffects(Vec3 projectilePos, MatrixFrame projFrame)
    {
      Mission.Current.AddParticleSystemBurstByName("cla_explosion", projFrame, false);
      int eventIdFromString = SoundEvent.GetEventIdFromString("cla_cannon_1");
      if (this._fireSound == null || !this._fireSound.IsValid)
        this._fireSound = SoundEvent.CreateEvent(eventIdFromString, Mission.Current.Scene);
      this._fireSound.SetPosition(projectilePos);
      this._fireSound.Play();
    }

    private void SetTargetingTimer() => this._delayTimer = this._delayDuration + MBRandom.RandomFloat * 0.5f;
  }
}

}
