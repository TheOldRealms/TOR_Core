using HarmonyLib;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;

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
                    int pendingResourceChange = doesUpgradeRequireResources ? CustomResourceManager.GetPendingFor(customResourceRequirement.Item1.StringId) : 0;

                    bool doesPartyHaveEnoughResources = doesUpgradeRequireResources ? Hero.MainHero.GetCustomResourceValue(customResourceRequirement.Item1.StringId) - pendingResourceChange >= customResourceRequirement.Item2 : true;
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
                    int numberPossibleWithResources = doesUpgradeRequireResources ? (((int)Hero.MainHero.GetCustomResourceValue(customResourceRequirement.Item1.StringId) - pendingResourceChange) / customResourceRequirement.Item2) : __instance.Troop.Number;


                    numberOfAvailableUpgrades = MathF.Min(MathF.Min(numberPossibleWithGold, numberPossibleWithItems), MathF.Min(numberPossibleWithXp, numberPossibleWithResources));

                    numberOfAvailableUpgrades = isUpgradePossible ? numberOfAvailableUpgrades : 0;
                    bool canPartyUpgrade = !Campaign.Current.Models.PartyTroopUpgradeModel.CanPartyUpgradeTroopToTarget(PartyBase.MainParty, troopToUpgrade, upgradeTarget);

                    string hintText = GetUpgradeHint(i, doesUpgradeRequireItems ? __instance.GetNumOfCategoryItemPartyHas(partyScreenLogic.RightOwnerParty.ItemRoster, upgradeTarget.UpgradeRequiresItemFromCategory) : 0, numberOfAvailableUpgrades, upgradeGoldCost, doesPartyHaveRequiredPerks, requiredPerk, troopToUpgrade, __instance.Troop, partyScreenLogic.CurrentData.PartyGoldChangeAmount, partyScreenLogic.IsTroopUpgradesDisabled);

                    __instance.Upgrades[i].Refresh(numberOfAvailableUpgrades, isUpgradePossible, canPartyUpgrade, doesPartyHaveRequiredItems, doesPartyHaveRequiredPerks, hintText, troopToUpgrade.GetTraitLevel(DefaultTraits.NavalSoldier) != 0);
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PartyVM), "TransferAllCharacters")]
        private static void TransferAllCharactersPostFix(PartyVM __instance,
            PartyScreenLogic.PartyRosterSide rosterSide,
            PartyScreenLogic.TroopType type)
        {
            var partyScreenLogic = __instance.PartyScreenLogic;
            var roster = type == PartyScreenLogic.TroopType.Prisoner ? partyScreenLogic.PrisonerRosters[(int)rosterSide] : partyScreenLogic.MemberRosters[(int)rosterSide];

            foreach (var elem in roster.GetTroopRoster())
            {
                CustomResourceManager.OnTroopTransferred(__instance, rosterSide, elem.Character, elem.Number);
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(PartyVM), "OnTransferTroop")]
        public static void OnTransferTroopPostFix(PartyVM __instance, PartyCharacterVM troop,
            int newIndex,
            int transferAmount,
            PartyScreenLogic.PartyRosterSide fromSide)
        {
            CustomResourceManager.OnTroopTransferred(__instance, fromSide, troop.Character, transferAmount);
        }

        public static string GetUpgradeHint(int index, int numOfItems, int availableUpgrades, int upgradeCoinCost, bool hasRequiredPerk, PerkObject requiredPerk, CharacterObject character, TroopRosterElement troop, int partyGoldChangeAmount, bool areUpgradesDisabled)
        {
            if (areUpgradesDisabled)
            {
                return new TextObject("{=R4rTlKMU}Troop upgrades are currently disabled.", null).ToString();
            }
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
            }
            return text;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PartyScreenLogic), "AddCommand")]
        public static bool ClampUpgradeInAdd_Prefix(ref PartyScreenLogic.PartyCommand command)
        {
            if (command.Code != PartyScreenLogic.PartyCommandCode.UpgradeTroop)
                return true;

            var troopToUpgrade = command.Character;
            var upgradeTarget = troopToUpgrade.UpgradeTargets[command.UpgradeTarget];

            if (!TryGetUpgradeRequirement(upgradeTarget, out var resource, out var costPerUnit))
                return true;

            int requestedCount = GetTotalNumber(command);
            int appliedCount = CustomResourceManager.ClampMassUpgradeAndConsume(resource.StringId, costPerUnit, requestedCount);

            if (appliedCount < requestedCount)
                MBInformationManager.AddQuickInformation(new TextObject(
                    $"You only have enough {resource.Name} for {appliedCount} upgrade(s)."));
            SetTotalNumber(ref command, appliedCount);
            return appliedCount > 0;
        }

        private static bool TryGetUpgradeRequirement(CharacterObject target, out CustomResource resource, out int perUnit)
        {
            var t = target.GetCustomResourceRequiredForUpgrade(true);
            if (t == null) { resource = null; perUnit = 0; return false; }
            resource = t.Item1;
            perUnit = t.Item2;
            return true;
        }

        private static int GetTotalNumber(PartyScreenLogic.PartyCommand command)
        {
            var type = typeof(PartyScreenLogic.PartyCommand);

            var field = type.GetField("TotalNumber", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? type.GetField("<TotalNumber>k__BackingField", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null) return (int)field.GetValue(command);

            var prop = type.GetProperty("TotalNumber", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null) return (int)prop.GetValue(command);

            return 0;
        }

        private static void SetTotalNumber(ref PartyScreenLogic.PartyCommand command, int value)
        {
            var type = typeof(PartyScreenLogic.PartyCommand);
            object box = command;
            var field = type.GetField("TotalNumber", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? type.GetField("<TotalNumber>k__BackingField", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(box, value);
                command = (PartyScreenLogic.PartyCommand)box;
                return;
            }

            var prop = type.GetProperty("TotalNumber", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop?.CanWrite == true)
            {
                prop.SetValue(box, value);
                command = (PartyScreenLogic.PartyCommand)box;
                return;
            }
        }
    }
}