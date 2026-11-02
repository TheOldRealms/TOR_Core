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
            var hero = casterAgent?.GetHero();
            if (hero != null && hero.GetExtendedInfo() != null)
            {
                var info = hero.GetExtendedInfo();
                if (info.GetCustomResourceValue("WindsOfMagic") < hero.GetEffectiveWindsCostForSpell(this))
                {
                    disabledReason = TORTextHelper.GetTextObject("tor_spell_not_enough_wom", "Not enough winds of magic");
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
                var windsCost = hero.GetEffectiveWindsCostForSpell(this);
                info.AddCustomResource("WindsOfMagic", -windsCost);//Sly : this causes catalyst and other WoM bonuses to be lost on the first cast during battle if they exceeded maximum wom. A design choice will have to be made here to clarify intention in the future.

                if (Template.StringID == "GazeUvMork" && hero.HasAttribute("FreeGazeUvMorkNextCast"))
                {
                    hero.RemoveAttribute("FreeGazeUvMorkNextCast");
                }
            }
        }
    }
}