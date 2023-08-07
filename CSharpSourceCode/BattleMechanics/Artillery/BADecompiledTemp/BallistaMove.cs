using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;


namespace TOR_Core.BattleMechanics.Artillery.BA
{
    class BallistaMove
    {
        private float _wheelCircumference;
        private float _wheelDiameter;
        public float axleLength = 0.7f;
        private float _advancementError;
        private bool InitMove = true;
        private float Travelled;
        private float Travelled_sv;
        private MatrixFrame myFrame;
        private bool _isMoveSoundPlaying;
        private SoundEvent _movementSound;

        public float CurrentSpeed { get; private set; }

        public float MinSpeed { get; set; }

        public float MaxSpeed { get; set; }

        public Vec3 Destination { get; set; }

        public bool DestinationReached { get; private set; }

        public Team BallistaTeam { get; set; }

        public int MovementSoundCodeID { get; set; }

        public float WheelDiameter
        {
            set
            {
                this._wheelDiameter = value;
                this._wheelCircumference = this._wheelDiameter * 3.1415927f;
            }
        }

        public SynchedMissionObject MainObject { get; set; }

        private void PlayMovementSound()
        {
            if (!this._isMoveSoundPlaying)
            {
                this._movementSound = SoundEvent.CreateEvent(this.MovementSoundCodeID, this.MainObject.GameEntity.Scene);
                this._movementSound.Play();
                this._isMoveSoundPlaying = true;
            }
            this._movementSound.SetPosition(this.MainObject.GameEntity.GlobalPosition);
        }

        private void StopMovementSound()
        {
            if (!this._isMoveSoundPlaying)
                return;
            this._movementSound.Stop();
            this._isMoveSoundPlaying = false;
        }

        private void TickSound()
        {
            if ((double)this.CurrentSpeed > 0.0)
                this.PlayMovementSound();
            else
                this.StopMovementSound();
        }

        private Mat3 lookAt(in Vec3 target, in Vec3 eye)
        {
            Vec3 vec3_1 = (target - eye).NormalizedCopy();
            if ((double)vec3_1.Length == 0.0)
                vec3_1.z = 1f;
            Vec3 vec3_2 = Vec3.CrossProduct(vec3_1, Vec3.Up).NormalizedCopy();
            if ((double)vec3_2.Length == 0.0)
            {
                vec3_1.z += 0.0001f;
                Vec3.CrossProduct(vec3_1, Vec3.Up).NormalizedCopy();
            }
            Vec3 vec3_3 = Vec3.CrossProduct(vec3_2, vec3_1);
            vec3_2.w = -Vec3.DotProduct(vec3_2, eye);
            vec3_1.w = -Vec3.DotProduct(vec3_1, eye);
            vec3_3.w = -Vec3.DotProduct(vec3_3, eye);
            return new Mat3(vec3_2, vec3_1, vec3_3);
        }

        private Vec3 GetMoveDirection(Vec3 u)
        {
            Mat3 mat3 = new Mat3() {f = u, u = Vec3.Up};
            mat3.Orthonormalize();
            return mat3.f;
        }

        private Mat3 GetOrientation2D(Vec3 Pos, Vec3 cible)
        {
            Pos.z = cible.z;
            Mat3 mat3 = this.lookAt(in cible, in Pos);
            Mat3 identity = Mat3.Identity;
            identity.f = this.GetMoveDirection(mat3.f);
            identity.Orthonormalize();
            return identity;
        }

        private Mat3 GetMoveOrientation3D(Vec3 Pos, Vec3 cible)
        {
            Mat3 mat3 = this.lookAt(in cible, in Pos);
            Mat3 identity = Mat3.Identity;
            identity.f = this.GetMoveDirection(mat3.f);
            identity.Orthonormalize();
            return identity;
        }

        private Vec2 GetEnemyPos(Team team) => team.GetAveragePositionOfEnemies();

        private void BallistaBatterie()
        {
            Vec3 origin = this.myFrame.origin;
            this.myFrame.rotation = this.GetMoveOrientation3D(new Vec3(this.GetEnemyPos(this.BallistaTeam), origin.Z), origin);
            this.MainObject.GameEntity.SetGlobalFrame(in this.myFrame);
        }

        protected internal void OnAdded()
        {
            this.DestinationReached = false;
            this.CurrentSpeed = this.MaxSpeed;
            this.SetInitialFrame();
        }

