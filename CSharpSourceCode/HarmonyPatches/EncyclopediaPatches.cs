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
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Information;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;
using static TOR_Core.Utilities.TORConstants;

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

            if (__instance.IsMultiLine && __instance.TextHeight == 20)
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

                UnitPropertyTooltips.Remove(unitProperty.Hint);
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
                new TooltipProperty(string.Empty, description, 20, false, TooltipProperty.TooltipPropertyFlags.MultiLine)
            };
        }

        private static string GetUnitAttributeLocalizationId(string attribute)
        {
            switch (attribute)
            {
                case CharacterAttributes.BULWARK_2:
                case CharacterAttributes.BULWARK_3:
                    return CharacterAttributes.BULWARK;
                case CharacterAttributes.SWIFT_2:
                case CharacterAttributes.SWIFT_3:
                    return CharacterAttributes.SWIFT;
                case CharacterAttributes.POISONOUS_2:
                    return CharacterAttributes.POISONOUS;
                case CharacterAttributes.PIERCING_2:
                    return CharacterAttributes.PIERCING;
                case CharacterAttributes.ETHEREAL_2:
                    return CharacterAttributes.ETHEREAL;
                case CharacterAttributes.MONSTER_SLAYER_2:
                    return CharacterAttributes.MONSTER_SLAYER;
                case CharacterAttributes.UNDEAD_SLAYER_2:
                    return CharacterAttributes.UNDEAD_SLAYER;
                case CharacterAttributes.REGENERATION_2:
                case CharacterAttributes.REGENERATION_3:
                    return CharacterAttributes.REGENERATION;
                default:
                    return attribute;
            }
        }

        private static int GetUnitAttributeTier(string attribute)
        {
            switch (attribute)
            {
                case CharacterAttributes.BULWARK_3:
                case CharacterAttributes.SWIFT_3:
                case CharacterAttributes.REGENERATION_3:
                    return 3;
                case CharacterAttributes.BULWARK_2:
                case CharacterAttributes.SWIFT_2:
                case CharacterAttributes.POISONOUS_2:
                case CharacterAttributes.PIERCING_2:
                case CharacterAttributes.ETHEREAL_2:
                case CharacterAttributes.MONSTER_SLAYER_2:
                case CharacterAttributes.UNDEAD_SLAYER_2:
                case CharacterAttributes.REGENERATION_2:
                    return 2;
                default:
                    return 1;
            }
        }

        private static bool IsTieredUnitAttribute(string attribute)
        {
            return attribute == CharacterAttributes.BULWARK ||
                   attribute == CharacterAttributes.SWIFT ||
                   attribute == CharacterAttributes.POISONOUS ||
                   attribute == CharacterAttributes.PIERCING ||
                   attribute == CharacterAttributes.ETHEREAL ||
                   attribute == CharacterAttributes.MONSTER_SLAYER ||
                   attribute == CharacterAttributes.UNDEAD_SLAYER ||
                   attribute == CharacterAttributes.REGENERATION;
        }

        private static void SetUnitAttributeTextVariables(TextObject text, string attribute, int tier)
        {
            switch (attribute)
            {
                case CharacterAttributes.BULWARK:
                    text.SetTextVariable("VALUE", tier == 3 ? 60 : tier == 2 ? 40 : 20);
                    break;
                case CharacterAttributes.SWIFT:
                    text.SetTextVariable("VALUE", tier == 3 ? 40 : tier == 2 ? 30 : 20);
                    break;
                case CharacterAttributes.POISONOUS:
                    text.SetTextVariable("CHANCE", tier == 2 ? 35 : 20);
                    text.SetTextVariable("DURATION", 8);
                    text.SetTextVariable("DAMAGE", 2);
                    break;
                case CharacterAttributes.PIERCING:
                    text.SetTextVariable("VALUE", tier == 2 ? 40 : 30);
                    break;
                case CharacterAttributes.THE_HUNGER:
                    text.SetTextVariable("VALUE", 15);
                    break;
                case CharacterAttributes.FRENZY:
                    text.SetTextVariable("VALUE", 20);
                    text.SetTextVariable("DURATION", 40);
                    text.SetTextVariable("STACKS", 5);
                    break;
                case CharacterAttributes.ETHEREAL:
                    text.SetTextVariable("VALUE", tier == 2 ? 40 : 25);
                    break;
                case CharacterAttributes.MONSTER_SLAYER:
                    text.SetTextVariable("VALUE", tier == 2 ? 150 : 75);
                    break;
                case CharacterAttributes.UNDEAD_SLAYER:
                    text.SetTextVariable("VALUE", tier == 2 ? 60 : 30);
                    break;
                case CharacterAttributes.REGENERATION:
                    text.SetTextVariable("VALUE", tier == 3 ? 12 : tier == 2 ? 5 : 2);
                    break;
            }
        }

        private static string GetUnitAttributeIconPath(string attribute)
        {
            switch (attribute)
            {
                case CharacterAttributes.UNDEAD:
                    return "attribute_icon_undead";
                case CharacterAttributes.ETHEREAL:
                    return "attribute_icon_ethereal";
                case CharacterAttributes.WIGHT_KING:
                    return "attribute_icon_wight_king";
                case CharacterAttributes.BRUTE:
                    return "attribute_icon_orc";
                case CharacterAttributes.THE_HUNGER:
                    return "attribute_icon_the_hunger"; 
                case CharacterAttributes.FRENZY:
                    return "attribute_icon_frenzy";
                case CharacterAttributes.UNDEAD_SLAYER:
                    return "attribute_icon_undead_slayer";
                case CharacterAttributes.IMMORTALITY:
                    return "attribute_icon_immortality";
                case CharacterAttributes.DEADEYE:
                    return "attribute_icon_deadeye";
                case CharacterAttributes.KILLING_BLOW:
                    return "attribute_icon_killing_blow";
                case CharacterAttributes.MONSTER_SLAYER:
                    return "attribute_icon_monster_slayer";
                case CharacterAttributes.PIERCING:
                    return "attribute_icon_piercing";
                case CharacterAttributes.BULWARK:
                    return "attribute_icon_bulwark";
                case CharacterAttributes.SWIFT:
                    return "attribute_icon_swift";
                case CharacterAttributes.POISONOUS:
                    return "attribute_icon_poisonous";
                case CharacterAttributes.REGENERATION:
                    return "attribute_icon_regeneration";
                case CharacterAttributes.UNBREAKABLE:
                    return "attribute_icon_unbreakable";
                case CharacterAttributes.UNSTOPPABLE:
                    return "attribute_icon_unstoppable";
                default:
                    return null;
            }
        }
    }
}