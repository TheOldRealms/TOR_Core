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

public class RunelordCareerButtonBehavior(CareerObject career) : CareerButtonBehaviorBase(career)
{
    private string _fireIcon = "CareerSystem\\aqshy";
    private string _lightIcon = "CareerSystem\\hysh";
    private string _heavensIcon = "CareerSystem\\azyr";
    private string _lifeIcon = "CareerSystem\\ghyran";
    private string _beastIcon = "CareerSystem\\ghur";
    private string _grungniRune = "CareerSystem\\chamon";
    private string _deathIcon = "CareerSystem\\chamon";

    private static readonly List<UnitRune> UnitRunes =
    [
        new("unit_rune_guarding", new TextObject("Rune of Guarding"), new TextObject("15% extra physical resistance."), "unit_rune_guarding",
            new List<string> {"dw_rune_stone","dw_rune_protection", "dw_rune_shielding" }, 1),
        new("unit_rune_sanctuary", new TextObject("Rune of Sanctuary"), new TextObject("30% extra magic resistance."), "unit_rune_sanctuary",
            new List<string> { "dw_rune_vigour","dw_rune_parrying","dw_rune_protection" }, 1),
        new("unit_rune_battle", new TextObject("Rune of Battle"), new TextObject("30% extra physical damage."), "unit_rune_battle",
            new List<string> { "dw_rune_striking","dw_rune_might","dw_rune_fire" }, 1),
        new("unit_rune_retribution", new TextObject("Rune of Retribution"),
            new TextObject("25% extra fire resistance, 35% extra fire damage."), "unit_rune_retribution", new List<string> { "dw_rune_spell_eating","dw_rune_preservation","dw_rune_fortitude" }, 2),
        new("unit_rune_rapid_fire", new TextObject("Rune of Rapid Fire"), new TextObject("60% extra reload speed."), "unit_rune_rapid_fire",
            new List<string> { "dw_rune_head_wrecking","dw_rune_beastslaying","dw_rune_reloading" }, 2),
        new("unit_rune_strollaz", new TextObject("Strollaz’ Rune"), new TextObject("35% extra movement speed."), "unit_rune_strollaz",
            new List<string> { "dw_rune_speed","dw_rune_striking","dw_rune_impact" }, 2),
        new("unit_rune_valaya", new TextObject("Master Rune of Valaya"), new TextObject("30% extra Ward Save."), "unit_rune_valaya",
            new List<string> { "dw_master_rune_adamant","dw_master_rune_gromril","dw_master_rune_skaldour" }, 3),
        new("unit_rune_grungni", new TextObject("Master Rune of Grungni"), new TextObject("Reduce incoming ranged damage by 75%."),
            "unit_rune_grungni", new List<string> { "dw_master_rune_steel","dw_master_rune_gromril","dw_master_rune_preservation" }, 3),
        new("unit_rune_grimnir", new TextObject("Master Rune of Grimnir"),
            new TextObject("20% extra attack speed, 35% extra physical and fire damage."), "unit_rune_grimnir", new List<string> { "dw_rune_master_swiftness","dw_rune_master_breaking","dw_rune_fury" }, 3)
    ];


    public static List<string> GetRuneIds => UnitRunes.SelectQ(x => x.EffectId).ToListQ();

    private CharacterObject _currentCharacter = null;


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
            warningText = new TextObject("WARNING : Current {CURRENT_RUNE} will removed without compensation.");
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

            if (failed.Any(x => x.notKnown == true))
            {
                var entries = new StringBuilder();
                foreach (var entry in failed)
                {
                    var trait = ItemTrait.All.FirstOrDefault(x => x.ItemTraitStringId == entry.Id);
                    entries.Append(trait.ItemTraitName + "{newline}");
                }

                hint = new TextObject("You do not know the required runes: " + entries.ToString());
                list.Add(new InquiryElement(unitRune, unitRune.RuneName.ToString(), null, false, hint.ToString()));
                continue;
            }
            if (!hasIngredients)
            {
                var entries = new StringBuilder();
                foreach (var entry in failed)
                {
                    entries.Append(entry.cost + " " + "(" + entry.available + ")" + entry.ingredient.Name + "{newline}");
                }

                GameTexts.SetVariable("RUNECRAFT_FAILED_ENTRIES", entries.ToString());
                hint = new TextObject("You  do not have enough ingredients requires : {RUNECRAFT_FAILED_ENTRIES}");
            }

