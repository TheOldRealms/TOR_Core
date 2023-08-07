using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;
using TOR_Core.BattleMechanics.TriggeredEffect;


namespace TOR_Core.BattleMechanics.Artillery.BA
{
    internal class MissionBallista3 : MissionLogic
    {
        public static Dictionary<Agent, MissionBallista3.BurningEffet> BurningEffets = new Dictionary<Agent, MissionBallista3.BurningEffet>();
        private Stack ToBurning = new Stack();
        private Stack ToDest = new Stack();
        private MissionTimer QueueTimer = new MissionTimer(3.23f);
        private List<MissionBallista3.MvB> Queue = new List<MissionBallista3.MvB>();
        private float MvtStart = 1.75f;
        private static sbyte[] _BoneIndexes = new sbyte[14]
        {
            (sbyte)0,
            (sbyte)1,
            (sbyte)2,
            (sbyte)3,
            (sbyte)5,
            (sbyte)6,
            (sbyte)7,
            (sbyte)9,
            (sbyte)12,
            (sbyte)13,
            (sbyte)15,
            (sbyte)17,
            (sbyte)22,
            (sbyte)24
        };
        private OrderFlag Otf;
        private MissionScreen _missionScreen;
        private int PlaceOrder_ix;
        private int PlaceOrder_nb;
        private bool isRanged;
        private MatrixFrame[] PlacePoint = new MatrixFrame[12];
        private bool _initialize;
        private BattleSideEnum Myside;
        private BattleSideEnum EnySide;
        private bool _BurningEffect = true;
        private int _Coef = 3;
        private MatrixFrame b_init = MatrixFrame.Zero;
        private int iBalRank;
        private int iBalLine = 1;
        private const int cte_BalRank = 6;
        private const float SpacingRank = 3.2f;
        private const float SpacingLine = 3.5f;
        private int Max_Ballistas = 12;
        private bool CheatMode = false;
        private int Ballistas_Units_Created;
        private int Mod_Ballista;
        private int straf = -7;
        private FormationClass BallistaFormationClass = FormationClass.Ranged;
        private Formation BallistaFormation;
        private int BallistaFormationOrder;
        private string BallistaUnits;
        private bool BallistasUnitsReq;
        public static Vec3 TargetArea = Vec3.Invalid;
        public static bool showSphereEffect = true;
        private List<Agent> Pilots = new List<Agent>();
        private List<Ballista69> MyBallistas = new List<Ballista69>();
        private List<Ballista69> rmBallistas = new List<Ballista69>();
        private List<MissionBallista3.MvB> PreparMvt = new List<MissionBallista3.MvB>();
        private List<MissionBallista3.MvB> MoveBallistas = new List<MissionBallista3.MvB>();
        private List<GameEntity> _orderPositionEntities;

        public MissionBallista3()
        {
            this._Coef = 3;
            this._BurningEffect = true;
            this.Max_Ballistas = 5;
            this.CheatMode = true; // cfp.CheatMode;
            this.BallistaUnits = "1"; //cfp.Units;
            this.BallistasUnitsReq = this.BallistaUnits != "*";
            if (Mission.Current.PlayerTeam.IsAttacker)
            {
                this.Myside = BattleSideEnum.Attacker;
                this.EnySide = BattleSideEnum.Defender;
            }
            else
            {
                this.Myside = BattleSideEnum.Defender;
                this.EnySide = BattleSideEnum.Attacker;
            }
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);
            try
            {
                if (agent.Team != Mission.Current.PlayerTeam || agent.IsHero)
                    return;
                if (this.BallistasUnitsReq)
                {
                    if (!(agent.Character.StringId == this.BallistaUnits))
                        ;
                }
                else if (agent.Formation.RepresentativeClass != this.BallistaFormationClass)
                    ;
            }
            catch (Exception ex)
            {
                // LogHelper.WriteToFile(ex.Message, nameof (OnAgentBuild));
            }
        }

