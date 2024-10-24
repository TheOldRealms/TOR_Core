using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core;
using TOR_Core.Extensions;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.CustomResources;

namespace TOR_Core.HarmonyPatches
{
    
    [HarmonyPatch]
    public class CustomResourcePatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PartyCharacterVM), "InitializeUpgrades")]
        public static bool AdditionalUpgradeLogic(PartyCharacterVM __instance, PartyScreenLogic ____partyScreenLogic)
        {
            PartyScreenLogic partyScreenLogic = ____partyScreenLogic;
            //if (!__instance.Character.UpgradeTargets.Any(x => x.HasCustomResourceUpgradeRequirement())) return true;
            if (__instance.IsUpgradableTroop)
            {
                for (int i = 0; i < __instance.Character.UpgradeTargets.Length; i++)
                {
                    //variables
                    var troopToUpgrade = __instance.Character;
                    var upgradeTarget = troopToUpgrade.UpgradeTargets[i];
                    int upgradeGoldCost = troopToUpgrade.GetUpgradeGoldCost(PartyBase.MainParty, i);
                    int upgradeXpCost = troopToUpgrade.GetUpgradeXpCost(PartyBase.MainParty, i);
                    var customResourceRequirement = upgradeTarget.GetCustomResourceRequiredForUpgrade(true);
                    bool doesUpgradeRequireItems = upgradeTarget.UpgradeRequiresItemFromCategory != null;
                    bool doesUpgradeRequireResources = customResourceRequirement != null;
                    
                    PerkObject requiredPerk;

                    bool doesPartyHaveRequiredPerks = Campaign.Current.Models.PartyTroopUpgradeModel.DoesPartyHaveRequiredPerksForUpgrade(PartyBase.MainParty, troopToUpgrade, upgradeTarget, out requiredPerk);
                    bool doesPartyHaveRequiredItems = doesUpgradeRequireItems ? __instance.GetNumOfCategoryItemPartyHas(partyScreenLogic.RightOwnerParty.ItemRoster, upgradeTarget.UpgradeRequiresItemFromCategory) > 0 : true;
                    bool doesPartyHaveEnoughGold = Hero.MainHero.Gold + partyScreenLogic.CurrentData.PartyGoldChangeAmount >= upgradeGoldCost;
                    int pendingResourceChange = (doesUpgradeRequireResources && CustomResourceManager.GetPendingResources().ContainsKey(customResourceRequirement.Item1)) ? CustomResourceManager.GetPendingResources()[customResourceRequirement.Item1] : 0;
                    bool doesPartyHaveEnoughResources = doesUpgradeRequireResources ? Hero.MainHero.GetCustomResourceValue(customResourceRequirement.Item1.StringId) - pendingResourceChange > customResourceRequirement.Item2 : true;
                    bool doesTroopHaveEnoughXp = __instance.Troop.Xp >= upgradeXpCost;

                    bool isUpgradePossible = !partyScreenLogic.IsTroopUpgradesDisabled &&
                        upgradeTarget.Level > troopToUpgrade.Level &&
                        doesPartyHaveRequiredPerks &&
                        doesPartyHaveRequiredItems &&
                        doesPartyHaveEnoughGold &&
                        doesPartyHaveEnoughResources &&
                        doesTroopHaveEnoughXp;

                    int numberOfAvailableUpgrades = 0;

                    //calculate number of possible upgrades
                    int numberPossibleWithGold = upgradeGoldCost != 0 ? (Hero.MainHero.Gold + partyScreenLogic.CurrentData.PartyGoldChangeAmount) / upgradeGoldCost : __instance.Troop.Number;
                    int numberPossibleWithItems = doesUpgradeRequireItems ? __instance.GetNumOfCategoryItemPartyHas(partyScreenLogic.RightOwnerParty.ItemRoster, upgradeTarget.UpgradeRequiresItemFromCategory) : __instance.Troop.Number;
                    int numberPossibleWithXp = __instance.Troop.Xp / upgradeXpCost;
                    int numberPossibleWithResources = doesUpgradeRequireResources ? (int)(Hero.MainHero.GetCustomResourceValue(customResourceRequirement.Item1.StringId) / customResourceRequirement.Item2) : __instance.Troop.Number;
                    
                    numberOfAvailableUpgrades = MathF.Min(MathF.Min(numberPossibleWithGold, numberPossibleWithItems), MathF.Min(numberPossibleWithXp, numberPossibleWithResources));

                    numberOfAvailableUpgrades = isUpgradePossible ? numberOfAvailableUpgrades : 0;

                    string hintText = GetUpgradeHint(i, doesUpgradeRequireItems ? __instance.GetNumOfCategoryItemPartyHas(partyScreenLogic.RightOwnerParty.ItemRoster, upgradeTarget.UpgradeRequiresItemFromCategory) : 0, numberOfAvailableUpgrades, upgradeGoldCost, doesPartyHaveRequiredPerks, requiredPerk, troopToUpgrade, __instance.Troop, partyScreenLogic.CurrentData.PartyGoldChangeAmount, PartyCharacterVM.EntireStackShortcutKeyText, PartyCharacterVM.FiveStackShortcutKeyText);

                    __instance.Upgrades[i].Refresh(numberOfAvailableUpgrades, hintText, isUpgradePossible, false, doesPartyHaveRequiredItems, doesPartyHaveRequiredPerks);
                    if (i == 0)
                    {
                        __instance.UpgradeCostText = upgradeGoldCost.ToString();
                        __instance.HasEnoughGold = doesPartyHaveEnoughGold;
                        __instance.NumOfReadyToUpgradeTroops = numberPossibleWithXp;
                        __instance.MaxXP = __instance.Character.GetUpgradeXpCost(PartyBase.MainParty, i);
                        __instance.CurrentXP = ((__instance.Troop.Xp >= __instance.Troop.Number * __instance.MaxXP) ? __instance.MaxXP : (__instance.Troop.Xp % __instance.MaxXP));
                    }
                }
                __instance.AnyUpgradeHasRequirement = Enumerable.Any(__instance.Upgrades, (UpgradeTargetVM x) => x.Requirements.HasItemRequirement || x.Requirements.HasPerkRequirement);
            }
            int totalUpgradeable = 0;
            foreach (UpgradeTargetVM upgradeTargetVM in __instance.Upgrades)
            {
                if (upgradeTargetVM.AvailableUpgrades > totalUpgradeable)
                {
                    totalUpgradeable = upgradeTargetVM.AvailableUpgrades;
                }
            }
            __instance.NumOfUpgradeableTroops = totalUpgradeable;
            __instance.IsTroopUpgradable = (__instance.NumOfUpgradeableTroops > 0 && !partyScreenLogic.IsTroopUpgradesDisabled);
            GameTexts.SetVariable("LEFT", __instance.NumOfReadyToUpgradeTroops);
            GameTexts.SetVariable("RIGHT", __instance.Troop.Number);
            __instance.StrNumOfUpgradableTroop = GameTexts.FindText("str_LEFT_over_RIGHT", null).ToString();
            __instance.OnPropertyChanged("AmountOfUpgrades");

            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PartyVM), "Update")]
        public static void UpgradeTroopPostFix(PartyVM __instance, PartyScreenLogic.PartyCommand command)
        {
            CustomResourceManager.OnPartyScreenTroopUpgrade(__instance, command);
        }

        public static string GetUpgradeHint(int index, int numOfItems, int availableUpgrades, int upgradeCoinCost, bool hasRequiredPerk, PerkObject requiredPerk, CharacterObject character, TroopRosterElement troop, int partyGoldChangeAmount, string entireStackShortcutKeyText, string fiveStackShortcutKeyText)
        {
            string text = null;
            CharacterObject characterObject = character.UpgradeTargets[index];
            int level = characterObject.Level;
            if (character.Culture.IsBandit ? (level >= character.Level) : (level > character.Level))
            {
                int upgradeXpCost = character.GetUpgradeXpCost(PartyBase.MainParty, index);
                GameTexts.SetVariable("newline", "\n");
                TextObject textObject = new TextObject("{=f4nc7FfE}Upgrade to {UPGRADE_NAME}", null);
                textObject.SetTextVariable("UPGRADE_NAME", characterObject.Name);
                text = textObject.ToString();
                if (troop.Xp < upgradeXpCost)
                {
                    TextObject textObject2 = new TextObject("{=Voa0sinH}Required: {NEEDED_EXP_AMOUNT}xp (You have {CURRENT_EXP_AMOUNT})", null);
                    textObject2.SetTextVariable("NEEDED_EXP_AMOUNT", upgradeXpCost);
                    textObject2.SetTextVariable("CURRENT_EXP_AMOUNT", troop.Xp);
                    GameTexts.SetVariable("STR1", text);
                    GameTexts.SetVariable("STR2", textObject2);
                    text = GameTexts.FindText("str_string_newline_string", null).ToString();
                }
                if (characterObject.UpgradeRequiresItemFromCategory != null)
                {
                    TextObject textObject3 = new TextObject((numOfItems > 0) ? "{=Raa4j4rF}Required: {UPGRADE_ITEM}" : "{=rThSy9ed}Required: {UPGRADE_ITEM} (You have none)", null);
                    textObject3.SetTextVariable("UPGRADE_ITEM", characterObject.UpgradeRequiresItemFromCategory.GetName().ToString());
                    GameTexts.SetVariable("STR1", text);
                    GameTexts.SetVariable("STR2", textObject3.ToString());
                    text = GameTexts.FindText("str_string_newline_string", null).ToString();
                }
                var resource = characterObject.GetCustomResourceRequiredForUpgrade(true);
                if (resource != null)
                {
                    TextObject resourceText = new TextObject("{=partyscreen_resource_text}Required: {NEEDED_AMOUNT} {RESOURCE_ICON}", null);
                    resourceText.SetTextVariable("NEEDED_AMOUNT", resource.Item2);
                    resourceText.SetTextVariable("RESOURCE_ICON", resource.Item1.GetCustomResourceIconAsText());
                    GameTexts.SetVariable("STR1", text);
                    GameTexts.SetVariable("STR2", resourceText);
                    text = GameTexts.FindText("str_string_newline_string", null).ToString();
                }
                TextObject textObject4 = new TextObject((Hero.MainHero.Gold + partyGoldChangeAmount < upgradeCoinCost) ? "{=63Ic1Ahe}Cost: {UPGRADE_COST} (You don't have)" : "{=McJjNM50}Cost: {UPGRADE_COST}", null);
                textObject4.SetTextVariable("UPGRADE_COST", upgradeCoinCost);
                GameTexts.SetVariable("STR1", textObject4);
                GameTexts.SetVariable("STR2", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
                string content = GameTexts.FindText("str_STR1_STR2", null).ToString();
                GameTexts.SetVariable("STR1", text);
                GameTexts.SetVariable("STR2", content);
                text = GameTexts.FindText("str_string_newline_string", null).ToString();
                if (!hasRequiredPerk)
                {
                    GameTexts.SetVariable("STR1", text);
                    TextObject textObject5 = new TextObject("{=68IlDbA2}You need to have {PERK_NAME} perk to upgrade a bandit troop to a normal troop.", null);
                    textObject5.SetTextVariable("PERK_NAME", requiredPerk.Name);
                    GameTexts.SetVariable("STR2", textObject5);
                    text = GameTexts.FindText("str_string_newline_string", null).ToString();
                }
                GameTexts.SetVariable("STR2", "");
                if (availableUpgrades > 0 && !string.IsNullOrEmpty(entireStackShortcutKeyText))
                {
                    GameTexts.SetVariable("KEY_NAME", entireStackShortcutKeyText);
                    string content2 = GameTexts.FindText("str_entire_stack_shortcut_upgrade_units", null).ToString();
                    GameTexts.SetVariable("STR1", content2);
                    GameTexts.SetVariable("STR2", "");
                    if (availableUpgrades >= 5 && !string.IsNullOrEmpty(fiveStackShortcutKeyText))
                    {
                        GameTexts.SetVariable("KEY_NAME", fiveStackShortcutKeyText);
                        string content3 = GameTexts.FindText("str_five_stack_shortcut_upgrade_units", null).ToString();
                        GameTexts.SetVariable("STR2", content3);
                    }
                    string content4 = GameTexts.FindText("str_string_newline_string", null).ToString();
                    GameTexts.SetVariable("STR2", content4);
                }
                GameTexts.SetVariable("STR1", text);
                text = GameTexts.FindText("str_string_newline_string", null).ToString();
            }
            return text;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PartyScreenLogic), "DoneLogic")]
        public static void AdditionalPartyScreenClosingLogic(ref bool __result)
        {
            foreach (var resourceKVP in CustomResourceManager.GetPendingResources())
            {
                var currentResource = Hero.MainHero.GetCustomResourceValue(resourceKVP.Key.StringId);
                if(currentResource < resourceKVP.Value)
                {
                    MBInformationManager.AddQuickInformation(new($"You don't have enough {resourceKVP.Key.Name}."));
                    __result = false;
                    break;
                }
            }
        }
    }
}