            list.Add(new InquiryElement(unitRune, unitRune.RuneName.ToString(), null, hasIngredients, hint.ToString()));
        }
        var title = new TextObject("{=unit_rune_title_str}Unit runes");
        var text = new TextObject("{=unit_rune_text_description_str}Choose a rune to add to the equipment of your units. {RUNE_WARNING_TEXT}");
        text.SetTextVariable("RUNE_WARNING_TEXT", warningText);
        var inquirydata = new MultiSelectionInquiryData(title.ToString(),
            text.ToString(), list, true, 1, 1, "Confirm",
            "Cancel", SelectedRune, null);

        MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
    }


    private string GetCurrentRuneId(CharacterObject character)
    {
        var partyExtendedInfo =
            ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);
        var attributes = partyExtendedInfo.TroopAttributes.FirstOrDefault(x => x.Key == character.StringId).Value;
        return attributes?.FirstOrDefault(x => UnitRunes.Any(y => y.EffectId == x));
    }

    private void SelectedRune(List<InquiryElement> inquirydata)
    {
        var rune = (UnitRune)inquirydata.FirstOrDefault().Identifier;

        var itemRoster = Hero.MainHero.PartyBelongedTo.ItemRoster;
        foreach (var traitId in rune.EnchantmentBluePrintIdList)
        {
            var itemTrait = ItemTrait.All.FirstOrDefault(x => x.ItemTraitStringId == traitId);
            var cost = GetIngredientCost(itemTrait);
            var ingredient = TorEnchantingIngredients.GetItemObjectForIngredient(itemTrait.IngredientItem);

            itemRoster.AddToCounts(ingredient, -cost);

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

        foreach (var itemTrait in itemTraits)
        {
            bool notKnown = !Hero.MainHero.HasKnownEnchantmentBlueprint(itemTrait.ItemTraitStringId);
            var cost = GetIngredientCost(itemTrait);
            var ingredient = TorEnchantingIngredients.GetItemObjectForIngredient(itemTrait.IngredientItem);

            var available = itemRoster.GetItemNumber(ingredient);
            if (notKnown || available < cost)
            {
                failed.Add((itemTrait.ItemTraitStringId, ingredient, cost, available, notKnown));
            }
        }

        return !failed.Any();
    }

    public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner)
    {
        return isPrisoner == false && characterObject.Culture.StringId == TORConstants.Cultures.DAWI;
    }

    public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner)
    {
        var hasRune = false;
        displayText = null;

        if (characterObject.IsHero)
        {
            displayText = new TextObject("Doesnt work for heroes");
            return false;
        }

        if (characterObject.HasUnitRune())
        {
            var id = GetCurrentRuneId(characterObject);
            if (id != null)
            {
                var rune = UnitRunes.FirstOrDefault(x => x.Id == id);

                displayText = new TextObject(rune.RuneName.ToString());
                hasRune = true;
            }

        }

        var extendedInfo = Hero.MainHero.GetExtendedInfo();

        if (!extendedInfo.KnownEnchantmentBlueprints.AnyQ())
        {
            displayText = new TextObject("Hero doesn't know any Runes yet");
            return false;
        }


        if (Hero.MainHero.CurrentSettlement == null || Hero.MainHero.CurrentSettlement != null && !Hero.MainHero.CurrentSettlement.IsDwarfKarak())
        {
            if (!hasRune)
            {
                displayText = new TextObject("Only possible inside a dwarf Karak. Visit a Dwarf Karak");
            }

            return false;
        }



        if (!hasRune)
        {
            displayText = new TextObject("add a Rune for Units");
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