        public override void OnTeamDeployed(Team team)
        {
            base.OnTeamDeployed(team);
            try
            {
                if (!team.IsPlayerTeam || this.MyBallistas.Count <= 0)
                    return;
                // LogHelper.WriteToFile(string.Format("MyBallistas : {0}", (object) this.MyBallistas.Count), "OnFormationUnitsSpawned");
                foreach (Formation formation in (List<Formation>)Mission.Current.PlayerTeam.FormationsIncludingEmpty)
                {
                    if (formation.CountOfUnits < 11)
                    {
                        this.BallistaFormation = formation;
                        break;
                    }
                }
                if (this.BallistaFormation == null)
                    this.BallistaFormation = team.GetFormation(this.BallistaFormationClass);
                if (this.BallistaFormation != null)
                {
                    foreach (Agent pilot in this.Pilots)
                        ;
                    this.Pilots.Clear();
                    foreach (UsableMachine ballista in this.MyBallistas)
                        this.BallistaFormation.StartUsingMachine(ballista, true);
                }
            }
            catch (Exception ex)
            {
                // LogHelper.WriteToFile(ex.Message, "OnFormationUnitsSpawned");
            }
        }

        public override void OnScoreHit(
            Agent affectedAgent,
            Agent affectorAgent,
            WeaponComponentData attackerWeapon,
            bool isBlocked,
            bool isSiegeEngineHit,
            in Blow blow,
            in AttackCollisionData collisionData,
            float damagedHp,
            float hitDistance,
            float shotDifficulty)
        {
            base.OnScoreHit(affectedAgent, affectorAgent, attackerWeapon, isBlocked, isSiegeEngineHit, in blow, in collisionData, damagedHp, hitDistance, shotDifficulty);
            if (!this._BurningEffect || affectedAgent == null || !affectedAgent.IsHuman || (double)hitDistance <= 0.0 || attackerWeapon.ThrustDamage < 400)
                return;
            Mission.Current.AddParticleSystemBurstByName("cla_explosion", affectedAgent.Frame, false);
            int num1 = 0;
            MBList<Agent> agents = new MBList<Agent>();
            foreach (Agent agent in Mission.Current.GetNearbyAgents(affectedAgent.Position.AsVec2, 5.25f, agents).ToList<Agent>())
            {
                if (agent.IsActive() && agent.IsHuman && agent.Index != affectedAgent.Index && agent.Team.IsEnemyOf(Mission.Current.PlayerTeam))
                {
                    Random random = new Random();
                    Blow blow1 = new Blow(agent.Index)
                    {
                        DamageType = DamageTypes.Blunt,
                        StrikeType = StrikeType.Thrust,
                        BoneIndex = agent.Monster.HeadLookDirectionBoneIndex,
                        GlobalPosition = agent.Position
                    };
                    blow1.GlobalPosition.z = blow.GlobalPosition.z + agent.GetEyeGlobalHeight();
                    blow1.BaseMagnitude = 97f;
                    blow1.InflictedDamage = 97;
                    Vec3 v = new Vec3(y: 1f);
                    blow1.SwingDirection = agent.Frame.rotation.TransformToParent(v);
                    double num2 = (double)blow1.SwingDirection.Normalize();
                    blow1.Direction = blow.SwingDirection;
                    blow1.DamageCalculated = true;
                    sbyte handItemBoneIndex = agent.Monster.MainHandItemBoneIndex;
                    AttackCollisionData collisionData1 = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(false, false, false, true, false, false, false, false, false, false, false, false, CombatCollisionResult.StrikeAgent, -1, 1, 2, blow1.BoneIndex, blow1.VictimBodyPart, handItemBoneIndex, Agent.UsageDirection.AttackUp, -1, CombatHitResultFlags.NormalHit, 0.5f, 1f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, Vec3.Up, blow1.Direction,
                        blow1.GlobalPosition, Vec3.Zero, Vec3.Zero, agent.Velocity, Vec3.Up);
                    agent.RegisterBlow(blow1, in collisionData1);
                    ++num1;
                    if (num1 > 4)
                        break;
                }
            }
        }

        public override void OnObjectUsed(Agent userAgent, UsableMissionObject usedObject)
        {
            base.OnObjectUsed(userAgent, usedObject);
            if (!(usedObject is StandingPoint standingPoint) || !standingPoint.DescriptionMessage.Contains("Ballista3_") || !userAgent.Team.IsPlayerTeam)
                return;
            foreach (Ballista69 ballista69 in this.rmBallistas.ToList<Ballista69>())
            {
                if (ballista69.PilotStandingPoint.DescriptionMessage == standingPoint.DescriptionMessage)
                {
                    this.MyBallistas.Add(ballista69);
                    this.rmBallistas.Remove(ballista69);
                    break;
                }
            }
        }

