using HarmonyLib;
using SandBox.GauntletUI.Encyclopedia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Religion;

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
            if(!__instance.GetType().GetCustomAttributes(typeof(TorEncyclopediaModel), true).Any())
            {
                return true;
            }
			if (!(__instance is IPublicEncyclopediaPage)) return true;

			IPublicEncyclopediaPage page = __instance as IPublicEncyclopediaPage;
			____filters = page.PublicInitializeFilterItems();
			____items = page.PublicInitializeListItems();
			____sortControllers = new List<EncyclopediaSortController>
			{
				new EncyclopediaSortController(new TextObject("{=koX9okuG}None", null), new TorEncyclopediaListItemNameComparer())
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
			EncyclopediaPageArgs encyclopediaPageArgs = new EncyclopediaPageArgs(o);
			foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
				foreach(var type in assembly.GetTypes())
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
			foreach(var item in items)
            {
				__instance.Lists.Remove(item);
            }
		}
    }
}
