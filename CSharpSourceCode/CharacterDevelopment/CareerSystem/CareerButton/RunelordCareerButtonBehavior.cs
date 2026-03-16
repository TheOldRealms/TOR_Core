using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Extensions.UI;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;

public class RunelordCareerButtonBehavior : CareerButtonBehaviorBase
{
    private string _fireIcon = "CareerSystem\\aqshy";
    private string _lightIcon = "CareerSystem\\hysh";
    private string _heavensIcon = "CareerSystem\\azyr";
    private string _lifeIcon = "CareerSystem\\ghyran";
    private string _beastIcon = "CareerSystem\\ghur";
    private string _grungniRune = "CareerSystem\\chamon";
    private string _deathIcon = "CareerSystem\\chamon";

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

    private CharacterObject _currentCharacter = null;
    private CharacterObject _setCharacter;

    public override string CareerButtonIcon
    {
        get
        {
            var currentRuneId = GetCurrentRuneId(_setCharacter);

            if (currentRuneId == null)
            {
                return "CareerSystem\\rune_empty";
            }

            return currentRuneId switch
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
        MBTextManager.SetTextVariable("DEATH_ICON", string.Format("<img src=\"{0}\"/>", _grungniRune));

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

        var currentRuneId = GetCurrentRuneId(_currentCharacter);
        var warningText = new TextObject("");
        if (currentRuneId != null)
        {
            warningText = TORTextHelper.GetTextObject("tor_unit_rune_warning_text", "WARNING : Current {CURRENT_RUNE} will removed without compensation.");
        }


        foreach (var unitRune in available)
        {

            if (currentRuneId == unitRune.EffectId)
            {
                warningText.SetTextVariable("CURRENT_RUNE", UnitRunes.First(x => x.EffectId == currentRuneId).RuneName.ToString());
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
                    entries.Append(trait.ItemTraitName + "{newline}");
                }

                hint = TORTextHelper.GetTextObject("tor_unit_rune_unknown_runes_text", "You do not know the required runes: " + entries.ToString());
                list.Add(new InquiryElement(unitRune, new TextObject(displayText).ToString(), null, false, hint.ToString()));
                continue;
            }

            // Build cost display - aggregate costs by ingredient type
            var costEntries = new StringBuilder();
            var itemTraits = ItemTrait.All.WhereQ(x => blueprintList.Contains(x.ItemTraitStringId));
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
                hint = TORTextHelper.GetTextObject("tor_unit_rune_cost_insufficient", "{RUNE_DESCRIPTION}{newline}{newline}Required ingredients:{newline}{RUNE_COST_LIST}{newline}You do not have enough ingredients!");
            }
            else
            {
                hint = TORTextHelper.GetTextObject("tor_unit_rune_cost_display", "{RUNE_DESCRIPTION}{newline}{newline}Required ingredients:{newline}{RUNE_COST_LIST}");
            }

            list.Add(new InquiryElement(unitRune, new TextObject(displayText).ToString(), null, hasIngredients, hint.ToString()));
        }
        var title = TORTextHelper.GetTextObject("tor_unit_rune_title", "Unit runes");
        var text = TORTextHelper.GetTextObject("tor_unit_rune_description", "Choose a rune to add to the equipment of your units. {RUNE_WARNING_TEXT}");
        text.SetTextVariable("RUNE_WARNING_TEXT", warningText);
        var inquirydata = new MultiSelectionInquiryData(title.ToString(),
            text.ToString(), list, true, 1, 1, TORTextHelper.GetText("tor_inquiry_confirm_text", "Confirm"),
            TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"), SelectedRune, null);

        MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
    }


    private string GetCurrentRuneId(CharacterObject character)
    {
        if (character == null) return null;
        var partyExtendedInfo =
            ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);
        var attributes = partyExtendedInfo.TroopAttributes.FirstOrDefault(x => x.Key == character.StringId).Value;
        return attributes?.FirstOrDefault(x => UnitRunes.Any(y => y.EffectId == x));
    }

    private void SelectedRune(List<InquiryElement> inquirydata)
    {
        var rune = (UnitRune)inquirydata.FirstOrDefault().Identifier;

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

        var currentRuneId = GetCurrentRuneId(_currentCharacter);
        var partyExtendedInfo = ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);
        if (currentRuneId != null)
        {
            partyExtendedInfo.RemoveTroopAttribute(_currentCharacter.StringId, currentRuneId);
        }

        partyExtendedInfo.AddTroopAttribute(_currentCharacter, rune.EffectId);

        ExtendedInfoManager.Instance.ValidatePartyInfos(MobileParty.MainParty);

        if (PartyVMExtension.ViewModelInstance != null)
        {
            PartyVMExtension.ViewModelInstance.RefreshValues();
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
        
        if (!Hero.MainHero.HasAttribute("PlayerRunesmith"))
        {
            return false;
        }
        
        return isPrisoner == false && characterObject.Culture.StringId == TORConstants.Cultures.DAWI;
    }

    public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner)
    {
        var hasRune = false;
        displayText = null;

        if (characterObject.IsHero)
        {
            displayText = TORTextHelper.GetTextObject("tor_unit_rune_not_for_heroes_text", "Doesnt work for heroes");
            return false;
        }

        if (characterObject.HasUnitRune())
        {
            var id = GetCurrentRuneId(characterObject);
            if (id != null)
            {
                var rune = UnitRunes.FirstOrDefault(x => x.EffectId == id);

                if (rune != null)
                {
                    GameTexts.SetVariable("RUNE_NAME", rune.RuneName.ToString());
                    GameTexts.SetVariable("RUNE_DESC", rune.HintText.ToString());
                    displayText = TORTextHelper.GetTextObject("tor_unit_rune_active_display", "{RUNE_NAME}{newline}{RUNE_DESC}");
                    hasRune = true;
                }
            }

        }

        var extendedInfo = Hero.MainHero.GetExtendedInfo();

        if (!extendedInfo.KnownEnchantmentBlueprints.AnyQ())
        {
            displayText = TORTextHelper.GetTextObject("tor_unit_rune_no_runes_known_text", "Hero doesn't know any Runes yet");
            return false;
        }


        if (Hero.MainHero.CurrentSettlement == null || Hero.MainHero.CurrentSettlement != null && !Hero.MainHero.CurrentSettlement.IsDwarfKarak())
        {
            if (!hasRune)
            {
                displayText = TORTextHelper.GetTextObject("tor_unit_rune_only_in_karak_text", "Only possible inside a dwarf Karak. Visit a Dwarf Karak");
            }

            return false;
        }



        if (!hasRune)
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