        public override void OnObjectStoppedBeingUsed(Agent userAgent, UsableMissionObject usedObject)
        {
            base.OnObjectStoppedBeingUsed(userAgent, usedObject);
            if (!(usedObject is StandingPoint standingPoint) || !standingPoint.DescriptionMessage.Contains("Ballista3_") || !userAgent.Team.IsPlayerTeam)
                return;
            foreach (Ballista69 ballista69 in this.MyBallistas.ToList<Ballista69>())
            {
                if (ballista69.PilotStandingPoint.DescriptionMessage == standingPoint.DescriptionMessage)
                {
                    this.rmBallistas.Add(ballista69);
                    this.MyBallistas.Remove(ballista69);
                    break;
                }
            }
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (Mission.Current.MissionTeamAIType != Mission.MissionTeamAITypeEnum.FieldBattle && Mission.Current.MissionTeamAIType != Mission.MissionTeamAITypeEnum.Siege)
                return;
            if (Mission.Current.MainAgent == null || Agent.Main.Controller == Agent.ControllerType.AI)
            {
                if (!Input.IsKeyReleased(InputKey.B))
                    return;
                bool flag = false;
                int num = 0;
                foreach (Formation formation in (List<Formation>)Mission.Current.PlayerTeam.FormationsIncludingEmpty)
                {
                    if (formation.CountOfUnits < 11 && formation.CountOfUnits > 0) //!ModuleExtensions.IsCavalry(formation) && !ModuleExtensions.IsMounted(formation)
                    {
                        flag = true;
                        this.BallistaFormation = formation;
                        this.BallistaFormationOrder = num;
                        break;
                    }
                    ++num;
                }
                if (!flag)
                {
                    MBInformationManager.AddQuickInformation(new TextObject("{=3wzCrzEq }Please have at least one formation with troops less than 11 to operate artillery"));
                }
                else
                {
                    MatrixFrame orderFlagFrame = (ScreenManager.TopScreen as MissionScreen).GetOrderFlagFrame();
                    if (Mission.Current.MissionTeamAIType != Mission.MissionTeamAITypeEnum.Siege)
                        this.BallistaCreate(orderFlagFrame, Mission.Current.MainAgent);
                }
            }
            else
            {
                if (this.MyBallistas.Count == 0)
                    return;
                if (!this._initialize)
                    this.InitMission();
                this.ManageBallistas();
                if (this.MoveBallistas.Count > 0)
                {
                    foreach (MissionBallista3.MvB mvB in this.MoveBallistas.ToList<MissionBallista3.MvB>())
                    {
                        if (mvB.T.Check())
                        {
                            mvB.B.MoveBallista(mvB.dest);
                            this.MoveBallistas.Remove(mvB);
                        }
                    }
                }
                if (MissionBallista3.BurningEffets.Count > 0)
                {
                    foreach (KeyValuePair<Agent, MissionBallista3.BurningEffet> burningEffet in MissionBallista3.BurningEffets)
                    {
                        if (burningEffet.Value.burningTimer.Check(true) && burningEffet.Value.isBurning)
                            burningEffet.Value.isBurning = this.BurnAgent(burningEffet.Key);
                    }
                }
                if (this.ToBurning.Count > 0)
                {
                    this.SetAgentOnFire(this.ToBurning.Peek() as Agent);
                    this.ToBurning.Pop();
                }
                if (!this.QueueTimer.Check(true) || this.rmBallistas.Count <= 0)
                    return;
                this.AbondBallista();
            }
        }

        public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
        {
            base.OnMissionModeChange(oldMissionMode, atStart);
            if (Mission.Current.CurrentState != Mission.State.Initializing)
                return;
            this.MissionInitialize();
        }

        private void MissionInitialize()
        {
            if (!Mission.Current.NeedsMemoryCleanup)
                return;
            Mission.Current.ClearUnreferencedResources(true);
        }

        private void InitMission()
        {
            this._missionScreen = ScreenManager.TopScreen as MissionScreen;
            if (this._missionScreen == null)
                return;
            MBInformationManager.AddQuickInformation(new TextObject("{=3wzCrzEq }Artillery will now only listen to command ordered for formation " + this.BallistaFormation.RepresentativeClass.GetName()));
            this._initialize = true;
            this._orderPositionEntities = new List<GameEntity>();
        }

