using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class CallOfDaGreen : CareerAbilityScript
    {
        protected override List<TriggeredEffect> GetEffectsToTrigger()
        {
            List<TriggeredEffect> result = base.GetEffectsToTrigger();

            if (CasterAgent == null || CasterAgent.GetHero() == null) return result;

            var hero = CasterAgent.GetHero();
            var info = hero.GetExtendedInfo();

            if (info == null || string.IsNullOrEmpty(info.CareerID)) return result;

            var career = hero.GetCareer();
            if (career == null) return result;

            // Base ability: 15 second buff that generates WoM from nearby Greenskins dealing damage
            // Additional keystone effects will be added through mutations

            return result;
        }
    }
}
