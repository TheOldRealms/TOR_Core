using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.StatusEffect
{
    public class StatusEffect
    {
        private StatusEffectTemplate _template;
        public Agent ApplierAgent = null;
        public int CurrentDuration = 0;
        public StatusEffectTemplate Template => _template;

        public StatusEffect(StatusEffectTemplate template)
        {
            _template = template;
        }
    }
}