        public void OnTick(float dt)
        {
            if (this.MainObject.IsDisabled)
            {
                this.CurrentSpeed = 0.0f;
                this.TickSound();
                this.DestinationReached = true;
            }
            else if (this.InitMove)
            {
                this.OnAdded();
                this.TickSound();
                this.InitMove = false;
            }
            else
            {
                float num1 = this.Destination.AsVec2.Distance(this.myFrame.origin.AsVec2);
                if ((double)num1 < 0.800000011920929)
                {
                    this.CurrentSpeed = 0.0f;
                    this.TickSound();
                    this.DestinationReached = true;
                    this.BallistaBatterie();
                }
                else
                {
                    float maxSpeed = this.MaxSpeed;
                    float minSpeed = this.MinSpeed;
                    this.CurrentSpeed = Math.Min(maxSpeed, Math.Max((float)((double)maxSpeed * (double)num1 / 300.0), minSpeed));
                    if (!this.MainObject.SynchronizeCompleted || this.CurrentSpeed.ApproximatelyEqualsTo(0.0f))
                        return;
                    float Distance = this.CurrentSpeed * dt;
                    if (!this._advancementError.ApproximatelyEqualsTo(0.0f))
                    {
                        float num2 = 3f * this.CurrentSpeed * dt * (float)Math.Sign(this._advancementError);
                        if ((double)Math.Abs(num2) >= (double)Math.Abs(this._advancementError))
                        {
                            num2 = this._advancementError;
                            this._advancementError = 0.0f;
                        }
                        else
                            this._advancementError -= num2;
                        Distance += num2;
                    }
                    this.myFrame = this.myFrame.Advance(-Distance);
                    this.SetDistanceTravelledAsClient(Distance);
                    this.myFrame.origin.z = this.SetHeightDeltaFrame(this.myFrame);
                    this.MainObject.GameEntity.SetGlobalFrame(in this.myFrame);
                }
            }
        }

        public void SetDistanceTravelledAsClient(float Distance)
        {
            this.Travelled += Distance;
            this._advancementError = this.Travelled - this.Travelled_sv;
            this.Travelled_sv = this.Travelled;
        }

        internal void SetInitialFrame()
        {
            this.myFrame = this.MainObject.GameEntity.GetGlobalFrame();
            this.myFrame.rotation = this.GetOrientation2D(this.Destination, this.myFrame.origin);
            this.MainObject.GameEntity.SetGlobalFrame(in this.myFrame);
        }

        private float SetHeightDeltaFrame(MatrixFrame Framx) => this.MainObject.GameEntity.Scene.GetTerrainHeight(this.myFrame.origin.AsVec2);

        private MatrixFrame PlaneCompass(MatrixFrame frame)
        {
            List<Vec3> vec3List = new List<Vec3>(4);
            foreach (MatrixFrame matrixFrame in new List<MatrixFrame>()
                     {
                         new MatrixFrame()
                         {
                             origin = new Vec3(y: 0.75f, z: 0.3f),
                             rotation = Mat3.Identity
                         },
                         new MatrixFrame()
                         {
                             origin = new Vec3(y: -0.75f, z: 0.3f),
                             rotation = Mat3.Identity
                         }
                     })
            {
                Vec3 parent = frame.TransformToParent(matrixFrame.origin);
                Vec3 vec3_1 = parent + frame.rotation.s * this.axleLength + (float)((double)this._wheelDiameter * 0.5 + 0.5) * frame.rotation.u;
                Vec3 vec3_2 = parent - frame.rotation.s * this.axleLength + (float)((double)this._wheelDiameter * 0.5 + 0.5) * frame.rotation.u;
                vec3_1.z = this.MainObject.GameEntity.Scene.GetTerrainHeight(vec3_1.AsVec2);
                vec3_2.z = this.MainObject.GameEntity.Scene.GetTerrainHeight(vec3_2.AsVec2);
                vec3List.Add(vec3_1);
                vec3List.Add(vec3_2);
            }
            float num1 = 0.0f;
            float num2 = 0.0f;
            float num3 = 0.0f;
            float num4 = 0.0f;
            float num5 = 0.0f;
            Vec3 vec3_3 = new Vec3();
            foreach (Vec3 vec3_4 in vec3List)
                vec3_3 += vec3_4;
            Vec3 vec3_5 = vec3_3 / (float)vec3List.Count;
            foreach (Vec3 vec3_6 in vec3List)
            {
                Vec3 vec3_7 = vec3_6 - vec3_5;
                num1 += vec3_7.x * vec3_7.x;
                num2 += vec3_7.x * vec3_7.y;
                num3 += vec3_7.y * vec3_7.y;
                num4 += vec3_7.x * vec3_7.z;
                num5 += vec3_7.y * vec3_7.z;
            }
            float num6 = (float)((double)num1 * (double)num3 - (double)num2 * (double)num2);
            float x = (float)((double)num5 * (double)num2 - (double)num4 * (double)num3) / num6;
            float y = (float)((double)num2 * (double)num4 - (double)num1 * (double)num5) / num6;
            MatrixFrame matrixFrame1;
            matrixFrame1.origin = vec3_5;
            matrixFrame1.rotation.u = new Vec3(x, y, 1f);
            double num7 = (double)matrixFrame1.rotation.u.Normalize();
            matrixFrame1.rotation.f = frame.rotation.f;
            matrixFrame1.rotation.f -= Vec3.DotProduct(matrixFrame1.rotation.f, matrixFrame1.rotation.u) * matrixFrame1.rotation.u;
            double num8 = (double)matrixFrame1.rotation.f.Normalize();
            matrixFrame1.rotation.s = Vec3.CrossProduct(matrixFrame1.rotation.f, matrixFrame1.rotation.u);
            double num9 = (double)matrixFrame1.rotation.s.Normalize();
            return matrixFrame1;
        }
    }
}
