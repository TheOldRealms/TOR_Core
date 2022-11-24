using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.CampaignMechanics.Career;

namespace TOR_Core.AbilitySystem.Spells
{
    public class CareerAbility: Ability
    {
        public CareerAbility(AbilityTemplate template) : base(template)
        {
            
        }
        
        public override void ActivateAbility(Agent casterAgent)
        {

            if (Game.Current.GameType is Campaign)
            {
                var career =Campaign.Current.GetCampaignBehavior<CareerCampaignBase>();
            }

        //   Template.AssociatedTriggeredEffectTemplate.ImbuedStatusEffectID = "fireball_dot";
         //   Template.AssociatedTriggeredEffectTemplate.DamageType = DamageType.Holy;
            
            base.ActivateAbility(casterAgent);
        }
    }
}