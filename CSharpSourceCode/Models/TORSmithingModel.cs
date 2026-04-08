using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.HarmonyPatches;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORSmithingModel : DefaultSmithingModel
    {
        public static List<string> HiddenCraftingTemplateIds = ["tor_large_monster_weapon_template", "tor_dual_wield_mainhand", "tor_trolltwohandedmace"];
        public static List<CraftingTemplate> ValidPlayerCraftingTemplates = new List<CraftingTemplate>();

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

        public virtual void ValidateHiddenCraftingTemplates()
        {
            List<string> newHiddenList = [];
            List<CraftingTemplate> validTemplates = [];
            foreach (var template in CraftingTemplate.All)
            {
                if (template.StringId == "tor_dual_wield_mainhand")
                {
                    newHiddenList.Add(template.StringId);
                    TORCommon.Log("TORSmithingModel : invalid crafting template : " + template.ToString() + ". Dual wield hidden from player despite accessible pieces.", NLog.LogLevel.Info);
                    continue;
                }

                //default them to true
                bool handle = true;
                bool blade = true;
                bool guard = true;
                bool pommel = true;

                var possiblePieces = template.BuildOrders;
                //if a piece type exists in the PieceData array, then it's set to false and we verify that a valid piece exists for it in the template. Anything remaining true after is an irrelevant piece type for the template.
                //handles and blades are assumed to always be present by the game, but check them anyways
                foreach (var viablePiece in possiblePieces) {
                    switch (viablePiece.PieceType)
                    {
                        case CraftingPiece.PieceTypes.Handle:
                            handle = false;
                            break;
                        case CraftingPiece.PieceTypes.Blade:
                            blade = false;
                            break;
                        case CraftingPiece.PieceTypes.Guard:
                            guard = false;
                            break;
                        case CraftingPiece.PieceTypes.Pommel:
                            pommel = false;
                            break;
                        case CraftingPiece.PieceTypes.Invalid:
                        case CraftingPiece.PieceTypes.NumberOfPieceTypes:
                            break;
                    }
                }

                foreach (var piece in template.Pieces)
                {
                    //find a valid piece for all required types - piece tier must be below 6 so that it is visible in the UI and therefore that the player can interact with the template. If all pieces are inaccessible from the native categories, they are effectively hidden and the template can also be hidden.
                    switch (piece.PieceType)
                    {
                        case CraftingPiece.PieceTypes.Handle when !handle && !piece.IsHiddenOnDesigner && piece.PieceTier < 6:
                            handle = true;
                            break;
                        case CraftingPiece.PieceTypes.Blade when !blade && !piece.IsHiddenOnDesigner && piece.PieceTier < 6:
                            blade = true;
                            break;
                        case CraftingPiece.PieceTypes.Guard when !guard && !piece.IsHiddenOnDesigner && piece.PieceTier < 6:
                            guard = true;
                            break;
                        case CraftingPiece.PieceTypes.Pommel when !pommel && !piece.IsHiddenOnDesigner && piece.PieceTier < 6:
                            pommel = true;
                            break;
                        case CraftingPiece.PieceTypes.Invalid:
                        case CraftingPiece.PieceTypes.NumberOfPieceTypes:
                            break;
                    }
                }

                if (handle && blade && guard && pommel)
                {
                    validTemplates.Add(template);
                }
                else
                {
                    newHiddenList.Add(template.StringId);
                    TORCommon.Log("TORSmithingModel : invalid crafting template : " + template.ToString() + ". Template hidden from player due to piece categories with no accessible pieces.", NLog.LogLevel.Info);
                }
            }

            if (validTemplates.Any())
            {
                ValidPlayerCraftingTemplates = validTemplates;
                var templateList = "TORSmithingModel : Valid crafting templates are :";
                ValidPlayerCraftingTemplates.ForEach(x => templateList = templateList.Add(" " + x.StringId + ",", false));
                templateList = templateList.TrimEnd(',');
                TORCommon.Log(templateList, NLog.LogLevel.Info);
            }
            else
            {
                var errorText = "TORSmithingModel : No valid crafting templates found. Pieces of all categories are hidden or of tiers above 5 for all templates. Smithy will be empty.";
                TORCommon.Log(errorText, NLog.LogLevel.Error);
                throw new MBIllegalValueException(errorText);
            }

            var oldHiddenListText = "TORSmithingModel : Previous hidden crafting templates :";
            HiddenCraftingTemplateIds.ForEach(x => oldHiddenListText = oldHiddenListText.Add(" " + x + ",", false));
            oldHiddenListText = oldHiddenListText.TrimEnd(',');
            TORCommon.Log(oldHiddenListText, NLog.LogLevel.Info);

            if (newHiddenList.Any())
            {
                HiddenCraftingTemplateIds = newHiddenList;
                var hiddenTemplateListText = "TORSmithingModel : Hidden crafting templates are :";
                newHiddenList.ForEach(x => hiddenTemplateListText = hiddenTemplateListText.Add(" " + x + ",", false));
                hiddenTemplateListText = hiddenTemplateListText.TrimEnd(',');
                TORCommon.Log(hiddenTemplateListText, NLog.LogLevel.Info);
            }
            else
            {
                TORCommon.Log("TORSmithingModel : No new crafting templates to hide.", NLog.LogLevel.Info);
            }
        }
    }
}