        private int CountOwnedBallista() => this.CheatMode ? 10 : MobileParty.MainParty.ItemRoster.GetItemNumber(MBObjectManager.Instance.GetObject<ItemObject>("bt_cannon"));

        private void BallistaCreate(MatrixFrame userFrame, Agent triggerer)
        {
            var foo = TriggeredEffectManager.CreateNew("place_greatcannon");

            foo.Trigger(userFrame.origin, Vec3.One, triggerer);
        }

        private void AbondBallista()
        {
            if (this.BallistaFormation.CountOfUnits < this.Ballistas_Units_Created)
            {
                Formation formation1 = (Formation)null;
                foreach (Formation formation2 in (List<Formation>)Mission.Current.PlayerTeam.FormationsIncludingEmpty)
                {
                    if (formation2.Index != this.BallistaFormation.Index && formation2.RepresentativeClass == FormationClass.Ranged && formation2.CountOfUnits > 0)
                    {
                        formation1 = formation2;
                        break;
                    }
                }
                if (formation1 == null)
                    return;
                formation1.GetFirstUnit().Formation = this.BallistaFormation;
            }
            foreach (Ballista69 usable in this.rmBallistas.ToList<Ballista69>())
            {
                if (!usable.isMoving)
                {
                    if (usable.IsDeactivated || usable.IsDestroyed || usable.IsDisabled)
                    {
                        --this.Ballistas_Units_Created;
                        this.rmBallistas.Remove(usable);
                    }
                    else if (usable.UserCountIncludingInStruckAction <= 0 && !usable.IsUsed)
                    {
                        int order = (int)usable.GetOrder(this.Myside);
                        if (!this.BallistaFormation.Detachments.Contains((IDetachment)usable))
                        {
                            this.BallistaFormation.StartUsingMachine((UsableMachine)usable, true);
                            break;
                        }
                    }
                }
            }
        }

        private void ManageBallistas()
        {
            this.Otf = this._missionScreen.OrderFlag;
            if (this.Otf == null || !Mission.Current.IsOrderMenuOpen || !Mission.Current.MainAgent.Team.PlayerOrderController.SelectedFormations.Contains(this.BallistaFormation))
                return;
            if (Input.IsKeyDown(InputKey.LeftShift))
            {
                if (Input.IsKeyReleased(InputKey.LeftMouseButton))
                    this.MousePosToFlag(true, this._missionScreen.GetOrderFlagFrame(), this._missionScreen.GetOrderFlagPosition());
            }
            else if (Input.IsKeyDown(InputKey.LeftMouseButton))
                this.RangedMove(this.Otf.Frame);
            if (Input.IsKeyReleased(InputKey.LeftMouseButton) && this.isRanged)
                this.MousePosToFlag(false, MatrixFrame.Zero, Vec3.Zero);
            if (Input.IsKeyReleased(InputKey.LeftShift))
                this.MousePosToFlag(false, MatrixFrame.Zero, Vec3.Zero);
            if (Input.IsKeyReleased(InputKey.MiddleMouseButton))
                this.CarpetBomber(true);
            if (Input.IsKeyPressed(InputKey.RightMouseButton))
                this.CarpetBomber(false);
            if (!MissionBallista3.TargetArea.IsValid || !Input.IsKeyDown(InputKey.LeftControl))
                return;
            MissionBallista3.Circle.tick();
        }

        private void CarpetBomber(bool OnOff)
        {
            if (OnOff)
            {
                MissionBallista3.TargetArea = this.Otf.Position;
                MissionBallista3.Circle.reset();
                MissionBallista3.Circle.tick();
            }
            else
                MissionBallista3.TargetArea = Vec3.Invalid;
            foreach (Ballista69 ballista in this.MyBallistas)
                ballista.TargetArea = MissionBallista3.TargetArea;
        }

        private void MakeVoice() => Mission.Current.MainAgent.MakeVoice(SkinVoiceManager.VoiceType.UseSiegeWeapon, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);

