using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;
using TOR_Core.BattleMechanics.Artillery.BA;
using TOR_Core.BattleMechanics.TriggeredEffect;

namespace TOR_Core.BattleMechanics.Artillery
{
    public class ArtilleryMissionBehavior : MissionLogic
    {
        private Stack ToBurning = new Stack();
        private Stack ToDest = new Stack();
        private MissionTimer QueueTimer = new MissionTimer(3.23f);
        private List<MvB> Queue = new List<MvB>();
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
        private List<BaseFieldSiegeWeapon> MyBallistas = new List<BaseFieldSiegeWeapon>();
        private List<BaseFieldSiegeWeapon> rmBallistas = new List<BaseFieldSiegeWeapon>();
        private List<MvB> PreparMvt = new List<MvB>();
        private List<MvB> MoveBallistas = new List<MvB>();
        private List<GameEntity> _orderPositionEntities;

        public ArtilleryMissionBehavior()
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

        public override void OnMissionTick(float dt)
        {
            if (Mission.Current.MissionTeamAIType != Mission.MissionTeamAITypeEnum.FieldBattle && Mission.Current.MissionTeamAIType != Mission.MissionTeamAITypeEnum.Siege)
                return;
            if (Mission.Current.MainAgent == null || Agent.Main.Controller == Agent.ControllerType.AI)
            {

                var missionScreen = (ScreenManager.TopScreen as MissionScreen);
                if (missionScreen.IsDeploymentActive)
                {
                    // ScreenManager.TopScreen.AddComponent();
                }


                if (!Input.IsKeyReleased(InputKey.B))
                    return;


                var orderFlag = missionScreen.OrderFlag;
                if (orderFlag.IsVisible)
                {
                    MatrixFrame orderFlagFrame = missionScreen.GetOrderFlagFrame();
                    ArtilleryCreate(orderFlagFrame, Mission.Current.MainAgent);
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
                    foreach (ArtilleryMissionBehavior.MvB mvB in this.MoveBallistas.ToList<ArtilleryMissionBehavior.MvB>())
                    {
                        if (mvB.T.Check())
                        {
                            mvB.B.MoveBallista(mvB.dest);
                            this.MoveBallistas.Remove(mvB);
                        }
                    }
                }


                // this.AbondBallista();
            }
        }

