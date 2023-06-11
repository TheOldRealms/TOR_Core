using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem.Spells
{
    public class Spell : Ability
    {
        public Spell(AbilityTemplate template) : base(template)
        {
        }

        public override bool CanCast(Agent casterAgent)
        {
            var hero = casterAgent.GetHero();
            if (hero != null && hero.GetExtendedInfo() != null)
            {
                var info = hero.GetExtendedInfo();
                if (info.CurrentWindsOfMagic < hero.GetEffectiveWindsCostForSpell(this))
                {
                    return false;
                }
            }
            return base.CanCast(casterAgent);
        }

        protected override void DoCast(Agent casterAgent)
        {
            base.DoCast(casterAgent);
            var hero = casterAgent.GetHero();
            if (hero != null && hero.GetExtendedInfo() != null)
            {
                var info = hero.GetExtendedInfo();
                info.CurrentWindsOfMagic -= hero.GetEffectiveWindsCostForSpell(this);
            }
        }
    }
}