        private void AddOrderPositionEntity(
            int entityIndex,
            ref WorldFrame frame,
            bool fadeOut,
            float alpha = -1f)
        {
            while (this._orderPositionEntities.Count <= entityIndex)
            {
                GameEntity empty = GameEntity.CreateEmpty(this.Mission.Scene);
                empty.EntityFlags |= EntityFlags.NotAffectedBySeason;
                MetaMesh copy = MetaMesh.GetCopy("order_flag_small");
                copy.SetMaterial(copy.GetMeshAtIndex(0).GetMaterial().CreateCopy());
                empty.AddComponent((GameEntityComponent)copy);
                empty.SetVisibilityExcludeParents(false);
                this._orderPositionEntities.Add(empty);
            }
            GameEntity orderPositionEntity = this._orderPositionEntities[entityIndex];
            if (!fadeOut)
            {
                Vec3 rayBegin;
                this._missionScreen.ScreenPointToWorldRay(Vec2.One * 0.5f, out rayBegin, out Vec3 _);
                float rotationZ = MatrixFrame.CreateLookAt(rayBegin, frame.Origin.GetGroundVec3(), Vec3.Up).rotation.f.RotationZ;
                frame.Rotation = Mat3.Identity;
                frame.Rotation.RotateAboutUp(rotationZ);
                MatrixFrame groundMatrixFrame = frame.ToGroundMatrixFrame();
                orderPositionEntity.SetFrame(ref groundMatrixFrame);
            }
            if ((double)alpha != -1.0)
            {
                orderPositionEntity.SetVisibilityExcludeParents(true);
                orderPositionEntity.SetAlpha(alpha);
            }
            else if (fadeOut)
                orderPositionEntity.FadeOut(0.3f, false);
            else
                orderPositionEntity.FadeIn();
        }

        private void MousePosToFlag(bool OnOff, MatrixFrame m, Vec3 p)
        {
            if (OnOff)
            {
                if (!this.isRanged && this._orderPositionEntities.Count >= this.checkDispoBalista() || !this._missionScreen.GetProjectedMousePositionOnGround(out Vec3 _, out Vec3 _, BodyFlags.None, false))
                    return;
                WorldPosition origin = new WorldPosition(Mission.Current.Scene, p);
                WorldFrame frame = new WorldFrame(m.rotation, origin);
                this.AddOrderPositionEntity(this._orderPositionEntities.Count, ref frame, false, 0.0f);
            }
            else
            {
                this.PlaceOrder_ix = 0;
                this.PlaceOrder_nb = 0;
                foreach (GameEntity orderPositionEntity in this._orderPositionEntities)
                {
                    WorldPosition origin = new WorldPosition(Mission.Current.Scene, orderPositionEntity.GlobalPosition);
                    WorldFrame frame = new WorldFrame(orderPositionEntity.GetGlobalFrame().rotation, origin);
                    this.AddOrderPositionEntity(this.PlaceOrder_ix, ref frame, true);
                    this.PlacePoint[this.PlaceOrder_ix] = orderPositionEntity.GetGlobalFrame();
                    ++this.PlaceOrder_ix;
                    ++this.PlaceOrder_nb;
                }
                if (this.PlaceOrder_nb > 0)
                {
                    this.MakeVoice();
                    this.PickBallista();
                }
                this.isRanged = false;
                this._orderPositionEntities.Clear();
            }
        }

        private int checkDispoBalista()
        {
            int count = this.MyBallistas.Count;
            foreach (Ballista69 ballista in this.MyBallistas)
            {
                if (ballista.isMoving && !this.isRanged)
                    --count;
            }
            return count;
        }

        private void RangedMove(MatrixFrame destMve)
        {
            if (this.isRanged)
                return;
            this.isRanged = true;
            this.MousePosToFlag(true, destMve, destMve.origin);
            MatrixFrame matrixFrame1 = destMve;
            MatrixFrame matrixFrame2 = destMve;
            bool flag = false;
            int num1 = 1;
            int num2 = 0;
            for (int index = 0; index < this.MyBallistas.Count - 1; ++index)
            {
                MatrixFrame m;
                if (flag)
                {
                    matrixFrame2 = matrixFrame2.Strafe(-(float)this.straf);
                    m = matrixFrame2;
                }
                else
                {
                    matrixFrame1 = matrixFrame1.Strafe((float)this.straf);
                    m = matrixFrame1;
                }
                flag = !flag;
                this.MousePosToFlag(true, m, m.origin);
                ++num1;
                if (num1 > 6)
                {
                    num2 = (num2 + 1) % 2;
                    MatrixFrame matrixFrame3 = destMve;
                    MatrixFrame matrixFrame4 = destMve;
                    MatrixFrame matrixFrame5 = matrixFrame3.Advance(-3.5f);
                    matrixFrame1 = matrixFrame5.Strafe(-1.6f * (float)num2);
                    matrixFrame5 = matrixFrame4.Advance(-3.5f);
                    matrixFrame2 = matrixFrame5.Strafe(-1.6f * (float)num2);
                    num1 = 0;
                }
            }
        }

