using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Items;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class KnightlyStrike : CareerAbilityScript
    {
        protected override void OnAfterTick(float dt)
        {
            base.OnAfterTick(dt);
            
            var duration = this.Ability.Template.Duration; // is this the modified Duration?

            float additionalLoads = Hero.MainHero.GetAllCareerChoices().WhereQ(x => x.Contains("Keystone")).Count();

            additionalLoads +=  this.Ability.Template.ScaleVariable1;
            
            int loads = (int)Mathf.Clamp(additionalLoads, 1, 15);
            

            var traitList = new List<ItemTrait>();
            
            

            if (Hero.MainHero.HasCareerChoice("PathOfGloryKeystone"))
            {
                var holyTrait = CareerHelper.GetTraitForReligion(Hero.MainHero, Hero.MainHero.GetDominantReligion());
                if (holyTrait != null && holyTrait != ItemTrait.Invalid) traitList.Add(holyTrait);
            }

            var knightlytrait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "ca_knightlystrike");
            
            
            
            
            if (knightlytrait != null) traitList.Add(knightlytrait);

            CasterAgent.ApplyStatusEffect("knightly_strike", CasterAgent, duration, false, false, true);
            for (int i = 0; i < loads; i++)
            {
                CasterAgent.ApplyStatusEffect("knightly_strike", CasterAgent, duration, false, false, true);
            }
            
            
        }
    }
}