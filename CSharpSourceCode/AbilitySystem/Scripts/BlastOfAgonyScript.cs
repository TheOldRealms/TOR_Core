using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class BlastOfAgonyScript : CareerAbilityMissleScript
    {
        private bool _init;
        private float _radius = 0f;

        protected override void OnInit()
        {
            base.OnInit();
            var _effects = this.GetEffectsToTrigger();
                var radius = 0f;
                foreach (var effect in _effects)
                {
                    if (effect.EffectRadius > radius)
                    {
                        radius = effect.EffectRadius;
                    }
                }

                _radius = radius;
                TORCommon.Say(_radius + " radius");
        }
        

        protected override void HandleCollision(Vec3 position, Vec3 normal)
        {
            if(Hero.MainHero.HasCareerChoice("HungerForKnowledgeKeystone"))
            { 
                var agents = Mission.Current.GetNearbyEnemyAgents(position.AsVec2, _radius, CasterAgent.Team, new MBList<Agent>());
                Hero.MainHero.AddCustomResource("DarkEnergy", agents.Count);
                TORCommon.Say(agents.Count+ " got added radius "+ _radius);
            }
            base.HandleCollision(position, normal);
           
        }
    }
}