using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;
using TOR_Core.Extensions.UI;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;

public class SlayerCareerButtonBehavior(CareerObject career) : CareerButtonBehaviorBase(career)
{
    public override string CareerButtonIcon => "CareerSystem\\slayer_icon";

    private const string SlayerUnitId = "tor_dw_slayer";
    private const string GrimnirReligionId = "cult_of_grimnir";
    private const int OathgoldGainPerTier = 4;
    private const int GoldGainPerTier = 250;
    private const int FaithXpPerTier = 50;
    private const float DevotionPerTier = 0.5f;

    public override void ButtonClickedEvent(CharacterObject characterObject, bool isPrisoner, bool shiftClick)
    {
        var slayerUnit = MBObjectManager.Instance.GetObject<CharacterObject>(SlayerUnitId);
        var tier = characterObject.Tier;

        if (shiftClick)
        {
            var buyableTroops = 5;

            for (int i = 0; i < buyableTroops; i++)
            {
                ApplyRewards(tier);
                CareerButtonHelper.ExchangeUnitForNewUnit(characterObject, slayerUnit, true);
            }
        }
        else
        {
            ApplyRewards(tier);
            CareerButtonHelper.ExchangeUnitForNewUnit(characterObject, slayerUnit, true);
        }

        PartyVMExtension.ViewModelInstance.RefreshValues();
    }

    private void ApplyRewards(int tier)
    {
        CustomResourceManager.AddResourceChanges(Hero.MainHero.GetCultureSpecificCustomResource(), OathgoldGainPerTier * tier);
        PartyScreenHelper.GetActivePartyState().PartyScreenLogic.CurrentData.PartyGoldChangeAmount += tier * GoldGainPerTier;
        Hero.MainHero.AddSkillXp(TORSkills.Faith, FaithXpPerTier * tier);

        var religion = ReligionObject.All.FirstOrDefault(x => x.StringId == GrimnirReligionId);
        if (religion != null)
        {
            Hero.MainHero.AddReligiousInfluence(religion, (int)(DevotionPerTier * tier));
        }
    }

    public override bool ShouldButtonBeVisible(CharacterObject characterObject, bool isPrisoner)
    {

        if (!Hero.MainHero.HasCareerChoice("ShameOfTheAncestorsPassive4"))
        {
            return false;
        }
        if (isPrisoner)
        {
            return false;
        }
        if (!characterObject.IsRegular)
            return false;

        if (characterObject.Culture.StringId != TORConstants.Cultures.DAWI)
        {
            return false;
        }

        if (characterObject.StringId.Contains("slayer"))
        {
            return false;
        }

        return true;
    }

    public override bool ShouldButtonBeActive(CharacterObject characterObject, out TextObject displayText, bool isPrisoner)
    {
        displayText = new TextObject("");

        if (characterObject.IsEliteTroop())
        {
            if (characterObject.Tier < 6)
            {
                displayText = TORTextHelper.GetTextObject("tor_slayer_tier_too_low_text", "Tier too low. Unit can't be ashamed");
                return false;
            }
        }
        else
        {
            if (characterObject.Tier < 4)
            {
                displayText = TORTextHelper.GetTextObject("tor_slayer_tier_too_low_text", "Tier too low. Unit can't be ashamed");
                return false;
            }
        }

        return true;
    }
}