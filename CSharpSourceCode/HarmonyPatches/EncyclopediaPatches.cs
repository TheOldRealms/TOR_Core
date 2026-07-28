using HarmonyLib;
using SandBox.GauntletUI.Encyclopedia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.Encyclopedia.Pages;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Information;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class EncyclopediaPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EncyclopediaPage), MethodType.Constructor)]
        public static bool PatchEncyclopediaPageCtor(EncyclopediaPage __instance,
            ref Type[] ____identifierTypes,
            ref Dictionary<Type, string> ____identifiers,
            ref IEnumerable<EncyclopediaFilterGroup> ____filters,
            ref IEnumerable<EncyclopediaListItem> ____items,
            ref IEnumerable<EncyclopediaSortController> ____sortControllers)
        {
            if (!__instance.GetType().GetCustomAttributes(typeof(TorEncyclopediaModel), true).Any())
            {
                return true;
            }
            if (!(__instance is IPublicEncyclopediaPage)) return true;

            IPublicEncyclopediaPage page = __instance as IPublicEncyclopediaPage;
            ____filters = page.PublicInitializeFilterItems();
            ____items = page.PublicInitializeListItems();
            ____sortControllers = new List<EncyclopediaSortController>
            {
                new EncyclopediaSortController(TORTextHelper.GetTextObject("tor_encyclopedia_sort_none", "None"), new TorEncyclopediaListItemNameComparer())
            };

            ((List<EncyclopediaSortController>)____sortControllers).AddRange(page.PublicInitializeSortControllers());

            foreach (object obj in __instance.GetType().GetCustomAttributes(typeof(TorEncyclopediaModel), true))
            {
                if (obj is TorEncyclopediaModel)
                {
                    ____identifierTypes = (obj as TorEncyclopediaModel).PageTargetTypes;
                    break;
                }
            }
            ____identifiers = new Dictionary<Type, string>();
            foreach (Type type in ____identifierTypes)
            {
                if (Game.Current.ObjectManager.HasType(type))
                {
                    ____identifiers.Add(type, Game.Current.ObjectManager.FindRegisteredClassPrefix(type));
                }
                else
                {
                    string text = type.Name.ToString();
                    ____identifiers.Add(type, text);
                }
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EncyclopediaData), "GetEncyclopediaPageInstance")]
        public static bool AssemblyForPageInstance(ref EncyclopediaPageVM __result, EncyclopediaPage page, object o)
        {
            // Guard against null objects to prevent crashes
            if (o == null)
            {
                return true; // Let the original method handle null objects
            }

            EncyclopediaPageArgs encyclopediaPageArgs = new EncyclopediaPageArgs(o);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(EncyclopediaPageVM).IsAssignableFrom(type))
                    {
                        object[] customAttributes = type.GetCustomAttributes(typeof(EncyclopediaViewModel), false);
                        for (int j = 0; j < customAttributes.Length; j++)
                        {
                            EncyclopediaViewModel encyclopediaViewModel;
                            if ((encyclopediaViewModel = (customAttributes[j] as EncyclopediaViewModel)) != null && page.HasIdentifierType(encyclopediaViewModel.PageTargetType))
                            {
                                __result = Activator.CreateInstance(type, new object[]
                                {
                                encyclopediaPageArgs
                                }) as EncyclopediaPageVM;
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EncyclopediaHomeVM), MethodType.Constructor, typeof(EncyclopediaPageArgs))]
        public static void DontAddReligionToHomePage(EncyclopediaHomeVM __instance)
        {
            var items = __instance.Lists.Where(x => x.Order > 600).ToList();
            foreach (var item in items)
            {
                __instance.Lists.Remove(item);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(DefaultEncyclopediaConceptPage), "InitializeFilterItems")]
        public static void EncyclopediaPatch(ref IEnumerable<EncyclopediaFilterGroup> __result)
        {
            var result = new EncyclopediaFilterGroup(new List<EncyclopediaFilterItem>()
            {
                new EncyclopediaFilterItem(TORTextHelper.GetTextObject("tor_encyclopedia_filter_characters", "Characters"), (Predicate<object>)(c => Concept.IsGroupMember("Characters", (Concept)c))),
                new EncyclopediaFilterItem(TORTextHelper.GetTextObject("tor_encyclopedia_filter_kingdoms", "Kingdoms"), (Predicate<object>)(c => Concept.IsGroupMember("Kingdoms", (Concept)c))),
                new EncyclopediaFilterItem(TORTextHelper.GetTextObject("tor_encyclopedia_filter_clans", "Clans"), (Predicate<object>)(c => Concept.IsGroupMember("Clans", (Concept)c))),
                new EncyclopediaFilterItem(TORTextHelper.GetTextObject("tor_encyclopedia_filter_parties", "Parties"), (Predicate<object>)(c => Concept.IsGroupMember("Parties", (Concept)c))),
                new EncyclopediaFilterItem(TORTextHelper.GetTextObject("tor_encyclopedia_filter_armies", "Armies"), (Predicate<object>)(c => Concept.IsGroupMember("Armies", (Concept)c))),
                new EncyclopediaFilterItem(TORTextHelper.GetTextObject("tor_encyclopedia_filter_troops", "Troops"), (Predicate<object>)(c => Concept.IsGroupMember("Troops", (Concept)c))),
                new EncyclopediaFilterItem(TORTextHelper.GetTextObject("tor_encyclopedia_filter_items", "Items"), (Predicate<object>)(c => Concept.IsGroupMember("Items", (Concept)c))),
                new EncyclopediaFilterItem(TORTextHelper.GetTextObject("tor_encyclopedia_filter_old_realms", "The Old Realms"), (Predicate<object>)(c => Concept.IsGroupMember("The Old Realms", (Concept)c))),
                new EncyclopediaFilterItem(TORTextHelper.GetTextObject("tor_encyclopedia_filter_campaign_issues", "Campaign Issues"), (Predicate<object>)(c => Concept.IsGroupMember("CampaignIssues", (Concept)c)))

            }, TORTextHelper.GetTextObject("tor_encyclopedia_filter_types", "Types"));

            var list = new List<EncyclopediaFilterGroup>();
            list.Add(result);
            __result = list;
        }
    }

    [HarmonyPatch(typeof(HintViewModel), nameof(HintViewModel.ExecuteBeginHint))]
    public static class EncyclopediaUnitPropertyHintPatch
    {
        [HarmonyPrefix]
        private static bool ShowUnitAttributeTooltip(HintViewModel __instance)
        {
            return !EncyclopediaUnitPropertyPatches.TryShowUnitPropertyTooltip(__instance);
        }
    }

    [HarmonyPatch(typeof(TooltipPropertyWidget), "RefreshText")]
    public static class EncyclopediaUnitTooltipTextHeightPatch
    {
        [HarmonyPostfix]
        private static void ApplyUnitTooltipTextLayout(TooltipPropertyWidget __instance)
        {
            var isTitle = (__instance.PropertyModifierAsFlag & TooltipPropertyWidget.TooltipPropertyFlags.Title) != 0;
            if (isTitle && __instance.TextHeight == 26)
            {
                __instance.DefinitionLabel.Brush.FontSize = __instance.TextHeight;
                __instance.ValueLabel.Brush.FontSize = __instance.TextHeight;
            }

            if (__instance.IsMultiLine && __instance.TextHeight == 21)
            {
                var attributeDescriptionWidth = 192f;
                __instance.ValueLabel.Brush.FontSize = __instance.TextHeight;
                __instance.ValueLabel.WidthSizePolicy = SizePolicy.Fixed;
                __instance.ValueLabelContainer.WidthSizePolicy = SizePolicy.Fixed;
                __instance.ValueLabel.SuggestedWidth = attributeDescriptionWidth;
                __instance.ValueLabelContainer.SuggestedWidth = attributeDescriptionWidth;
            }
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory("LatePatches")]
    public static class EncyclopediaUnitPropertyPatches
    {
        private static readonly ConditionalWeakTable<HintViewModel, List<TooltipProperty>> UnitPropertyTooltips = new();

        internal static bool TryShowUnitPropertyTooltip(HintViewModel hint)
        {
            if (!UnitPropertyTooltips.TryGetValue(hint, out var tooltipProperties))
            {
                return false;
            }

            InformationManager.ShowTooltip(typeof(List<TooltipProperty>), tooltipProperties);
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EncyclopediaUnitPageVM), nameof(EncyclopediaUnitPageVM.RefreshValues))]
        private static void AddUnitAttributeIcons(EncyclopediaUnitPageVM __instance)
        {
            var character = __instance.Obj as CharacterObject;
            if (character == null || __instance.PropertiesList == null)
            {
                return;
            }

            for (var propertyIndex = 0; propertyIndex < Math.Min(2, __instance.PropertiesList.Count); propertyIndex++)
            {
                var unitProperty = __instance.PropertiesList[propertyIndex];
                if (TextObject.IsNullOrEmpty(unitProperty.Hint.HintText))
                {
                    continue;
                }

                UnitPropertyTooltips.Add(unitProperty.Hint, new List<TooltipProperty>
                {
                    new TooltipProperty(unitProperty.Hint.HintText.ToString(), string.Empty, 24, false, TooltipProperty.TooltipPropertyFlags.Title)
                });
            }

            var displayedAttributes = character.GetAttributes()
                .GroupBy(GetUnitAttributeLocalizationId)
                .Select(group => group.OrderByDescending(GetUnitAttributeTier).First());

            foreach (var attribute in displayedAttributes)
            {
                var localizationId = GetUnitAttributeLocalizationId(attribute);
                var iconPath = GetUnitAttributeIconPath(localizationId);
                if (string.IsNullOrEmpty(iconPath))
                {
                    continue;
                }

                for (var propertyIndex = __instance.PropertiesList.Count - 1; propertyIndex >= 0; propertyIndex--)
                {
                    if (__instance.PropertiesList[propertyIndex].Text == iconPath)
                    {
                        __instance.PropertiesList.RemoveAt(propertyIndex);
                    }
                }

                if (!GameTexts.TryGetText("tor_extendedInfo", out TextObject descriptionText, localizationId))
                {
                    continue;
                }

                var tier = GetUnitAttributeTier(attribute);
                SetUnitAttributeTextVariables(descriptionText, localizationId, tier);

                var attributeItem = new StringItemWithHintVM(iconPath, TextObject.GetEmpty());
                UnitPropertyTooltips.Add(attributeItem.Hint, CreateUnitAttributeTooltip(descriptionText, localizationId, tier));
                __instance.PropertiesList.Add(attributeItem);
            }
        }

        private static List<TooltipProperty> CreateUnitAttributeTooltip(TextObject attributeText, string attribute, int tier)
        {
            var localizedText = attributeText.ToString();
            var descriptionSeparatorIndex = localizedText.IndexOf(" - ", StringComparison.Ordinal);
            var attributeName = descriptionSeparatorIndex >= 0
                ? localizedText.Substring(0, descriptionSeparatorIndex)
                : attribute;
            var description = descriptionSeparatorIndex >= 0
                ? localizedText.Substring(descriptionSeparatorIndex + 3)
                : localizedText;

            if (IsTieredUnitAttribute(attribute))
            {
                var romanTier = tier switch
                {
                    3 => "III",
                    2 => "II",
                    _ => "I"
                };

                attributeName += " " + romanTier;
            }

            return new List<TooltipProperty>
            {
                new TooltipProperty(attributeName, string.Empty, 0, false, TooltipProperty.TooltipPropertyFlags.Title),
                new TooltipProperty(string.Empty, string.Empty, 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator),
                new TooltipProperty(string.Empty, description, 21, false, TooltipProperty.TooltipPropertyFlags.MultiLine)
            };
        }

        private static string GetUnitAttributeLocalizationId(string attribute)
        {
            switch (attribute)
            {
                case "Bulwark2":
                case "Bulwark3":
                    return "Bulwark";
                case "Swift2":
                case "Swift3":
                    return "Swift";
                case "Poisonous2":
                    return "Poisonous";
                case "Piercing2":
                    return "Piercing";
                case "Ethereal2":
                    return "Ethereal";
                case "MonsterSlayer2":
                    return "MonsterSlayer";
                case "UndeadSlayer2":
                    return "UndeadSlayer";
                case "Regeneration2":
                case "Regeneration3":
                    return "Regeneration";
                default:
                    return attribute;
            }
        }

        private static int GetUnitAttributeTier(string attribute)
        {
            switch (attribute)
            {
                case "Bulwark3":
                case "Swift3":
                case "Regeneration3":
                    return 3;
                case "Bulwark2":
                case "Swift2":
                case "Poisonous2":
                case "Piercing2":
                case "Ethereal2":
                case "MonsterSlayer2":
                case "UndeadSlayer2":
                case "Regeneration2":
                    return 2;
                default:
                    return 1;
            }
        }

        private static bool IsTieredUnitAttribute(string attribute)
        {
            return attribute == "Bulwark" ||
                   attribute == "Swift" ||
                   attribute == "Poisonous" ||
                   attribute == "Piercing" ||
                   attribute == "Ethereal" ||
                   attribute == "MonsterSlayer" ||
                   attribute == "UndeadSlayer" ||
                   attribute == "Regeneration";
        }

        private static void SetUnitAttributeTextVariables(TextObject text, string attribute, int tier)
        {
            switch (attribute)
            {
                case "Bulwark":
                    text.SetTextVariable("VALUE", tier == 3 ? 60 : tier == 2 ? 40 : 20);
                    break;
                case "Swift":
                    text.SetTextVariable("VALUE", tier == 3 ? 40 : tier == 2 ? 30 : 20);
                    break;
                case "Poisonous":
                    text.SetTextVariable("CHANCE", tier == 2 ? 35 : 20);
                    text.SetTextVariable("DURATION", 8);
                    text.SetTextVariable("DAMAGE", 2);
                    break;
                case "Piercing":
                    text.SetTextVariable("VALUE", tier == 2 ? 40 : 30);
                    break;
                case "TheHunger":
                    text.SetTextVariable("VALUE", 15);
                    break;
                case "Frenzy":
                    text.SetTextVariable("VALUE", 20);
                    text.SetTextVariable("DURATION", 40);
                    text.SetTextVariable("STACKS", 5);
                    break;
                case "Ethereal":
                    text.SetTextVariable("VALUE", tier == 2 ? 40 : 25);
                    break;
                case "MonsterSlayer":
                    text.SetTextVariable("VALUE", tier == 2 ? 150 : 75);
                    break;
                case "UndeadSlayer":
                    text.SetTextVariable("VALUE", tier == 2 ? 60 : 30);
                    break;
                case "Regeneration":
                    text.SetTextVariable("VALUE", tier == 3 ? 12 : tier == 2 ? 5 : 2);
                    break;
            }
        }

        private static string GetUnitAttributeIconPath(string attribute)
        {
            switch (attribute)
            {
                case "Undead":
                    return "attribute_icon_undead";
                case "Ethereal":
                    return "attribute_icon_ethereal";
                case "WightKing":
                    return "attribute_icon_wight_king";
                case "Brute":
                    return "attribute_icon_orc";
                case "TheHunger":
                    return "attribute_icon_the_hunger"; 
                case "Frenzy":
                    return "attribute_icon_frenzy";
                case "UndeadSlayer":
                    return "attribute_icon_undead_slayer";
                case "Immortality":
                    return "attribute_icon_immortality";
                case "Deadeye":
                    return "attribute_icon_deadeye";
                case "KillingBlow":
                    return "attribute_icon_killing_blow";
                case "MonsterSlayer":
                    return "attribute_icon_monster_slayer";
                case "Piercing":
                    return "attribute_icon_piercing";
                case "Bulwark":
                    return "";
                case "Swift":
                    return "";
                case "Poisonous":
                    return "";
                case "Regeneration":
                    return "attribute_icon_regeneration";
                case "Unbreakable":
                    return "attribute_icon_unbreakable";
                case "Unstoppable":
                    return "attribute_icon_unstoppable";
                default:
                    return null;
            }
        }
    }
}