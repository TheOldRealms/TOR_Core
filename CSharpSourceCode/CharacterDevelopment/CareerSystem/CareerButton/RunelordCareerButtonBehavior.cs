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
    private string _fireIcon = TORPaths.NormalizeAssetPath("CareerSystem\\aqshy");
    private string _lightIcon = TORPaths.NormalizeAssetPath("CareerSystem\\hysh");
    private string _heavensIcon = TORPaths.NormalizeAssetPath("CareerSystem\\azyr");
    private string _lifeIcon = TORPaths.NormalizeAssetPath("CareerSystem\\ghyran");
    private string _beastIcon = TORPaths.NormalizeAssetPath("CareerSystem\\ghur");
    private string _grungniRune = TORPaths.NormalizeAssetPath("CareerSystem\\chamon");
    private string _deathIcon = TORPaths.NormalizeAssetPath("CareerSystem\\chamon");

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

            if (failed.Any(x => x.notKnown == true))
            {
                var entries = new StringBuilder();
                foreach (var entry in failed)
                {
                    var trait = ItemTrait.All.FirstOrDefault(x => x.ItemTraitStringId == entry.Id);
                    entries.Append(trait.ItemTraitName + "{newline}");
                }

                hint = TORTextHelper.GetTextObject("tor_unit_rune_unknown_runes_text", "You do not know the required runes: " + entries.ToString());
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
                hint = TORTextHelper.GetTextObject("tor_unit_rune_not_enough_ingredients_text", "You  do not have enough ingredients requires : {RUNECRAFT_FAILED_ENTRIES}");
            }

            list.Add(new InquiryElement(unitRune, unitRune.RuneName.ToString(), null, hasIngredients, hint.ToString()));
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
            displayText = TORTextHelper.GetTextObject("tor_unit_rune_not_for_heroes_text", "Doesnt work for heroes");
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