using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORSmithingModel : DefaultSmithingModel
    {
        public override int GetEnergyCostForRefining(ref Crafting.RefiningFormula refineFormula, Hero hero)
        {
            var value = base.GetEnergyCostForRefining(ref refineFormula, hero);
            return ApplyEnergyCostModifiers(value, hero);
        }

        public override int GetEnergyCostForSmelting(ItemObject item, Hero hero)
        {
            var value = base.GetEnergyCostForSmelting(item, hero);
            return ApplyEnergyCostModifiers(value, hero);
        }

        public override int GetEnergyCostForSmithing(ItemObject item, Hero hero)
        {
            var value = base.GetEnergyCostForSmithing(item, hero);
            return ApplyEnergyCostModifiers(value, hero);
        }

        private int ApplyEnergyCostModifiers(int value, Hero hero)
        {
            if (hero.HasCareer(TORCareers.Runelord))
            {
                if (Hero.MainHero.HasCareerChoice("ForgefireBurningPassive3"))
                {
                    var reduction = value * 0.4f;
                    value -= (int)MathF.Round(reduction);
                }
            }

            if (hero.PartyBelongedTo != null && hero.PartyBelongedTo.HasBlessing("cult_of_grungni"))
            {
                var reduction = value * 0.25f;
                value -= (int)MathF.Round(reduction);
            }

            return value;
        }

        public override IEnumerable<Crafting.RefiningFormula> GetRefiningFormulas(
            Hero weaponsmith)
        {
            var values = base.GetRefiningFormulas(weaponsmith);



            if (weaponsmith.HasCareer(TORCareers.Runelord))
            {
                var newValues = new List<Crafting.RefiningFormula>();
                foreach (var value in values)
                {
                    if (weaponsmith.HasCareerChoice("ForgefireBurningPassive1") && value.Output == CraftingMaterials.Charcoal)
                    {
                        var entry = new Crafting.RefiningFormula(value.Input1, value.Input1Count, value.Input2, value.Input2Count, value.Output,
                            value.OutputCount + 1);
                        newValues.Add(entry);
                        continue;
                    }
                    if (weaponsmith.HasCareerChoice("ForgefireBurningPassive2") && value.Output is CraftingMaterials.Iron1 or CraftingMaterials.Iron2 or CraftingMaterials.Iron3 or CraftingMaterials.Iron4)
                    {

                        var entry = new Crafting.RefiningFormula(value.Input1, value.Input1Count, value.Input2, value.Input2Count, value.Output,
                            value.OutputCount * 2);
                        newValues.Add(entry);
                        continue;
                    }
                    newValues.Add(value);
                }

                values = newValues;
            }

            return values;
        }
    }
}