        private void PickBallista()
        {
            this.PreparMvt.Clear();
            int num = this.checkDispoBalista();
            if (this.PlaceOrder_nb < 1 || num == 0)
                return;
            int length = this.PlaceOrder_nb * num;
            MissionBallista3.TriPoints[] array = new MissionBallista3.TriPoints[length];
            int index1 = 0;
            for (int index2 = 0; index2 < this.PlaceOrder_nb; ++index2)
            {
                for (int index3 = 0; index3 < this.MyBallistas.Count; ++index3)
                {
                    if (!this.MyBallistas.ElementAt<Ballista69>(index3).isMoving || this.isRanged)
                    {
                        array[index1].BalliNo = index3;
                        array[index1].PointNo = index2;
                        array[index1].P1 = this.MyBallistas.ElementAt<Ballista69>(index3).GameEntity.GetGlobalFrame().origin.AsVec2;
                        array[index1].P2 = this.PlacePoint[index2].origin.AsVec2;
                        array[index1].dist = array[index1].P1.Distance(array[index1].P2);
                        ++index1;
                    }
                }
            }
            Array.Sort<MissionBallista3.TriPoints>(array, (Comparison<MissionBallista3.TriPoints>)((x, y) =>
            {
                if ((double)x.dist == 0.0 && (double)y.dist == 0.0)
                    return 0;
                if ((double)x.dist == 0.0)
                    return -1;
                return (double)y.dist == 0.0 ? 1 : x.dist.CompareTo(y.dist);
            }));
            int iStart = 0;
            for (int index4 = 0; index4 < length; ++index4)
            {
                if (!array[index4].exclude)
                {
                    this.CreateMovPt(this.MyBallistas.ElementAt<Ballista69>(array[index4].BalliNo), this.PlacePoint[array[index4].PointNo].origin, (float)iStart);
                    ++iStart;
                    if (iStart != this.PlaceOrder_nb)
                    {
                        for (int index5 = index4 + 1; index5 < length; ++index5)
                        {
                            if (array[index5].PointNo == array[index4].PointNo || array[index5].BalliNo == array[index4].BalliNo)
                                array[index5].exclude = true;
                        }
                    }
                    else
                        break;
                }
            }
            this.RefineMvt();
        }

        public bool segment2segment(Vec2 A, Vec2 B, Vec2 C, Vec2 D)
        {
            float x1 = A.x;
            float y1 = A.y;
            float x2 = B.x;
            float y2 = B.y;
            float x3 = C.x;
            float y3 = C.y;
            float x4 = D.x;
            float y4 = D.y;
            double num1;
            double num2;
            if ((double)x1 == (double)x2)
            {
                if ((double)x3 == (double)x4)
                    return false;
                double num3 = ((double)y3 - (double)y4) / ((double)x3 - (double)x4);
                num1 = (double)x1;
                num2 = num3 * ((double)x1 - (double)x3) + (double)y3;
            }
            else if ((double)x3 == (double)x4)
            {
                double num4 = ((double)y1 - (double)y2) / ((double)x1 - (double)x2);
                num1 = (double)x3;
                num2 = num4 * ((double)x3 - (double)x1) + (double)y1;
            }
            else
            {
                double num5 = ((double)y3 - (double)y4) / ((double)x3 - (double)x4);
                double num6 = ((double)y1 - (double)y2) / ((double)x1 - (double)x2);
                double num7 = (double)y3 - num5 * (double)x3;
                num1 = ((double)y1 - num6 * (double)x1 - num7) / (num5 - num6);
                num2 = num5 * num1 + num7;
            }
            return (num1 >= (double)x1 || num1 >= (double)x2) && (num1 <= (double)x1 || num1 <= (double)x2) && (num1 >= (double)x3 || num1 >= (double)x4) && (num1 <= (double)x3 || num1 <= (double)x4) && (num2 >= (double)y1 || num2 >= (double)y2) && (num2 <= (double)y1 || num2 <= (double)y2) && (num2 >= (double)y3 || num2 >= (double)y4) && (num2 <= (double)y3 || num2 <= (double)y4);
        }

