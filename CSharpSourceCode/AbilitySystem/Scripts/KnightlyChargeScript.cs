using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class KnightlyChargeScript : CareerAbilityScript
    {
        private bool propagateToCompanions;
        protected override void OnInit()
        {
            base.OnInit();
            
            propagateToCompanions = Hero.MainHero.HasCareerChoice("BlackGrailVowKeystone") || Hero.MainHero.HasCareerChoice("HolyCrusaderKeystone");
        }

        protected override void OnBeforeTick(float dt)
        {
            if (!propagateToCompanions) return;
            
            MBList<Agent> list = new MBList<Agent>();

            var heroes = Agent.Main.GetOriginMobileParty().GetMemberHeroes();

            foreach (var hero in heroes)
            {
                var agentHero= Mission.Current.Agents.WhereQ(x => x.GetHero() == hero).FirstOrDefault();

                if (agentHero != null)
                {
                    var targets = Mission.Current.GetNearbyAgents(agentHero.Position.AsVec2, 5, new MBList<Agent>()).WhereQ(x => x.BelongsToMainParty()).ToMBList();

                    if (!targets.IsEmpty())
                    {
                        list.AddRange(targets);
                    }
                    
                }
            }

            if (list.IsEmpty())
            {
                return;
            }
                
            SetExplicitTargetAgents(list);
        }
    }
}