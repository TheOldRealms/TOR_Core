using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using TOR_Core.BattleMechanics.TriggeredEffect;

namespace TOR_Core.BattleMechanics.Artillery
{
    public class ArtilleryMissionBehavior : MissionLogic
    {
        private Stack ToDest = new Stack();
        private List<MvB> Queue = new List<MvB>();
        private float MovementStartDelay = 1.75f;
        private OrderFlag Otf;
        private MissionScreen _missionScreen;
        private int PlaceOrder_ix;
        private int PlaceOrder_nb;
        private bool isRanged;
        private MatrixFrame[] PlacePoint = new MatrixFrame[12];
        private int straf = -7;
        private FormationClass BallistaFormationClass = FormationClass.Ranged;
        private Formation BallistaFormation;

        private List<BaseFieldSiegeWeapon> MyBallistas = new List<BaseFieldSiegeWeapon>();
        private List<BaseFieldSiegeWeapon> rmBallistas = new List<BaseFieldSiegeWeapon>();
        
        private List<MvB> PreparMvt = new List<MvB>();
        private List<MvB> MoveBallistas = new List<MvB>();
        private List<GameEntity> _orderPositionEntities;

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
                if (MyBallistas.Count == 0)
                    return;

                ManageBallistas();
                if (MoveBallistas.Count > 0)
                {
                    foreach (MvB mvB in MoveBallistas.ToList())
                    {
                        if (mvB.Timer.Check())
                        {
                            mvB.BaseSiegeWeapon.MoveBallista(mvB.Destination);
                            MoveBallistas.Remove(mvB);
                        }
                    }
                }

            }
        }

        public override void OnObjectUsed(Agent userAgent, UsableMissionObject usedObject)
        {
            base.OnObjectUsed(userAgent, usedObject);
            if (!(usedObject is StandingPoint standingPoint) || !standingPoint.DescriptionMessage.Contains("Ballista3_") || !userAgent.Team.IsPlayerTeam)
                return;
            foreach (BaseFieldSiegeWeapon ballista69 in rmBallistas.ToList())
            {
                if (ballista69.PilotStandingPoint.DescriptionMessage == standingPoint.DescriptionMessage)
                {
                    MyBallistas.Add(ballista69);
                    rmBallistas.Remove(ballista69);
                    break;
                }
            }
        }

        public override void OnObjectStoppedBeingUsed(Agent userAgent, UsableMissionObject usedObject)
        {
            base.OnObjectStoppedBeingUsed(userAgent, usedObject);
            if (!(usedObject is StandingPoint standingPoint) || !standingPoint.DescriptionMessage.Contains("Ballista3_") || !userAgent.Team.IsPlayerTeam)
                return;
            foreach (BaseFieldSiegeWeapon ballista69 in MyBallistas.ToList())
            {
                if (ballista69.PilotStandingPoint.DescriptionMessage == standingPoint.DescriptionMessage)
                {
                    rmBallistas.Add(ballista69);
                    MyBallistas.Remove(ballista69);
                    break;
                }
            }
        }

        private void ArtilleryCreate(MatrixFrame userFrame, Agent triggerer)
        {
            var foo = TriggeredEffectManager.CreateNew("place_greatcannon");
            foo.Trigger(userFrame.origin, Vec3.One, triggerer);
        }

        public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
        {
            base.OnMissionModeChange(oldMissionMode, atStart);
            if (oldMissionMode == MissionMode.Deployment && Mission.Current.Mode == MissionMode.Battle)
            {
                Initialize();
            }
        }

        public void Initialize()
        {
            var mbReadOnlyList = Mission.Current.MissionObjects;
            var baseFieldSiegeWeapons = mbReadOnlyList
                .FindAll(obj => obj.GetType() == typeof(ArtilleryRangedSiegeWeapon))
                .Select(obj => (BaseFieldSiegeWeapon)obj).ToList();
            MyBallistas = baseFieldSiegeWeapons;

            var team = Mission.Current.PlayerTeam;

            if (BallistaFormation == null)
                BallistaFormation = team.GetFormation(BallistaFormationClass);

            _missionScreen = ScreenManager.TopScreen as MissionScreen;
            if (_missionScreen == null)
                return;
            MBInformationManager.AddQuickInformation(new TextObject("{=3wzCrzEq }Artillery will now only listen to command ordered for formation " + BallistaFormation.RepresentativeClass.GetName()));
            _orderPositionEntities = new List<GameEntity>();
        }


        private void ManageBallistas()
        {
            Otf = _missionScreen.OrderFlag;
            if (Otf == null || !Mission.Current.IsOrderMenuOpen || !Mission.Current.MainAgent.Team.PlayerOrderController.SelectedFormations.Contains(BallistaFormation))
                return;
            if (Input.IsKeyDown(InputKey.LeftShift))
            {
                if (Input.IsKeyReleased(InputKey.LeftMouseButton))
                    MousePosToFlag(true, _missionScreen.GetOrderFlagFrame(), _missionScreen.GetOrderFlagPosition());
            }
            else if (Input.IsKeyDown(InputKey.LeftMouseButton))
                RangedMove(Otf.Frame);
            if (Input.IsKeyReleased(InputKey.LeftMouseButton) && isRanged)
                MousePosToFlag(false, MatrixFrame.Zero, Vec3.Zero);
            if (Input.IsKeyReleased(InputKey.LeftShift))
                MousePosToFlag(false, MatrixFrame.Zero, Vec3.Zero);
        }

        private void MakeVoice() => Mission.Current.MainAgent.MakeVoice(SkinVoiceManager.VoiceType.UseSiegeWeapon, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);

        private void AddOrderPositionEntity(int entityIndex, ref WorldFrame frame, bool fadeOut, float alpha = -1f)
        {
            while (_orderPositionEntities.Count <= entityIndex)
            {
                GameEntity empty = GameEntity.CreateEmpty(Mission.Scene);
                empty.EntityFlags |= EntityFlags.NotAffectedBySeason;
                MetaMesh copy = MetaMesh.GetCopy("order_flag_small");
                copy.SetMaterial(copy.GetMeshAtIndex(0).GetMaterial().CreateCopy());
                empty.AddComponent(copy);
                empty.SetVisibilityExcludeParents(false);
                _orderPositionEntities.Add(empty);
            }
            GameEntity orderPositionEntity = _orderPositionEntities[entityIndex];
            if (!fadeOut)
            {
                Vec3 rayBegin;
                _missionScreen.ScreenPointToWorldRay(Vec2.One * 0.5f, out rayBegin, out Vec3 _);
                float rotationZ = MatrixFrame.CreateLookAt(rayBegin, frame.Origin.GetGroundVec3(), Vec3.Up).rotation.f.RotationZ;
                frame.Rotation = Mat3.Identity;
                frame.Rotation.RotateAboutUp(rotationZ);
                MatrixFrame groundMatrixFrame = frame.ToGroundMatrixFrame();
                orderPositionEntity.SetFrame(ref groundMatrixFrame);
            }

            if (alpha != -1.0)
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
                if (!isRanged && _orderPositionEntities.Count >= checkDispoBalista() || !_missionScreen.GetProjectedMousePositionOnGround(out Vec3 _, out Vec3 _, BodyFlags.None, false))
                    return;
                WorldPosition origin = new WorldPosition(Mission.Current.Scene, p);
                WorldFrame frame = new WorldFrame(m.rotation, origin);
                AddOrderPositionEntity(_orderPositionEntities.Count, ref frame, false, 0.0f);
            }
            else
            {
                PlaceOrder_ix = 0;
                PlaceOrder_nb = 0;
                foreach (GameEntity orderPositionEntity in _orderPositionEntities)
                {
                    WorldPosition origin = new WorldPosition(Mission.Current.Scene, orderPositionEntity.GlobalPosition);
                    WorldFrame frame = new WorldFrame(orderPositionEntity.GetGlobalFrame().rotation, origin);
                    AddOrderPositionEntity(PlaceOrder_ix, ref frame, true);
                    PlacePoint[PlaceOrder_ix] = orderPositionEntity.GetGlobalFrame();
                    ++PlaceOrder_ix;
                    ++PlaceOrder_nb;
                }
                if (PlaceOrder_nb > 0)
                {
                    MakeVoice();
                    PickBallista();
                }
                isRanged = false;
                _orderPositionEntities.Clear();
            }
        }

        private int checkDispoBalista()
        {
            int count = MyBallistas.Count;
            foreach (BaseFieldSiegeWeapon ballista in MyBallistas)
            {
                if (ballista.isMoving && !isRanged)
                    --count;
            }
            return count;
        }

        private void RangedMove(MatrixFrame destMve)
        {
            if (isRanged)
                return;
            isRanged = true;
            MousePosToFlag(true, destMve, destMve.origin);
            MatrixFrame matrixFrame1 = destMve;
            MatrixFrame matrixFrame2 = destMve;
            bool flag = false;
            int num1 = 1;
            int num2 = 0;
            for (int index = 0; index < MyBallistas.Count - 1; ++index)
            {
                MatrixFrame m;
                if (flag)
                {
                    matrixFrame2 = matrixFrame2.Strafe(-(float)straf);
                    m = matrixFrame2;
                }
                else
                {
                    matrixFrame1 = matrixFrame1.Strafe(straf);
                    m = matrixFrame1;
                }
                flag = !flag;
                MousePosToFlag(true, m, m.origin);
                ++num1;
                if (num1 > 6)
                {
                    num2 = (num2 + 1) % 2;
                    MatrixFrame matrixFrame3 = destMve;
                    MatrixFrame matrixFrame4 = destMve;
                    MatrixFrame matrixFrame5 = matrixFrame3.Advance(-3.5f);
                    matrixFrame1 = matrixFrame5.Strafe(-1.6f * num2);
                    matrixFrame5 = matrixFrame4.Advance(-3.5f);
                    matrixFrame2 = matrixFrame5.Strafe(-1.6f * num2);
                    num1 = 0;
                }
            }
        }

        private void PickBallista()
        {
            PreparMvt.Clear();
            int num = checkDispoBalista();
            if (PlaceOrder_nb < 1 || num == 0)
                return;
            int length = PlaceOrder_nb * num;
            TriPoints[] array = new TriPoints[length];
            int index1 = 0;
            for (int index2 = 0; index2 < PlaceOrder_nb; ++index2)
            {
                for (int index3 = 0; index3 < MyBallistas.Count; ++index3)
                {
                    if (!MyBallistas.ElementAt(index3).isMoving || isRanged)
                    {
                        array[index1].BalliNo = index3;
                        array[index1].PointNo = index2;
                        array[index1].P1 = MyBallistas.ElementAt(index3).GameEntity.GetGlobalFrame().origin.AsVec2;
                        array[index1].P2 = PlacePoint[index2].origin.AsVec2;
                        array[index1].dist = array[index1].P1.Distance(array[index1].P2);
                        ++index1;
                    }
                }
            }
            Array.Sort(array, (x, y) =>
            {
                if (x.dist == 0.0 && y.dist == 0.0)
                    return 0;
                if (x.dist == 0.0)
                    return -1;
                return y.dist == 0.0 ? 1 : x.dist.CompareTo(y.dist);
            });
            int iStart = 0;
            for (int index4 = 0; index4 < length; ++index4)
            {
                if (!array[index4].exclude)
                {
                    CreateMovPt(MyBallistas.ElementAt(array[index4].BalliNo), PlacePoint[array[index4].PointNo].origin, iStart);
                    ++iStart;
                    if (iStart != PlaceOrder_nb)
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
            RefineMvt();
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
            if (x1 == (double)x2)
            {
                if (x3 == (double)x4)
                    return false;
                double num3 = (y3 - (double)y4) / (x3 - (double)x4);
                num1 = x1;
                num2 = num3 * (x1 - (double)x3) + y3;
            }
            else if (x3 == (double)x4)
            {
                double num4 = (y1 - (double)y2) / (x1 - (double)x2);
                num1 = x3;
                num2 = num4 * (x3 - (double)x1) + y1;
            }
            else
            {
                double num5 = (y3 - (double)y4) / (x3 - (double)x4);
                double num6 = (y1 - (double)y2) / (x1 - (double)x2);
                double num7 = y3 - num5 * x3;
                num1 = (y1 - num6 * x1 - num7) / (num5 - num6);
                num2 = num5 * num1 + num7;
            }
            return (num1 >= x1 || num1 >= x2) && (num1 <= x1 || num1 <= x2) && (num1 >= x3 || num1 >= x4) && (num1 <= x3 || num1 <= x4) && (num2 >= y1 || num2 >= y2) && (num2 <= y1 || num2 <= y2) && (num2 >= y3 || num2 >= y4) && (num2 <= y3 || num2 <= y4);
        }

        private void RecursifWay(int n)
        {
            MvB mvB1 = (MvB)ToDest.Pop();
            MvB mvB2 = (MvB)ToDest.Pop();
            Vec2 asVec2_1 = mvB1.BaseSiegeWeapon.GameEntity.GlobalPosition.AsVec2;
            Vec2 asVec2_2 = mvB1.Destination.AsVec2;
            Vec2 asVec2_3 = mvB2.BaseSiegeWeapon.GameEntity.GlobalPosition.AsVec2;
            Vec2 asVec2_4 = mvB2.Destination.AsVec2;
            if (segment2segment(asVec2_1, asVec2_2, asVec2_3, asVec2_4))
            {
                Vec3 dest = mvB1.Destination;
                mvB1.Destination = mvB2.Destination;
                mvB2.Destination = dest;
            }
            if (ToDest.Count > 0)
            {
                ToDest.Push(mvB1);
                Queue.Add(mvB2);
                RecursifWay(n);
            }
            else
            {
                mvB1.dist = asVec2_1.Distance(asVec2_2);
                PreparMvt.Add(mvB1);
                ToDest.Push(mvB2);
                --n;
                for (int index = Queue.Count - 1; index > -1; --index)
                    ToDest.Push(Queue.ElementAt(index));
                Queue.Clear();
                if (n > 1)
                {
                    RecursifWay(n);
                }
                else
                {
                    mvB1 = (MvB)ToDest.Pop();
                    mvB1.dist = asVec2_3.Distance(asVec2_4);
                    PreparMvt.Add(mvB1);
                }
            }
        }

        private void RefineMvt()
        {
            if (PreparMvt.Count > 1)
            {
                foreach (MvB mvB in PreparMvt)
                    ToDest.Push(mvB);
                int count = PreparMvt.Count;
                PreparMvt.Clear();
                RecursifWay(count);
                MoveBallistas.Clear();
                PreparMvt.Sort((x, y) =>
                {
                    if (x.dist == 0.0 && y.dist == 0.0)
                        return 0;
                    if (x.dist == 0.0)
                        return -1;
                    return y.dist == 0.0 ? 1 : x.dist.CompareTo(y.dist);
                });
                PreparMvt.Reverse();
            }
            int num = 0;
            foreach (MvB mvB in PreparMvt)
            {
                var item = mvB;
                item.Timer = new MissionTimer(MovementStartDelay * num);
                MoveBallistas.Add(item);
                ++num;
            }
        }

        private void CreateMovPt(BaseFieldSiegeWeapon obj, Vec3 dest, float iStart)
        {
            PreparMvt.Add(new MvB
            {
                Timer = new MissionTimer(MovementStartDelay * iStart),
                BaseSiegeWeapon = obj,
                Destination = dest
            });
        }


        private struct MvB
        {
            public BaseFieldSiegeWeapon BaseSiegeWeapon;
            public Vec3 Destination;
            public MissionTimer Timer;
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
