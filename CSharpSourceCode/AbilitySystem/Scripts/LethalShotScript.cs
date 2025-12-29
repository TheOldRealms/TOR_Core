using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Items;
using TOR_Core.Items.WeaponHitScripts;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class LethalShotScript : CareerAbilityScript
    {
        protected override void OnInit()
        {
            var duration = Ability.Template.Duration;

            // Calculate arrow count
            // Base: 3 arrows
            // +1 per keystone selected
            // +2 bonus for Protector of the Woods
            // +2 bonus for Eye of the Hunter (on top of +1)
            int arrowCount = 3;

            // Count keystones and add +1 per keystone
            var allChoices = Hero.MainHero.GetAllCareerChoices();

            if (allChoices.Any(x => x.Contains("ProtectorOfTheWoodsKeystone")))
            {
                arrowCount += 1; // base keystone bonus
                arrowCount += 2; // Protector bonus
            }
            else if (allChoices.Any(x => x.Contains("PathfinderKeystone")))
            {
                arrowCount += 1;
            }
            else if (allChoices.Any(x => x.Contains("ForestStalkerKeystone")))
            {
                arrowCount += 1;
            }

            if (allChoices.Any(x => x.Contains("HailOfArrowsKeystone")))
            {
                arrowCount += 1;
            }
            else if (allChoices.Any(x => x.Contains("HawkeyedKeystone")))
            {
                arrowCount += 1;
            }
            else if (allChoices.Any(x => x.Contains("StarfireEssenceKeystone")))
            {
                arrowCount += 1;
            }

            if (allChoices.Any(x => x.Contains("EyeOfTheHunterKeystone")))
            {
                arrowCount += 1; // base keystone bonus
                arrowCount += 2; // Eye of the Hunter bonus
            }

            // Build trait list based on selected keystones
            var traitList = new List<ItemTrait>();

            // Base trait: +50% physical damage
            var baseTrait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "ca_lethal_shot");
            if (baseTrait != null && baseTrait != ItemTrait.Invalid)
            {
                traitList.Add(baseTrait);
            }

            // Tier 1 Keystones
            if (Hero.MainHero.HasCareerChoice("PathfinderKeystone"))
            {
                // Hagbane poison slow
                var hagbaneTrait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "ca_lethal_shot_hagbane");
                if (hagbaneTrait != null && hagbaneTrait != ItemTrait.Invalid)
                {
                    traitList.Add(hagbaneTrait);
                }
            }

            if (Hero.MainHero.HasCareerChoice("ForestStalkerKeystone"))
            {
                // Trickery of Loec - backstab damage
                var loecTrait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "ca_lethal_shot_loec");
                if (loecTrait != null && loecTrait != ItemTrait.Invalid)
                {
                    traitList.Add(loecTrait);
                }
            }

            // Tier 2 Keystones
            if (Hero.MainHero.HasCareerChoice("HailOfArrowsKeystone"))
            {
                // Scatter arrows + reload speed
                var scatterTrait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "ca_lethal_shot_scatter");
                if (scatterTrait != null && scatterTrait != ItemTrait.Invalid)
                {
                    traitList.Add(scatterTrait);
                }
                var reloadTrait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "ca_lethal_shot_reload");
                if (reloadTrait != null && reloadTrait != ItemTrait.Invalid)
                {
                    traitList.Add(reloadTrait);
                }
            }

            if (Hero.MainHero.HasCareerChoice("HawkeyedKeystone"))
            {
                // Arrows pierce through targets
                var pierceTrait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "ca_lethal_shot_pierce");
                if (pierceTrait != null && pierceTrait != ItemTrait.Invalid)
                {
                    traitList.Add(pierceTrait);
                }

                // Swiftshiver effect (magic damage + particle)
                var swiftshiverTrait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "ca_lethal_shot_swiftshiver");
                if (swiftshiverTrait != null && swiftshiverTrait != ItemTrait.Invalid)
                {
                    traitList.Add(swiftshiverTrait);
                }

                // Missile speed
                var speedTrait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "ca_lethal_shot_speed");
                if (speedTrait != null && speedTrait != ItemTrait.Invalid)
                {
                    traitList.Add(speedTrait);
                }
            }

            if (Hero.MainHero.HasCareerChoice("StarfireEssenceKeystone"))
            {
                // Armor penetration
                var starfireTrait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "ca_lethal_shot_starfire");
                if (starfireTrait != null && starfireTrait != ItemTrait.Invalid)
                {
                    traitList.Add(starfireTrait);
                }

                // Shield penetration
                var shieldPenTrait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "ca_lethal_shot_starfire_shield");
                if (shieldPenTrait != null && shieldPenTrait != ItemTrait.Invalid)
                {
                    traitList.Add(shieldPenTrait);
                }
            }

            // Tier 3 Keystone
            if (Hero.MainHero.HasCareerChoice("EyeOfTheHunterKeystone"))
            {
                // Moonfire: magic damage + explosion + magic vulnerability
                var moonfireTrait = ItemTrait.All.FirstOrDefaultQ(x => x.ItemTraitStringId == "ca_lethal_shot_moonfire");
                if (moonfireTrait != null && moonfireTrait != ItemTrait.Invalid)
                {
                    traitList.Add(moonfireTrait);
                }
            }

            // Apply status effect stacks for arrow count tracking
            for (int i = 0; i < arrowCount; i++)
            {
                CasterAgent.ApplyStatusEffect("lethal_shot", CasterAgent, duration, false, false, true);
            }

            // Add traits to wielded bow
            var comp = CasterAgent.GetComponent<ItemTraitAgentComponent>();
            if (comp != null)
            {
                foreach (var trait in traitList)
                {
                    comp.AddTraitToWieldedWeapon(trait, duration);
                }
            }

            LethalShotHitScript.id = -1;
        }


        protected override void OnBeforeRemoved(int removeReason)
        {
            base.OnBeforeRemoved(removeReason);
            LethalShotHitScript.id = -1;
        }
    }
}