        private void RecursifWay(int n)
        {
            MissionBallista3.MvB mvB1 = (MissionBallista3.MvB)this.ToDest.Pop();
            MissionBallista3.MvB mvB2 = (MissionBallista3.MvB)this.ToDest.Pop();
            Vec2 asVec2_1 = mvB1.B.GameEntity.GlobalPosition.AsVec2;
            Vec2 asVec2_2 = mvB1.dest.AsVec2;
            Vec2 asVec2_3 = mvB2.B.GameEntity.GlobalPosition.AsVec2;
            Vec2 asVec2_4 = mvB2.dest.AsVec2;
            if (this.segment2segment(asVec2_1, asVec2_2, asVec2_3, asVec2_4))
            {
                Vec3 dest = mvB1.dest;
                mvB1.dest = mvB2.dest;
                mvB2.dest = dest;
            }
            if (this.ToDest.Count > 0)
            {
                this.ToDest.Push((object)mvB1);
                this.Queue.Add(mvB2);
                this.RecursifWay(n);
            }
            else
            {
                mvB1.dist = asVec2_1.Distance(asVec2_2);
                this.PreparMvt.Add(mvB1);
                this.ToDest.Push((object)mvB2);
                --n;
                for (int index = this.Queue.Count - 1; index > -1; --index)
                    this.ToDest.Push((object)this.Queue.ElementAt<MissionBallista3.MvB>(index));
                this.Queue.Clear();
                if (n > 1)
                {
                    this.RecursifWay(n);
                }
                else
                {
                    mvB1 = (MissionBallista3.MvB)this.ToDest.Pop();
                    mvB1.dist = asVec2_3.Distance(asVec2_4);
                    this.PreparMvt.Add(mvB1);
                }
            }
        }

        private void RefineMvt()
        {
            if (this.PreparMvt.Count > 1)
            {
                foreach (MissionBallista3.MvB mvB in this.PreparMvt)
                    this.ToDest.Push((object)mvB);
                int count = this.PreparMvt.Count;
                this.PreparMvt.Clear();
                this.RecursifWay(count);
                this.MoveBallistas.Clear();
                this.PreparMvt.Sort((Comparison<MissionBallista3.MvB>)((x, y) =>
                {
                    if ((double)x.dist == 0.0 && (double)y.dist == 0.0)
                        return 0;
                    if ((double)x.dist == 0.0)
                        return -1;
                    return (double)y.dist == 0.0 ? 1 : x.dist.CompareTo(y.dist);
                }));
                this.PreparMvt.Reverse();
            }
            int num = 0;
            foreach (MissionBallista3.MvB mvB in this.PreparMvt)
            {
                var item = mvB;
                item.T = new MissionTimer(this.MvtStart * (float)num);
                this.MoveBallistas.Add(item);
                ++num;
            }
        }

        private void CreateMovPt(Ballista69 obj, Vec3 dest, float iStart) => this.PreparMvt.Add(new MissionBallista3.MvB()
        {
            T = new MissionTimer(this.MvtStart * iStart),
            B = obj,
            dest = dest
        });

        private void SetAgentOnFire(Agent affectedAgent)
        {
            if (affectedAgent == null || MissionBallista3.BurningEffets.ContainsKey(affectedAgent))
                return;
            Skeleton skeleton = (Skeleton)null;
            try
            {
                if (affectedAgent.IsActive())
                    skeleton = affectedAgent.AgentVisuals.GetSkeleton();
            }
            catch (Exception ex)
            {
                // LogHelper.WriteToFile(ex.Message, nameof (SetAgentOnFire));
            }
            if ((NativeObject)skeleton == (NativeObject)null)
                return;
            MissionBallista3.BurningEffet burningEffet = new MissionBallista3.BurningEffet();
            for (int index = 0; index < MissionBallista3._BoneIndexes.Length; ++index)
            {
                MatrixFrame boneLocalFrame = new MatrixFrame(Mat3.Identity, new Vec3()).Elevate(0.6f);
                burningEffet.particles = ParticleSystem.CreateParticleSystemAttachedToEntity("psys_game_burning_agent", affectedAgent.AgentVisuals.GetEntity(), ref boneLocalFrame);
                skeleton.AddComponentToBone(MissionBallista3._BoneIndexes[index], (GameEntityComponent)burningEffet.particles);
            }
            Random random = new Random();
            affectedAgent.SetMorale(0.0f);
            burningEffet.isBurning = true;
            burningEffet.burningTimer = new MissionTimer((float)(1.2300000190734863 + (double)random.NextFloat() * 1.309999942779541));
            MissionBallista3.BurningEffets.Add(affectedAgent, burningEffet);
        }

