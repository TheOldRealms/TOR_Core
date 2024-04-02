using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class BlastOfAgonyScript : CareerAbilityMissleScript
    {
        private float _radius;

        protected override void OnInit()
        {
            base.OnInit();
            var _effects = GetEffectsToTrigger();
            var radius = 0f;
            foreach (var effect in _effects)
            {
                if (effect.EffectRadius > radius)
                    radius = effect.EffectRadius;
            }
            
            _radius = radius;
        }
        
        protected override void HandleCollision(Vec3 position, Vec3 normal)
        {
            if (Agent.Main == null)
                return;
            if (Hero.MainHero.HasCareerChoice("WitchSightKeystone"))
            {
                var agents = Mission.Current.GetNearbyEnemyAgents(position.AsVec2, _radius, CasterAgent.Team, new MBList<Agent>());
                var maxWindsOfMagic = Hero.MainHero.GetExtendedInfo().MaxWindsOfMagic;

                var value = maxWindsOfMagic * 0.025f;
                Hero.MainHero.AddCustomResource("WindsOfMagic", value);
            }

            base.HandleCollision(position, normal);
        }
    }
}