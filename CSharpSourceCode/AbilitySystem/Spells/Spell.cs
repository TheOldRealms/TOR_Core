using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem.Spells
{
    public class Spell : Ability
    {
        public Spell(AbilityTemplate template) : base(template)
        {
        }

        public override bool IsDisabled(Agent casterAgent, out TextObject disabledReason)
        {
            var hero = casterAgent.GetHero();
            if (hero != null && hero.GetExtendedInfo() != null)
            {
                var info = hero.GetExtendedInfo();
                if (info.GetCustomResourceValue("WindsOfMagic") < hero.GetEffectiveWindsCostForSpell(this))
                {
                    disabledReason = new TextObject("{=!}Not enough winds of magic");
                    return true;
                }
            }
            return base.IsDisabled(casterAgent, out disabledReason);
        }

        protected override void DoCast(Agent casterAgent)
        {
            base.DoCast(casterAgent);
            var hero = casterAgent.GetHero();
            if (hero != null && hero.GetExtendedInfo() != null)
            {
                var info = hero.GetExtendedInfo();
                info.AddCustomResource("WindsOfMagic", -hero.GetEffectiveWindsCostForSpell(this));
            }
        }
    }
}