using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.Artillery.BA.BattleArtillery;


namespace TOR_Core.BattleMechanics.Artillery.BA
{
   public class Ballista69 : Ballista
  {
    public string wptag;
    private bool _BallistaMove;
    private bool _BallistaMoveInit;
    private bool _InitParticles;
    private Vec3 _Destination;
    private float WheelDiameter = 0.6f;
    private float MinSpeed = 0.5f;
    private float MaxSpeed = 1.25f;
    private BallistaMove MovementComponent;
    private SynchedMissionObject _wheel_L;
    private SynchedMissionObject _wheel_R;

    public override UsableMachineAIBase CreateAIBehaviorObject() => (UsableMachineAIBase) new Ballista3AI(this);

    protected override void OnInit()
    {
      base.OnInit();
      this.isMoving = false;
      this._defaultSide = Mission.Current.PlayerTeam.Side;
    }

    private void CollectEntities()
    {
    }

    public void MoveBallista(Vec3 dest)
    {
      WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, dest);
      if (this.PilotAgent == null || !this.PilotAgent.CanMoveDirectlyToPosition(worldPosition.AsVec2))
        return;
      if (this._BallistaMove)
        this.EndMoving();
      if (this.PilotAgent.Team == Mission.Current.PlayerTeam)
      {
        this._Destination = dest;
        this._BallistaMoveInit = true;
        this.isMoving = true;
      }
    }

    public Vec3 TargetArea { get; set; }

    public bool isMoving { get; private set; }

    protected override void OnTick(float dt)
    {
      base.OnTick(dt);
      if (this.IsDestroyed)
      {
        this.Deactivate();
        this.Disable();
      }
      else if (this._BallistaMoveInit)
      {
        this._BallistaMoveInit = false;
        this._BallistaMove = true;
        this.AddRegularMovementComponent();
      }
      else if ((this.PilotAgent == null || !this.PilotAgent.IsUsingGameObject) && this.isMoving)
      {
        this.EndMoving();
      }
      else
      {
        if (!this._BallistaMove)
          return;
        if (this.MovementComponent.DestinationReached)
          this.EndMoving();
        else
          this.MovementComponent.OnTick(dt);
      }
    }

    private void EndMoving()
    {
      this.MovementComponent = (BallistaMove) null;
      this._BallistaMove = false;
      this.isMoving = false;
      if (this.PilotAgent == null)
        return;
      this.PilotAgent.AIStateFlags -= Agent.AIStateFlag.UseObjectMoving;
    }

    private void AddRegularMovementComponent()
    {
      this.MovementComponent = new BallistaMove()
      {
        Destination = this._Destination,
        MinSpeed = this.MinSpeed,
        MaxSpeed = this.MaxSpeed,
        BallistaTeam = this.PilotAgent.Team,
        MainObject = (SynchedMissionObject) this,
        MovementSoundCodeID = SoundEvent.GetEventIdFromString("event:/mission/siege/siegetower/move"),
        WheelDiameter = this.WheelDiameter
      };
      if (this.PilotAgent != null)
        this.PilotAgent.AIStateFlags |= Agent.AIStateFlag.UseObjectMoving;
      if (this._InitParticles)
        return;
      GameEntity gameEntity = this.GameEntity;
      MatrixFrame boneLocalFrame = new MatrixFrame(Mat3.Identity, new Vec3(y: -0.7f, z: 0.6f));
      ParticleSystem attachedToEntity = ParticleSystem.CreateParticleSystemAttachedToEntity("psys_game_burning_agent", gameEntity, ref boneLocalFrame);
      gameEntity.AddComponent((GameEntityComponent) attachedToEntity);
      this._InitParticles = true;
    }
  }
}
