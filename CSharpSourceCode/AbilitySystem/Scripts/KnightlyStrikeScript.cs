using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.LinQuick;
using TaleWorlds.TwoDimension;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Items;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class KnightlyStrike : CareerAbilityScript
    {
        protected override void OnInit()
        {
             var duration = this.Ability.Template.Duration; // is this the modified Duration?

            float additionalLoads = Hero.MainHero.GetAllCareerChoices().WhereQ(x => x.Contains("Keystone")).Count();

            additionalLoads +=  this.Ability.Template.ScaleVariable1;
            
            int loads = (int)Mathf.Clamp(additionalLoads, 1, 15);
            

            var traitList = new List<ItemTrait>();
            
            if (Hero.MainHero.HasCareerChoice("PathOfConquestKeystone"))
            {
                var cleaveTrait = ItemTrait.All.FirstOrDefault(x => x.ItemTraitStringId == "ca_knightlystrike_cleave");
                if (cleaveTrait != null && cleaveTrait != ItemTrait.Invalid) traitList.Add(cleaveTrait);
            }
            
            if (Hero.MainHero.HasCareerChoice("SecularOrdersKeystone"))
            {
                additionalLoads +=  1;
                
            }
            
            if (Hero.MainHero.HasCareerChoice("SquiresKeystone"))
            {
                var damagetrait = ItemTrait.All.FirstOrDefault(x => x.ItemTraitStringId == "ca_knightlystrike_extra_damage");
                if (damagetrait != null && damagetrait != ItemTrait.Invalid) traitList.Add(damagetrait);
            }
            
            if (Hero.MainHero.HasCareerChoice("TemplarOrdersKeystone"))
            {
                additionalLoads +=  1;
                
            }
            

            if (Hero.MainHero.HasCareerChoice("PathOfVigilanceKeystone"))
            {
                var swingSpeedTrait = ItemTrait.All.FirstOrDefault(x => x.ItemTraitStringId == "ca_knightlystrike_swing_speed");
                if (swingSpeedTrait != null && swingSpeedTrait != ItemTrait.Invalid) traitList.Add(swingSpeedTrait);
            }
            
            if (Hero.MainHero.HasCareerChoice("WrathAgainstChaosKeystone"))
            {
                var armorPenTrait = ItemTrait.All.FirstOrDefault(x => x.ItemTraitStringId == "ca_knightlystrike_extra_armorpen");
                if (armorPenTrait != null && armorPenTrait != ItemTrait.Invalid) traitList.Add(armorPenTrait);
            }


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
            
            var comp = CasterAgent.GetComponent<ItemTraitAgentComponent>();
            if (comp != null)
            {
                foreach (var trait in traitList)
                {
                    comp.AddTraitToWieldedWeapon(trait, duration);
                }
            }
        }
        

    }
}