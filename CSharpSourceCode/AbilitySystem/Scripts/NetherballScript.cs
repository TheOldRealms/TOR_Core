using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class NetherballScript : CareerAbilityMissleScript
    {
        private bool _init;
        private float _radius = 0f;

        protected override void OnAfterTick(float dt)
        {
            if (!_init)
            {
                _radius = Ability.Template.Radius;
                var template = Ability.Template.AssociatedTriggeredEffectTemplates.FirstOrDefault();
                _radius = template.Radius;
            }
            
        }

        protected override void OnBeforeRemoved(int removeReason)
        {
            base.OnBeforeRemoved(removeReason);
            var targets = this.ExplicitTargetAgents;

            if (targets != null)
            {
                TORCommon.Say(targets.Count+"");
            }
        }

        protected override void HandleCollision(Vec3 position, Vec3 normal)
        {
            base.HandleCollision(position, normal);
            
           
            
            if(Hero.MainHero.HasCareerChoice("HungerForKnowledgeKeystone"))
            { 
                var agents = Mission.Current.GetNearbyEnemyAgents(position.AsVec2, _radius, CasterAgent.Team, new MBList<Agent>());
                Hero.MainHero.AddWindsOfMagic(agents.Count);
            }

           
        }
    }
}