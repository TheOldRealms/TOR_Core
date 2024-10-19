using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class AxeOfUlricScript : CareerAbilityScript
    {
        private bool init;
        protected override void OnInit()
        {
            base.OnInit();

            if (init)
            {
                return;
            }
            
            if (!Hero.MainHero.HasCareerChoice("FlameOfUlricKeystone")) return;
            var comp = Agent.Main.GetComponent<AbilityComponent>();
            if (comp == null) return;

            var abilities = comp.KnownAbilitySystem.Where(x => x.StringID != "AxeOfUlric" && x.GetCoolDownLeft() > 0).ToList();
            var chosen = abilities.TakeRandom(1).FirstOrDefault();
            
            if (chosen == null) return;
            chosen.SetCoolDown(0);
            init = true;
        }

    }
}