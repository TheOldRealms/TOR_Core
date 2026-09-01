using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Items;
using TOR_Core.Utilities;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;

public class RunelordCareerButtonBehavior : CareerButtonBehaviorBase
{
    private string _runeEmptyIcon = "CareerSystem\\rune_empty";
    private string _runeBattleIcon = "CareerSystem\\rune_battle";
    private string _runeGuardingIcon = "CareerSystem\\rune_guarding";
    private string _runeRapidFireIcon = "CareerSystem\\rune_rapid_fire";
    private string _runeSanctuaryIcon = "CareerSystem\\rune_sanctuary";
    private string _runeStrollazIcon = "CareerSystem\\strollaz_rune";

    public RunelordCareerButtonBehavior(CareerObject career) : base(career)
    {
        MBTextManager.SetTextVariable("RUNE_EMPTY_ICON", string.Format("<img src=\"{0}\"/>", _runeEmptyIcon));
        MBTextManager.SetTextVariable("RUNE_BATTLE_ICON", string.Format("<img src=\"{0}\"/>", _runeBattleIcon));
        MBTextManager.SetTextVariable("RUNE_GUARDING_ICON", string.Format("<img src=\"{0}\"/>", _runeGuardingIcon));
        MBTextManager.SetTextVariable("RUNE_RAPID_FIRE_ICON", string.Format("<img src=\"{0}\"/>", _runeRapidFireIcon));
        MBTextManager.SetTextVariable("RUNE_SANCTUARY_ICON", string.Format("<img src=\"{0}\"/>", _runeSanctuaryIcon));
        MBTextManager.SetTextVariable("RUNE_STROLLAZ_ICON", string.Format("<img src=\"{0}\"/>", _runeStrollazIcon));
    }

    private static readonly List<UnitRune> UnitRunes =
    [
        new("unit_rune_guarding", TORTextHelper.GetTextObject("tor_unit_rune_guarding_name", "Rune of Guarding"), TORTextHelper.GetTextObject("tor_unit_rune_guarding_desc", "15% extra physical resistance."), "unit_rune_guarding",
            new List<string> {"dw_rune_stone","dw_rune_protection", "dw_rune_shielding" }, 1),
        new("unit_rune_sanctuary", TORTextHelper.GetTextObject("tor_unit_rune_sanctuary_name", "Rune of Sanctuary"), TORTextHelper.GetTextObject("tor_unit_rune_sanctuary_desc", "30% extra magic resistance."), "unit_rune_sanctuary",
            new List<string> { "dw_rune_vigour","dw_rune_parrying","dw_rune_protection" }, 1),
        new("unit_rune_battle", TORTextHelper.GetTextObject("tor_unit_rune_battle_name", "Rune of Battle"), TORTextHelper.GetTextObject("tor_unit_rune_battle_desc", "30% extra physical damage."), "unit_rune_battle",
            new List<string> { "dw_rune_striking","dw_rune_might","dw_rune_fire" }, 1),
        new("unit_rune_retribution", TORTextHelper.GetTextObject("tor_unit_rune_retribution_name", "Rune of Retribution"),
            TORTextHelper.GetTextObject("tor_unit_rune_retribution_desc", "25% extra fire resistance, 35% extra fire damage."), "unit_rune_retribution", new List<string> { "dw_rune_spell_eating","dw_rune_preservation","dw_rune_fortitude" }, 2),
        new("unit_rune_rapid_fire", TORTextHelper.GetTextObject("tor_unit_rune_rapid_fire_name", "Rune of Rapid Fire"), TORTextHelper.GetTextObject("tor_unit_rune_rapid_fire_desc", "60% extra reload speed."), "unit_rune_rapid_fire",
            new List<string> { "dw_rune_head_wrecking","dw_rune_beastslaying","dw_rune_reloading" }, 2),
        new("unit_rune_strollaz", TORTextHelper.GetTextObject("tor_unit_rune_strollaz_name", "Strollaz' Rune"), TORTextHelper.GetTextObject("tor_unit_rune_strollaz_desc", "35% extra movement speed."), "unit_rune_strollaz",
            new List<string> { "dw_rune_speed","dw_rune_striking","dw_rune_impact" }, 2),
        new("unit_rune_valaya", TORTextHelper.GetTextObject("tor_unit_rune_valaya_name", "Master Rune of Valaya"), TORTextHelper.GetTextObject("tor_unit_rune_valaya_desc", "30% extra Ward Save."), "unit_rune_valaya",
            new List<string> { "dw_master_rune_adamant","dw_master_rune_gromril","dw_master_rune_skaldour" }, 3),
        new("unit_rune_grungni", TORTextHelper.GetTextObject("tor_unit_rune_grungni_name", "Master Rune of Grungni"), TORTextHelper.GetTextObject("tor_unit_rune_grungni_desc", "Reduce incoming ranged damage by 75%."),
            "unit_rune_grungni", new List<string> { "dw_master_rune_steel","dw_master_rune_gromril","dw_master_rune_preservation" }, 3),
        new("unit_rune_grimnir", TORTextHelper.GetTextObject("tor_unit_rune_grimnir_name", "Master Rune of Grimnir"),
            TORTextHelper.GetTextObject("tor_unit_rune_grimnir_desc", "20% extra attack speed, 35% extra physical and fire damage."), "unit_rune_grimnir", new List<string> { "dw_master_rune_swiftness","dw_master_rune_breaking","dw_rune_fury" }, 3)
    ];