        private bool BurnAgent(Agent agent)
        {
            bool flag1 = false;
            bool flag2 = false;
            if (agent == null)
                return flag1;
            try
            {
                flag2 = agent.IsActive();
            }
            catch (Exception ex)
            {
                // LogHelper.WriteToFile(ex.Message, nameof (BurnAgent));
            }
            if (flag2)
            {
                Random random = new Random();
                Blow blow = new Blow(agent.Index);
                blow.DamageType = DamageTypes.Blunt;
                blow.StrikeType = StrikeType.Thrust;
                blow.BoneIndex = agent.Monster.HeadLookDirectionBoneIndex;
                blow.GlobalPosition = agent.Position;
                blow.GlobalPosition.z += agent.GetEyeGlobalHeight();
                blow.BaseMagnitude = 97f;
                blow.InflictedDamage = 97;
                Vec3 v = new Vec3(y: 1f);
                blow.SwingDirection = agent.Frame.rotation.TransformToParent(v);
                double num = (double)blow.SwingDirection.Normalize();
                blow.Direction = blow.SwingDirection;
                blow.DamageCalculated = true;
                sbyte handItemBoneIndex = agent.Monster.MainHandItemBoneIndex;
                AttackCollisionData collisionData = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(false, false, false, true, false, false, false, false, false, false, false, false, CombatCollisionResult.StrikeAgent, -1, 1, 2, blow.BoneIndex, blow.VictimBodyPart, handItemBoneIndex, Agent.UsageDirection.AttackUp, -1, CombatHitResultFlags.NormalHit, 0.5f, 1f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, Vec3.Up, blow.Direction,
                    blow.GlobalPosition, Vec3.Zero, Vec3.Zero, agent.Velocity, Vec3.Up);
                agent.RegisterBlow(blow, in collisionData);
                flag1 = true;
            }
            return flag1;
        }

        public static class Circle
        {
            private static uint color;
            public static float radius;
            public static Vec3 center = new Vec3();
            public static float animationDuration = 0.75f;

            public static void reset()
            {
                Vec3 targetArea = MissionBallista3.TargetArea;
                MissionBallista3.Circle.center.x = targetArea.X;
                MissionBallista3.Circle.center.y = targetArea.Y;
                MissionBallista3.Circle.center.z = targetArea.Z;
                MissionBallista3.Circle.radius = 15f;
                MissionBallista3.Circle.color = MissionBallista3.Circle.ToUnsignedInteger((int)byte.MaxValue, 0, 0, 1);
            }

            public static uint ToUnsignedInteger(int Red, int Green, int Blue, int Alpha)
            {
                byte num1 = (byte)((double)Red * (double)byte.MaxValue);
                byte num2 = (byte)((double)Green * (double)byte.MaxValue);
                byte num3 = (byte)((double)Blue * (double)byte.MaxValue);
                return (uint)(((int)(byte)((double)Alpha * (double)byte.MaxValue) << 24) + ((int)num1 << 16) + ((int)num2 << 8)) + (uint)num3;
            }

            public static void tick()
            {
                if (!MissionBallista3.showSphereEffect)
                    ;
            }
        }

        public class BurningEffet
        {
            public MissionTimer burningTimer;
            public ParticleSystem particles;
            public bool isBurning;
        }

        private struct MvB
        {
            public Ballista69 B;
            public Vec3 dest;
            public MissionTimer T;
            public float dist;
        }

        private struct TriPoints
        {
            public float dist;
            public int PointNo;
            public int BalliNo;
            public Vec2 P1;
            public Vec2 P2;
            public bool exclude;
        }
    }
}
