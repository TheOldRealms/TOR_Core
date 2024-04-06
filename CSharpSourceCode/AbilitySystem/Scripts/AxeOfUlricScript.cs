using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class AxeOfUlricScript: CareerAbilityScript
    {
        protected override void OnInit()
        {
            base.OnInit();

            if (Hero.MainHero.HasCareerChoice("FlameOfUlricKeystone"))
            {
                
                if (Agent.Main.GetComponent<AbilityComponent>() != null)
                {
                    var abilities = Agent.Main.GetComponent<AbilityComponent>().KnownAbilitySystem;
                    if (abilities.Count > 2)
                    {
                        var chosen = Agent.Main.GetComponent<AbilityComponent>().KnownAbilitySystem.TakeRandom(2).ToList();
                        if(chosen[0].StringID == this.Ability.StringID)
                            chosen[1].SetCoolDown(0);
                        else
                        {
                            chosen[0].SetCoolDown(0);
                        }
                    }
                }

            }
        }
    }
}