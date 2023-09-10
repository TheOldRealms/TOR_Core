using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.SFX
{
    public class TORSpinner :ScriptComponentBehavior
    {
        public float RotationSpeed = 100f;
        
        protected override void OnInit() => this.SetScriptComponentToTick(this.GetTickRequirement());
        
        private void Rotate(float dt)
        {
            GameEntity gameEntity = GameEntity;
            float a = this.RotationSpeed * (1f / 1000f) * dt;
            MatrixFrame frame = gameEntity.GetFrame();
            frame.rotation.RotateAboutUp(a);
            gameEntity.SetFrame(ref frame);
        }
        
        public override ScriptComponentBehavior.TickRequirement GetTickRequirement() => base.GetTickRequirement() | ScriptComponentBehavior.TickRequirement.TickParallel;

        protected  override void OnTickParallel(float dt)
        {
            this.Rotate(dt);
        }

        protected  override void OnEditorTick(float dt)
        {
            base.OnEditorTick(dt);
            this.Rotate(dt);
        }
        
    }
}