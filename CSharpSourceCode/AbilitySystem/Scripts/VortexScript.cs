using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class VortexScript : AbilityScript
    {
        private float _counter = 1f;
        private float _maxDeviation;
        private float _currentDeviation;
        private GameEntity _vortexPrefab;

        public override void Initialize(Ability ability)
        {
            base.Initialize(ability);
            _maxDeviation = Ability.Template.MaxRandomDeviation;
            var children = GameEntity.GetChildren().ToList();
            _vortexPrefab = children[0];
        }

        protected override void OnAfterTick(float dt)
        {
            if (Ability.Template.ShouldRotateVisuals)
            {
                var vortexFrame = _vortexPrefab.GetFrame();
                vortexFrame.rotation.RotateAboutUp(Ability.Template.VisualsRotationVelocity);
                _vortexPrefab.SetFrame(ref vortexFrame);
            }
        }

        protected override MatrixFrame GetNextGlobalFrame(MatrixFrame oldFrame, float dt)
        {
            if (_counter >= 1)
            {
                _counter = 0;
                _currentDeviation = MBRandom.RandomFloatRanged(-_maxDeviation, _maxDeviation) * dt;
            }
            else if (_counter < 1)
            {
                _counter += dt;
            }
            oldFrame.rotation.RotateAboutUp(_currentDeviation);
            var distance = Ability.Template.BaseMovementSpeed * dt;
            oldFrame.Advance(distance);
            var heightAtPosition = Mission.Current.Scene.GetGroundHeightAtPosition(oldFrame.origin);
            oldFrame.origin.z = heightAtPosition + Ability.Template.Radius / 2;
            return oldFrame;
        }
    }
}