    public static List<string> GetRuneIds => UnitRunes.SelectQ(x => x.EffectId).ToListQ();

    private static int GetMaxRunesPerUnit()
    {
        return Hero.MainHero.HasCareerChoice("AnvilOfDoomPassive4") ? 2 : 1;
    }

    private CharacterObject _currentCharacter = null;
    private CharacterObject _setCharacter;

    public override string CareerButtonIcon
    {
        get
        {
            var currentRunes = GetCurrentActiveRunes(_setCharacter);

            if (currentRunes == null || !currentRunes.Any())
            {
                return "CareerSystem\\rune_empty";
            }

            var firstRuneId = currentRunes.First().EffectId;
            return firstRuneId switch
            {
                "unit_rune_guarding" => "CareerSystem\\rune_guarding",
                "unit_rune_sanctuary" => "CareerSystem\\rune_sanctuary",
                "unit_rune_battle" => "CareerSystem\\rune_battle",
                "unit_rune_rapid_fire" => "CareerSystem\\rune_rapid_fire",
                "unit_rune_strollaz" => "CareerSystem\\strollaz_rune",
                _ => "CareerSystem\\rune_empty"
            };
        }
    }

    public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner, bool shiftClick)
    {
        _currentCharacter = characterObject;

        var tier = 0;
        for (var i = 1; i < 4; i++)
        {

            if (!Hero.MainHero.HasUnlockedCareerChoiceTier(i))
            {
                break;
            }
            tier = i;
        }

        PromptUnitRunes(tier);
    }

    private string GetRuneIcon(string runeEffectId)
    {
        return runeEffectId switch
        {
            "unit_rune_guarding" => "RUNE_GUARDING_ICON",
            "unit_rune_sanctuary" => "RUNE_SANCTUARY_ICON",
            "unit_rune_battle" => "RUNE_BATTLE_ICON",
            "unit_rune_rapid_fire" => "RUNE_RAPID_FIRE_ICON",
            "unit_rune_strollaz" => "RUNE_STROLLAZ_ICON",
            _ => "RUNE_EMPTY_ICON"
        };
    }

    private void PromptUnitRunes(int tier)
    {
        var available = UnitRunes
            .Where(x => x.CareerTier <= tier)
            .Where(x => x.EnchantmentBluePrintIdList.All(blueprintId =>
                ItemTrait.All.Any(trait => trait.ItemTraitStringId == blueprintId)))
            .ToList();

        var list = new List<InquiryElement>();

        var currentRunes = GetCurrentActiveRunes(_currentCharacter);
        var currentRuneIds = currentRunes?.Select(r => r.EffectId).ToList() ?? new List<string>();
        var hasRunes = currentRunes != null && currentRunes.Any();

        var warningText = new TextObject("");
        if (hasRunes)
        {
            var runeNames = string.Join(", ", currentRunes.Select(r => r.RuneName.ToString()));
            warningText = TORTextHelper.GetTextObject("tor_unit_rune_warning_text", "WARNING : Current {CURRENT_RUNE} will be removed without compensation.");
            warningText.SetTextVariable("CURRENT_RUNE", runeNames);
        }

        foreach (var unitRune in available)
        {
            // Skip runes already applied to this unit
            if (currentRuneIds.Contains(unitRune.EffectId))
            {
                continue;
            }

            var blueprintList = unitRune.EnchantmentBluePrintIdList;

            var hint = unitRune.HintText;

            var hasIngredients = HasIngredientsForUse(blueprintList, out var failed);

            var icon = GetRuneIcon(unitRune.EffectId);
            var displayText = $"{{{icon}}}{unitRune.RuneName}";

            if (failed.Any(x => x.notKnown == true))
            {
                var entries = new StringBuilder();
                foreach (var entry in failed)
                {
                    var trait = ItemTrait.All.FirstOrDefault(x => x.ItemTraitStringId == entry.Id);
                    entries.Append("{newline}"+trait.ItemTraitName);
                }
                
                GameTexts.SetVariable("UNKNOWN_RUNES_LIST", entries.ToString());

                hint = TORTextHelper.GetTextObject("tor_unit_rune_unknown_runes_text", "You do not know the required runes: {REQUIRED_RUNES_LIST}");
                
                
                list.Add(new InquiryElement(unitRune, new TextObject(displayText).ToString(), null, false, hint.ToString()));
                continue;
            }

            // Build required runes display
            var itemTraits = ItemTrait.All.WhereQ(x => blueprintList.Contains(x.ItemTraitStringId)).ToList();
            var requiredRunesEntries = new StringBuilder();
            foreach (var trait in itemTraits)
            {
                requiredRunesEntries.Append(trait.ItemTraitName + "{newline}");
            }

            // Build cost display - aggregate costs by ingredient type
            var costEntries = new StringBuilder();
            var itemRoster = Hero.MainHero.PartyBelongedTo.Party.ItemRoster;

            // Aggregate total cost per ingredient
            var ingredientCosts = new Dictionary<ItemObject, int>();
            foreach (var itemTrait in itemTraits)
            {
                var cost = GetIngredientCost(itemTrait);
                var ingredient = TorEnchantingIngredients.GetItemObjectForIngredient(itemTrait.IngredientItem);

                if (ingredient != null)
                {
                    if (ingredientCosts.ContainsKey(ingredient))
                    {
                        ingredientCosts[ingredient] += cost;
                    }
                    else
                    {
                        ingredientCosts[ingredient] = cost;
                    }
                }
            }

            // Display aggregated costs
            foreach (var kvp in ingredientCosts)
            {
                var availableCount = itemRoster.GetItemNumber(kvp.Key);
                costEntries.Append(kvp.Value + " (" + availableCount + ") " + kvp.Key.Name + "{newline}");
            }

            GameTexts.SetVariable("RUNE_COST_LIST", costEntries.ToString());
            GameTexts.SetVariable("RUNE_DESCRIPTION", hint.ToString());

            if (!hasIngredients)
            {
                hint = TORTextHelper.GetTextObject("tor_unit_rune_cost_insufficient", "{RUNE_DESCRIPTION}{newline}Required ingredients:{newline}{RUNE_COST_LIST}{newline}You do not have enough ingredients!");
            }
            else
            {
                hint = TORTextHelper.GetTextObject("tor_unit_rune_cost_display", "{RUNE_DESCRIPTION}{newline}{newline}Required ingredients:{newline}{RUNE_COST_LIST}");
            }

            list.Add(new InquiryElement(unitRune, new TextObject(displayText).ToString(), null, hasIngredients, hint.ToString()));
        }

        // Add remove option if unit has runes
        if (hasRunes)
        {
            var runeNames = string.Join(", ", currentRunes.Select(r => r.RuneName.ToString()));
            list.Add(CareerButtonHelper.CreateRemoveOption(runeNames, "tor_unit_rune_remove_hint", "Remove all runes from this unit without compensation."));
        }

        var count = GetMaxRunesPerUnit();

        var title = TORTextHelper.GetTextObject("tor_unit_rune_title", "Unit runes");
        var text = TORTextHelper.GetTextObject("tor_unit_rune_description", "Choose a rune to add to the equipment of your units. {RUNE_WARNING_TEXT}");
        text.SetTextVariable("RUNE_WARNING_TEXT", warningText);
        var inquirydata = new MultiSelectionInquiryData(title.ToString(),
            text.ToString(), list, true, 1, count, TORTextHelper.GetText("tor_inquiry_confirm_text", "Confirm"),
            TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"), SelectedRunes, null);

        MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
    }


    private List<UnitRune> GetCurrentActiveRunes(CharacterObject character)
    {
        return CareerButtonHelper.GetCurrentActiveItems(character, UnitRunes, r => r.EffectId);
    }

    private void SelectedRunes(List<InquiryElement> inquiryElements)
    {
        var currentRunes = GetCurrentActiveRunes(_currentCharacter);

        CareerButtonHelper.ProcessSelection(
            _currentCharacter,
            inquiryElements,
            currentRunes,
            rune => rune.EffectId,
            rune => DeductRuneCost(rune)
        );
    }

    private void DeductRuneCost(UnitRune rune)
    {
        var itemRoster = Hero.MainHero.PartyBelongedTo.ItemRoster;

        // Aggregate total cost per ingredient type
        var ingredientCosts = new Dictionary<ItemObject, int>();
        foreach (var traitId in rune.EnchantmentBluePrintIdList)
        {
            var itemTrait = ItemTrait.All.FirstOrDefault(x => x.ItemTraitStringId == traitId);
            if (itemTrait == null) continue;

            var cost = GetIngredientCost(itemTrait);
            var ingredient = TorEnchantingIngredients.GetItemObjectForIngredient(itemTrait.IngredientItem);
            if (ingredient == null) continue;

            if (ingredientCosts.ContainsKey(ingredient))
            {
                ingredientCosts[ingredient] += cost;
            }
            else
            {
                ingredientCosts[ingredient] = cost;
            }
        }

        // Remove aggregated costs
        foreach (var kvp in ingredientCosts)
        {
            itemRoster.AddToCounts(kvp.Key, -kvp.Value);
        }
    }

    private int GetIngredientCost(ItemTrait itemTrait)
    {
        if (itemTrait == null) return 0;
        var costMultiplier = 3;

        if (Hero.MainHero.HasCareerChoice("LegacyOfGrungniPassive3"))
        {
            costMultiplier = 2;
        }

        return costMultiplier * itemTrait.IngredientAmount;
    }

    private bool HasIngredientsForUse(List<string> blueprintList, out List<(string Id, ItemObject ingredient, int cost, int available, bool notKnown)> failed)
    {
        failed = [];
        var itemTraits = ItemTrait.All.WhereQ(x => blueprintList.Contains(x.ItemTraitStringId));

        var itemRoster = Hero.MainHero.PartyBelongedTo.Party.ItemRoster;

        // First check for unknown runes
        foreach (var itemTrait in itemTraits)
        {
            bool notKnown = !Hero.MainHero.HasKnownEnchantmentBlueprint(itemTrait.ItemTraitStringId);
            if (notKnown)
            {
                var cost = GetIngredientCost(itemTrait);
                var ingredient = TorEnchantingIngredients.GetItemObjectForIngredient(itemTrait.IngredientItem);
                var available = itemRoster.GetItemNumber(ingredient);
                failed.Add((itemTrait.ItemTraitStringId, ingredient, cost, available, notKnown));
            }
        }

        // If any runes are unknown, return early
        if (failed.Any())
        {
            return false;
        }

        // Aggregate total cost per ingredient type
        var ingredientCosts = new Dictionary<ItemObject, int>();
        foreach (var itemTrait in itemTraits)
        {
            var cost = GetIngredientCost(itemTrait);
            var ingredient = TorEnchantingIngredients.GetItemObjectForIngredient(itemTrait.IngredientItem);

            if (ingredient != null)
            {
                if (ingredientCosts.ContainsKey(ingredient))
                {
                    ingredientCosts[ingredient] += cost;
                }
                else
                {
                    ingredientCosts[ingredient] = cost;
                }
            }
        }

        // Check if we have enough of each aggregated ingredient
        foreach (var kvp in ingredientCosts)
        {
            var ingredient = kvp.Key;
            var totalCost = kvp.Value;
            var available = itemRoster.GetItemNumber(ingredient);

            if (available < totalCost)
            {
                failed.Add(("", ingredient, totalCost, available, false));
            }
        }

        return !failed.Any();
    }

    public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner)
    {
        _setCharacter = characterObject;
        
        if (!Hero.MainHero.HasAttribute(CharacterAttributes.PLAYER_RUNESMITH))
        {
            return false;
        }
        
        return isPrisoner == false && characterObject.Culture.StringId == TORConstants.Cultures.DAWI;
    }

    public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner)
    {
        displayText = null;
        var currentRunes = GetCurrentActiveRunes(characterObject);
        var hasRunes = currentRunes != null && currentRunes.Any();

        if (characterObject.IsHero)
        {
            displayText = TORTextHelper.GetTextObject("tor_unit_rune_not_for_heroes_text", "Doesnt work for heroes");
            return false;
        }

        if (hasRunes)
        {
            displayText = CareerButtonHelper.BuildCurrentItemsDisplayText(
                currentRunes,
                r => r.RuneName.ToString(),
                r => r.HintText.ToString()
            );
        }

        var extendedInfo = Hero.MainHero.GetExtendedInfo();

        if (!extendedInfo.KnownEnchantmentBlueprints.AnyQ())
        {
            displayText = TORTextHelper.GetTextObject("tor_unit_rune_no_runes_known_text", "Hero doesn't know any Runes yet");
            return false;
        }

        if (Hero.MainHero.CurrentSettlement == null || (Hero.MainHero.CurrentSettlement != null && !Hero.MainHero.CurrentSettlement.IsDwarfKarak()))
        {
            if (!hasRunes)
            {
                displayText = TORTextHelper.GetTextObject("tor_unit_rune_only_in_karak_text", "Only possible inside a dwarf Karak. Visit a Dwarf Karak");
            }

            return false;
        }

        if (!hasRunes)
        {
            displayText = TORTextHelper.GetTextObject("tor_unit_rune_add_rune_text", "add a Rune for Units");
        }

        return true;
    }

}


public class UnitRune()
{
    private readonly int _price;
    private readonly TorTradeGoodType _enchantmentGood;
    public List<string> EnchantmentBluePrintIdList { get; }
    public TextObject RuneName { get; set; }
    public string Id { get; set; }
    public string EffectId { get; set; }
    public TextObject HintText { get; set; }

    public int CareerTier { get; }

    public UnitRune(string id, TextObject text, TextObject hintText, string effect, List<string> enchantmentBluePrintIdList, int careerTier) : this()
    {
        this.RuneName = text;
        HintText = hintText;
        Id = id;
        EffectId = effect;
        EnchantmentBluePrintIdList = enchantmentBluePrintIdList;
        CareerTier = careerTier;
    }


}