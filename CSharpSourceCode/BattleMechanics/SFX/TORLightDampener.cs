using TaleWorlds.Engine;
using TaleWorlds.Library;
using Light = TaleWorlds.Engine.Light;

namespace TOR_Core.BattleMechanics.SFX
{
    public class TORLightDampener: ScriptComponentBehavior
    {
        public float Duration=9;
        public float FadeInDuration=3f;
        public float FadeOutDuration=2f;
        public float FadeInIntenisityChange=500;
        public float FadeOutIntensityChange=1200;
        public float BeginIntensity=0;
        public float MaximumIntensity = 2000;


        private bool _init;
        private bool _passedFadeIn;
        private float _timer;
        private float _currentIntensity;
        private float _fadeOutBegin;
        private Light _light;


        protected override void OnInit()
        {
            base.OnInit();
            Init();
            
        }

        protected override void OnEditorInit()
        {
            base.OnEditorInit();
            Init();
        }

        private void Init()
        {
            _fadeOutBegin = Duration - FadeOutDuration;
            _light = GameEntity.GetLight();
            _light.Intensity = BeginIntensity;
            _init = true;
            this.SetScriptComponentToTick(GetTickRequirement());
            
        }
        
        public override TickRequirement GetTickRequirement() => TickRequirement.Tick | base.GetTickRequirement();
        
        protected override void OnTick(float dt)
        {
            base.OnTick(dt);
            if(!_init) return;
            
            _timer += dt;
            if (_timer <= FadeInDuration)
            {
                _currentIntensity += FadeInIntenisityChange*dt;
            }


            if (_timer >= _fadeOutBegin)
            {
                _currentIntensity -= FadeOutIntensityChange * dt;
            }
            
            
            if (_timer >= Duration)
            {
                _timer = 0;
                _currentIntensity = BeginIntensity;
                _passedFadeIn = false;
            }
            
            MathF.Clamp(_currentIntensity, 0f, MaximumIntensity);
            _light.Intensity = _currentIntensity;
        }

        protected override void OnEditorTick(float dt)
        {
            base.OnEditorTick(dt);
            if(!_init) return;
            
            _timer += dt;
            if (_timer <= FadeInDuration&&!_passedFadeIn)
            {
                _currentIntensity += FadeInIntenisityChange*dt;
            }
            else
            {
                _passedFadeIn = true;
            }


            if (_timer >= _fadeOutBegin)
            {
                _currentIntensity -= FadeOutIntensityChange * dt;
            }
            
            
            if (_timer >= Duration)
            {
                _timer = 0;
                _currentIntensity = BeginIntensity;
                _passedFadeIn = false;
            }
            
            MathF.Clamp(_currentIntensity, 0f, MaximumIntensity);
            _light.Intensity = _currentIntensity;
        }
    }
}