        public override void OnObjectUsed(Agent userAgent, UsableMissionObject usedObject)
        {
            base.OnObjectUsed(userAgent, usedObject);
            if (!(usedObject is StandingPoint standingPoint) || !standingPoint.DescriptionMessage.Contains("Ballista3_") || !userAgent.Team.IsPlayerTeam)
                return;
            foreach (BaseFieldSiegeWeapon ballista69 in this.rmBallistas.ToList<BaseFieldSiegeWeapon>())
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
            foreach (BaseFieldSiegeWeapon ballista69 in this.MyBallistas.ToList<BaseFieldSiegeWeapon>())
            {
                if (ballista69.PilotStandingPoint.DescriptionMessage == standingPoint.DescriptionMessage)
                {
                    this.rmBallistas.Add(ballista69);
                    this.MyBallistas.Remove(ballista69);
                    break;
                }
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

        private void ArtilleryCreate(MatrixFrame userFrame, Agent triggerer)
        {
            var foo = TriggeredEffectManager.CreateNew("place_greatcannon");
            foo.Trigger(userFrame.origin, Vec3.One, triggerer);
            
            var mbReadOnlyList = Mission.Current.MissionObjects;
            var baseFieldSiegeWeapons = mbReadOnlyList
                .FindAll(obj => obj.GetType() == typeof(ArtilleryRangedSiegeWeapon))
                .Select(obj => (BaseFieldSiegeWeapon)obj).ToList();
            MyBallistas = baseFieldSiegeWeapons;
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
            // MBInformationManager.AddQuickInformation(new TextObject("{=3wzCrzEq }Artillery will now only listen to command ordered for formation " + this.BallistaFormation.RepresentativeClass.GetName()));
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
            foreach (BaseFieldSiegeWeapon usable in this.rmBallistas.ToList<BaseFieldSiegeWeapon>())
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
            if (this.Otf == null || !Mission.Current.IsOrderMenuOpen) // || !Mission.Current.MainAgent.Team.PlayerOrderController.SelectedFormations.Contains(this.BallistaFormation))
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
            // if (Input.IsKeyReleased(InputKey.MiddleMouseButton))
            //     this.CarpetBomber(true);
            // if (Input.IsKeyPressed(InputKey.RightMouseButton))
            //     this.CarpetBomber(false);
            // if (!ArtilleryMissionBehavior.TargetArea.IsValid || !Input.IsKeyDown(InputKey.LeftControl))
            //     return;
            // ArtilleryMissionBehavior.Circle.tick();
        }

        // private void CarpetBomber(bool OnOff)
        // {
        //     if (OnOff)
        //     {
        //         ArtilleryMissionBehavior.TargetArea = this.Otf.Position;
        //         ArtilleryMissionBehavior.Circle.reset();
        //         ArtilleryMissionBehavior.Circle.tick();
        //     }
        //     else
        //         ArtilleryMissionBehavior.TargetArea = Vec3.Invalid;
        //     foreach (BaseFieldSiegeWeapon ballista in this.MyBallistas)
        //         ballista.TargetArea = ArtilleryMissionBehavior.TargetArea;
        // }

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
            foreach (BaseFieldSiegeWeapon ballista in this.MyBallistas)
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
            ArtilleryMissionBehavior.TriPoints[] array = new ArtilleryMissionBehavior.TriPoints[length];
            int index1 = 0;
            for (int index2 = 0; index2 < this.PlaceOrder_nb; ++index2)
            {
                for (int index3 = 0; index3 < this.MyBallistas.Count; ++index3)
                {
                    if (!this.MyBallistas.ElementAt<BaseFieldSiegeWeapon>(index3).isMoving || this.isRanged)
                    {
                        array[index1].BalliNo = index3;
                        array[index1].PointNo = index2;
                        array[index1].P1 = this.MyBallistas.ElementAt<BaseFieldSiegeWeapon>(index3).GameEntity.GetGlobalFrame().origin.AsVec2;
                        array[index1].P2 = this.PlacePoint[index2].origin.AsVec2;
                        array[index1].dist = array[index1].P1.Distance(array[index1].P2);
                        ++index1;
                    }
                }
            }
            Array.Sort<ArtilleryMissionBehavior.TriPoints>(array, (Comparison<ArtilleryMissionBehavior.TriPoints>)((x, y) =>
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
                    this.CreateMovPt(this.MyBallistas.ElementAt<BaseFieldSiegeWeapon>(array[index4].BalliNo), this.PlacePoint[array[index4].PointNo].origin, (float)iStart);
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
            ArtilleryMissionBehavior.MvB mvB1 = (ArtilleryMissionBehavior.MvB)this.ToDest.Pop();
            ArtilleryMissionBehavior.MvB mvB2 = (ArtilleryMissionBehavior.MvB)this.ToDest.Pop();
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
                    this.ToDest.Push((object)this.Queue.ElementAt<ArtilleryMissionBehavior.MvB>(index));
                this.Queue.Clear();
                if (n > 1)
                {
                    this.RecursifWay(n);
                }
                else
                {
                    mvB1 = (ArtilleryMissionBehavior.MvB)this.ToDest.Pop();
                    mvB1.dist = asVec2_3.Distance(asVec2_4);
                    this.PreparMvt.Add(mvB1);
                }
            }
        }

        private void RefineMvt()
        {
            if (this.PreparMvt.Count > 1)
            {
                foreach (ArtilleryMissionBehavior.MvB mvB in this.PreparMvt)
                    this.ToDest.Push((object)mvB);
                int count = this.PreparMvt.Count;
                this.PreparMvt.Clear();
                this.RecursifWay(count);
                this.MoveBallistas.Clear();
                this.PreparMvt.Sort((Comparison<ArtilleryMissionBehavior.MvB>)((x, y) =>
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
            foreach (ArtilleryMissionBehavior.MvB mvB in this.PreparMvt)
            {
                var item = mvB;
                item.T = new MissionTimer(this.MvtStart * (float)num);
                this.MoveBallistas.Add(item);
                ++num;
            }
        }

        private void CreateMovPt(BaseFieldSiegeWeapon obj, Vec3 dest, float iStart) => this.PreparMvt.Add(new ArtilleryMissionBehavior.MvB()
        {
            T = new MissionTimer(this.MvtStart * iStart),
            B = obj,
            dest = dest
        });

        // private void ManageBallistas()
        // {
        //     this.Otf = this._missionScreen.OrderFlag;
        //     if (this.Otf == null || !Mission.Current.IsOrderMenuOpen || !Mission.Current.MainAgent.Team.PlayerOrderController.SelectedFormations.Contains(this.BallistaFormation))
        //         return;
        //     if (Input.IsKeyDown(InputKey.LeftShift))
        //     {
        //         if (Input.IsKeyReleased(InputKey.LeftMouseButton))
        //             this.MousePosToFlag(true, this._missionScreen.GetOrderFlagFrame(), this._missionScreen.GetOrderFlagPosition());
        //     }
        //     else if (Input.IsKeyDown(InputKey.LeftMouseButton))
        //         this.RangedMove(this.Otf.Frame);
        //     if (Input.IsKeyReleased(InputKey.LeftMouseButton) && this.isRanged)
        //         this.MousePosToFlag(false, MatrixFrame.Zero, Vec3.Zero);
        //     if (Input.IsKeyReleased(InputKey.LeftShift))
        //         this.MousePosToFlag(false, MatrixFrame.Zero, Vec3.Zero);
        //     if (Input.IsKeyReleased(InputKey.MiddleMouseButton))
        //         this.CarpetBomber(true);
        //     if (Input.IsKeyPressed(InputKey.RightMouseButton))
        //         this.CarpetBomber(false);
        //     if (!ArtilleryMissionBehavior.TargetArea.IsValid || !Input.IsKeyDown(InputKey.LeftControl))
        //         return;
        //     ArtilleryMissionBehavior.Circle.tick();
        // }

        public class BurningEffet
        {
            public MissionTimer burningTimer;
            public ParticleSystem particles;
            public bool isBurning;
        }

        private struct MvB
        {
            public BaseFieldSiegeWeapon B;
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
