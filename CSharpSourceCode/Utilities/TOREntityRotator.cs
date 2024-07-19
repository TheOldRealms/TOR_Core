using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TOR_Core.Utilities
{
    public class TOREntityRotator : ScriptComponentBehavior
    {
        public float RotationSpeedX = 0f;
        public float RotationSpeedY = 0f;
        public float RotationSpeedZ = 10f;

        public override TickRequirement GetTickRequirement()
        {
            return TickRequirement.Tick;
        }

        protected override void OnTick(float dt)
        {
            Rotate(dt);
        }

        protected override void OnEditorTick(float dt)
        {
            Rotate(dt);
        }

        private void Rotate(float dt)
        {
            float speedX = RotationSpeedX * 0.001f * dt;
            float speedY = RotationSpeedY * 0.001f * dt;
            float speedZ = RotationSpeedZ * 0.001f * dt;
            MatrixFrame frame = GameEntity.GetFrame();
            frame.rotation.RotateAboutForward(speedX);
            frame.rotation.RotateAboutSide(speedY);
            frame.rotation.RotateAboutUp(speedZ);
            GameEntity.SetFrame(ref frame);
        }
    }
}
